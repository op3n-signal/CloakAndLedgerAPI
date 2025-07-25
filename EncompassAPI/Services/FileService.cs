using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using Microsoft.Extensions.Options;
using System.IO;
using System.Text.RegularExpressions;

namespace EncompassAPI.Services
{
    public class FileService : IFileService
    {
        private IHttpService _http;
        private readonly FileConfig _fileConfig;
        private readonly LoggerService<FileService> _logger;

        public FileService(IHttpService http, LoggerService<FileService> logger, IOptionsSnapshot<FileConfig> fileOptions)
        {
            _http = http;
            _fileConfig = fileOptions.Value;
            _logger = logger;
        }

        public async Task<ProcessResult> DownloadFile(string urlPdfPath, string authorization)
        {
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>() { { "Authorization", authorization } };

                // Get file content from skydrive
                HttpResponseMessage response = await _http.GetAsync(urlPdfPath, headers);

                if (response == null)
                {
                    _logger.Base.LogError($"Error getting file: {urlPdfPath}");
                    return new ProcessResult(false, CommonStrings.ServerError);
                }

                // Get file bytes
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                // Get file name
                string filename = GetFileNameFromResponse(response);
                string fullPath = Path.Combine(_fileConfig.LocalDownloadPath, filename + ".pdf");

                // Save file to local folder
                await File.WriteAllBytesAsync(fullPath, fileBytes);

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to download file from {urlPdfPath}{Environment.NewLine}Exception: {ex}");

                // Failed, we do not want to continue
                return new ProcessResult(false, CommonStrings.FailedToDownloadFile);
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
        Task<ProcessResult> DownloadFile(string urlPdfPath, string authorization);
    }
}
