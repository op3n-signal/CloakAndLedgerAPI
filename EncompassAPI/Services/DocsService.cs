using Amazon.CloudWatchLogs;
using Amazon.S3.Model;
using AutoMapper;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Docusign;
using EncompassAPI.Models.Encompass;
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
using static System.Net.Mime.MediaTypeNames;

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
            _logger.Base.LogInformation($"Entering {nameof(GetDisclosureStage)}");

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

        public async Task<ProcessResult<string>> GetApplicationIdFromLoan(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(GetApplicationIdFromLoan)}");

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
                    return new ProcessResult<string>(false, "Application id not found");
                }

                return new ProcessResult<string>(true)
                {
                    Result = loanInfo.applications[0].id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve application id for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to retrieve application id for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> CreateLoanAuditForOrder(string orgId, string loanId, string applicationId, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation($"Entering {nameof(CreateLoanAuditForOrder)}");
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
            _logger.Base.LogInformation($"Entering {nameof(GetDocumentAuditForOrder)}");

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
            _logger.Base.LogInformation($"Entering {nameof(GenerateDocSet)}");

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
            _logger.Base.LogInformation($"Entering {nameof(GetOrderStatus)}");

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

        public async Task<ProcessResult<string>> AddToDocSet(string orgId, string loanId, string orderId, string applicationId, List<string> templateNames, string token, DisclosureStage stage)
        {
            _logger.Base.LogInformation($"Entering {nameof(AddToDocSet)}");

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
            _logger.Base.LogInformation($"Entering {nameof(GetDocSet)}");

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
            _logger.Base.LogInformation($"Entering {nameof(CheckIfAllDocumentsExist)}");

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
            _logger.Base.LogInformation($"Entering {nameof(RemoveUnneededDocuments)}");

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

        public ProcessResult<DraftEditRequest> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents, Dictionary<string, int> filePageCounts)
        {
            try
            {
                const string documentId = "3";
                DraftEditRequest draftEditRequest = _mapper.Map<DraftEditRequest>(request);
                bool isOnlyOneRecipient = draftEditRequest.Recipients.Count == 1;
                int totalPageCount = filePageCounts.Last().Value;
                List<string> keys = filePageCounts.Keys.ToList();

                _logger.LogVerbose("Page counts: ");
                _logger.LogVerbose(JsonSerializer.Serialize(filePageCounts));

                foreach (Document doc in documents ?? new List<Document>())
                {
                    _logger.LogVerbose($"Mapping document: {doc.name}");

                    // Get current item in page count dictionary
                    KeyValuePair<string, int> currentItem = filePageCounts.First(item => item.Key.Replace(".pdf", "") == doc.name.Replace("/", "_"));
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

                    foreach (Signer signer in doc.signers ?? Array.Empty<Signer>())
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
                        Models.Docusign.Recipient recipient = draftEditRequest.Recipients.Find(rec => rec.RecipientName.ToLower() == signerName.ToLower());

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
                            return new ProcessResult<DraftEditRequest>(false, $"Cannot find recipient {signerName} in the request.");
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

                                    if (doc.name == "Loan Estimate")
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

                return new ProcessResult<DraftEditRequest>(true)
                {
                    Result = draftEditRequest
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to map to draft edit request.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DraftEditRequest>(false, $"Failed to map to draft edit request.");
            }
        }

        public ProcessResult<Dictionary<string, int>> MergePdfs(string downloadPathPrefix, string disclosuresName, List<string> attachmentNames)
        {
            try
            {
                Dictionary<string, int> filePageCounts = new Dictionary<string, int>();

                int currentPageCount = 0;

                // Get all PDF files with path in the folder
                string[] inputFiles = Directory.GetFiles(downloadPathPrefix, "*.pdf");

                // Order the files based on how they were ordered
                var sortedFiles = attachmentNames
                .Select(name => inputFiles.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == name.Replace("/", "_")))
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

                            filePageCounts.Add(Path.GetFileName(file), currentPageCount);

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

        public ProcessResult<string> GetDisclosuresName(DisclosureStage stage, int revision)
        {
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
        Task<ProcessResult<string>> GetApplicationIdFromLoan(string orgId, string loanId, string token);

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

        Task<ProcessResult<string>> AddToDocSet(string orgId, string loanId, string orderId, string applicationId, List<string> templateNames, string token, DisclosureStage stage);

        Task<ProcessResult<List<Document>>> GetDocSet(string orgId, string loanId, string orderId, string jobId, string token, DisclosureStage stage);

        ProcessResult CheckIfAllDocumentsExist(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents);

        ProcessResult<DraftEditRequest> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents, Dictionary<string, int> filePageCounts);

        ProcessResult<Dictionary<string, int>> MergePdfs(string downloadPathPrefix, string disclosuresName, List<string> attachmentNames);

        List<Document> RemoveUnneededDocuments(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents);
        ProcessResult<string> GetDisclosuresName(DisclosureStage stage, int revision);
    }
}
