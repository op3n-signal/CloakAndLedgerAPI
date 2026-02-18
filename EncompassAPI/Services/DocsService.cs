using Amazon.CloudWatchLogs;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Transform;
using Amazon.S3.Model;
using AutoMapper;
using EncompassAPI.AppLayer;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Encompass.LoanInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Shared.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DocsService : IEncompassService
    {
        private readonly IMapper _mapper;
        private readonly LoggerService<DocsService> _logger;
        private readonly EncompassConfig _encompassConfig;
        private readonly IHttpService _http;
        private readonly FileConfig _fileConfig;

        public DocsService(IMapper mapper, LoggerService<DocsService> logger, IOptionsSnapshot<EncompassConfig> encompassOptions, IEncompassHttpService http, IOptionsSnapshot<FileConfig> fileOptions)
        {
            _mapper = mapper;
            _logger = logger;
            _encompassConfig = encompassOptions.Value;
            _http = http;
            _fileConfig = fileOptions.Value;
        }

        public async Task<ProcessResult<DisclosureStage>> GetDisclosureStage(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation(nameof(GetDisclosureStage));

            try
            {
                // Get field values
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV1}/{loanId}/fieldReader";

                string[] payload = new string[] { "3152", "3154", "3977", "3979" };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                FieldReaderResponse[] response = await _http.PostAsync<FieldReaderResponse[]>(url, headers, JsonSerializer.Serialize(payload));

                if (response == null || response.Length == 0)
                {
                    _logger.Base.LogError("No field values found");
                    return new ProcessResult<DisclosureStage>(false, "No field values found");
                }

                IEnumerable<FieldReaderResponse> fieldReaderResponse = response.OrderByDescending(x => x.fieldId);

                _logger.LogVerbose("Field reader response: " + JsonSerializer.Serialize(fieldReaderResponse));

                foreach (FieldReaderResponse fieldReader in fieldReaderResponse)
                {
                    if(string.IsNullOrEmpty(fieldReader.value)) 
                    { 
                        continue;
                    }

                    switch (fieldReader.fieldId)
                    {
                        case "3979":
                            return new ProcessResult<DisclosureStage>(true) { Result = DisclosureStage.RevisedInitialClosingDisclosures };
                        case "3977":
                            return new ProcessResult<DisclosureStage>(true) { Result = DisclosureStage.InitialClosingDisclosures };
                        case "3154":
                            return new ProcessResult<DisclosureStage>(true) { Result = DisclosureStage.RevisedInitialDisclosures };
                        case "3152":
                            return new ProcessResult<DisclosureStage>(true) { Result = DisclosureStage.InitialDisclosures };
                        default:
                            break;
                    }
                }

                return new ProcessResult<DisclosureStage>(false, "No matching field id found");
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve loan audit for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DisclosureStage>(false, $"Failed to retrieve loan audit for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> GetResourceLock(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation(nameof(GetResourceLock));
            try
            {
                // Get resource locks
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.ResourceLockEndpoint}?resourceType=loan&resourceId={loanId}";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                ProcessId[] lockIds = await _http.GetAsync<ProcessId[]>(url, headers);

                if (lockIds == null || lockIds.Length == 0)
                {
                    _logger.Base.LogError($"No resource locks found for loan: {loanId} in org: {orgId}");
                    return new ProcessResult<string>(false, $"No resource locks found for loan: {loanId} in org: {orgId}");
                }
                else
                {
                    _logger.Base.LogInformation($"Loan is locked for loan: {loanId} in org: {orgId}");
                }

                return new ProcessResult<string>(true)
                {
                    Result = lockIds[0].id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to resource locks for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to retrieve resource locks for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> RemoveResourceLock(string orgId, string loanId, string lockId, string token)
        {
            try
            {
                // Remove resource lock since user is in the loan file
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.ResourceLockEndpoint}/{lockId}?resourceType=loan&resourceId={loanId}&force=true";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                bool blDeleted = await _http.DeleteAsync(url, headers);

                return new ProcessResult(blDeleted);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to remove resource lock for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to remove resource lock for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<LoanInfo>> GetLoanInfo(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation(nameof(GetLoanInfo));

            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                LoanInfo loanInfo = await _http.GetAsync<LoanInfo>(url, headers);

                if (loanInfo == null || loanInfo.applications == null || loanInfo.applications.Length == 0)
                {
                    _logger.Base.LogError("Application id not found");
                    return new ProcessResult<LoanInfo>(false, "Application id not found");
                }

                return new ProcessResult<LoanInfo>(true)
                {
                    Result = loanInfo
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve application id for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<LoanInfo>(false, $"Failed to retrieve application id for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> CreateLoanAuditForOrder(string orgId, string loanId, string applicationId, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation(nameof(CreateLoanAuditForOrder));
            try
            {
                // Run loan audit
                string url = _encompassConfig.BaseUrl;

                if (stage.ToString().ToLower().Contains("closing")) 
                {
                    url += _encompassConfig.ClosingAuditEndpoint;
                }
                else
                {
                    url += _encompassConfig.OpeningAuditEndpoint;
                }

                AuditRequest auditRequest = new AuditRequest()
                {
                    entity = new Entity()
                    {
                        entityid = loanId,
                        entitytype = CommonFields.LoanEntityType
                    },
                    scope = new Entity()
                    {
                        entityid = applicationId,
                        entitytype = CommonFields.BorrowerEntityType
                    }
                };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                ProcessId process = await _http.PostAsync<ProcessId>(url, headers, JsonSerializer.Serialize(auditRequest));

                return new ProcessResult<string>(true)
                {
                    Result = process.id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve loan audit for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to retrieve loan audit for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> GetDocumentAuditForOrder(string orgId, string loanId, string auditId, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation(nameof(GetDocumentAuditForOrder));

            try
            {
                // Get loan audit result
                string url = _encompassConfig.BaseUrl;

                if (stage.ToString().ToLower().Contains("closing"))
                {
                    url += $"{_encompassConfig.ClosingAuditEndpoint}/{auditId}";
                }
                else
                {
                    url += $"{_encompassConfig.OpeningAuditEndpoint}/{auditId}";
                }

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                AuditResult auditResult = await _http.GetAsync<AuditResult>(url, headers, maxRetries: 5, delayMilliseconds: 10000);

                if (auditResult.status.ToLower() != "succeeded")
                {
                    _logger.Base.LogError("The audit has errors");
                    return new ProcessResult(false, "The audit has errors");
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve loan audit result for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to retrieve loan audit result for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> GenerateDocSet(string orgId, string loanId, string auditId, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation(nameof(GenerateDocSet));

            try
            {
                // Run loan audit
                string url = _encompassConfig.BaseUrl;

                if (stage.ToString().ToLower().Contains("closing"))
                {
                    url += _encompassConfig.ClosingDocSetEndpoint;
                }
                else
                {
                    url += _encompassConfig.OpeningDocSetEndpoint;
                }

                DocSetRequest docSetRequest = new DocSetRequest()
                {
                    auditId = auditId,
                    printMode = CommonFields.PrintMode
                };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                ProcessId process = await _http.PostAsync<ProcessId>(url, headers, JsonSerializer.Serialize(docSetRequest));

                return new ProcessResult<string>(true)
                {
                    Result = process.id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to generate doc set for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to generate doc set for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<Document>>> GetOrderStatus(string orgId, string loanId, string orderId, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation(nameof(GetOrderStatus));

            try
            {
                // Get loan audit result
                string url = _encompassConfig.BaseUrl;

                if (stage.ToString().ToLower().Contains("closing"))
                {
                    url += $"{_encompassConfig.ClosingDocSetEndpoint}/{orderId}";
                }
                else
                {
                    url += $"{_encompassConfig.OpeningDocSetEndpoint}/{orderId}";
                }

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                DocumentOrders documentOrders = await _http.GetAsync<DocumentOrders>(url, headers, maxRetries: 5, delayMilliseconds: 10000);

                if (documentOrders.status.ToLower() != "succeeded")
                {
                    _logger.Base.LogError("The document orders response has errors"); //todo
                    return new ProcessResult<List<Document>>(false, "The document orders response has errors");
                }

                return new ProcessResult<List<Document>>(true)
                {
                    Result = documentOrders.documents
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document orders for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<Document>>(false, $"Failed to retrieve document orders for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<string>>> GetEFolderDocuments(string orgId, string loanId, Dictionary<string, bool> dictAttachmentNamesToFind, string token)
        {
            _logger.Base.LogInformation(nameof(GetEFolderDocuments));
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.GetEFolderDocsEndpoint}";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                EFolderDocument[] efolderDocuments = await _http.GetAsync<EFolderDocument[]>(url, headers);

                if (efolderDocuments == null || efolderDocuments.Length == 0)
                {
                    _logger.Base.LogError("No docs found"); //todo
                    return new ProcessResult<List<string>>(false, "No docs found");
                }

                List<string> efolderDocumentNames = new List<string>();

                foreach (string name in efolderDocuments.Select(doc => doc.name))
                {
                    bool exists = dictAttachmentNamesToFind.ContainsKey(name);

                    if(exists && dictAttachmentNamesToFind[name] == false)
                    {
                        efolderDocumentNames.Add(name);
                    }
                }

                return new ProcessResult<List<string>>(true)
                {
                    Result = efolderDocumentNames
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document from e-folder for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<string>>(false, $"Failed to retrieve document from e-folder for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<string>>> GetAllEFolderDocuments(string orgId, string loanId, string token)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.GetEFolderDocsEndpoint}";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                EFolderDocument[] efolderDocuments = await _http.GetAsync<EFolderDocument[]>(url, headers);

                if (efolderDocuments == null || efolderDocuments.Length == 0)
                {
                    _logger.Base.LogError("No docs found");
                    return new ProcessResult<List<string>>(false, "No docs found");
                }

                return new ProcessResult<List<string>>(true)
                {
                    Result = efolderDocuments.Where(doc => doc.signatureType == "ESignable").Select(doc => doc.name).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document from e-folder for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<string>>(false, $"Failed to retrieve document from e-folder for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> AddToDocSet(string orgId, string loanId, string orderId, string applicationId, List<string> templateNames, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation(nameof(AddToDocSet));

            try
            {
                // Run loan audit
                string url = _encompassConfig.BaseUrl;

                if (stage.ToString().ToLower().Contains("closing"))
                {
                    url += $"{_encompassConfig.ClosingDocSetEndpoint}/{orderId}/documents";
                }
                else
                {
                    url += $"{_encompassConfig.OpeningDocSetEndpoint}/{orderId}/documents";
                }

                AddToDocSetRequest addToDocSetRequest = new AddToDocSetRequest()
                {
                    documentType = "forms",
                    forms = new Forms()
                    {
                        scope = new Entity()
                        {
                            entityid = applicationId,
                            entitytype = CommonFields.BorrowerEntityType
                        },
                        templates = templateNames,
                        printMode = CommonFields.PrintMode
                    }
                };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                ProcessId process = await _http.PostAsync<ProcessId>(url, headers, JsonSerializer.Serialize(addToDocSetRequest));

                return new ProcessResult<string>(true)
                {
                    Result = process.id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to add to doc set for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to add to doc set for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<Document>>> GetDocSet(string orgId, string loanId, string orderId, string jobId, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation(nameof(GetDocSet));

            try
            {
                // Get loan audit result
                string url = _encompassConfig.BaseUrl;

                if (stage.ToString().ToLower().Contains("closing"))
                {
                    url += $"{_encompassConfig.ClosingDocSetEndpoint}/{orderId}/documents/{jobId}";
                }
                else
                {
                    url += $"{_encompassConfig.OpeningDocSetEndpoint}/{orderId}/documents/{jobId}";
                }

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                DocumentOrders documentOrders = await _http.GetAsync<DocumentOrders>(url, headers, maxRetries: 5, delayMilliseconds: 10000);

                if (documentOrders.status.ToLower() != "succeeded")
                {
                    _logger.Base.LogError("The document orders response has errors"); //todo
                    return new ProcessResult<List<Document>>(false, "The document orders response has errors");
                }

                _logger.LogVerbose($"Document orders retrieved: {JsonSerializer.Serialize(documentOrders.documents)}");

                return new ProcessResult<List<Document>>(true)
                {
                    Result = documentOrders.documents
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document orders for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<List<Document>>(false, $"Failed to retrieve document orders for loan {loanId} in org {orgId}.");
            }
        }

        // attachmentNamesToFind is the attachment names we retrieved when disclosures were ordered
        // documents are the docs we got from the doc set api that hold the signature coordinates
        public ProcessResult CheckIfAllDocumentsExist(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents)
        {
            _logger.Base.LogInformation(nameof(CheckIfAllDocumentsExist));

            foreach (var item in dictAttachmentNamesToFind)
            {
                bool exists = documents.Any(doc => doc.name == item.Key);

                if (!exists)
                {
                    _logger.Base.LogInformation($"Document {item.Key} not found in the document set.");
                    continue;
                }

                dictAttachmentNamesToFind[item.Key] = true; // Mark as found
            }

            bool allFound = dictAttachmentNamesToFind.Any(kv => kv.Value == false) == false;

            return new ProcessResult(allFound);
        }

        public List<Document> RemoveUnneededDocuments(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents)
        {
            _logger.Base.LogInformation(nameof(RemoveUnneededDocuments));

            List<Document> neededDocuments = new List<Document>();

            foreach (var document in documents)
            {
                //Let's not remove wet sign only documents for now
                /* if (document.signatureType == "Wet Sign Only")
                 {
                     _logger.Base.LogInformation($"Removing {document.name} since it is wet sign only");
                     continue;
                 }*/

                bool exists = dictAttachmentNamesToFind.ContainsKey(document.name);

                if (exists)
                {
                    neededDocuments.Add(document);
                    continue;
                }

                _logger.Base.LogInformation($"Removing {document.name}");
            }

            return neededDocuments;
        }

        public List<EncompassDocument> RemoveUnneededDocuments(Dictionary<string, bool> dictAttachmentNamesToFind, List<EncompassDocument> documents)
        {
            _logger.Base.LogInformation($"Entering {nameof(RemoveUnneededDocuments)}");

            List<EncompassDocument> neededDocuments = new List<EncompassDocument>();

            IOrderedEnumerable<EncompassDocument> orderedDocs = documents.OrderByDescending(x => x.createdDate);

            _logger.LogVerbose(JsonSerializer.Serialize(orderedDocs));

            foreach (var document in orderedDocs)
            {
                //Let's not remove wet sign only documents for now
                /* if (document.signatureType == "Wet Sign Only")
                 {
                     _logger.Base.LogInformation($"Removing {document.name} since it is wet sign only");
                     continue;
                 }*/

                bool exists = dictAttachmentNamesToFind.ContainsKey(document.title);

                if (exists)
                {
                    neededDocuments.Add(document);
                    continue;
                }

                _logger.Base.LogInformation($"Removing {document.title}");
            }

            return neededDocuments;
        }

        public ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents, Dictionary<string, int> filePageCounts)
        {
            try
            {
                const string documentId = "3";
                DraftEditRequestV4 draftEditRequest = _mapper.Map<DraftEditRequestV4>(request);
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;
                int totalPageCount = filePageCounts.Last().Value;
                List<string> keys = filePageCounts.Keys.ToList();

                _logger.LogVerbose("Page counts: ");
                _logger.LogVerbose(JsonSerializer.Serialize(filePageCounts));

                foreach (Document doc in documents ?? new List<Document>())
                {
                    _logger.LogVerbose($"Mapping document: {doc.name}");

                    // Get current item in page count dictionary
                    KeyValuePair<string, int> currentItem = filePageCounts.First(item => item.Key.Replace(".pdf", "") == doc.name.Replace("/", "%2F"));
                    int indexOfCurrentItem = keys.ToList().IndexOf(currentItem.Key);
                    // This page number is what page the current document starts from
                    int beginningPageNumber = 0;

                    if(indexOfCurrentItem == 0)
                    {
                        beginningPageNumber = 0;
                    }
                    else 
                    {
                        beginningPageNumber = filePageCounts.ElementAt(indexOfCurrentItem - 1).Value;
                    }

                    _logger.LogVerbose($"Beginning page number for {doc.name} is {beginningPageNumber}");

                    string tabLabelSignature1 = "";
                    string tabLabelSignature2 = "";
                    bool alreadySignedFirstHalf = false;

                    if (doc.name == "Loan Estimate")
                    {
                        tabLabelSignature1 = "Applicant Signature";
                        tabLabelSignature2 = "Co-Applicant Signature";
                    }

                    foreach (Models.Encompass.Signer signer in doc.signers ?? Array.Empty<Models.Encompass.Signer>())
                    {
                        _logger.LogVerbose($"Mapping signer: {signer.partyId}");

                        if (signer.partyId.ToLower().Contains("originator"))
                        {
                            // Skip originator
                            _logger.LogVerbose("Skipping originator");
                            continue;
                        }

                        string signerName = signer.partyId.Replace("Borrower-", "");

                        // Find recipient by the partyId
                        Recipient recipient = draftEditRequest.Recipients.Find(rec => rec.RecipientName.ToLower() == signerName.ToLower());

                        // There is a bug with the doc set api where they have the borrower and co-borrower credit disclosure forms mixed up
                        if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == false)
                        {
                            recipient = draftEditRequest.Recipients[1];
                        }
                        else if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == true)
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }
                        else if(doc.name == "Credit Score Disclosure, 2")
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }

                        if (recipient == null)
                        {
                            //todo cannot find recipient in request
                            _logger.Base.LogError($"Cannot find recipient {signerName} in the request.");
                            return new ProcessResult<DraftEditRequestV4>(false, $"Cannot find recipient {signerName} in the request.");
                        }

                        // Initialize tabs if null
                        if (recipient.Tabs == null)
                        {
                            recipient.Tabs = new Tabs()
                            {
                                SignHereTabs = new List<SignHere>(),
                                InitialHereTabs = new List<InitialHere>(),
                                DateSignedTabs = new List<DateSigned>()
                            };
                        }

                        _logger.LogVerbose($"Mapping fields for signer: {JsonSerializer.Serialize(signer.fields)}");

                        // Map fields into recipient tabs
                        foreach (Field field in signer.fields ?? Array.Empty<Field>())
                        {
                            switch (field.type.ToLower())
                            {
                                case "signature":
                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "initials":
                                    recipient.Tabs.InitialHereTabs.Add(new InitialHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "signeddate":
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;
                                case "signanddate":

                                    float width = field.right - field.left;
                                    float leftPosition = field.left;
                                    float leftDatePosition = field.right - 55;
                                    float topPosition = field.top;
                                    float bottomDatePosition = field.bottom - 15;

                                    if (doc.name == "Loan Estimate")
                                    {
                                        width = width / 2;

                                        leftPosition = alreadySignedFirstHalf ? field.left - width : field.right + width;
                                    }
                                    else if(doc.name == "Disclosures - Reconsideration of Value") // form is not formatted correctly on customers end // width not set correctly
                                    {
                                        leftDatePosition = field.left;
                                        bottomDatePosition = field.bottom + 40;
                                    }
                                    else if (doc.name == "Disclosures - Right to Receive a Copy of Appraisal Report")
                                    {
                                        leftDatePosition = field.left;
                                        bottomDatePosition = field.bottom + 40;
                                    }
                                    else if (doc.name == "Disclosures - Reconsideration of Value")
                                    {
                                        leftDatePosition = field.left;
                                        bottomDatePosition = field.bottom + 40;
                                    }
                                    else if (doc.name == "Disclosures - Reconsideration of Value")
                                    {
                                        leftDatePosition = field.left;
                                        bottomDatePosition = field.bottom + 40;
                                    }

                                    _logger.LogVerbose($"Current Field Map SignAndDate: {JsonSerializer.Serialize(field)}");

                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        TabLabel = alreadySignedFirstHalf == false ? tabLabelSignature1 : tabLabelSignature2,
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(leftDatePosition)).ToString(),
                                        YPosition = ((int)Math.Round(bottomDatePosition)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });

                                    alreadySignedFirstHalf = true;
                                    break;
                                default:
                                    // handle other types if needed
                                    break;
                            }
                        }
                    }
                }

                return new ProcessResult<DraftEditRequestV4>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequestV4>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents)
        {
            try
            {
                int documentId = 1;
                DraftEditRequestV4 draftEditRequest = _mapper.Map<DraftEditRequestV4>(request);
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;

                foreach (Document doc in documents ?? new List<Document>())
                {
                    _logger.LogVerbose($"Mapping document: {doc.name}");

                    string tabLabelSignature1 = "";
                    string tabLabelSignature2 = "";
                    bool alreadySignedFirstHalf = false;

                    if (doc.name == "Loan Estimate")
                    {
                        tabLabelSignature1 = "Applicant Signature";
                        tabLabelSignature2 = "Co-Applicant Signature";
                    }

                    foreach (Models.Encompass.Signer signer in doc.signers ?? Array.Empty<Models.Encompass.Signer>())
                    {
                        _logger.LogVerbose($"Mapping signer: {signer.partyId}");

                        if (signer.partyId.ToLower().Contains("originator"))
                        {
                            // Skip originator
                            _logger.LogVerbose("Skipping originator");
                            continue;
                        }

                        string signerName = signer.partyId.Replace("Borrower-", "");

                        // Find recipient by the partyId
                        Recipient recipient = draftEditRequest.Recipients.Find(rec => rec.RecipientName.ToLower() == signerName.ToLower());

                        // There is a bug with the doc set api where they have the borrower and co-borrower credit disclosure forms mixed up
                        if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == false)
                        {
                            recipient = draftEditRequest.Recipients[1];
                        }
                        else if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == true)
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }
                        else if (doc.name == "Credit Score Disclosure, 2")
                        {
                            {
                                recipient = draftEditRequest.Recipients[0];
                            }

                            if (recipient == null)
                            {
                                //todo cannot find recipient in request
                                _logger.Base.LogError($"Cannot find recipient {signerName} in the request.");
                                return new ProcessResult<DraftEditRequestV4>(false, $"Cannot find recipient {signerName} in the request.");
                            }

                            // Initialize tabs if null
                            if (recipient.Tabs == null)
                            {
                                recipient.Tabs = new Tabs()
                                {
                                    SignHereTabs = new List<SignHere>(),
                                    InitialHereTabs = new List<InitialHere>(),
                                    DateSignedTabs = new List<DateSigned>()
                                };
                            }

                            _logger.LogVerbose($"Mapping fields for signer: {JsonSerializer.Serialize(signer.fields)}");

                            // Map fields into recipient tabs
                            foreach (Field field in signer.fields ?? Array.Empty<Field>())
                            {
                                switch (field.type.ToLower())
                                {
                                    case "signature":
                                        recipient.Tabs.SignHereTabs.Add(new SignHere
                                        {
                                            DocumentId = documentId.ToString(),
                                            PageNumber = (field.page).ToString(),
                                            RecipientId = recipient.RecipientId.ToString(),
                                            XPosition = ((int)Math.Round(field.left)).ToString(),
                                            YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                            ScaleValue = field.scale.ToString(),
                                            Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                            Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                        });
                                        break;

                                    case "initials":
                                        recipient.Tabs.InitialHereTabs.Add(new InitialHere
                                        {
                                            DocumentId = documentId.ToString(),
                                            PageNumber = (field.page).ToString(),
                                            RecipientId = recipient.RecipientId.ToString(),
                                            XPosition = ((int)Math.Round(field.left)).ToString(),
                                            YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                            ScaleValue = field.scale.ToString(),
                                            Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                            Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                        });
                                        break;

                                    case "signeddate":
                                        recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                        {
                                            DocumentId = documentId.ToString(),
                                            PageNumber = (field.page).ToString(),
                                            RecipientId = recipient.RecipientId.ToString(),
                                            XPosition = ((int)Math.Round(field.left)).ToString(),
                                            YPosition = ((int)Math.Round(field.top)).ToString(),
                                            ScaleValue = field.scale.ToString(),
                                            Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                            Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                        });
                                        break;
                                    case "signanddate":

                                        float width = field.right - field.left;
                                        float leftPosition = field.left;
                                        float leftDatePosition = field.right - 55;
                                        float topPosition = field.top;
                                        float bottomDatePosition = field.bottom - 15;

                                        if (doc.name == "Loan Estimate")
                                        {
                                            width = width / 2;

                                            leftPosition = alreadySignedFirstHalf ? field.left - width : field.right + width;
                                        }
                                        else if (doc.name == "Disclosures - Reconsideration of Value") // form is not formatted correctly on customers end // width not set correctly
                                        {
                                            leftDatePosition = field.left;
                                            bottomDatePosition = field.bottom + 40;
                                        }
                                        else if (doc.name == "Disclosures - Right to Receive a Copy of Appraisal Report")
                                        {
                                            leftDatePosition = field.left;
                                            bottomDatePosition = field.bottom + 40;
                                        }
                                        else if (doc.name == "Disclosures - Reconsideration of Value")
                                        {
                                            leftDatePosition = field.left;
                                            bottomDatePosition = field.bottom + 40;
                                        }
                                        else if (doc.name == "Disclosures - Reconsideration of Value")
                                        {
                                            leftDatePosition = field.left;
                                            bottomDatePosition = field.bottom + 40;
                                        }

                                        _logger.LogVerbose($"Current Field Map SignAndDate: {JsonSerializer.Serialize(field)}");

                                        recipient.Tabs.SignHereTabs.Add(new SignHere
                                        {
                                            DocumentId = documentId.ToString(),
                                            PageNumber = (field.page).ToString(),
                                            RecipientId = recipient.RecipientId.ToString(),
                                            TabLabel = alreadySignedFirstHalf == false ? tabLabelSignature1 : tabLabelSignature2,
                                            XPosition = ((int)Math.Round(field.left)).ToString(),
                                            YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                            ScaleValue = field.scale.ToString(),
                                            Width = ((int)Math.Round(width)).ToString(),
                                            Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                        });
                                        recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                        {
                                            DocumentId = documentId.ToString(),
                                            PageNumber = (field.page).ToString(),
                                            RecipientId = recipient.RecipientId.ToString(),
                                            XPosition = ((int)Math.Round(leftDatePosition)).ToString(),
                                            YPosition = ((int)Math.Round(bottomDatePosition)).ToString(),
                                            ScaleValue = field.scale.ToString(),
                                            Width = ((int)Math.Round(width)).ToString(),
                                            Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                        });

                                        alreadySignedFirstHalf = true;
                                        break;
                                    default:
                                        // handle other types if needed
                                        break;
                                }
                            }
                        }

                        documentId++;
                    }
                }

                    return new ProcessResult<DraftEditRequestV4>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequestV4>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<DraftEditRequestV5> MapToDraftEditRequestV5(CandidIncomingRequest request, List<Document> documents, List<string> documentsOrder)
        {
            _logger.Base.LogInformation(nameof(MapToDraftEditRequestV5));

            try
            {
                DraftEditRequestV5 draftEditRequest = _mapper.Map<DraftEditRequestV5>(request);
                draftEditRequest.DocumentIdMap = new Dictionary<string, string>();
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;

                foreach (Document doc in documents ?? new List<Document>())
                {
                    _logger.LogVerbose($"Mapping document: {doc.name}");

                    int index = documentsOrder.IndexOf(doc.name);
                    int documentId = index + 1;
                    string tabLabelSignature1 = "";
                    string tabLabelSignature2 = "";
                    bool alreadySignedFirstHalf = false;

                    if (doc.name == "Loan Estimate")
                    {
                        tabLabelSignature1 = "Applicant Signature";
                        tabLabelSignature2 = "Co-Applicant Signature";
                    }

                    foreach (Models.Encompass.Signer signer in doc.signers ?? Array.Empty<Models.Encompass.Signer>())
                    {
                        _logger.LogVerbose($"Mapping signer: {signer.partyId}");

                        if (signer.partyId.ToLower().Contains("originator"))
                        {
                            // Skip originator
                            _logger.LogVerbose("Skipping originator");
                            continue;
                        }

                        string signerName = signer.partyId.Replace("Borrower-", "");

                        // Find recipient by the partyId
                        Recipient recipient = draftEditRequest.Recipients.Find(rec => rec.RecipientName.ToLower() == signerName.ToLower());

                        // There is a bug with the doc set api where they have the borrower and co-borrower credit disclosure forms mixed up
                        if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == false)
                        {
                            recipient = draftEditRequest.Recipients[1];
                        }
                        else if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == true)
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }
                        else if (doc.name == "Credit Score Disclosure, 2")
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }

                        if (recipient == null)
                        {
                            //todo cannot find recipient in request
                            _logger.Base.LogError($"Cannot find recipient {signerName} in the request.");
                            return new ProcessResult<DraftEditRequestV5>(false, $"Cannot find recipient {signerName} in the request.");
                        }

                        // Initialize tabs if null
                        if (recipient.Tabs == null)
                        {
                            recipient.Tabs = new Tabs()
                            {
                                SignHereTabs = new List<SignHere>(),
                                InitialHereTabs = new List<InitialHere>(),
                                DateSignedTabs = new List<DateSigned>()
                            };
                        }

                        _logger.LogVerbose($"Mapping fields for signer: {JsonSerializer.Serialize(signer.fields)}");

                        // Map fields into recipient tabs
                        foreach (Field field in signer.fields ?? Array.Empty<Field>())
                        {
                            switch (field.type.ToLower())
                            {
                                case "signature":
                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId.ToString(),
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "initials":
                                    recipient.Tabs.InitialHereTabs.Add(new InitialHere
                                    {
                                        DocumentId = documentId.ToString(),
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "signeddate":
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId.ToString(),
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;
                                case "signanddate":

                                    float width = field.right - field.left;
                                    float leftPosition = field.left;
                                    float leftDatePosition = field.right - 55;
                                    float topPosition = field.top;
                                    float bottomDatePosition = field.bottom - 15;

                                    if (doc.name == "Loan Estimate")
                                    {
                                        width = width / 2;

                                        leftPosition = alreadySignedFirstHalf ? field.left - width : field.right + width;
                                    }
                                    else if (doc.name == "Disclosures - Reconsideration of Value") // form is not formatted correctly on customers end // width not set correctly
                                    {
                                        leftDatePosition = field.left;
                                        bottomDatePosition = field.bottom + 40;
                                    }

                                    _logger.LogVerbose($"Current Field Map SignAndDate: {JsonSerializer.Serialize(field)}");

                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId.ToString(),
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        TabLabel = alreadySignedFirstHalf == false ? tabLabelSignature1 : tabLabelSignature2,
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId.ToString(),
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(leftDatePosition)).ToString(),
                                        YPosition = ((int)Math.Round(bottomDatePosition)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });

                                    alreadySignedFirstHalf = true;
                                    break;
                                default:
                                    // handle other types if needed
                                    break;
                            }
                        }
                    }

                    draftEditRequest.DocumentIdMap.Add(doc.name, documentId.ToString());

                    // Increase document id number
                    documentId++;
                }

                return new ProcessResult<DraftEditRequestV5>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequestV5>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<DraftEditRequestV4> MapToDraftEditRequestV4(CandidIncomingRequest request, List<Document> documents, List<string> documentsOrder)
        {
            _logger.Base.LogInformation(nameof(MapToDraftEditRequest));

            try
            {
                DraftEditRequestV4 draftEditRequest = _mapper.Map<DraftEditRequestV4>(request);
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;

                foreach (Document doc in documents ?? new List<Document>())
                {
                    _logger.LogVerbose($"Mapping document: {doc.name}");

                    int index = documentsOrder.IndexOf(doc.name);
                    const string documentId = "3";
                    string tabLabelSignature1 = "";
                    string tabLabelSignature2 = "";
                    bool alreadySignedFirstHalf = false;

                    if (doc.name == "Loan Estimate")
                    {
                        tabLabelSignature1 = "Applicant Signature";
                        tabLabelSignature2 = "Co-Applicant Signature";
                    }

                    foreach (Models.Encompass.Signer signer in doc.signers ?? Array.Empty<Models.Encompass.Signer>())
                    {
                        _logger.LogVerbose($"Mapping signer: {signer.partyId}");

                        if (signer.partyId.ToLower().Contains("originator"))
                        {
                            // Skip originator
                            _logger.LogVerbose("Skipping originator");
                            continue;
                        }

                        string signerName = signer.partyId.Replace("Borrower-", "");

                        // Find recipient by the partyId
                        Recipient recipient = draftEditRequest.Recipients.Find(rec => rec.RecipientName.ToLower() == signerName.ToLower());

                        // There is a bug with the doc set api where they have the borrower and co-borrower credit disclosure forms mixed up
                        if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == false)
                        {
                            recipient = draftEditRequest.Recipients[1];
                        }
                        else if (doc.name == "Credit Score Disclosure" && isOnlyOneRecipient == true)
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }
                        else if (doc.name == "Credit Score Disclosure, 2")
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }

                        if (recipient == null)
                        {
                            //todo cannot find recipient in request
                            _logger.Base.LogError($"Cannot find recipient {signerName} in the request.");
                            return new ProcessResult<DraftEditRequestV4>(false, $"Cannot find recipient {signerName} in the request.");
                        }

                        // Initialize tabs if null
                        if (recipient.Tabs == null)
                        {
                            recipient.Tabs = new Tabs()
                            {
                                SignHereTabs = new List<SignHere>(),
                                InitialHereTabs = new List<InitialHere>(),
                                DateSignedTabs = new List<DateSigned>()
                            };
                        }

                        _logger.LogVerbose($"Mapping fields for signer: {JsonSerializer.Serialize(signer.fields)}");

                        // Map fields into recipient tabs
                        foreach (Field field in signer.fields ?? Array.Empty<Field>())
                        {
                            switch (field.type.ToLower())
                            {
                                case "signature":
                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "initials":
                                    recipient.Tabs.InitialHereTabs.Add(new InitialHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "signeddate":
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;
                                case "signanddate":

                                    float width = field.right - field.left;
                                    float leftPosition = field.left;
                                    float leftDatePosition = field.right - 55;
                                    float topPosition = field.top;
                                    float bottomDatePosition = field.bottom - 15;

                                    if (doc.name == "Loan Estimate")
                                    {
                                        width = width / 2;

                                        leftPosition = alreadySignedFirstHalf ? field.left - width : field.right + width;
                                    }
                                    else if (doc.name == "Disclosures - Reconsideration of Value") // form is not formatted correctly on customers end // width not set correctly
                                    {
                                        leftDatePosition = field.left;
                                        bottomDatePosition = field.bottom + 40;
                                    }

                                    _logger.LogVerbose($"Current Field Map SignAndDate: {JsonSerializer.Serialize(field)}");

                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        TabLabel = alreadySignedFirstHalf == false ? tabLabelSignature1 : tabLabelSignature2,
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(leftDatePosition)).ToString(),
                                        YPosition = ((int)Math.Round(bottomDatePosition)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });

                                    alreadySignedFirstHalf = true;
                                    break;
                                default:
                                    // handle other types if needed
                                    break;
                            }
                        }
                    }
                }

                return new ProcessResult<DraftEditRequestV4>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequestV4>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(CandidIncomingRequest request, List<DocumentSignerMapping> docMappings, Dictionary<string, int> filePageCounts)
        {
            try
            {
                const string documentId = "3";
                DraftEditRequestV4 draftEditRequest = _mapper.Map<DraftEditRequestV4>(request);
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;
                int totalPageCount = filePageCounts.Last().Value;
                List<string> keys = filePageCounts.Keys.ToList();

                _logger.LogVerbose("Page counts: ");
                _logger.LogVerbose(JsonSerializer.Serialize(filePageCounts));

                foreach (DocumentSignerMapping docMapping in docMappings)
                {
                    _logger.LogVerbose($"Entering document mapping item: {docMapping.DocumentName}");

                    bool docMappingNeeded = keys.Any(key =>
                    {
                        string filename = key.Replace(".pdf", "");

                        string docMappingName = docMapping.DocumentName.Replace("/", "%2F");

                        return filename == docMappingName;
                    });

                    // Continue over any docMappings we don't need
                    if (docMappingNeeded == false)
                    {
                        _logger.LogVerbose($"Skipping over document mapping item: {docMapping.DocumentName}");
                        continue;
                    }

                    _logger.LogVerbose($"Mapping document: {docMapping.DocumentName}");

                    // Get current item in page count dictionary
                    KeyValuePair<string, int> currentItem = filePageCounts.First(item => item.Key.Replace(".pdf", "") == docMapping.DocumentName.Replace("/", "%2F"));
                    _logger.LogVerbose($"current item: {JsonSerializer.Serialize(currentItem)}");
                    int indexOfCurrentItem = keys.ToList().IndexOf(currentItem.Key);
                    _logger.LogVerbose($"indexOfCurrentItem: {JsonSerializer.Serialize(currentItem)}");
                    // This page number is what page the current document starts from
                    int beginningPageNumber = 0;

                    if (indexOfCurrentItem == 0)
                    {
                        beginningPageNumber = 0;
                    }
                    else
                    {
                        beginningPageNumber = filePageCounts.ElementAt(indexOfCurrentItem - 1).Value;
                    }

                    _logger.LogVerbose($"Beginning page number for {docMapping.DocumentName} is {beginningPageNumber}");

                    string tabLabelSignature1 = "";
                    string tabLabelSignature2 = "";
                    bool alreadySignedFirstHalf = false;

                    if (docMapping.DocumentName == "Loan Estimate")
                    {
                        tabLabelSignature1 = "Applicant Signature";
                        tabLabelSignature2 = "Co-Applicant Signature";
                    }

                    //todo still have to map to signer1 and signer2. This will break currently
                    foreach (Models.Encompass.Signer signer in docMapping.Signers)
                    {
                        _logger.LogVerbose($"Mapping signer: {signer.partyId}");

                        // Find recipient by the partyId
                        Recipient recipient = signer.partyId == "signer1" ? draftEditRequest.Recipients[0] : signer.partyId == "signer2" ?draftEditRequest.Recipients[1] : null;

                        if (signer.partyId.ToLower().Contains("originator"))
                        {
                            // Skip originator
                            _logger.LogVerbose("Skipping originator");
                            continue;
                        }

                        if (recipient == null)
                        {
                            _logger.Base.LogError($@"Cannot find recipient {signer.partyId} in the request.{Environment.NewLine}
                                Logic is not implemented for signer: {signer.partyId}");
                            return new ProcessResult<DraftEditRequestV4>(false, $"Cannot find recipient {signer.partyId} in the request.");
                        }

                        _logger.LogVerbose($"Found recipient for signer {signer.partyId}: {recipient.RecipientName}");

                        // Initialize tabs if null
                        if (recipient.Tabs == null)
                        {
                            recipient.Tabs = new Tabs()
                            {
                                SignHereTabs = new List<SignHere>(),
                                InitialHereTabs = new List<InitialHere>(),
                                DateSignedTabs = new List<DateSigned>()
                            };
                        }

                        _logger.LogVerbose($"Mapping fields for signer: {JsonSerializer.Serialize(signer.fields)}");

                        // Map fields into recipient tabs
                        foreach (Field field in signer.fields ?? Array.Empty<Field>())
                        {
                            switch (field.type.ToLower())
                            {
                                case "signature":
                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "initials":
                                    recipient.Tabs.InitialHereTabs.Add(new InitialHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;

                                case "signeddate":
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(field.right - field.left)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    break;
                                case "signanddate":

                                    float width = field.right - field.left;
                                    float leftPosition = field.left;

                                    if (docMapping.DocumentName == "Loan Estimate")
                                    {
                                        width = width / 2;

                                        leftPosition = alreadySignedFirstHalf ? field.left - width : field.right + width;
                                    }

                                    _logger.LogVerbose($"Current Field Map SignAndDate: {JsonSerializer.Serialize(field)}");

                                    recipient.Tabs.SignHereTabs.Add(new SignHere
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        TabLabel = alreadySignedFirstHalf == false ? tabLabelSignature1 : tabLabelSignature2,
                                        XPosition = ((int)Math.Round(field.left)).ToString(),
                                        YPosition = ((int)Math.Round(field.top - 5)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });
                                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                                    {
                                        DocumentId = documentId,
                                        PageNumber = (beginningPageNumber + field.page).ToString(),
                                        RecipientId = recipient.RecipientId.ToString(),
                                        XPosition = ((int)Math.Round(612 - leftPosition)).ToString(),
                                        YPosition = ((int)Math.Round(field.top)).ToString(),
                                        ScaleValue = field.scale.ToString(),
                                        Width = ((int)Math.Round(width)).ToString(),
                                        Height = ((int)Math.Round(field.bottom - field.top)).ToString()
                                    });

                                    alreadySignedFirstHalf = true;
                                    break;
                                default:
                                    // handle other types if needed
                                    break;
                            }
                        }
                    }
                }

                return new ProcessResult<DraftEditRequestV4>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequestV4>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(bool urlaAdditionalBorrowerPage, CandidIncomingRequest request, Dictionary<string, int> filePageCounts)
        {
            try
            {
                const string documentId = "3";
                DraftEditRequestV4 draftEditRequest = _mapper.Map<DraftEditRequestV4>(request);
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;
                int totalPageCount = filePageCounts.Last().Value;
                List<string> keys = filePageCounts.Keys.ToList();

                _logger.LogVerbose("Page counts: ");
                _logger.LogVerbose(JsonSerializer.Serialize(filePageCounts));

                foreach (string documentName in keys)
                {
                    _logger.LogVerbose($"Mapping document: {documentName}");

                    // Get current item in page count dictionary
                    KeyValuePair<string, int> currentItem = filePageCounts.First(item => item.Key == documentName);

                    _logger.LogVerbose($"current item: {JsonSerializer.Serialize(currentItem)}");

                    int indexOfCurrentItem = keys.ToList().IndexOf(currentItem.Key);

                    _logger.LogVerbose($"indexOfCurrentItem: {JsonSerializer.Serialize(currentItem)}");

                    // This page number is what page the current document starts from
                    int beginningPageNumber = 0;

                    if (indexOfCurrentItem == 0)
                    {
                        beginningPageNumber = 0;
                    }
                    else
                    {
                        beginningPageNumber = filePageCounts.ElementAt(indexOfCurrentItem - 1).Value;
                    }

                    string tabLabelSignature1 = "";
                    string tabLabelSignature2 = "";
                    bool alreadySignedFirstHalf = false;

                    if (documentName == "Loan Estimate")
                    {
                        tabLabelSignature1 = "Applicant Signature";
                        tabLabelSignature2 = "Co-Applicant Signature";
                    }

                    _logger.LogVerbose($"Beginning page number for {documentName} is {beginningPageNumber}");


                    if ((documentName == "1003 - URLA" || documentName.Contains("1003 URLA")) && !urlaAdditionalBorrowerPage)
                    {
                        // 16 pages
                        draftEditRequest.Recipients[0].Tabs.InitialHereTabs.Add(new InitialHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "1 of 16",
                            AnchorXOffset = "-45",
                            AnchorYOffset = "-445",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 16",
                            AnchorXOffset = "-180",
                            AnchorYOffset = "-90",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 16",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-95",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        // 10 pages
                        draftEditRequest.Recipients[0].Tabs.InitialHereTabs.Add(new InitialHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "1 of 10",
                            AnchorXOffset = "-45",
                            AnchorYOffset = "-437",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "5 of 10",
                            AnchorXOffset = "-180",
                            AnchorYOffset = "-90",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "5 of 10",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-95",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        // 18 pages
                        draftEditRequest.Recipients[0].Tabs.InitialHereTabs.Add(new InitialHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "1 of 18",
                            AnchorXOffset = "-45",
                            AnchorYOffset = "-445",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 18",
                            AnchorXOffset = "-180",
                            AnchorYOffset = "-90",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 18",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-95",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "18 of 18",
                            AnchorXOffset = "-170",
                            AnchorYOffset = "-630",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "18 of 18",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-635",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        if (draftEditRequest.Recipients.Count == 2)
                        {
                            // 16 pages
                            draftEditRequest.Recipients[1].Tabs.InitialHereTabs.Add(new InitialHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "9 of 16",
                                AnchorXOffset = "-45",
                                AnchorYOffset = "-445",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "14 of 16",
                                AnchorXOffset = "-130",
                                AnchorYOffset = "-90",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "14 of 16",
                                AnchorXOffset = "175",
                                AnchorYOffset = "-95",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            // 18 pages
                            draftEditRequest.Recipients[1].Tabs.InitialHereTabs.Add(new InitialHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "9 of 18",
                                AnchorXOffset = "-45",
                                AnchorYOffset = "-445",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "14 of 18",
                                AnchorXOffset = "-130",
                                AnchorYOffset = "-90",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "14 of 18",
                                AnchorXOffset = "175",
                                AnchorYOffset = "-95",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "18 of 18",
                                AnchorXOffset = "-130",
                                AnchorYOffset = "-580",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "18 of 18",
                                AnchorXOffset = "180",
                                AnchorYOffset = "-585",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });
                        }
                    }
                    else if ((documentName == "1003 - URLA" || documentName.Contains("1003 URLA")) && urlaAdditionalBorrowerPage)
                    {
                        // 12 pages
                        draftEditRequest.Recipients[0].Tabs.InitialHereTabs.Add(new InitialHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "1 of 12",
                            AnchorXOffset = "-45",
                            AnchorYOffset = "-445",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 12",
                            AnchorXOffset = "-100",
                            AnchorYOffset = "-90",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        // 14 pages
                        draftEditRequest.Recipients[0].Tabs.InitialHereTabs.Add(new InitialHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "1 of 14",
                            AnchorXOffset = "-45",
                            AnchorYOffset = "-445",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 14",
                            AnchorXOffset = "-180",
                            AnchorYOffset = "-90",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "6 of 14",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-95",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "14 of 14",
                            AnchorXOffset = "-170",
                            AnchorYOffset = "-630",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "14 of 14",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-635",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        // 10 pages
                        draftEditRequest.Recipients[0].Tabs.InitialHereTabs.Add(new InitialHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "1 of 10",
                            AnchorXOffset = "-45",
                            AnchorYOffset = "-437",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "5 of 10",
                            AnchorXOffset = "-180",
                            AnchorYOffset = "-90",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            AnchorString = "5 of 10",
                            AnchorXOffset = "180",
                            AnchorYOffset = "-95",
                            AnchorIgnoreIfNotPresent = "true",
                            AnchorUnits = "pixels",
                            AnchorCaseSensitive = "true",
                            AnchorMatchWholeWord = "true",
                            ScaleValue = "0.75"
                        });

                        if (draftEditRequest.Recipients.Count == 2)
                        {
                            // 12 pages
                            draftEditRequest.Recipients[1].Tabs.InitialHereTabs.Add(new InitialHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "9 of 12",
                                AnchorXOffset = "-45",
                                AnchorYOffset = "-445",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "6 of 12",
                                AnchorXOffset = "-25",
                                AnchorYOffset = "-100",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            // 14 pages
                            draftEditRequest.Recipients[1].Tabs.InitialHereTabs.Add(new InitialHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "9 of 14",
                                AnchorXOffset = "-45",
                                AnchorYOffset = "-455",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "6 of 14",
                                AnchorXOffset = "-135",
                                AnchorYOffset = "-40",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "6 of 14",
                                AnchorXOffset = "180",
                                AnchorYOffset = "-45",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "14 of 14",
                                AnchorXOffset = "-130",
                                AnchorYOffset = "-580",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                AnchorString = "14 of 14",
                                AnchorXOffset = "180",
                                AnchorYOffset = "-585",
                                AnchorIgnoreIfNotPresent = "true",
                                AnchorUnits = "pixels",
                                AnchorCaseSensitive = "true",
                                AnchorMatchWholeWord = "true",
                                ScaleValue = "0.75"
                            });
                        }
                    }
                    else if (documentName == "Loan Estimate")
                    {
                        // Sign and date tabs with the anchor string as Applicant Signature 
                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            DocumentId = documentId,
                            PageNumber = (beginningPageNumber + 3).ToString(), // hard-coded to 3
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            XPosition = "42",
                            YPosition = "705",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            DocumentId = documentId,
                            PageNumber = (beginningPageNumber + 3).ToString(), // hard-coded to 3
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            XPosition = "195",
                            YPosition = "715",
                            ScaleValue = "0.75"
                        });

                        if (draftEditRequest.Recipients.Count == 2)
                        {
                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                DocumentId = documentId,
                                PageNumber = (beginningPageNumber + 3).ToString(), // hard-coded to 3
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                XPosition = "310",
                                YPosition = "705",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                DocumentId = documentId,
                                PageNumber = (beginningPageNumber + 3).ToString(), // hard-coded to 3
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                XPosition = "445",
                                YPosition = "715",
                                ScaleValue = "0.75"
                            });
                        }
                    }
                    else if(documentName == "Privacy Policy")
                    {
                        draftEditRequest.Recipients[0].Tabs.SignHereTabs.Add(new SignHere
                        {
                            DocumentId = documentId,
                            PageNumber = (beginningPageNumber + 1).ToString(), 
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            XPosition = "72",
                            YPosition = "665",
                            ScaleValue = "0.75"
                        });

                        draftEditRequest.Recipients[0].Tabs.DateSignedTabs.Add(new DateSigned
                        {
                            DocumentId = documentId,
                            PageNumber = (beginningPageNumber + 1).ToString(), 
                            RecipientId = draftEditRequest.Recipients[0].RecipientId.ToString(),
                            XPosition = "225",
                            YPosition = "685",
                            ScaleValue = "0.75"
                        });

                        if (draftEditRequest.Recipients.Count == 2)
                        {
                            draftEditRequest.Recipients[1].Tabs.SignHereTabs.Add(new SignHere
                            {
                                DocumentId = documentId,
                                PageNumber = (beginningPageNumber + 1).ToString(), 
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                XPosition = "325",
                                YPosition = "665",
                                ScaleValue = "0.75"
                            });

                            draftEditRequest.Recipients[1].Tabs.DateSignedTabs.Add(new DateSigned
                            {
                                DocumentId = documentId,
                                PageNumber = (beginningPageNumber + 1).ToString(), 
                                RecipientId = draftEditRequest.Recipients[1].RecipientId.ToString(),
                                XPosition = "475",
                                YPosition = "685",
                                ScaleValue = "0.75"
                            });
                        }
                    }
                }

                // Find recipient by the partyId
                foreach (Recipient recipient in draftEditRequest.Recipients)
                {
                    _logger.LogVerbose($"Mapping tabs for recipient: {recipient.RecipientName}");

                    // Sign and date tabs with the borrow name capitalized
                    recipient.Tabs.SignHereTabs.Add(new SignHere
                    {
                        RecipientId = recipient.RecipientId.ToString(),
                        AnchorString = recipient.RecipientName.ToUpper(), // capitalized
                        AnchorXOffset = "4",
                        AnchorYOffset = "-6",
                        AnchorIgnoreIfNotPresent = "true",
                        AnchorUnits = "pixels",
                        AnchorCaseSensitive = "true",
                        AnchorMatchWholeWord = "true",
                        ScaleValue = "0.75"
                    });

                    recipient.Tabs.DateSignedTabs.Add(new DateSigned
                    {
                        RecipientId = recipient.RecipientId.ToString(),
                        AnchorString = recipient.RecipientName.ToUpper(), // capitalized
                        AnchorXOffset = "390",
                        AnchorYOffset = "-12",
                        AnchorIgnoreIfNotPresent = "true",
                        AnchorUnits = "pixels",
                        AnchorCaseSensitive = "true",
                        AnchorMatchWholeWord = "true",
                        ScaleValue = "0.75"
                    });
                }

                return new ProcessResult<DraftEditRequestV4>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequestV4>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<Dictionary<string, int>> MergePdfs(bool isJointApplication, string downloadPathPrefix, string disclosuresName, List<string> attachmentNames)
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

                        if(string.IsNullOrEmpty(file))
                        {

                            _logger.Base.LogError("One of the files to merge is null or empty. Maybe this file could not be found.");
                            continue;
                        }

                        using (PdfDocument inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import))
                        {
                            // Add the page count as the total page count so far
                            currentPageCount += inputDocument.PageCount;

                            string fileName = Path.GetFileName(file);

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

        public ProcessResult<Dictionary<string, int>> MergePdfs(string disclosuresDownloadPath, string disclosuresName, List<string> attachmentNames)
        {
            _logger.Base.LogInformation(nameof(MergePdfs));
            try
            {
                Dictionary<string, int> filePageCounts = new Dictionary<string, int>();

                int currentPageCount = 0;

                // Get all PDF files with path in the folder
                string[] inputFiles = Directory.GetFiles(disclosuresDownloadPath, "*.pdf");

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

                            string fileName = Path.GetFileName(file);

                            filePageCounts.Add(fileName, currentPageCount);

                            foreach (PdfPage page in inputDocument.Pages)
                            {
                                outputDocument.AddPage(page);
                            }
                        }
                    }

                    // Save the merged document
                    outputDocument.Save($"{disclosuresDownloadPath}/{disclosuresName}");
                }

                return new ProcessResult<Dictionary<string, int>>(true)
                {
                    Result = filePageCounts
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to merge pdfs in path {disclosuresDownloadPath}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<Dictionary<string, int>>(false, $"Failed to merge pdfs in path {disclosuresDownloadPath}.");
            }
        }

        public ProcessResult<string> GetDisclosuresName(DisclosureStage stage, int revision)
        {
            _logger.Base.LogInformation(nameof(GetDisclosuresName));
            try
            {
                string disclosuresName;

                switch (stage)
                {
                    case DisclosureStage.RevisedInitialClosingDisclosures:
                        disclosuresName = _fileConfig.RevisedInitialClosingDisclosuresName;
                        break;
                    case DisclosureStage.InitialClosingDisclosures:
                        disclosuresName = _fileConfig.InitialClosingDisclosuresName;
                        break;
                    case DisclosureStage.RevisedInitialDisclosures:
                        disclosuresName = _fileConfig.RevisedIntialDisclosuresName;
                        break;
                    case DisclosureStage.InitialDisclosures:
                        disclosuresName = _fileConfig.IntialDisclosuresName;
                        break;
                    default:
                        disclosuresName = "";
                        break;
                }

                _logger.LogVerbose($"Disclosures name for type {stage} is {disclosuresName}");
                _logger.LogVerbose(revision.ToString());

                if (string.IsNullOrWhiteSpace(disclosuresName))
                {
                    _logger.LogVerbose($"No disclosures name found for type {stage}");
                    return new ProcessResult<string>(false, $"No disclosures name found for type {stage}");
                }

                if (stage.ToString().ToLower().Contains("revised") && revision > 1)
                {
                    disclosuresName = disclosuresName.Replace(".pdf", $" v{revision}.pdf");
                }

                return new ProcessResult<string>(true)
                {
                    Result = disclosuresName
                };
            }
            catch(Exception ex)
            {
                _logger.Base.LogError($"Failed to get disclosures name for type {stage}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to get disclosures name for type {stage}. Exception: {ex}");
            }
        }
    }

    public interface IEncompassService
    {
        Task<ProcessResult<DisclosureStage>> GetDisclosureStage(string orgId, string loanId, string token);
        Task<ProcessResult<string>> GetResourceLock(string orgId, string loanId, string token);
        Task<ProcessResult> RemoveResourceLock(string orgId, string loanId, string lockId, string token);
        Task<ProcessResult<LoanInfo>> GetLoanInfo(string orgId, string loanId, string token);

        /// <summary>
        /// Creates the initial audit job for an order, which is typical for initial disclosures. 
        /// This will run asynchronously until it has completed. 
        /// A Loan Audit ID is returned in the response and a webhook notification will be generated when complete.
        /// Borrower Id is also the application Id in Encompass.
        /// </summary>
        /// <param name="loanId"></param>
        /// <param name="applicationId"></param>
        /// <returns>Audit Id - 202 Accepted</returns>
        Task<ProcessResult<string>> CreateLoanAuditForOrder(string orgId, string loanId, string applicationId, string token, DisclosureStage stage);

        /// <summary>
        /// Returns the status of an audit request for an order.
        /// This tells the user needs to fill out information.
        /// </summary>
        /// <param name="auditId">The unique identifier of the audit to retrieve. Cannot be null or empty.</param>
        /// <returns>Audit results</returns>
        Task<ProcessResult> GetDocumentAuditForOrder(string orgId, string loanId, string auditId, string token, DisclosureStage stage);

        /// <summary>
        /// Generates a doc set for the package.
        /// </summary>
        /// <param name="auditId"></param>
        /// <returns>Order Id - 202 Accepted</returns>
        Task<ProcessResult<string>> GenerateDocSet(string orgId, string loanId, string auditId, string token, DisclosureStage stage);

        /// <summary>
        /// Retrieves doc status and details for the specified order (orderID).
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns>Order Details</returns>
        Task<ProcessResult<List<Document>>> GetOrderStatus(string orgId, string loanId, string orderId, string token, DisclosureStage stage);

        Task<ProcessResult<List<string>>> GetEFolderDocuments(string orgId, string loanId, Dictionary<string, bool> dictAttachmentNamesToFind, string token);

        Task<ProcessResult<List<string>>> GetAllEFolderDocuments(string orgId, string loanId, string token);

        Task<ProcessResult<string>> AddToDocSet(string orgId, string loanId, string orderId, string applicationId, List<string> templateNames, string token, DisclosureStage stage);

        Task<ProcessResult<List<Document>>> GetDocSet(string orgId, string loanId, string orderId, string jobId, string token, DisclosureStage stage);

        ProcessResult CheckIfAllDocumentsExist(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents);

        ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents, Dictionary<string, int> filePageCounts);
        ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents);
        ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(CandidIncomingRequest request, List<DocumentSignerMapping> docMappings, Dictionary<string, int> filePageCounts);
        ProcessResult<DraftEditRequestV4> MapToDraftEditRequest(bool urlaAdditionalBorrowerPage, CandidIncomingRequest request, Dictionary<string, int> filePageCounts);

        ProcessResult<Dictionary<string, int>> MergePdfs(string disclosuresDownloadPath, string disclosuresName, List<string> attachmentNames);
        ProcessResult<Dictionary<string, int>> MergePdfs(bool isJointApplication, string downloadPathPrefix, string disclosuresName, List<string> attachmentNames);

        List<Document> RemoveUnneededDocuments(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents);
        List<EncompassDocument> RemoveUnneededDocuments(Dictionary<string, bool> dictAttachmentNamesToFind, List<EncompassDocument> documents);

        ProcessResult<string> GetDisclosuresName(DisclosureStage stage, int revision);
        ProcessResult<DraftEditRequestV5> MapToDraftEditRequestV5(CandidIncomingRequest request, List<Document> documents, List<string> documentsOrder);
        ProcessResult<DraftEditRequestV4> MapToDraftEditRequestV4(CandidIncomingRequest request, List<Document> documents, List<string> documentsOrder);
    }
}
