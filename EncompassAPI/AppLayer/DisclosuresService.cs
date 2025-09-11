using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Models;
using System.Security.Cryptography;

namespace EncompassAPI.AppLayer
{
    public class DisclosuresService : IDisclosuresService
    {
        private readonly IFileService _fileService;
        private readonly IeFolderService _eFolderService;
        private readonly S3Service _s3Service;
        private readonly ICandidDataService _candidDataService;
        private readonly ISalesforceService _sfService;
        private readonly IUploadService _uploadService;
        private readonly IEncompassService _docsService;
        private readonly IDocusignService _docusignService;
        private readonly IDisclosureAuditService _disclosureAuditService;

        public DisclosuresService(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IUploadService uploadService, IEncompassService docsService, IDocusignService docusignService, IDisclosureAuditService disclosureAuditService)
        {
            _fileService = fileService;
            _eFolderService = eFolderService;
            _s3Service = s3Service;
            _candidDataService = candidDataService;
            _sfService = sfService;
            _uploadService = uploadService;
            _docsService = docsService;
            _docusignService = docusignService;
            _disclosureAuditService = disclosureAuditService;
        }

        public async Task Retrieve(CandidIncomingRequest request, string token)
        {
            // Get the resource lock id from loan
            ProcessResult<string> prRetrievedLockId = await _docsService.GetResourceLock(request.OrgId, request.LoanId, token);

            if (prRetrievedLockId.Success)
            {
                // Remove resource lock from loan
                ProcessResult prLockRemoved = await _docsService.RemoveResourceLock(request.OrgId, request.LoanId, prRetrievedLockId.Result, token);

                if (!prLockRemoved.Success)
                {
                    // If we can't remove the lock, we can't proceed
                    return;
                }
            }

            // Get all disclosure logs that are included in timeline, set to not included in timeline, then at the end of this process set back to included in timeline
            ProcessResult<IEnumerable<DisclosureTrackingLog>> prDisclosureLogs = await _disclosureAuditService.GetDisclosureTrackingLogs(request.OrgId, request.LoanId, token);

            if (!prDisclosureLogs.Success)
            {
                return;
            }

            // We need the latest disclosure tracking log. This is the log that is generated automatically from the print function
            ProcessResult<DisclosureTrackingLog> prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLog(request.OrgId, request.LoanId, prDisclosureLogs.Result);

            if (!prDisclosureTrackingLog.Success)
            {
                return;
            }

            ProcessResult<DisclosureStage> prDisclosureStage = await _docsService.GetDisclosureStage(request.OrgId, request.LoanId, token);

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
            ProcessResult prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, false, token);

            if (!prLogUpdated.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Retrieve documents
            ProcessResult<IEnumerable<EncompassDocument>> prEncompassDocuments = await _eFolderService.GetListOfDocuments(request.OrgId, request.LoanId, token);

            if (!prEncompassDocuments.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            ProcessResult prRemoved = _eFolderService.RemoveDocumentsNotInDisclosureLog(request.OrgId, request.LoanId, prDisclosureTrackingLog.Result.documents.forms, prDisclosureTrackingLog.Result.id, prEncompassDocuments.Result);

            if (!prRemoved.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            ProcessResult<Attachment[]> prAttachments = await _eFolderService.GetAttachments(request.OrgId, request.LoanId, prEncompassDocuments.Result);

            if (!prAttachments.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            ProcessResult<AttachmentUrls> prAttachmentsUrls = await _eFolderService.GetAttachmentDownloadUrls(request.OrgId, request.LoanId, prAttachments.Result, token);

            if (!prAttachmentsUrls.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Download attachments from encompass. It has to be multiple attachments because we can only get the signature coordinates by each document. Documents have page numbers.
            ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(request.ProspectId, prAttachmentsUrls.Result, prAttachments.Result);

            if (!prFileDownload.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Make list of documents we have to find
            Dictionary<string, bool> dictAttachmentNamesToFind = prAttachments.Result.Select(att => att.entityName).ToDictionary(name => name, name => false);

            // Get application id from loan because we need this for the audit
            ProcessResult<string> prApplicationId = await _docsService.GetApplicationIdFromLoan(request.OrgId, request.LoanId, token);

            if (!prApplicationId.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOpeningOrder(request.OrgId, request.LoanId, prApplicationId.Result, token);

            if (!prAuditId.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            //todo maybe return response of what failed in the audit // send back in response to candid
            ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOpeningOrder(request.OrgId, request.LoanId, prAuditId.Result, token);

            if (!prAuditSuccess.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Get documents with signature coordinates
            ProcessResult<string> prOrderId = await _docsService.GenerateOpeningDocSet(request.OrgId, request.LoanId, prAuditId.Result, token);

            if(!prOrderId.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // This only returns a select amount of documents
            ProcessResult<List<Models.Encompass.Document>> prDocuments = await _docsService.GetOrderStatus(request.OrgId, request.LoanId, prOrderId.Result, token);

            if(!prDocuments.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
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
                ProcessResult<List<string>> prEFolderDocumentNames = await _docsService.GetEFolderDocuments(request.OrgId, request.LoanId, dictAttachmentNamesToFind, token);

                if (!prEFolderDocumentNames.Success)
                {
                    // We need to update the log to be included in the timeline now that we are done
                    prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                    return;
                }

                //todo then add to list
                ProcessResult<string> prJobId = await _docsService.AddToOpeningDocSet(request.OrgId, request.LoanId, prOrderId.Result, prApplicationId.Result, prEFolderDocumentNames.Result, token);

                if (!prJobId.Success)
                {
                    // We need to update the log to be included in the timeline now that we are done
                    prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                    return;
                }

                prDocuments = await _docsService.GetOpeningDocSet(request.OrgId, request.LoanId, prOrderId.Result, prJobId.Result, token);

                if (!prDocuments.Success)
                {
                    // We need to update the log to be included in the timeline now that we are done
                    prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
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

            // Merge files here and this file is what I send back to candid... 
            ProcessResult<Dictionary<string, int>> prFilePageCounts = _docsService.MergePdfs(prFileDownload.Result, prDisclosuresName.Result, dictAttachmentNamesToFind.Keys.ToList());

            if (!prFilePageCounts.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Map signature coordinates to draft edit request for docusign
            ProcessResult<DraftEditRequest> prDraftEditRequest = _docsService.MapToDraftEditRequest(request, docSetDocuments, prFilePageCounts.Result);

            if (!prDraftEditRequest.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Get salesforce credentials for org
            ProcessResult<OrgInfo> prOrgInfo = await _candidDataService.GetCandidCredentialsByOrg(request.OrgId);

            if (!prOrgInfo.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Get org request info so we can make api calls to salesforce
            ProcessResult<OrgRequestInfo> prOrgRequestInfo = await _sfService.AuthenticateToSalesforce(prOrgInfo.Result);

            if (!prOrgRequestInfo.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Get AWS settings
            ProcessResult<GoogleDriveClone> prAWSSettings = await _sfService.GetOrgAWSSettings(request.OrgId, request.LoId, prOrgRequestInfo.Result);

            if (!prAWSSettings.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Set up bucket name
            string bucket = prAWSSettings.Result.Root_Folder_Name__c.Replace(" ", "-").ToLower();
            prDraftEditRequest.Result.S3RootFolderName = bucket;
            prDraftEditRequest.Result.Status = "sent";

            ProcessResult<string> prUpload = await _s3Service.PushFileToS3(bucket, request.Path, request.ProspectId, prDisclosuresName.Result);

            if (!prUpload.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            ProcessResult<string> prPushDocument = await _sfService.PushUploadUrlToCandid(prUpload.Result, request, prOrgRequestInfo.Result, prDisclosuresName.Result);

            if (!prPushDocument.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // Set document id
            prDraftEditRequest.Result.DocumentId = prPushDocument.Result;

            ProcessResult<string> prEnvelopeId = await _docusignService.RequestDraftEdit(prDraftEditRequest.Result, prDisclosuresName.Result);

            if (!prEnvelopeId.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }

            // We need to update the log to be included in the timeline now that we are done
            prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);

            if (!prLogUpdated.Success)
            {
                // We need to update the log to be included in the timeline now that we are done
                prLogUpdated = await _disclosureAuditService.UpdateDisclosureTrackingLogs(request.OrgId, request.LoanId, prDisclosureLogs.Result, true, token);
                return;
            }
        }
    }

    public interface IDisclosuresService
    {
        /// <summary>
        /// This will handle the event notification from Encompass.
        /// This will detect the org the event is for, and then retrieve the access token for that org.
        /// This will then retrieve files from efolder, save to s3, and notify candid
        /// </summary>
        /// <param name="eventNotification"></param>
        /// <returns></returns>
        Task Retrieve(CandidIncomingRequest request, string token);
    }
}
