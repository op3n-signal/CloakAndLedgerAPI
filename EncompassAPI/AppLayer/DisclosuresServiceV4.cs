using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Docusign.AnchorTags;
using EncompassAPI.Models.Dynamo;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Encompass.LoanInfo;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace EncompassAPI.AppLayer
{
    public class DisclosuresServiceV4 : IDisclosuresServiceV4, IAsyncDisposable
    {
        private readonly IFileService _fileService;
        private readonly IeFolderService _eFolderService;
        private readonly S3Service _s3Service;
        private readonly ICandidDataService _candidDataService;
        private readonly ISalesforceService _sfService;
        private readonly IEncompassService _docsService;
        private readonly IDocusignService _docusignService;
        private readonly IDisclosureAuditServiceV3 _disclosureAuditService;
        private readonly IDisclosuresQueueService _disclosuresQueueService;

        private string _orgId;
        private string _loanId;
        private string _token;
        private string _latestLogId;
        private DisclosureStatus _status;

        public DisclosuresServiceV4(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IEncompassService docsService, IDocusignService docusignService, IDisclosureAuditServiceV3 disclosureAuditService, IDisclosuresQueueService disclosuresQueueService)
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

                if (prDisclosureQueue.Success && prDisclosureQueue.Result.status != "Dead")
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

                // Document names from disclosure tracking log
                Dictionary<string, bool> dictAttachmentNamesToFind = prDisclosureTrackingLog.Result.documents.forms.Select(att => att.formName).ToDictionary(name => name, name => false);

                ProcessResult<LoanInfo> prLoanInfo = await _docsService.GetLoanInfo(request.OrgId, request.LoanId, _token);

                if (!prLoanInfo.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                int borrowerCount = prLoanInfo.Result.expectedBorrowerCount;
                bool urlaAdditionalBorrowerPage = prLoanInfo.Result.applications[0].coborrower.printOnAdditionalBorrowerPage;

                // Get documents from encompass efolder of type edisclosures
                ProcessResult<List<EncompassDocument>> ieEFolderDocuments = await _eFolderService.GetListOfDocuments(_orgId, _loanId, _token);

                if (!ieEFolderDocuments.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Filter out documents that are not in disclosure log
                List<EncompassDocument> neededDocuments = _docsService.RemoveUnneededDocuments(dictAttachmentNamesToFind, ieEFolderDocuments.Result);

                // Get the attachments for all documents
                ProcessResult<Attachment[]> prAttachments = await _eFolderService.GetAttachments(request.OrgId, request.LoanId, neededDocuments);

                if (!prAttachments.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Get the download urls
                ProcessResult<AttachmentUrls> prAttachmentDownloadUrls = await _eFolderService.GetAttachmentDownloadUrls(request.OrgId, request.LoanId, prAttachments.Result, _token);

                if (!prAttachmentDownloadUrls.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Download docs from encompass using attachment download urls
                // It has to be multiple attachments because we can only get the signature coordinates by each document. Documents have page numbers.
                ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(request.ProspectId, prAttachmentDownloadUrls.Result, prAttachments.Result);

                if (!prFileDownload.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Merge files here and this file is what I send back to candid... 
                ProcessResult<Dictionary<string, int>> prFilePageCounts = _fileService.MergePdfsV4(prFileDownload.Result, prDisclosuresName.Result, dictAttachmentNamesToFind.Keys.ToList());

                if (!prFilePageCounts.Success)
                {
                    _status = DisclosureStatus.Failed;
                    return;
                }

                // Map signature coordinates to draft edit _request for docusign
                ProcessResult<DraftEditRequestV4> prDraftEditRequest = _docsService.MapToDraftEditRequest(urlaAdditionalBorrowerPage, request, prFilePageCounts.Result);

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
                prDraftEditRequest.Result.S3RootFolderName = bucket;
                prDraftEditRequest.Result.Status = "sent";

                ProcessResult<string> prUpload = await _s3Service.PushFileToS3(bucket, request.Path, request.ProspectId, prDisclosuresName.Result);

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

                ProcessResult<string> prEnvelopeId = await _docusignService.RequestDraftEdit(prDraftEditRequest.Result, prDisclosuresName.Result);

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

                _status = DisclosureStatus.Completed;
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
        }
    }

    public interface IDisclosuresServiceV4
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
