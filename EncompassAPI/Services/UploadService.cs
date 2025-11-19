using Amazon.S3.Model;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using Microsoft.Extensions.Options;

namespace EncompassAPI.Services
{
    public class UploadService : IUploadService
    {
        private readonly IFileService _fileService;
        private readonly S3Service _s3Service;
        private readonly LoggerService<UploadService> _logger;
        private readonly FileConfig _fileConfig;

        public UploadService(IFileService fileService, S3Service s3Service, LoggerService<UploadService> logger, IOptionsSnapshot<FileConfig> fileOptions)
        {
            _fileService = fileService;
            _s3Service = s3Service;
            _logger = logger;
            _fileConfig = fileOptions.Value;
        }

        public async Task<ProcessResult<string>> UploadMultiPart(string orgId, GoogleDriveClone awsSettings, string path, string fileName)
        {
            try
            {
                string fileToPushToS3 = _fileConfig.LocalDownloadPath + "/" + fileName;
                string bucket = awsSettings.Root_Folder_Name__c.Replace(" ", "-").ToLower();
                string key = path + "/" + fileName;

                // Initiate multipart upload
                string uploadId = await _s3Service.InitiateMultiPartUpload(bucket, key);
                List<string> filePaths = new List<string>();

                int partNumber = 1;
                List<PartETag> partETags = new List<PartETag>();

                try
                {
                    // Upload part
                    using (var stream = new FileStream(fileToPushToS3, FileMode.Open, FileAccess.Read))
                    {
                        stream.Position = 0;
                        var partResponse = await _s3Service.UploadPartAsync(bucket, key, uploadId, partNumber, stream, true);

                        partETags.Add(new PartETag(partResponse.PartNumber ?? 0, partResponse.ETag));

                        partNumber++;
                    }

                    // Complete multipart upload
                    string uri = await _s3Service.CompleteMultipartUploadAsync(bucket, key, uploadId, partETags);

                    return new ProcessResult<string>(true)
                    {
                        Result = uri
                    };
                }
                catch (Exception ex)
                {
                    // Abort multipart upload in case of failure
                    await _s3Service.AbortMultipartUploadAsync(bucket, key, uploadId);

                    throw new Exception(ex.Message);
                }

            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to upload files to S3 for org: {orgId}{Environment.NewLine}Exception: {ex.Message}");
                return new ProcessResult<string>(false, $"Failed to upload files to S3 for org: {orgId}");
            }
        }
    }

    public interface IUploadService
    {
        Task<ProcessResult<string>> UploadMultiPart(string orgId, GoogleDriveClone awsSettings, string path, string fileName);
    }
}
