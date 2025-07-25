using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using Microsoft.Extensions.Options;

namespace EncompassAPI.Services
{
    public class S3Service
    {
        private readonly LoggerService<S3Service> _logger;
        private readonly FileConfig _fileConfig;
        private readonly S3Config _s3Config;

        public S3Service(LoggerService<S3Service> logger, IOptionsSnapshot<FileConfig> fileOptions, IOptionsSnapshot<S3Config> s3Options) 
        {
            _logger = logger;
            _fileConfig = fileOptions.Value;
            _s3Config = s3Options.Value;
        }

        public async Task<ProcessResult> PushFileToS3(string bucket, string fileName)
        {
            _logger.LogVerbose(nameof(PushFileToS3));

            string fileToPushToS3 = _fileConfig.LocalDownloadPath + fileName;

            try
            {
                using var s3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(_s3Config.AwsRegion));

                if (!File.Exists(fileToPushToS3))
                {
                    _logger.Base.LogError($"{CommonStrings.FileDoesNotExist}: {fileName}");
                    return new ProcessResult(false, CommonStrings.FileDoesNotExist);
                }

                TransferUtility xferUtility = new TransferUtility(s3Client);
                await xferUtility.UploadAsync(fileToPushToS3, bucket, fileName);

                _logger.LogVerbose($"File {fileName} has been successfully uploaded to bucket {bucket}.");
                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonStrings.FailedToUploadFile}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, CommonStrings.FailedToUploadFile);
            }
        }
    }
}
