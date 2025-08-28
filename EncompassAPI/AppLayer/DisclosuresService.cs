using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
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

        public DisclosuresService(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService, IUploadService uploadService, IEncompassService docsService)
        {
            _fileService = fileService;
            _eFolderService = eFolderService;
            _s3Service = s3Service;
            _candidDataService = candidDataService;
            _sfService = sfService;
            _uploadService = uploadService;
            _docsService = docsService;
        }

        public async Task Retrieve(CandidIncomingRequest request)
        {
            // Retrieve documents
            ProcessResult<IEnumerable<EncompassDocument>> prEncompassDocuments = await _eFolderService.GetListOfDocuments(request.OrgId, request.LoanId);

            if (!prEncompassDocuments.Success)
            {
                return;
            }

            ProcessResult<Attachment[]> prAttachments = await _eFolderService.GetAttachments(request.OrgId, request.LoanId, "EDisclosures", prEncompassDocuments.Result);

            if (!prAttachments.Success)
            {
                return;
            }

            ProcessResult<AttachmentUrls> prAttachmentsUrls = await _eFolderService.GetAttachmentDownloadUrls(request.OrgId, request.LoanId, prAttachments.Result);

            if (!prAttachmentsUrls.Success)
            {
                return;
            }

            // Download attachments from encompass
            ProcessResult<string> prFileDownload = await _fileService.DownloadMutipleFiles(prAttachmentsUrls.Result, prAttachments.Result[0].entityName);

            if (!prFileDownload.Success)
            {
                return;
            }

            // Get salesforce credentials for org
            ProcessResult<OrgInfo> prOrgInfo = await _candidDataService.GetCandidCredentialsByOrg(request.OrgId);

            if (!prOrgInfo.Success)
            {
                return;
            }

            // Get org request info so we can make api calls to salesforce
            ProcessResult<OrgRequestInfo> prOrgRequestInfo = await _sfService.AuthenticateToSalesforce(prOrgInfo.Result);

            if (!prOrgRequestInfo.Success)
            {
                return;
            }

            // Get AWS settings
            ProcessResult<GoogleDriveClone> prAWSSettings = await _sfService.GetOrgAWSSettings(request.OrgId, request.LoId, prOrgRequestInfo.Result);

            if (!prAWSSettings.Success)
            {
                return;
            }

            ProcessResult<string> prUpload = await _s3Service.PushFileToS3(prAWSSettings.Result, request.Path, prFileDownload.Result);

            if (!prUpload.Success)
            {
                return;
            }

            ProcessResult prPush = await _sfService.PushUploadUrlToCandid(prUpload.Result, request.OrgId, request.ProspectId, request.DocumentId, prOrgRequestInfo.Result);

            if (!prPush.Success)
            {
                return;
            }

            // Get application id from loan because we need this in the next requests
            ProcessResult<string> prApplicationId = await _docsService.GetApplicationIdFromLoan(request.OrgId, request.LoanId);

            if (!prApplicationId.Success)
            {
                return;
            }

            ProcessResult<string> prAuditId = await _docsService.CreateLoanAuditForOpeningOrder(request.OrgId, request.LoanId, prApplicationId.Result);

            if (!prAuditId.Success)
            {
                return;
            }

            //todo maybe return response of what failed in the audit
            ProcessResult prAuditSuccess = await _docsService.GetDocumentAuditForOpeningOrder(request.OrgId, request.LoanId, prAuditId.Result);

            if (!prAuditSuccess.Success)
            {
                return;
            }

            // Get documents with signature coordinates
            ProcessResult<string> prOrderId = await _docsService.GenerateOpeningDocSet(request.OrgId, request.LoanId, prAuditId.Result);

            if(!prOrderId.Success)
            {
                return;
            }

            ProcessResult<Models.Encompass.Document[]> prDocuments = await _docsService.GetOrderStatus(request.OrgId, request.LoanId, prOrderId.Result);

            if(!prDocuments.Success)
            {
                return;
            }
            
            //todo check if all documents are found

            //todo only enter here if not all docs were found
            if (false)
            {
                //todo retrieve efolder docs first only if not all docs are found
                ProcessResult<string[]> prEFolderDocuments = await _docsService.GetEFolderDocuments(request.OrgId, request.LoanId);

                if (!prEFolderDocuments.Success)
                {
                    return;
                }

                //todo then add to list
                ProcessResult<string> prJobId = await _docsService.AddToOpeningDocSet(request.OrgId, request.LoanId, prOrderId.Result, prApplicationId.Result, prEFolderDocuments.Result);

                if (!prJobId.Success)
                {
                    return;
                }

                prDocuments = await _docsService.GetOrderStatus(request.OrgId, request.LoanId, prJobId.Result);

                if (!prDocuments.Success)
                {
                    return;
                }
            }

            //todo matching logic
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
        Task Retrieve(CandidIncomingRequest request);
    }
}
