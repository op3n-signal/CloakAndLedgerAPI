using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Salesforce;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.IO;

namespace EncompassAPI.Services
{
    public class S3Service
    {
        private readonly LoggerService<S3Service> _logger;
        private readonly FileConfig _fileConfig;
        private readonly S3Config _s3Config;
        private readonly AmazonS3Client _s3Client;

        public S3Service(LoggerService<S3Service> logger, IOptionsSnapshot<FileConfig> fileOptions, IOptionsSnapshot<S3Config> s3Options) 
        {
            _logger = logger;
            _fileConfig = fileOptions.Value;
            _s3Config = s3Options.Value;
            _s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(_s3Config.AwsRegion)); ;
        }

        public async Task<string> InitiateMultiPartUpload(string bucketName, string key)
        {
            using var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(_s3Config.AwsRegion));

            var request = new InitiateMultipartUploadRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentType = "application/pdf",

            };

            InitiateMultipartUploadResponse response = await s3Client.InitiateMultipartUploadAsync(request);

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

        public async Task<ProcessResult> PushFileToS3(string bucket, string keyPath, string localFilePath)
        {
            _logger.LogVerbose(nameof(PushFileToS3));

            try
            {

                if (!File.Exists(localFilePath))
                {
                    _logger.Base.LogError($"{CommonFields.FileDoesNotExist}: {localFilePath}");
                    return new ProcessResult(false, CommonFields.FileDoesNotExist);
                }

                TransferUtility transferUtility = new TransferUtility(_s3Client);

                TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
                {
                    BucketName = bucket,
                    Key = keyPath,
                    FilePath = localFilePath
                };

                await transferUtility.UploadAsync(request);

                _logger.LogVerbose($"File {localFilePath} has been successfully uploaded to bucket {bucket}.");
                return new ProcessResult(true);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToUploadFile}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, CommonFields.FailedToUploadFile);
            }
        }

        public async Task<ProcessResult<string>> PushFileToS3(string bucket, string path, string prospectId, string disclosuresDownloadPath, string disclosuresName)
        {
            _logger.LogVerbose(nameof(PushFileToS3));

            string fileToPushToS3 = disclosuresDownloadPath + "/" + disclosuresName;

            try
            {

                if (!File.Exists(fileToPushToS3))
                {
                    _logger.Base.LogError($"{CommonFields.FileDoesNotExist}: {disclosuresName}");
                    return new ProcessResult<string>(false, CommonFields.FileDoesNotExist);
                }

                TransferUtility transferUtility = new TransferUtility(_s3Client);

                TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
                {
                    BucketName = bucket,
                    Key = path + "/" + disclosuresName,
                    FilePath = fileToPushToS3
                };

                await transferUtility.UploadAsync(request);

                _logger.LogVerbose($"File {disclosuresName} has been successfully uploaded to bucket {bucket}.");
                return new ProcessResult<string>(true)
                {
                    Result = "http://s3.amazonaws.com/" + bucket + "/" + path + "/" + disclosuresName
                };
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToUploadFile}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, CommonFields.FailedToUploadFile);
            }
        }

        public async Task<ProcessResult<string>> PushFileToS3(string bucket, string path, string prospectId, string disclosuresName)
        {
            _logger.LogVerbose(nameof(PushFileToS3));

            string fileToPushToS3 = _fileConfig.LocalDownloadPath + "/" + prospectId + "/" + disclosuresName;

            try
            {

                if (!File.Exists(fileToPushToS3))
                {
                    _logger.Base.LogError($"{CommonFields.FileDoesNotExist}: {disclosuresName}");
                    return new ProcessResult<string>(false, CommonFields.FileDoesNotExist);
                }

                TransferUtility transferUtility = new TransferUtility(_s3Client);

                TransferUtilityUploadRequest request = new TransferUtilityUploadRequest()
                {
                    BucketName = bucket,
                    Key = path + "/" + disclosuresName,
                    FilePath = fileToPushToS3
                };

                await transferUtility.UploadAsync(request);

                _logger.LogVerbose($"File {disclosuresName} has been successfully uploaded to bucket {bucket}.");
                return new ProcessResult<string>(true)
                {
                    Result = "http://s3.amazonaws.com/" + bucket + "/" + path + "/" + disclosuresName
                };
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToUploadFile}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, CommonFields.FailedToUploadFile);
            }
        }
    }
}
