using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Salesforce;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using Shared.Models;

namespace EncompassAPI.Services
{
    public class S3Service
    {
        private readonly LoggerService<S3Service> _logger;
        private readonly FileConfig _fileConfig;
        private readonly S3Config _s3Config;
        private readonly IAmazonS3 _s3Client;

        public S3Service(LoggerService<S3Service> logger, IOptionsSnapshot<FileConfig> fileOptions, IOptionsSnapshot<S3Config> s3Options, IAmazonS3 s3Client) 
        {
            _logger = logger;
            _fileConfig = fileOptions.Value;
            _s3Config = s3Options.Value;
            _s3Client = s3Client;
        }

        public async Task<string> InitiateMultiPartUpload(string bucketName, string key)
        {
            var request = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentType = "application/octet-stream"
            };

            InitiateMultipartUploadResponse response = await _s3Client.InitiateMultipartUploadAsync(request);

            return response.UploadId;
        }

        public async Task<UploadPartResponse> UploadPartAsync(string bucketName, string key, string uploadId, int partNumber, Stream partStream, bool isLastPart = false)
        {
            var request = new UploadPartRequest
            {
                BucketName = bucketName,
                Key = key,
                UploadId = uploadId,
                PartNumber = partNumber,
                InputStream = partStream,
                PartSize = partStream.Length,
                IsLastPart = isLastPart
            };

            return await _s3Client.UploadPartAsync(request);
        }

        public async Task<string> CompleteMultipartUploadAsync(string bucketName, string key, string uploadId, List<PartETag> partETags)
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                UploadId = uploadId,
                PartETags = partETags
            };

            CompleteMultipartUploadResponse response =  await _s3Client.CompleteMultipartUploadAsync(request);

            return response.Location;
        }

        public async Task AbortMultipartUploadAsync(string bucketName, string key, string uploadId)
        {
            var request = new AbortMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                UploadId = uploadId
            };

            await _s3Client.AbortMultipartUploadAsync(request);
        }

        public async Task<ProcessResult<string>> PushFileToS3(GoogleDriveClone awsSettings, string path, string fileName)
        {
            _logger.LogVerbose(nameof(PushFileToS3));

            string fileToPushToS3 = _fileConfig.LocalDownloadPath + fileName;
            string bucket = awsSettings.Root_Folder_Name__c.Replace(" ", "-").ToLower();

            try
            {
                using var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(_s3Config.AwsRegion));

                if (!File.Exists(fileToPushToS3))
                {
                    _logger.Base.LogError($"{CommonStrings.FileDoesNotExist}: {fileName}");
                    return new ProcessResult<string>(false, CommonStrings.FileDoesNotExist);
                }

                TransferUtility xferUtility = new TransferUtility(s3Client);
                await xferUtility.UploadAsync(fileToPushToS3, bucket, path + "/" + fileName);

                _logger.LogVerbose($"File {fileName} has been successfully uploaded to bucket {bucket}.");
                return new ProcessResult<string>(true)
                {
                    Result = "http://s3.amazonaws.com/" + bucket + "/" + path + "/" + fileName
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonStrings.FailedToUploadFile}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, CommonStrings.FailedToUploadFile);
            }
        }
    }
}
