using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Dynamo;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Encompass.LoanInfo;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace EncompassAPI.AppLayer
{
    public class DisclosuresServiceV5 : IDisclosuresServiceV5, IAsyncDisposable
    {
        private readonly IFileService _fileService;
        private readonly S3Service _s3Service;
        private readonly ICandidDataService _candidDataService;
        private readonly ISalesforceService _sfService;
        private readonly IEncompassService _docsService;
        private readonly IDocusignService _docusignService;
        private readonly IDisclosureAuditServiceV3 _disclosureAuditService;
        private readonly IDisclosuresQueueService _disclosuresQueueService;
        private readonly LoggerService<DisclosuresServiceV4> _logger;

        private string _orgId;
        private string _loanId;
        private string _token;
        private string _latestLogId;
        private DisclosureStatus _status;
        private List<string> _logIds = new List<string>();

        public DisclosuresServiceV5(IFileService fileService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IEncompassService docsService, IDocusignService docusignService, IDisclosureAuditServiceV3 disclosureAuditService, IDisclosuresQueueService disclosuresQueueService, LoggerService<DisclosuresServiceV4> logger)
        {
            _fileService = fileService;
            _s3Service = s3Service;
            _candidDataService = candidDataService;
            _sfService = sfService;
            _docsService = docsService;
            _docusignService = docusignService;
            _disclosureAuditService = disclosureAuditService;
            _disclosuresQueueService = disclosuresQueueService;
            _logger = logger;
        }

        public void Init(string token)
        {
            _token = token;
        }

        public void Init(string token, string logId)
        {
            _token = token;
            _latestLogId = logId;
        }

        public async Task Retrieve(CandidIncomingRequest request)
        {
            try
            {
                _orgId = request.OrgId;
                _loanId = request.LoanId;

                if(request.OrgId != "00D4x000007swJU")
                {
                    return;
                }

                // Get all disclosure logs that are included in timeline, set to not included in timeline, then at the end of this process set back to included in timeline
                ProcessResult<IEnumerable<DisclosureTrackingLog>> prDisclosureLogs = await _disclosureAuditService.GetDisclosureTrackingLogs(_orgId, _loanId, _token);

                if (!prDisclosureLogs.Success)
                {
                    _status = DisclosureStatus.None;
                    return;
                }

                ProcessResult<DisclosureTrackingLog> prDisclosureTrackingLog;

                if (!string.IsNullOrEmpty(_latestLogId))
                {
                    prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLogById(_orgId, _loanId, _latestLogId, prDisclosureLogs.Result);
                }
                else
                {
                    // We need the latest disclosure tracking log. This is the log that is generated automatically from the print function
                    prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLog(_orgId, _loanId, prDisclosureLogs.Result);
                }

                if (!prDisclosureTrackingLog.Success)
                {
                    _status = DisclosureStatus.None;
                    return;
                }

                _latestLogId = prDisclosureTrackingLog.Result.id;

                // Check if there is a disclosure queue item for this disclosure log id
                ProcessResult<DisclosureQueueItem> prDisclosureQueue = await _disclosuresQueueService.GetDisclosureQueueItemById(_latestLogId);

                if (prDisclosureQueue.Success)
                {
                    Enum.TryParse(prDisclosureQueue.Result.status, out DisclosureStatus status);

                    if (status == DisclosureStatus.Dead || status == DisclosureStatus.Retrying || status == DisclosureStatus.Processing || status == DisclosureStatus.None || status == DisclosureStatus.Completed)
                    {
                        if(status == DisclosureStatus.Dead)
                            await _disclosuresQueueService.DeleteDisclosureQueueItem(prDisclosureQueue.Result.id);

                        _status = status;
                        return;
                    }
                    else if(status == DisclosureStatus.Retrieved)
                    {
                        return;
                    }
                    else {
                        // Set status to retrying
                        await _disclosuresQueueService.UpdateStatus(_latestLogId, DisclosureStatus.Retrying);
                    }
                }
                else
                {
                    // Set status to processing
                    await _disclosuresQueueService.UpsertDisclosureQueue(_latestLogId, _token, request, DisclosureStatus.Processing);
                }

                // Get the resource lock id from loan
                ProcessResult<string> prRetrievedLockId = await _docsService.GetResourceLock(_orgId, request.LoanId, _token);

                if (prRetrievedLockId.Success)
                {
                    _status = DisclosureStatus.Locked;
                    return;
                }

                _logIds = prDisclosureLogs.Result.Select(log => log.id).ToList();

                ProcessResult<DisclosureStage> prDisclosureStage = await _docsService.GetDisclosureStage(_orgId, _loanId, _token);

                if (!prDisclosureStage.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                ProcessResult<string> prDisclosuresName = _docsService.GetDisclosuresName(prDisclosureStage.Result, prDisclosureTrackingLog.Result.revision);

                if (!prDisclosuresName.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                _status = DisclosureStatus.Processing;

                // We need to update the log to not be included in the timeline otherwise we cannot get the signature coordinates
                ProcessResult prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(_orgId, _loanId, prDisclosureLogs.Result, _latestLogId, _status, false, _token);

                if (!prLogUpdated.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Document names from disclosure tracking log
                Dictionary<string, bool> dictAttachmentNamesToFind = prDisclosureTrackingLog.Result.documents.forms.Select(att => att.formName).Distinct().ToDictionary(name => name, name => false);

                // Get application id from loan because we need this for the audit
                ProcessResult<LoanInfo> prLoanInfo = await _docsService.GetLoanInfo(_orgId, _loanId, _token);

                if (!prLoanInfo.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                int borrowerCount = prLoanInfo.Result.expectedBorrowerCount;
                string applicationId = prLoanInfo.Result.applications[0].id;

                ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOrder(_orgId, _loanId, applicationId, _token, prDisclosureStage.Result);

                if (!prAuditId.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                //todo maybe return response of what failed in the audit // send back in response to candid
                ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOrder(_orgId, _loanId, prAuditId.Result, _token, prDisclosureStage.Result);

                if (!prAuditSuccess.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Get documents with signature coordinates
                ProcessResult<string> prOrderId = await _docsService.GenerateDocSet(_orgId, _loanId, prAuditId.Result, _token, prDisclosureStage.Result);

                if (!prOrderId.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // This only returns a select amount of documents
                ProcessResult<List<Models.Encompass.Document>> prDocuments = await _docsService.GetOrderStatus(_orgId, _loanId, prOrderId.Result, _token, prDisclosureStage.Result);

                if (!prDocuments.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Removes documents that are not needed from the doc set api
                List<Models.Encompass.Document> docSetDocuments = _docsService.RemoveUnneededDocuments(dictAttachmentNamesToFind, prDocuments.Result);

                // Check if attachmentNamesToFind are found in documents
                ProcessResult prAllExist = _docsService.CheckIfAllDocumentsExist(dictAttachmentNamesToFind, docSetDocuments);

                // Only enter here if not all docs were found
                if (!prAllExist.Success)
                {
                    // Retrieve efolder docs
                    ProcessResult<List<string>> prEFolderDocumentNames = await _docsService.GetEFolderDocuments(_orgId, _loanId, dictAttachmentNamesToFind, _token);

                    if (!prEFolderDocumentNames.Success)
                    {
                        _status = DisclosureStatus.Failed;
                        return;
                    }

                    // Add to list
                    ProcessResult<string> prJobId = await _docsService.AddToDocSet(_orgId, _loanId, prOrderId.Result, applicationId, prEFolderDocumentNames.Result, _token, prDisclosureStage.Result);

                    if (!prJobId.Success)
                    {
                        _status = DisclosureStatus.Failed;
                        return;
                    }

                    prDocuments = await _docsService.GetDocSet(_orgId, _loanId, prOrderId.Result, prJobId.Result, _token, prDisclosureStage.Result);

                    if (!prDocuments.Success)
                    {
                        _status = DisclosureStatus.Failed;
                        return;
                    }

                    // Removes documents that are not needed from the doc set api
                    docSetDocuments = _docsService.RemoveUnneededDocuments(dictAttachmentNamesToFind, prDocuments.Result);

                    // Check if attachmentNamesToFind are found in documents
                    prAllExist = _docsService.CheckIfAllDocumentsExist(dictAttachmentNamesToFind, docSetDocuments);

                    if (!prAllExist.Success)
                    {
                        // todo find other documents if all are still not found, if we can't just respnd back to event, let candid know we couldn't find all documents
                        // todo maybe check efolder
                    }
                }

                // Download attachments from encompass. It has to be multiple attachments because we can only get the signature coordinates by each document. Documents have page numbers.
                ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(request.OrgId, request.ProspectId, docSetDocuments, prDisclosureStage.Result);

                if (!prFileDownload.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                string disclosuresDownloadPath = prFileDownload.Result;

                ProcessResult<(string bucket, string key)> prUploadFiles = await _fileService.PushFilesToS3(request.OrgId, request.ProspectId, prDisclosureStage.Result);

                if (!prUploadFiles.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                string disclosureBucket = prUploadFiles.Result.bucket;
                string disclosuresPathInS3 = prUploadFiles.Result.key;

                // Map signature coordinates to draft edit _request for docusign
                ProcessResult<DraftEditRequestV5> prDraftEditRequest = _docsService.MapToDraftEditRequestV5(request, docSetDocuments, dictAttachmentNamesToFind.Keys.ToList());

                if (!prDraftEditRequest.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Get salesforce credentials for org
                ProcessResult<OrgInfo> prOrgInfo = await _candidDataService.GetCandidCredentialsByOrg(_orgId);

                if (!prOrgInfo.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Get org _request info so we can make api calls to salesforce
                ProcessResult<OrgRequestInfo> prOrgRequestInfo = await _sfService.AuthenticateToSalesforce(prOrgInfo.Result);

                if (!prOrgRequestInfo.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Get AWS settings
                ProcessResult<GoogleDriveClone> prAWSSettings = await _sfService.GetOrgAWSSettings(_orgId, request.LoId, prOrgRequestInfo.Result);

                if (!prAWSSettings.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Set up bucket name
                string bucket = prAWSSettings.Result.Root_Folder_Name__c.Replace(" ", "-").ToLower();
                prDraftEditRequest.Result.S3RootFolderName = disclosureBucket;
                prDraftEditRequest.Result.S3Path = disclosuresPathInS3;
                prDraftEditRequest.Result.Status = "sent";

                // Merge files here and this file is what I send back to candid... 
                ProcessResult<Dictionary<string, int>> prFilePageCounts = _docsService.MergePdfs(disclosuresDownloadPath, prDisclosuresName.Result, dictAttachmentNamesToFind.Keys.ToList());

                if (!prFilePageCounts.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                ProcessResult<string> prUpload = await _s3Service.PushFileToS3(bucket, request.Path, request.ProspectId, disclosuresDownloadPath, prDisclosuresName.Result);

                if (!prUpload.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Create document in Candid
                ProcessResult<string> prDocumentId = await _sfService.CreateDocument(request, prOrgRequestInfo.Result, prDisclosuresName.Result);

                if (!prDocumentId.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Pass document id to docusign request
                prDraftEditRequest.Result.DocumentId = prDocumentId.Result;

                ProcessResult<string> prEnvelopeId = await _docusignService.RequestDraftEdit(prDraftEditRequest.Result);

                if (!prEnvelopeId.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                ProcessResult<string> prPushDocument = await _sfService.PushUploadUrlToCandid(prUpload.Result, request, prOrgRequestInfo.Result, prDisclosuresName.Result, prEnvelopeId.Result, prDocumentId.Result);

                if (!prPushDocument.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                _fileService.RemoveFolder(request.ProspectId);

                _status = DisclosureStatus.Retrieved;
            }
            catch (Exception ex)
            {
                _status = DisclosureStatus.Failed;
                _logger.Base.LogError(ex.ToString());
                return;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_status != DisclosureStatus.Dead && _status != DisclosureStatus.Completed)
            {
                await _disclosuresQueueService.UpdateStatus(_latestLogId, _status);

                string description = _status == DisclosureStatus.Retrieved ? "Candid" : "Error";

                // We need to update the log to be included in the timeline now that we are done
                ProcessResult prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(_orgId, _loanId, _logIds, _latestLogId, description, true, _token);

                if (!prLogUpdated.Success)
                {
                    return;
                }
            }
        }
    }

    public interface IDisclosuresServiceV5
    {
        void Init(string token);
        void Init(string token, string logId);
        /// <summary>
        /// This will handle the event notification from Encompass.
        /// This will detect the org the event is for, and then retrieve the access token for that org.
        /// This will then retrieve files from efolder, save to s3, and notify candid
        /// </summary>
        /// <param name="eventNotification"></param>
        /// <returns></returns>
        Task Retrieve(CandidIncomingRequest request);

        ValueTask DisposeAsync();
    }
}
