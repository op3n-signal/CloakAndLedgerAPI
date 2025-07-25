using EncompassAPI.Models.Configs;
using Microsoft.Extensions.Options;

namespace EncompassAPI.Services
{
    public class eFolderService : IeFolderService
    {
        private readonly EncompassConfig _encompassConfig;
        private readonly IHttpService _http;

        public eFolderService(IOptionsSnapshot<EncompassConfig> encompassOptions, IHttpService http)
        {
            _encompassConfig = encompassOptions.Value;
            _http = http;
        }

        public async Task GetListOfDocuments(string loanId)
        {
            // Get Documents from Encompass
            string url = $"{_encompassConfig.LoanEndpointV3}/{loanId}/documents?view=full";

            // Filter by eDisclosure document type

            throw new NotImplementedException();
        }

        public async Task GetAttachmentDownloadUrl(string loanId, string[] attachmentIds)
        {
            throw new NotImplementedException();
        }
    }

    public interface IeFolderService
    {

        /// <summary>
        /// Returns all eFolder documents for a specified loan. 
        /// The list can be filtered for retrieving documents which are not removed, or documents that have active attachments only. todo view full
        /// </summary>
        /// <param name="loanId"></param>
        /// <returns></returns>
        Task GetListOfDocuments(string loanId);

        /// <summary>
        /// Retrieves the download URLs for a given list of attachments.
        /// </summary>
        /// <param name="loanId"></param>
        /// <param name="attachmentIds"></param>
        /// <returns></returns>
        Task GetAttachmentDownloadUrl(string loanId, string[] attachmentIds);
    }
}
