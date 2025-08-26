using Amazon.S3.Model;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;

namespace EncompassAPI.AppLayer
{
    public class StorageService
    {
        private readonly IUploadService _uploadService;
        private readonly ISalesforceService _salesforceService;
        private readonly ICandidDataService _candidDataService;

        public StorageService(IUploadService uploadService, ISalesforceService salesforceService, ICandidDataService candidDataService) 
        { 
            _uploadService = uploadService;
            _salesforceService = salesforceService;
            _candidDataService = candidDataService;
        }

        public async Task UploadFilesToS3(AttachmentUploadRequest attachmentUploadRequest)
        {
            //tod get file upload url from S3
    /*        ProcessResult<string> prUploadUrl = await _uploadService.UploadAttachments(attachmentUploadRequest);

            if(!prUploadUrl.Success)
            {
                return;
            }

            //todo send back the upload url to the candid rest api
            ProcessResult<OrgInfo> prOrgInfo = await _candidDataService.GetCandidCredentialsByOrg(attachmentUploadRequest.orgId);

            if (!prOrgInfo.Success)
            {
                return;
            }

            ProcessResult<OrgRequestInfo> prOrgRequestInfo = await _salesforceService.AuthenticateToSalesforce(prOrgInfo.Result);

            if (!prOrgRequestInfo.Success)
            {
                return;
            }*/

            //await _salesforceService.PushUploadUrlToCandid();
        }
    }
}
