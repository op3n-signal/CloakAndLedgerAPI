using Amazon.Runtime;
using Amazon.Runtime.Documents;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Repos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Shared.Models;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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

        public async Task<ProcessResult<List<EncompassDocument>>> GetListOfDocuments(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(GetListOfDocuments)}");

            try
            {
                // Get Documents from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents?requireActiveAttachments=true";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                IEnumerable<EncompassDocument> ieEncompassDocuments = await _http.GetAsync<EncompassDocument[]>(url, headers);

                ieEncompassDocuments = ieEncompassDocuments.Where(doc => doc.documentTypes.Contains("EDisclosures")).DistinctBy(e => e.title);

                return new ProcessResult<List<EncompassDocument>>(true)
                {
                    Result = ieEncompassDocuments.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<EncompassDocument>>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<Models.Encompass.Attachment[]>> GetAttachments(string orgId, string loanId, List<EncompassDocument> documents)
        {
            _logger.Base.LogInformation($"Entering {nameof(GetAttachments)}");

            try
            {
                // Check if documents were retrieved or if there are no documents
                if (documents.Count() == 0)
                {
                    _logger.Base.LogError($"{loanId} in org {orgId} has no documents in eFolder.");
                    return new ProcessResult<Attachment[]>(false, $"{loanId} in org {orgId} has no documents in eFolder.");
                }

                // Get all attachments for the documents
                return new ProcessResult<Attachment[]>(true)
                {
                    Result = documents.SelectMany(doc => doc.attachments).DistinctBy(att => att.entityName).ToArray()
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
            _logger.Base.LogInformation($"Entering {nameof(GetAttachmentDownloadUrls)}");

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

                _logger.LogVerbose(JsonSerializer.Serialize(attachmentUrls));

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
        
        public async Task<ProcessResult<List<EncompassDocument>>> GetContainers(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation(nameof(GetContainers));

            try
            {
                // Get Document Containers from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token }
                };

                IEnumerable<EncompassDocument> ieEncompassDocuments = await _http.GetAsync<EncompassDocument[]>(url, headers);

                return new ProcessResult<List<EncompassDocument>>(true)
                {
                    Result = ieEncompassDocuments.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<EncompassDocument>>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<EncompassDocument>>> CreateContainers(string orgId, string loanId, List<Container> containers, string token)
        {
            _logger.Base.LogInformation(nameof(CreateContainers));

            try
            {
                // Create Document Containers in Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents?action=add&view=entity";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                IEnumerable<EncompassDocument> ieEncompassDocuments = await _http.PatchAsync<EncompassDocument[]>(url, headers, JsonSerializer.Serialize(containers));

                return new ProcessResult<List<EncompassDocument>>(true)
                {
                    Result = ieEncompassDocuments.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to create documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<EncompassDocument>>(false, $"Failed to create documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateContainers(string orgId, string loanId, List<string> containerIds, string token)
        {
            _logger.Base.LogInformation(nameof(UpdateContainers));

            try
            {
                // Create Document Containers in Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents?action=update&view=entity";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                List<object> payload = new List<object>();

                foreach(var id in containerIds)
                {
                    payload.Add( new
                    {
                        id = id,
                        isMarkedRemoved = false
                    });
                }

                IEnumerable<EncompassDocument> ieEncompassDocuments = await _http.PatchAsync<EncompassDocument[]>(url, headers, JsonSerializer.Serialize(payload));

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to create documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to create documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<UploadUrl>> CreateAttachmentUrl(string orgId, string loanId, string docName, string token)
        {
            try
            {
                // Create Document Containers in Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/attachmentUploadUrl";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                object payload = new
                {
                    file = new
                    {
                        contentType = "application/pdf",
                        name = docName + ".pdf",
                        size = 20000
                    },
                    title = docName
                };

                UploadUrl response = await _http.PostAsync<UploadUrl>(url, headers, JsonSerializer.Serialize(payload));

                if(response == null)
                {
                    _logger.Base.LogError($"No upload URL returned for document {docName} in loan {loanId} in org {orgId}.");
                    return new ProcessResult<UploadUrl>(false, $"No upload URL returned for document {docName} in loan {loanId} in org {orgId}.");
                }

                return new ProcessResult<UploadUrl>(true)
                {
                    Result = response
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to create documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<UploadUrl>(false, ResultStatus.InternalError, $"Failed to create documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UploadToMediaUrl(string orgId, string loanId, UploadUrl uploadUrl, byte[] fileData, string token)
        {
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", uploadUrl.authorizationHeader },
                };

                await _http.PutAsync(uploadUrl.uploadUrl, headers, fileData);

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to create documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, ResultStatus.InternalError, $"Failed to create documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> AssignAttachmentToContainer(string orgId, string loanId, string attachmentId, string docId, string token)
        {
            try
            {
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/documents/{docId}/attachments?action=add";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                string payload = "[{\"entityId\": \"" + attachmentId + "\", \"entityType\": \"attachment\"}]";

                bool response = await _http.PatchAsyncV2(url, headers, payload);

                return new ProcessResult(response);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to assign attachment for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, ResultStatus.InternalError, $"Failed to assign attachment for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> Upload(CandidAuditRequest request, Dictionary<string, byte[]> pdfObjects, string token)
        {
            _logger.Base.LogInformation(nameof(Upload));

            try
            {
                // Get encompass containers
                ProcessResult<List<EncompassDocument>> prDocuments = await GetContainers(request.orgId, request.loanId, token);

                if (!prDocuments.Success)
                {
                    return new ProcessResult(false, prDocuments.Message);
                }

                List<EncompassDocument> encompassDocuments = new List<EncompassDocument>();
                List<Container> containersToCreate = new List<Container>();

                // Check if there is a container we need to create
                foreach (string docName in pdfObjects.Keys)
                {
                    // Does any container match this document name?
                    bool blMatch = prDocuments.Result.Any(doc => doc.title == docName);

                    if (blMatch)
                    {
                        // Add document to list where containers already exist
                        EncompassDocument? documentFound = prDocuments.Result.Find(doc => doc.title == docName);

                        if(documentFound == null) 
                            return new ProcessResult(false, $"Failed to find document {docName} in loan {request.loanId} in org {request.orgId} after confirming it exists.");

                        encompassDocuments.Add(documentFound);
                    }
                    else
                    {
                        // Add doc name to list to be created
                        Container container = new Container()
                        {
                            title = docName,
                            description = CommonFields.DocumentCreateBy,
                            documentTypes = new List<string>() { "EDisclosures" }
                        };
                        containersToCreate.Add(container);
                    }
                }

                if (containersToCreate.Count > 0)
                {
                    // Create containers
                    ProcessResult<List<EncompassDocument>> prCreatedDocuments = await CreateContainers(request.orgId, request.loanId, containersToCreate, token);

                    if (!prCreatedDocuments.Success)
                    {
                        return new ProcessResult(false, prCreatedDocuments.Message);
                    }

                    _logger.Base.LogInformation($"Created {prCreatedDocuments.Result.Count} new Containers");

                    // Add newly created containers to list
                    encompassDocuments.AddRange(prCreatedDocuments.Result);
                }
                
                _logger.Base.LogInformation($"{encompassDocuments.Count} total Containers");
                _logger.Base.LogInformation($"Uploading {pdfObjects.Count} attachments for loan {request.loanId} in org {request.orgId}.");

                // Loop through each pdf object
                foreach (KeyValuePair<string, byte[]> kvp in pdfObjects)
                {
                    string fileName = kvp.Key;
                    byte[] fileData = kvp.Value;

                    // Upload Attachment info
                    ProcessResult<UploadUrl> prUploadUrl = await CreateAttachmentUrl(request.orgId, request.loanId, fileName, token);

                    if (!prUploadUrl.Success)
                    {
                        return new ProcessResult(false, prUploadUrl.Message);
                    }

                    string attachmentId = prUploadUrl.Result.attachmentId;

                    // Upload attachment data
                    ProcessResult prUpload = await UploadToMediaUrl(request.orgId, request.loanId, prUploadUrl.Result, fileData, token);

                    if (!prUpload.Success)
                    {
                        return new ProcessResult(false, prUpload.Message);
                    }

                    EncompassDocument document = encompassDocuments.Find(doc => doc.title == fileName);

                    // Assign attachment to efolder
                    //https://developer.icemortgagetechnology.com/developer-connect/reference/assign-document-attachments
                    ProcessResult prAssigned = await AssignAttachmentToContainer(request.orgId, request.loanId, attachmentId, document.id, token);

                    if (!prAssigned.Success)
                    {
                        return new ProcessResult(false, prAssigned.Message);
                    }
                }

                _logger.Base.LogInformation($"Successfully uploaded attachments for loan {request.loanId} in org {request.orgId}.");

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to upload documents for loan {request.loanId} in org {request.orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, ResultStatus.InternalError, $"Failed to upload documents for loan {request.loanId} in org {request.orgId}.");
            }
        }

        public struct Container
        {
            public string title { get; set; }
            public string description { get; set; }
            public List<string> documentTypes { get; set; }
        }
    }

    public interface IeFolderService
    {
        ProcessResult RemoveDocumentsNotInDisclosureLog(string orgId, string loanId, List<Form2> forms, string disclosureLogId, IEnumerable<EncompassDocument> documents);
        /// <summary>
        /// Returns all eFolder documents for a specified loan. 
        /// The list can be filtered for retrieving documents which are not removed, or documents that have active attachments only. todo view full
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="loanId"></param>
        /// <returns></returns>
        Task<ProcessResult<List<EncompassDocument>>> GetListOfDocuments(string orgId, string loanId, string token);

        /// <summary>
        /// Filters the documents by document type and retrieves all attachments for those documents.
        /// Document type is optional
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="documentType"></param>
        /// <param name="documents"></param>
        /// <returns></returns>
        Task<ProcessResult<Attachment[]>> GetAttachments(string orgId, string loanId, List<EncompassDocument> documents);

        /// <summary>
        /// Retrieves the download URLs for a given list of attachments.
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="loanId"></param>
        /// <param name="attachments"></param>
        /// <returns></returns>
        Task<ProcessResult<AttachmentUrls>> GetAttachmentDownloadUrls(string orgId, string loanId, Attachment[] attachments, string token);
        Task<ProcessResult<List<EncompassDocument>>> GetContainers(string orgId, string loanId, string token);
        Task<ProcessResult> Upload(CandidAuditRequest request, Dictionary<string, byte[]> pdfObjects, string token);
        Task<ProcessResult> UpdateContainers(string orgId, string loanId, List<string> containerIds, string token);
    }
}
