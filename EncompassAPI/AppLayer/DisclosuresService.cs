using EncompassAPI.Models;
using EncompassAPI.Services;

namespace EncompassAPI.AppLayer
{
    public class DisclosuresService : IDisclosuresService
    {
        private readonly IFileService _fileService;

        public DisclosuresService(IFileService fileService)
        {
            _fileService = fileService;
        }

        public async Task Run()
        {
            ProcessResult pr = await _fileService.DownloadFile("https://streaming.us-east-1.skydrive.ellieservices.com/v1/clients/3011350238/B2.ac487dd1-0e45-4830-955a-14928e701d02?validity=MTc1MzQ1MzUxODEwMQ%3D%3D&sub=dXJuOmVsbGk6ZW5jb21wYXNzOmJlMTEzNTAyMzg6dXNlcjpzZmludGVncmF0aW9u&instanceId=dXJuOmVsbGk6ZW5jb21wYXNzOkJFMTEzNTAyMzg%3D&entityId=OTNkN2M3ZDItN2E3Ni00NmFhLTgxNjgtZjFjOGU2Mzk2NTdl", "elli-signature 5F0C6F94600BFDDF5ADA3B1E9591F644966173A5E4455CD1E41436FEE73ED6986FCBFC6082980C08D8E6B62BAB7A85194C9D4213C67B5D98BCA09D5C75CBB98F"); //todo test with hardcoded url and authorization
        }
    }

    public interface IDisclosuresService
    {
        Task Run();
    }
}
