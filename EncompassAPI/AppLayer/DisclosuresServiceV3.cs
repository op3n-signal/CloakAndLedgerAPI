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
using System.Collections.Generic;
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
        private readonly IDisclosureAuditServiceV3 _disclosureAuditService;
        private readonly IDisclosuresQueueService _disclosuresQueueService;
        private readonly IDocumentSignerMappingService _documentMappingService;

        private string _orgId;
        private string _loanId;
        private string _token;
        private string _latestLogId;
        private DisclosureStatus _status;
        private List<string> _logIds = new List<string>();

        public DisclosuresServiceV3(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IEncompassService docsService, IDocusignService docusignService, IDisclosureAuditServiceV3 disclosureAuditService, IDisclosuresQueueService disclosuresQueueService, IDocumentSignerMappingService documentMappingService)
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
            _documentMappingService = documentMappingService;
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

                bool isJointApplication = prDisclosureTrackingLog.Result.disclosureRecipients.Any(rec => rec.recipientType.ToLower() == "coborrower");
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

                // Document names from disclosure tracking log
                Dictionary<string, bool> dictAttachmentNamesToFind = prDisclosureTrackingLog.Result.documents.forms.Select(att => att.formName).ToDictionary(name => name, name => false);

                ProcessResult<LoanInfo> prLoanInfo = await _docsService.GetLoanInfo(request.OrgId, request.LoanId, _token);

                if (!prLoanInfo.Success)
                {
                    return;
                }

                string loanType = prLoanInfo.Result.hmda.loanType;
                string state = prLoanInfo.Result.property.state;
                int borrowerCount = prLoanInfo.Result.expectedBorrowerCount;

                // Get document mapping data. 
                // ... such as signature coordinates...
                ProcessResult<List<DocumentSignerMapping>> prDocMappings = await _documentMappingService.GetDisclosureMappings(loanType, state, borrowerCount);

                if (!prDocMappings.Success)
                {
                    return;
                }


                // Get documents from encompass efolder of type edisclosures
                ProcessResult<List<EncompassDocument>> ieEFolderDocuments = await _eFolderService.GetListOfDocuments(_orgId, _loanId, _token);

                if (!ieEFolderDocuments.Success)
                {
                    return;
                }

                // Filter out documents that are not in disclosure log
                List<EncompassDocument> neededDocuments = _docsService.RemoveUnneededDocuments(dictAttachmentNamesToFind, ieEFolderDocuments.Result);

                // Get the attachments for all documents
                ProcessResult<Attachment[]> prAttachments = await _eFolderService.GetAttachments(request.OrgId, request.LoanId, neededDocuments);

                if (!prAttachments.Success)
                {
                    return;
                }

                // Get the download urls
                ProcessResult<AttachmentUrls> prAttachmentDownloadUrls = await _eFolderService.GetAttachmentDownloadUrls(request.OrgId, request.LoanId, prAttachments.Result, _token);

                if (!prAttachmentDownloadUrls.Success)
                {
                    return;
                }

                // Download docs from encompass using attachment download urls
                // It has to be multiple attachments because we can only get the signature coordinates by each document. Documents have page numbers.
                ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(request.ProspectId, prAttachmentDownloadUrls.Result, prAttachments.Result);

                if (!prFileDownload.Success)
                {
                    return;
                }

                // Merge files here and this file is what I send back to candid... 
                ProcessResult<Dictionary<string, int>> prFilePageCounts = _docsService.MergePdfs(isJointApplication, prFileDownload.Result, prDisclosuresName.Result, dictAttachmentNamesToFind.Keys.ToList());

                if (!prFilePageCounts.Success)
                {
                    return;
                }

                // Map signature coordinates to draft edit _request for docusign
                ProcessResult<DraftEditRequest> prDraftEditRequest = _docsService.MapToDraftEditRequest(request, prDocMappings.Result, prFilePageCounts.Result);

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
