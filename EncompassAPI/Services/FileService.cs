using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Transfer;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using Microsoft.Extensions.Options;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Shared.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EncompassAPI.Services
{
    public class FileService : IFileService
    {
        private IHttpService _http;
        private readonly S3Service _s3Service;
        private readonly FileConfig _fileConfig;
        private readonly LoggerService<FileService> _logger;
        private readonly CacheStorageConfig _cacheStorageConfig;

        public FileService(IDefaultHttpService http, S3Service s3Service, LoggerService<FileService> logger, IOptionsSnapshot<FileConfig> fileOptions, IOptionsSnapshot<CacheStorageConfig> cacheStorageOptions)
        {
            _http = http;
            _s3Service = s3Service;
            _fileConfig = fileOptions.Value;
            _logger = logger;
            _cacheStorageConfig = cacheStorageOptions.Value;
        }

        public async Task<ProcessResult<(string bucket, string key)>> PushFilesToS3(string orgId, string prospectId, DisclosureStage disclosureStage)
        {
            _logger.Base.LogInformation(nameof(PushFilesToS3));

            try
            {
                string[] inputFiles = Directory.GetFiles($"{_fileConfig.LocalDownloadPath}/{orgId}/{prospectId}/{disclosureStage}", "*.pdf");

                string key = $"{_cacheStorageConfig.S3BucketPrefix}/{orgId}/{prospectId}/{disclosureStage}/";

                foreach (string filePath in inputFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    string objectKey = $"{key}{fileName}";

                    ProcessResult prRespone = await _s3Service.PushFileToS3(_cacheStorageConfig.S3Bucket, objectKey, filePath);

                    if(!prRespone.Success)
                    {
                        _logger.Base.LogError($"{CommonFields.FailedToUploadFile} for file {filePath} to S3 at {key}");
                        return new ProcessResult<(string, string)>(false, prRespone.Message);
                    }
                }

                return new ProcessResult<(string, string)>(true)
                {
                    Result = (_cacheStorageConfig.S3Bucket, key)
                };
            }
            catch (AmazonS3Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToUploadFile}{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<(string, string)>(false, CommonFields.FailedToUploadFile);
            }
        }

        public async Task<ProcessResult<string>> DownloadMutipleFiles(string prospectId, AttachmentUrls attachmentUrls, Attachment[] attachmentInfo)
        {
            _logger.Base.LogInformation($"Entering {nameof(DownloadMutipleFiles)}");

            // Remove files if they exist already first
            RemoveFiles($"{_fileConfig.LocalDownloadPath}/{prospectId}");

            string prefixDownloadPath = $"{_fileConfig.LocalDownloadPath}/{prospectId}";

            try
            {
                string filename = "";
                //string timeStartedDownwloading = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Directory.CreateDirectory(prefixDownloadPath); // Create directory if not exists

                foreach (var item in attachmentUrls.attachments)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>();

                    if(item.authorizationHeader != null)
                    {
                        headers.Add("Authorization", item.authorizationHeader);
                    }

                    string url = item.url != null ? item.url : item.originalUrls[0];

                    // Get file content from skydrive
                    HttpResponseMessage response = await _http.GetAsync(url, headers);

                    if (response == null)
                    {
                        _logger.Base.LogError($"Error getting file: {url}");
                        return new ProcessResult<string>(false, CommonFields.ServerError);
                    }

                    // Get file bytes
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Get file name

                    Attachment attachment = attachmentInfo.First(att => att.entityId == item.id);

                    filename = $"{attachment.entityName}.pdf";
                    _logger.LogVerbose($"Downloaded filename: {filename}");

                    string fullPath = Path.Combine(prefixDownloadPath, filename.Replace("/", "%2F"));

                    // Save file to local folder
                    await File.WriteAllBytesAsync(fullPath, fileBytes);
                }


                return new ProcessResult<string>(true)
                {
                    Result = prefixDownloadPath
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToDownloadAllFiles}{Environment.NewLine}Exception: {ex}");

                // Failed, we do not want to continue
                return new ProcessResult<string>(false, CommonFields.FailedToDownloadAllFiles);
            }
        }

        public async Task<ProcessResult<string>> DownloadMutipleFiles(string orgId, string prospectId, List<Models.Encompass.Document> docSetDocuments, DisclosureStage disclosureStage)
        {
            _logger.Base.LogInformation(nameof(DownloadMutipleFiles));

            // Remove files if they exist already first
            RemoveFiles($"{_fileConfig.LocalDownloadPath}/{orgId}/{prospectId}/{disclosureStage}");

            string prefixDownloadPath = $"{_fileConfig.LocalDownloadPath}/{orgId}/{prospectId}/{disclosureStage}";

            try
            {
                string filename = "";
                //string timeStartedDownwloading = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Directory.CreateDirectory(prefixDownloadPath); // Create directory if not exists

                foreach (var item in docSetDocuments)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>() { { "Authorization", item.pdf.authorizationHeader } };

                    // Get file content from skydrive
                    HttpResponseMessage response = await _http.GetAsync(item.pdf.url, headers);

                    if (response == null)
                    {
                        _logger.Base.LogError($"Error getting file: {item.pdf.url}");
                        return new ProcessResult<string>(false, CommonFields.ServerError);
                    }

                    // Get file bytes
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Get file name
                    filename = $"{item.name}.pdf";
                    _logger.LogVerbose($"Downloaded filename: {filename}");

                    string fullPath = Path.Combine(prefixDownloadPath, filename.Replace("/", "%2F"));

                    // Save file to local folder
                    await File.WriteAllBytesAsync(fullPath, fileBytes);
                }


                return new ProcessResult<string>(true)
                {
                    Result = prefixDownloadPath
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToDownloadAllFiles}{Environment.NewLine}Exception: {ex}");

                // Failed, we do not want to continue
                return new ProcessResult<string>(false, CommonFields.FailedToDownloadAllFiles);
            }
        }

        public async Task<ProcessResult<string>> DownloadMutipleFiles(string prospectId, List<Models.Encompass.Document> docSetDocuments)
        {
            _logger.Base.LogInformation($"Entering {nameof(DownloadMutipleFiles)}");

            // Remove files if they exist already first
            RemoveFiles(prospectId);

            string prefixDownloadPath = $"{_fileConfig.LocalDownloadPath}/{prospectId}";

            try
            {
                string filename = "";
                //string timeStartedDownwloading = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Directory.CreateDirectory(prefixDownloadPath); // Create directory if not exists

                foreach (var item in docSetDocuments)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>() { { "Authorization", item.pdf.authorizationHeader } };

                    // Get file content from skydrive
                    HttpResponseMessage response = await _http.GetAsync(item.pdf.url, headers);

                    if (response == null)
                    {
                        _logger.Base.LogError($"Error getting file: {item.pdf.url}");
                        return new ProcessResult<string>(false, CommonFields.ServerError);
                    }

                    // Get file bytes
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Get file name
                    filename = $"{item.name}.pdf";
                    _logger.LogVerbose($"Downloaded filename: {filename}");

                    string fullPath = Path.Combine(prefixDownloadPath, filename.Replace("/", "%2F"));

                    // Save file to local folder
                    await File.WriteAllBytesAsync(fullPath, fileBytes);
                }


                return new ProcessResult<string>(true)
                {
                    Result = prefixDownloadPath
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"{CommonFields.FailedToDownloadAllFiles}{Environment.NewLine}Exception: {ex}");

                // Failed, we do not want to continue
                return new ProcessResult<string>(false, CommonFields.FailedToDownloadAllFiles);
            }
        }

        public async Task<ProcessResult<FileMetadata>> DownloadFile(string urlPdfPath, string authorization)
        {
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>() { { "Authorization", authorization } };

                // Get file content from skydrive
                HttpResponseMessage response = await _http.GetAsync(urlPdfPath, headers);

                if (response == null)
                {
                    _logger.Base.LogError($"Error getting file: {urlPdfPath}");
                    return new ProcessResult<FileMetadata>(false, CommonFields.ServerError);
                }

                // Get file bytes
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Get file name
                string filename = GetFileNameFromResponse(response);
                string fullPath = Path.Combine(_fileConfig.LocalDownloadPath, filename + ".pdf");

                // Save file to local folder
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                return new ProcessResult<FileMetadata>(true)
                {
                    Result = new FileMetadata()
                    {
                        Path = fullPath,
                        Name = filename
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to download file from {urlPdfPath}{Environment.NewLine}Exception: {ex}");

                // Failed, we do not want to continue
                return new ProcessResult<FileMetadata>(false, CommonFields.FailedToDownloadFile);
            }
        }

        public ProcessResult<Dictionary<string, int>> MergePdfsV4(string downloadPathPrefix, string disclosuresName, List<string> attachmentNames)
        {
            try
            {
                Dictionary<string, int> filePageCounts = new Dictionary<string, int>();

                int currentPageCount = 0;

                // Get all PDF files with path in the folder
                string[] inputFiles = Directory.GetFiles(downloadPathPrefix, "*.pdf");

                // Order the files based on how they were ordered
                var sortedFiles = attachmentNames
                .Select(name => inputFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name.Replace("/", "%2F")))
                .ToArray();

                _logger.LogVerbose("Sorted files: ");

                _logger.LogVerbose(JsonSerializer.Serialize(sortedFiles));

                using (PdfDocument outputDocument = new PdfDocument())
                {
                    foreach (string file in sortedFiles)
                    {
                        _logger.LogVerbose($"Merging file: {file}");

                        if (string.IsNullOrEmpty(file))
                        {

                            _logger.Base.LogError("One of the files to merge is null or empty. Maybe this file could not be found.");
                            continue;
                        }

                        using (PdfDocument inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import))
                        {
                            // Add the page count as the total page count so far
                            currentPageCount += inputDocument.PageCount;

                            string fileName = Path.GetFileName(file).Replace(".pdf", "");

                            filePageCounts.Add(fileName, currentPageCount);

                            foreach (PdfPage page in inputDocument.Pages)
                            {
                                outputDocument.AddPage(page);
                            }
                        }
                    }

                    // Save the merged document
                    outputDocument.Save($"{downloadPathPrefix}/{disclosuresName}");
                }

                return new ProcessResult<Dictionary<string, int>>(true)
                {
                    Result = filePageCounts
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to merge pdfs in path {downloadPathPrefix}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<Dictionary<string, int>>(false, $"Failed to merge pdfs in path {downloadPathPrefix}.");
            }
        }

        public void RemoveFolder(string prospectId)
        {
            string prefixDownloadPath = $"{_fileConfig.LocalDownloadPath}/{prospectId}";

            try
            {
                if (Directory.Exists(prefixDownloadPath))
                {
                    Directory.Delete(prefixDownloadPath, true);
                }
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to delete directory {prefixDownloadPath}. Exception: {ex}");
            }

        }

        public void RemoveFiles(string prefixPath)
        {
            if (Directory.Exists(prefixPath))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(prefixPath);

                foreach (string file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        _logger.Base.LogError($"Failed to delete file {file}{Environment.NewLine}Exception: {e}");
                    }
                }
            }
        }

        private void RemoveFilesExceptOne(string downloadPathPrefix, string fileToKeep)
        {
            if (Directory.Exists(downloadPathPrefix))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(downloadPathPrefix);

                foreach (string file in files)
                {
                    try
                    {
                        if (!file.Equals(fileToKeep, StringComparison.OrdinalIgnoreCase))
                            continue;

                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        _logger.Base.LogError($"Failed to delete file {file}{Environment.NewLine}Exception: {e}");
                    }
                }
            }
        }

        private string GetFileNameFromResponse(HttpResponseMessage response)
        {
            string filename = String.Empty;

            try
            {

                if (response.Content.Headers.ContentDisposition != null)
                {
                    filename = response.Content.Headers.ContentDisposition.FileName?.Trim('"');
                }
                else if (response.Content.Headers.TryGetValues("Content-Disposition", out var values))
                {
                    // Fallback if the parsed header is null
                    var contentDisposition = values.FirstOrDefault();
                    if (!string.IsNullOrEmpty(contentDisposition))
                    {
                        var match = Regex.Match(contentDisposition, @"filename=""?([^"";]+)""?");
                        if (match.Success)
                        {
                            filename = match.Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Base.LogError("Cannot get filename from response");
            }

            return filename;
        }
    }

    public interface IFileService
    {
        Task<ProcessResult<(string bucket, string key)>> PushFilesToS3(string orgId, string prospectId, DisclosureStage disclosureStage);
        Task<ProcessResult<string>> DownloadMutipleFiles(string prospectId, AttachmentUrls attachmentUrls, Attachment[] attachmentInfo);
        Task<ProcessResult<string>> DownloadMutipleFiles(string prospectId, List<Models.Encompass.Document> docSetDocuments);
        Task<ProcessResult<string>> DownloadMutipleFiles(string orgId, string prospectId, List<Models.Encompass.Document> docSetDocuments, DisclosureStage disclosureStage);
        Task<ProcessResult<FileMetadata>> DownloadFile(string urlPdfPath, string authorization);
        ProcessResult<Dictionary<string, int>> MergePdfsV4(string downloadPathPrefix, string disclosuresName, List<string> attachmentNames);
        void RemoveFolder(string prospectId);
        void RemoveFiles(string prefixPath);
    }
}
