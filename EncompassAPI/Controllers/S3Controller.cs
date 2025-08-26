using EncompassAPI.AppLayer;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Utility.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TokenGenerator;

namespace EncompassAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = Security.Policy)]
    [AuthRequired]
    public class S3Controller : ControllerBase
    {
        private readonly StorageService _storageService;

        public S3Controller(StorageService storageService)
        {
            _storageService = storageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] AttachmentUploadRequest attachmentUploadRequest)
        {
            await _storageService.UploadFilesToS3(attachmentUploadRequest);

            return Accepted();
        }
    }
}
