using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace EncompassAPI.Services
{
    public class FileService : IFileService
    {
        private IHttpService _http;
        private readonly FileConfig _fileConfig;
        private readonly LoggerService<FileService> _logger;

        public FileService(IDefaultHttpService http, LoggerService<FileService> logger, IOptionsSnapshot<FileConfig> fileOptions)
        {
            _http = http;
            _fileConfig = fileOptions.Value;
            _logger = logger;
        }

        public async Task<ProcessResult<string>> DownloadMutipleFiles(string prospectId, AttachmentUrls attachmentUrls, Attachment[] attachmentInfo)
        {
            // Remove files if they exist already first
            RemoveFiles(prospectId);

            string prefixDownloadPath = $"{_fileConfig.LocalDownloadPath}\\{prospectId}";

            try
            {
                string filename = "";
                //string timeStartedDownwloading = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                Directory.CreateDirectory(prefixDownloadPath); // Create directory if not exists

                foreach (var item in attachmentUrls.attachments)
                {
                    Dictionary<string, string> headers = new Dictionary<string, string>() { { "Authorization", item.authorizationHeader } };

                    // Get file content from skydrive
                    HttpResponseMessage response = await _http.GetAsync(item.url, headers);

                    if (response == null)
                    {
                        _logger.Base.LogError($"Error getting file: {item.url}");
                        return new ProcessResult<string>(false, CommonFields.ServerError);
                    }

                    // Get file bytes
                    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                    // Get file name

                    Attachment attachment = attachmentInfo.First(att => att.entityId == item.id);

                    filename = $"{attachment.entityName}.pdf";
                    _logger.LogVerbose($"Downloaded filename: {filename}");

                    string fullPath = Path.Combine(prefixDownloadPath, filename.Replace("/", "_"));

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

        public void RemoveFiles(string prospectId)
        {
            string prefixDownloadPath = $"{_fileConfig.LocalDownloadPath}\\{prospectId}";

            if (Directory.Exists(prefixDownloadPath))
            {
                // Get all files in the directory
                string[] files = Directory.GetFiles(prefixDownloadPath);

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
        Task<ProcessResult<string>> DownloadMutipleFiles(string prospectId, AttachmentUrls attachmentUrls, Attachment[] attachmentInfo);
        Task<ProcessResult<FileMetadata>> DownloadFile(string urlPdfPath, string authorization);
        void RemoveFiles(string prospectId);
    }
}
