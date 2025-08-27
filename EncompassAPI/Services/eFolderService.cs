using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Repos;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class eFolderService : IeFolderService
    {
        private readonly EncompassConfig _encompassConfig;
        private readonly IHttpService _http;
        private readonly ITokenRepo _tokenRepo;
        private readonly LoggerService<eFolderService> _logger;
        private static readonly Dictionary<string, string> _headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" }
        };

        public eFolderService(IOptionsSnapshot<EncompassConfig> encompassOptions, IHttpService http, ITokenRepo tokenRepo, LoggerService<eFolderService> logger)
        {
            _encompassConfig = encompassOptions.Value;
            _http = http;
            _tokenRepo = tokenRepo;
            _logger = logger;
        }

        public async Task<ProcessResult<IEnumerable<EncompassDocument>>> GetListOfDocuments(string orgId, string loanId)
        {
            try
            {
                // Get Documents from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents?requireActiveAttachments=true";

                IEnumerable<EncompassDocument> ieEncompassDocuments = await _http.GetAsync<EncompassDocument[]>(url, _headers);

                return new ProcessResult<IEnumerable<EncompassDocument>>(true)
                {
                    Result = ieEncompassDocuments
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<IEnumerable<EncompassDocument>>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<Attachment[]>> GetAttachments(string orgId, string loanId, string documentType, IEnumerable<EncompassDocument> documents)
        {
            try
            {
                // Check if documents were retrieved or if there are no documents
                if (documents.Count() == 0)
                {
                    _logger.Base.LogError($"{loanId} in org {orgId} has no documents in eFolder.");
                    return new ProcessResult<Attachment[]>(false, $"{loanId} in org {orgId} has no documents in eFolder.");
                }

                // Filter by eDisclosure document type
                IEnumerable<EncompassDocument> ieDocuments;

                if (string.IsNullOrEmpty(documentType))
                {
                    ieDocuments = documents;
                }
                else
                {
                    ieDocuments = documents.Where(doc => doc.documentTypes.Contains(documentType));

                    // Check if any documents were found with the specified type
                    if (ieDocuments.Count() == 0)
                    {
                        _logger.Base.LogError($"No documents found with type {documentType} in loan {loanId} for org {orgId}.");
                        return new ProcessResult<Attachment[]>(false, $"No documents found with type {documentType} in loan {loanId} for org {orgId}.");
                    }
                }

                // Get all attachments for the documents
                return new ProcessResult<Attachment[]>(true)
                {
                    Result = ieDocuments.SelectMany(doc => doc.attachments).ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve attachments for documents in loan {loanId} for org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<Attachment[]>(false, $"Failed to retrieve attachments for documents in loan {loanId} for org {orgId}.");
            }
        }

        public async Task<ProcessResult<AttachmentUrls>> GetAttachmentDownloadUrls(string orgId, string loanId, Attachment[] attachments)
        {
            try
            {
                // Check if any of the documents have attachments
                if (attachments.Length == 0)
                {
                    _logger.Base.LogError($"No attachments found for documents in loan {loanId} for org {orgId}.");
                    return new ProcessResult<AttachmentUrls>(false, $"No attachments found for documents in loan {loanId} for org {orgId}.");
                }

                // Get the attachment IDs from the attachments
                char[] attachmentIds = attachments.SelectMany(att => att.entityId).ToArray();

                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/attachmentDownloadUrl";

                string requestBody = JsonSerializer.Serialize(new
                {
                    attachments = attachmentIds
                });

                AttachmentUrls attachmentUrls = await _http.PostAsync<AttachmentUrls>(url, _headers, requestBody);

                if(attachmentUrls == null || attachmentUrls.attachments == null || attachmentUrls.attachments.Length == 0)
                {
                    _logger.Base.LogError($"No attachment download URLs found for attachments in loan {loanId} in org {orgId}.");
                    return new ProcessResult<AttachmentUrls>(false, $"No attachment download URLs found for attachments in loan {loanId} in org {orgId}.");
                }

                return new ProcessResult<AttachmentUrls>(true)
                {
                    Result = attachmentUrls
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve download URLs for attachments in loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<AttachmentUrls>(false, $"Failed to retrieve download URLs for attachments in loan {loanId} in org {orgId}.");
            }
        }
    }

    public interface IeFolderService
    {

        /// <summary>
        /// Returns all eFolder documents for a specified loan. 
        /// The list can be filtered for retrieving documents which are not removed, or documents that have active attachments only. todo view full
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="loanId"></param>
        /// <returns></returns>
        Task<ProcessResult<IEnumerable<EncompassDocument>>> GetListOfDocuments(string orgId, string loanId);

        /// <summary>
        /// Filters the documents by document type and retrieves all attachments for those documents.
        /// Document type is optional
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="documentType"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        Task<ProcessResult<Attachment[]>> GetAttachments(string orgId, string loanId, string documentType, IEnumerable<EncompassDocument> documents);

        /// <summary>
        /// Retrieves the download URLs for a given list of attachments.
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="loanId"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        Task<ProcessResult<AttachmentUrls>> GetAttachmentDownloadUrls(string orgId, string loanId, Attachment[] attachments);
    }
}
