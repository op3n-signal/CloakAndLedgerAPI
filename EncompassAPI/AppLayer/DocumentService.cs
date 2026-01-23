using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Microsoft.Extensions.Options;

namespace EncompassAPI.AppLayer
{
    public class DocumentService : IDocumentService, IAsyncDisposable
    {
        private readonly IeFolderService _eFolderService;
        private readonly S3Service _s3Service;
        private readonly CacheStorageConfig _cacheStorageConfig;

        public DocumentService(IeFolderService eFolderService, S3Service s3Service, IOptionsSnapshot<CacheStorageConfig> cacheStorageConfig)
        {
            _eFolderService = eFolderService;
            _s3Service = s3Service;
            _cacheStorageConfig = cacheStorageConfig.Value;

        }

        public async Task<ProcessResult> AutoIndex(CandidAuditRequest request, List<string> documentNamesInDTL, string token)
        {
            string keyPath = $"{_cacheStorageConfig.S3CachePrefix}/{request.orgId}/{request.documentId}/{request.envelopeId}/";

            // Get Documents from s3 
            ProcessResult<Dictionary<string, byte[]>> prObjects = await _s3Service.GetPdfObjects(_cacheStorageConfig.S3Bucket, keyPath);

            if (!prObjects.Success)
            {
                return new ProcessResult(false, prObjects.Message);
            }

            // Get all pdf objects that are not in the document names from DTL
            List<string> keysToRemove = prObjects.Result.Keys.Where(key => !documentNamesInDTL.Contains(key)).ToList();

            // Remove non-matching pdf objects from the dictionary
            foreach (string key in keysToRemove)
            {
                prObjects.Result.Remove(key);
            }

            ProcessResult prUpload = await _eFolderService.Upload(request, prObjects.Result, token);

            if(!prUpload.Success)
            {
                return new ProcessResult(false, prUpload.Message);
            }

            return new ProcessResult(true);
        }

        public async Task<ProcessResult> DeleteAutoIndexDisclosuresFromS3(CandidAuditRequest request)
        {
            string keyPath = $"{_cacheStorageConfig.S3CachePrefix}/{request.orgId}/{request.documentId}/{request.envelopeId}/";

            return await _s3Service.DeleteBucketContentsAsync(_cacheStorageConfig.S3Bucket, keyPath);
        }

        public async ValueTask DisposeAsync()
        {

        }
    }

    public interface IDocumentService
    {
        Task<ProcessResult> AutoIndex(CandidAuditRequest request, List<string> documentNamesInDTL, string token);
        Task<ProcessResult> DeleteAutoIndexDisclosuresFromS3(CandidAuditRequest request);
        ValueTask DisposeAsync();
    }
}
