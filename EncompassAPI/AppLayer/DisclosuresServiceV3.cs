using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Dynamo;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models;
using System.Security.Cryptography;

namespace EncompassAPI.AppLayer
{
    public class DisclosuresServiceV3 : IDisclosuresServiceV3, IAsyncDisposable
    {
        private readonly IFileService _fileService;
        private readonly IeFolderService _eFolderService;
        private readonly S3Service _s3Service;
        private readonly ICandidDataService _candidDataService;
        private readonly ISalesforceService _sfService;
        private readonly IEncompassService _docsService;
        private readonly IDocusignService _docusignService;
        private readonly IDisclosureAuditServiceV2 _disclosureAuditService;
        private readonly IDisclosuresQueueService _disclosuresQueueService;

        private string _orgId;
        private string _loanId;
        private string _token;
        private string _latestLogId;
        private DisclosureStatus _status;
        private List<string> _logIds = new List<string>();

        public DisclosuresServiceV3(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IEncompassService docsService, IDocusignService docusignService, IDisclosureAuditServiceV2 disclosureAuditService, IDisclosuresQueueService disclosuresQueueService)
        {
            _fileService = fileService;
            _eFolderService = eFolderService;
            _s3Service = s3Service;
            _candidDataService = candidDataService;
            _sfService = sfService;
            _docsService = docsService;
            _docusignService = docusignService;
            _disclosureAuditService = disclosureAuditService;
            _disclosuresQueueService = disclosuresQueueService;
        }

        public void Init(string token)
        {
            _token = token;
        }

        public async Task Retrieve(CandidIncomingRequest request)
        {
            try
            {
                _orgId = request.OrgId;
                _loanId = request.LoanId;

                // Get all disclosure logs that are included in timeline, set to not included in timeline, then at the end of this process set back to included in timeline
                ProcessResult<IEnumerable<DisclosureTrackingLog>> prDisclosureLogs = await _disclosureAuditService.GetDisclosureTrackingLogs(_orgId, _loanId, _token);

                if (!prDisclosureLogs.Success)
                {
                    _status = DisclosureStatus.None;
                    return;
                }

                // We need the latest disclosure tracking log. This is the log that is generated automatically from the print function
                ProcessResult<DisclosureTrackingLog> prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLog(_orgId, _loanId, prDisclosureLogs.Result);

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

                    if (status == DisclosureStatus.Processing)
                    {
                        // Exit if we are already processing
                        _status = DisclosureStatus.Processing;
                        return;
                    }
                    else if (status == DisclosureStatus.Retrying)
                    {
                        // Exit if we are retrying
                        _status = DisclosureStatus.Retrying;
                        return;
                    }
                    else if (status == DisclosureStatus.Completed)
                    {
                        // Exit if we are already completed
                        _status = DisclosureStatus.Completed;
                        return;
                    }

                    // Set status to retrying
                    await _disclosuresQueueService.UpdateStatus(_latestLogId, DisclosureStatus.Retrying);
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
                    return;
                }

                ProcessResult<string> prDisclosuresName = _docsService.GetDisclosuresName(prDisclosureStage.Result, prDisclosureTrackingLog.Result.revision);

                if (!prDisclosuresName.Success)
                {
                    return;
                }

                _status = DisclosureStatus.Processing;

                // Make list of documents we have to find
                Dictionary<string, bool> dictAttachmentNamesToFind = prDisclosureTrackingLog.Result.documents.forms.Select(att => att.formName).ToDictionary(name => name, name => false);

                // Get application id from loan because we need this for the audit
                ProcessResult<string> prApplicationId = await _docsService.GetApplicationIdFromLoan(_orgId, _loanId, _token);

                if (!prApplicationId.Success)
                {
                    ;
                    return;
                }

                ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOrder(_orgId, _loanId, prApplicationId.Result, _token, prDisclosureStage.Result);

                if (!prAuditId.Success)
                {
                    return;
                }

                //todo maybe return response of what failed in the audit // send back in response to candid
                ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOrder(_orgId, _loanId, prAuditId.Result, _token, prDisclosureStage.Result);

                if (!prAuditSuccess.Success)
                {
                    return;
                }

                // Get documents with signature coordinates
                ProcessResult<string> prOrderId = await _docsService.GenerateDocSet(_orgId, _loanId, prAuditId.Result, _token, prDisclosureStage.Result);

                if (!prOrderId.Success)
                {
                    return;
                }

                // This only returns a select amount of documents
                ProcessResult<List<Models.Encompass.Document>> prDocuments = await _docsService.GetOrderStatus(_orgId, _loanId, prOrderId.Result, _token, prDisclosureStage.Result);

                if (!prDocuments.Success)
                {
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
                        return;
                    }

                    // Add to list
                    ProcessResult<string> prJobId = await _docsService.AddToDocSet(_orgId, _loanId, prOrderId.Result, prApplicationId.Result, prEFolderDocumentNames.Result, _token, prDisclosureStage.Result);

                    if (!prJobId.Success)
                    {
                        return;
                    }

                    prDocuments = await _docsService.GetDocSet(_orgId, _loanId, prOrderId.Result, prJobId.Result, _token, prDisclosureStage.Result);

                    if (!prDocuments.Success)
                    {
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
                ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(request.ProspectId, docSetDocuments);

                if (!prFileDownload.Success)
                {
                    return;
                }

                // Merge files here and this file is what I send back to candid... 
                ProcessResult<Dictionary<string, int>> prFilePageCounts = _docsService.MergePdfs(prFileDownload.Result, prDisclosuresName.Result, dictAttachmentNamesToFind.Keys.ToList());

                if (!prFilePageCounts.Success)
                {
                    return;
                }

                // Map signature coordinates to draft edit _request for docusign
                ProcessResult<DraftEditRequest> prDraftEditRequest = _docsService.MapToDraftEditRequest(request, docSetDocuments, prFilePageCounts.Result);

                if (!prDraftEditRequest.Success)
                {
                    return;
                }

                // Get salesforce credentials for org
                ProcessResult<OrgInfo> prOrgInfo = await _candidDataService.GetCandidCredentialsByOrg(_orgId);

                if (!prOrgInfo.Success)
                {
                    return;
                }

                // Get org _request info so we can make api calls to salesforce
                ProcessResult<OrgRequestInfo> prOrgRequestInfo = await _sfService.AuthenticateToSalesforce(prOrgInfo.Result);

                if (!prOrgRequestInfo.Success)
                {
                    return;
                }

                // Get AWS settings
                ProcessResult<GoogleDriveClone> prAWSSettings = await _sfService.GetOrgAWSSettings(_orgId, request.LoId, prOrgRequestInfo.Result);

                if (!prAWSSettings.Success)
                {
                    return;
                }

                // Set up bucket name
                string bucket = prAWSSettings.Result.Root_Folder_Name__c.Replace(" ", "-").ToLower();
                prDraftEditRequest.Result.S3RootFolderName = bucket;
                prDraftEditRequest.Result.Status = "sent";

                ProcessResult<string> prUpload = await _s3Service.PushFileToS3(bucket, request.Path, request.ProspectId, prDisclosuresName.Result);

                if (!prUpload.Success)
                {
                    return;
                }

                // Create document in Candid
                ProcessResult<string> prDocumentId = await _sfService.CreateDocument(request, prOrgRequestInfo.Result, prDisclosuresName.Result);

                if (!prDocumentId.Success)
                {
                    return;
                }

                // Pass document id to docusign request
                prDraftEditRequest.Result.DocumentId = prDocumentId.Result;

                ProcessResult<string> prEnvelopeId = await _docusignService.RequestDraftEdit(prDraftEditRequest.Result, prDisclosuresName.Result);

                if (!prEnvelopeId.Success)
                {
                    return;
                }

                ProcessResult<string> prPushDocument = await _sfService.PushUploadUrlToCandid(prUpload.Result, request, prOrgRequestInfo.Result, prDisclosuresName.Result, prEnvelopeId.Result, prDocumentId.Result);

                if (!prPushDocument.Success)
                {
                    return;
                }

                _status = DisclosureStatus.Completed;

                ProcessResult prUpdateLogActualReceivedDate = await _disclosureAuditService.UpdateReceivedDateInTrackingLog(_orgId, _loanId, prDisclosureTrackingLog.Result, _token);

                if (!prUpdateLogActualReceivedDate.Success)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if(_status == DisclosureStatus.Processing || _status == DisclosureStatus.Retrying || _status == DisclosureStatus.None)
            {
                return;
            }

            await _disclosuresQueueService.UpdateStatus(_latestLogId, _status);

            // We need to update the log to be included in the timeline now that we are done
            ProcessResult prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(_orgId, _loanId, _logIds, _latestLogId, _status, true, _token);

            if (!prLogUpdated.Success)
            {
                return;
            }
        }
    }

    public interface IDisclosuresServiceV3
    {
        void Init(string token);
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
