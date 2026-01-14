using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
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

        public async Task AutoIndex(AutoIndexRequest request, string token)
        {

            // Get Documents from s3 
            ProcessResult<Dictionary<string, byte[]>> prObjects = await _s3Service.GetPdfObjects(_cacheStorageConfig.S3Bucket, _cacheStorageConfig.S3CachePrefix);

            if(!prObjects.Success)
            {
                return;
            }

            await _eFolderService.Upload(request, prObjects.Result, token);

        }

        public async ValueTask DisposeAsync()
        {

        }
    }

    public interface IDocumentService
    {
        Task AutoIndex(AutoIndexRequest request, string token);
        ValueTask DisposeAsync();
    }
}
