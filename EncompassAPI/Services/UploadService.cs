using Amazon.S3.Model;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;

namespace EncompassAPI.Services
{
    public class UploadService : IUploadService
    {
        private readonly IFileService _fileService;
        private readonly S3Service _s3Service;
        private readonly LoggerService<UploadService> _logger;

        public UploadService(IFileService fileService, S3Service s3Service, LoggerService<UploadService> logger)
        {
            _fileService = fileService;
            _s3Service = s3Service;
            _logger = logger;
        }

        /*public async Task<ProcessResult<string>> UploadAttachments(AttachmentUploadRequest attachmentUploadRequest)
        {
            try
            {
                // Initiate multipart upload
                string uploadId = await _s3Service.InitiateMultiPartUpload(attachmentUploadRequest.bucketName, attachmentUploadRequest.key);
                List<string> filePaths = new List<string>();

                // Loop through each attachment URL
                foreach (AttachmentUrl att in attachmentUploadRequest.attachments)
                {
                    // Get file content from skydrive
                    ProcessResult<FileMetadata> prFileMetadata = await _fileService.DownloadFile(att.url, att.authorizationHeader);

                    if (!prFileMetadata.Success)
                    {
                        //todo
                    }

                    // Add saved local file path to the list
                    filePaths.Add(prFileMetadata.Result.Path);
                }

                int partNumber = 1;
                List<PartETag> partETags = new List<PartETag>();

                try
                {
                    foreach (string filePath in filePaths)
                    {
                        // Read file bytes
                        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

                        // Upload part
                        using (var stream = new MemoryStream(fileBytes))
                        {
                            var partResponse = await _s3Service.UploadPartAsync(attachmentUploadRequest.bucketName, attachmentUploadRequest.key, uploadId, partNumber, stream, true);

                            partETags.Add(new PartETag(partResponse.PartNumber, partResponse.ETag));

                            partNumber++;
                        }
                    }

                    // Complete multipart upload
                    string uri = await _s3Service.CompleteMultipartUploadAsync(attachmentUploadRequest.bucketName, attachmentUploadRequest.key, uploadId, partETags);

                    return new ProcessResult<string>(true)
                    {
                        Result = uri
                    };
                }
                catch (Exception ex)
                {
                    // Abort multipart upload in case of failure
                    await _s3Service.AbortMultipartUploadAsync(attachmentUploadRequest.bucketName, attachmentUploadRequest.key, uploadId);

                    throw new Exception(ex.Message);
                }

            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to upload files to S3 for org: {attachmentUploadRequest.orgId}{Environment.NewLine}Exception: {ex.Message}");
                return new ProcessResult<string>(false, $"Failed to upload files to S3 for org: {attachmentUploadRequest.orgId}");
            }
        }*/
    }

    public interface IUploadService
    {
        //Task<ProcessResult<string>> UploadAttachments(AttachmentUploadRequest attachmentUploadRequest);
    }
}
