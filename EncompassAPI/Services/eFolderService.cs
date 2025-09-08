using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Repos;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class eFolderService : IeFolderService
    {
        private readonly EncompassConfig _encompassConfig;
        private readonly IHttpService _http;
        private readonly LoggerService<eFolderService> _logger;

        public eFolderService(IOptionsSnapshot<EncompassConfig> encompassOptions, IEncompassHttpService http, LoggerService<eFolderService> logger)
        {
            _encompassConfig = encompassOptions.Value;
            _http = http;
            _logger = logger;
        }

        public async Task<ProcessResult<DisclosureTrackingLog>> GetLastestDisclosureTrackingLog(string orgId, string loanId, string token)
        {
            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token }
                };

                IEnumerable<DisclosureTrackingLog> ieDisclosureLogs = await _http.GetAsync<DisclosureTrackingLog[]>(url, headers);

                if (!ieDisclosureLogs.Any())
                {
                    _logger.Base.LogError($"No disclosure tracking logs found for loan {loanId} in org {orgId}.");
                    return new ProcessResult<DisclosureTrackingLog>(false, $"No disclosure tracking logs found for loan {loanId} in org {orgId}.");
                }

                if(!ieDisclosureLogs.Any(log => log.includedInTimeline == true))
                {
                    _logger.Base.LogError($"No disclosure tracking logs found that are included in timeline for loan {loanId} in org {orgId}.");
                    return new ProcessResult<DisclosureTrackingLog>(false, $"No disclosure tracking logs found that are included in timeline for loan {loanId} in org {orgId}.");
                }

                return new ProcessResult<DisclosureTrackingLog>(true)
                {
                    Result = ieDisclosureLogs.First()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DisclosureTrackingLog>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool includeInTimeline, string token)
        {
            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLogId}";

                object payload = new
                {
                    includedInTimeline = includeInTimeline
                };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                HttpResponseMessage response = await _http.PatchAsync(url, headers, JsonSerializer.Serialize(payload));

                if(!response.IsSuccessStatusCode)
                {
                    _logger.Base.LogError($"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                    return new ProcessResult(false, $"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public ProcessResult RemoveDocumentsNotInDisclosureLog(string orgId, string loanId, List<Form2> forms, string disclosureLogId, IEnumerable<EncompassDocument> documents)
        {
            _logger.Base.LogInformation($"Entering {nameof(RemoveDocumentsNotInDisclosureLog)}");

            // First let's make sure all forms in disclosure log are in eFolder documents
            foreach (var form in forms)
            {
                bool found = documents.Any(doc => doc.title == form.formName);

                if (!found)
                {
                    return new ProcessResult(false, $"Form {form.formName} listed in disclosure log {disclosureLogId} for loan {loanId} in org {orgId} was not found in eFolder documents.");
                }
            }

            List<EncompassDocument> neededDocuments = new List<EncompassDocument>();

            // Now let's remove all documents that are not in disclosure log
            foreach (var document in documents)
            {
                bool exists = forms.Any(form => form.formName == document.title);

                if (exists)
                {
                    neededDocuments.Add(document);
                    continue;
                }

                _logger.Base.LogInformation($"Removing {document.title}");
            }

            documents = neededDocuments;

            return new ProcessResult(true);
        }

        public async Task<ProcessResult<IEnumerable<EncompassDocument>>> GetListOfDocuments(string orgId, string loanId, string token)
        {
            try
            {
                // Get Documents from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents?requireActiveAttachments=true";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                IEnumerable <EncompassDocument> ieEncompassDocuments = await _http.GetAsync<EncompassDocument[]>(url, headers);

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

/*                     // Do not allow documents with multiple attachments. For now, we only allow disclosures to be added to e-folder as one attachment
                    if(ieDocuments.Count() > 1)
                    {
                        _logger.Base.LogError($"Multiple documents found with type {documentType} in loan {loanId} for org {orgId}. Only one attachment is allowed. User probably did not add to e-folder correctly. We only allow users to add to e-folder as 1 attachment.");
                        return new ProcessResult<Attachment[]>(false, $"Multiple documents found with type {documentType} in loan {loanId} for org {orgId}. Only one attachment is allowed.");
                    } */
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

        public async Task<ProcessResult<AttachmentUrls>> GetAttachmentDownloadUrls(string orgId, string loanId, Attachment[] attachments, string token)
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
                string[] attachmentIds = attachments.Select(att => att.entityId).ToArray();

                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/attachmentDownloadUrl";

                string requestBody = JsonSerializer.Serialize(new
                {
                    attachments = attachmentIds
                });

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                AttachmentUrls attachmentUrls = await _http.PostAsync<AttachmentUrls>(url, headers, requestBody);

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
        Task<ProcessResult<DisclosureTrackingLog>> GetLastestDisclosureTrackingLog(string orgId, string loanId, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool includeInTimeline, string token);
        ProcessResult RemoveDocumentsNotInDisclosureLog(string orgId, string loanId, List<Form2> forms, string disclosureLogId, IEnumerable<EncompassDocument> documents);
        /// <summary>
        /// Returns all eFolder documents for a specified loan. 
        /// The list can be filtered for retrieving documents which are not removed, or documents that have active attachments only. todo view full
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="loanId"></param>
        /// <returns></returns>
        Task<ProcessResult<IEnumerable<EncompassDocument>>> GetListOfDocuments(string orgId, string loanId, string token);

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
        Task<ProcessResult<AttachmentUrls>> GetAttachmentDownloadUrls(string orgId, string loanId, Attachment[] attachments, string token);
    }
}
