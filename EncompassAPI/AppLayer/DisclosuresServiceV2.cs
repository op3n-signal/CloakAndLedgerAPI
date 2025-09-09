using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
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
    public class DisclosuresV2Service : IDisclosuresServiceV2, IAsyncDisposable
    {
        private readonly IFileService _fileService;
        private readonly IeFolderService _eFolderService;
        private readonly S3Service _s3Service;
        private readonly ICandidDataService _candidDataService;
        private readonly ISalesforceService _sfService;
        private readonly IEncompassService _docsService;
        private readonly IDocusignService _docusignService;
        private CandidIncomingRequest _request;
        private string _token;
        private List<string> _logIds = new List<string>();

        public DisclosuresV2Service(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IEncompassService docsService, IDocusignService docusignService)
        {
            _fileService = fileService;
            _eFolderService = eFolderService;
            _s3Service = s3Service;
            _candidDataService = candidDataService;
            _sfService = sfService;
            _docsService = docsService;
            _docusignService = docusignService;
        }

        public void Init(CandidIncomingRequest request, string token)
        {
            _request = request;
            _token = token;
        }

        public async Task Retrieve()
        {
            // Get the resource lock id from loan
            ProcessResult<string> prRetrievedLockId = await _docsService.GetResourceLock(_request.OrgId, _request.LoanId, _token);

            if (prRetrievedLockId.Success)
            {
                // Remove resource lock from loan
                ProcessResult prLockRemoved = await _docsService.RemoveResourceLock(_request.OrgId, _request.LoanId, prRetrievedLockId.Result, _token);

                if (!prLockRemoved.Success)
                {
                    // If we can't remove the lock, we can't proceed
                    return;
                }
            }

            // Get all disclosure logs that are included in timeline, set to not included in timeline, then at the end of this process set back to included in timeline
            ProcessResult<IEnumerable<DisclosureTrackingLog>> prDisclosureLogs = await _eFolderService.GetDisclosureTrackingLogs(_request.OrgId, _request.LoanId, _token);

            if (!prDisclosureLogs.Success)
            {
                return;
            }

            _logIds = prDisclosureLogs.Result.Select(log => log.id).ToList();

            // We need the latest disclosure tracking log. This is the log that is generated automatically from the print function
            ProcessResult<DisclosureTrackingLog> prDisclosureTrackingLog = _eFolderService.GetLastestDisclosureTrackingLog(_request.OrgId, _request.LoanId, prDisclosureLogs.Result);

            if (!prDisclosureTrackingLog.Success)
            {
                return;
            }

            ProcessResult<DisclosureStage> prDisclosureStage = await _docsService.GetDisclosureStage(_request.OrgId, _request.LoanId, _token);

            if (!prDisclosureStage.Success)
            {
                return;
            }

            ProcessResult<string> prDisclosuresName = _docsService.GetDisclosuresName(prDisclosureStage.Result, prDisclosureTrackingLog.Result.revision);

            if (!prDisclosuresName.Success)
            {
                return;
            }

            // We need to update the log to not be included in the timeline otherwise we cannot get the signature coordinates
            ProcessResult prLogUpdated = await _eFolderService.UpdateDisclosureTrackingLogs(_request.OrgId, _request.LoanId, prDisclosureLogs.Result, false, _token);

            if (!prLogUpdated.Success)
            {
                return;
            }

            // Make list of documents we have to find
            Dictionary<string, bool> dictAttachmentNamesToFind = prDisclosureTrackingLog.Result.documents.forms.Select(att => att.formName).ToDictionary(name => name, name => false);

            // Get application id from loan because we need this for the audit
            ProcessResult<string> prApplicationId = await _docsService.GetApplicationIdFromLoan(_request.OrgId, _request.LoanId, _token);

            if (!prApplicationId.Success)
            {;
                return;
            }

            ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOpeningOrder(_request.OrgId, _request.LoanId, prApplicationId.Result, _token);

            if (!prAuditId.Success)
            {
                return;
            }

            //todo maybe return response of what failed in the audit // send back in response to candid
            ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOpeningOrder(_request.OrgId, _request.LoanId, prAuditId.Result, _token);

            if (!prAuditSuccess.Success)
            {
                return;
            }

            // Get documents with signature coordinates
            ProcessResult<string> prOrderId = await _docsService.GenerateOpeningDocSet(_request.OrgId, _request.LoanId, prAuditId.Result, _token);

            if(!prOrderId.Success)
            {
                return;
            }

            // This only returns a select amount of documents
            ProcessResult<List<Models.Encompass.Document>> prDocuments = await _docsService.GetOrderStatus(_request.OrgId, _request.LoanId, prOrderId.Result, _token);

            if(!prDocuments.Success)
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
                ProcessResult<List<string>> prEFolderDocumentNames = await _docsService.GetEFolderDocuments(_request.OrgId, _request.LoanId, dictAttachmentNamesToFind, _token);

                if (!prEFolderDocumentNames.Success)
                {
                    return;
                }

                //todo then add to list
                ProcessResult<string> prJobId = await _docsService.AddToOpeningDocSet(_request.OrgId, _request.LoanId, prOrderId.Result, prApplicationId.Result, prEFolderDocumentNames.Result, _token);

                if (!prJobId.Success)
                {
                    return;
                }

                prDocuments = await _docsService.GetOpeningDocSet(_request.OrgId, _request.LoanId, prOrderId.Result, prJobId.Result, _token);

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
            ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(_request.ProspectId, docSetDocuments);

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
            ProcessResult<DraftEditRequest> prDraftEditRequest = _docsService.MapToDraftEditRequest(_request, docSetDocuments, prFilePageCounts.Result);

            if (!prDraftEditRequest.Success)
            {
                return;
            }

            // Get salesforce credentials for org
            ProcessResult<OrgInfo> prOrgInfo = await _candidDataService.GetCandidCredentialsByOrg(_request.OrgId);

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
            ProcessResult<GoogleDriveClone> prAWSSettings = await _sfService.GetOrgAWSSettings(_request.OrgId, _request.LoId, prOrgRequestInfo.Result);

            if (!prAWSSettings.Success)
            {
                return;
            }

            // Set up bucket name
            string bucket = prAWSSettings.Result.Root_Folder_Name__c.Replace(" ", "-").ToLower();
            prDraftEditRequest.Result.S3RootFolderName = bucket;
            prDraftEditRequest.Result.Status = "sent";

            ProcessResult<string> prUpload = await _s3Service.PushFileToS3(bucket, _request.Path, _request.ProspectId, prDisclosuresName.Result);

            if (!prUpload.Success)
            {
                return;
            }

            ProcessResult<string> prPushDocument = await _sfService.PushUploadUrlToCandid(prUpload.Result, _request, prOrgRequestInfo.Result, prDisclosuresName.Result);

            if (!prPushDocument.Success)
            {
                return;
            }

            // Set document id
            prDraftEditRequest.Result.DocumentId = prPushDocument.Result;

            ProcessResult<string> prEnvelopeId = await _docusignService.RequestDraftEdit(prDraftEditRequest.Result, prDisclosuresName.Result);

            if (!prEnvelopeId.Success)
            {
                return;
            }

            // We need to update the log to be included in the timeline now that we are done
            prLogUpdated = await _eFolderService.UpdateDisclosureTrackingLogs(_request.OrgId, _request.LoanId, prDisclosureLogs.Result, true, _token);

            if (!prLogUpdated.Success)
            {
                return;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // We need to update the log to be included in the timeline now that we are done
            ProcessResult prLogUpdated = await _eFolderService.UpdateDisclosureTrackingLogs(_request.OrgId, _request.LoanId, _logIds, true, _token);

            if (!prLogUpdated.Success)
            {
                return;
            }
        }
    }

    public interface IDisclosuresServiceV2
    {
        void Init(CandidIncomingRequest request, string token);
        /// <summary>
        /// This will handle the event notification from Encompass.
        /// This will detect the org the event is for, and then retrieve the access token for that org.
        /// This will then retrieve files from efolder, save to s3, and notify candid
        /// </summary>
        /// <param name="eventNotification"></param>
        /// <returns></returns>
        Task Retrieve();
    }
}
