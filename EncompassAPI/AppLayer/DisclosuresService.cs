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

        public DisclosuresService(IFileService fileService, IeFolderService eFolderService, S3Service s3Service, ICandidDataService candidDataService, ISalesforceService sfService)
        {
            _fileService = fileService;
            _eFolderService = eFolderService;
            _s3Service = s3Service;
            _candidDataService = candidDataService;
            _sfService = sfService;
        }

        public async Task RunEventHandler(CandidIncomingRequest request)
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
            ProcessResult prFileDownload = await _fileService.DownloadMutipleFiles(prAttachmentsUrls.Result);

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

            ProcessResult<string> prUpload = await _s3Service.PushFileToS3(prAWSSettings.Result, request.Path, prAttachments.Result[0].entityName);

            if (!prUpload.Success)
            {
                return;
            }

            ProcessResult prPush = await _sfService.PushUploadUrlToCandid(prUpload.Result, request.OrgId, request.DocumentId, prOrgRequestInfo.Result);

            if (!prPush.Success)
            {
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
        Task RunEventHandler(CandidIncomingRequest request);
    }
}
