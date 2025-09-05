using Amazon.CloudWatchLogs;
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

        public async Task<ProcessResult<string>> GetResourceLock(string orgId, string loanId)
        {
            try
            {
                // Get resource locks
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.ResourceLockEndpoint}?resourceType=loan&resourceId={loanId}";


                ProcessId[] lockIds = await _http.GetAsync<ProcessId[]>(url, CommonFields.EncompassHeaders);

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

        public async Task<ProcessResult> RemoveResourceLock(string orgId, string loanId, string lockId)
        {
            try
            {
                // Remove resource lock since user is in the loan file
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.ResourceLockEndpoint}/{lockId}?resourceType=loan&resourceId={loanId}&force=true";

                bool blDeleted = await _http.DeleteAsync(url, CommonFields.EncompassHeaders);

                return new ProcessResult(blDeleted);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to remove resource lock for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to remove resource lock for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> GetApplicationIdFromLoan(string orgId, string loanId)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}";


                LoanInfo loanInfo = await _http.GetAsync<LoanInfo>(url, CommonFields.EncompassHeaders);

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

        public async Task<ProcessResult<string>> CreateLoanAuditForOpeningOrder(string orgId, string loanId, string applicationId)
        {
            try
            {
                // Run loan audit
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningAuditEndpoint}";

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

                ProcessId process = await _http.PostAsync<ProcessId>(url, CommonFields.EncompassHeaders, JsonSerializer.Serialize(auditRequest));

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

        public async Task<ProcessResult> GetDocumentAuditForOpeningOrder(string orgId, string loanId, string auditId)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningAuditEndpoint}/{auditId}";


                AuditResult auditResult = await _http.GetAsync<AuditResult>(url, CommonFields.EncompassHeaders, maxRetries: 5, delayMilliseconds: 10000);

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

        public async Task<ProcessResult<string>> GenerateOpeningDocSet(string orgId, string loanId, string auditId)
        {
            try
            {
                // Run loan audit
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningDocSetEndpoint}";

                DocSetRequest docSetRequest = new DocSetRequest()
                {
                    auditId = auditId,
                    printMode = CommonFields.PrintMode
                };

                ProcessId process = await _http.PostAsync<ProcessId>(url, CommonFields.EncompassHeaders, JsonSerializer.Serialize(docSetRequest));

                return new ProcessResult<string>(true)
                {
                    Result = process.id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to generate opening doc set for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to generate opening doc set for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<Document>>> GetOrderStatus(string orgId, string loanId, string orderId)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningDocSetEndpoint}/{orderId}";


                DocumentOrders documentOrders = await _http.GetAsync<DocumentOrders>(url, CommonFields.EncompassHeaders, maxRetries: 5, delayMilliseconds: 10000);

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

        public async Task<ProcessResult<List<string>>> GetEFolderDocuments(string orgId, string loanId, Dictionary<string, bool> dictAttachmentNamesToFind)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.GetEFolderDocsEndpoint}";


                EFolderDocument[] efolderDocuments = await _http.GetAsync<EFolderDocument[]>(url, CommonFields.EncompassHeaders);

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

        public async Task<ProcessResult<string>> AddToOpeningDocSet(string orgId, string loanId, string orderId, string applicationId, List<string> templateNames)
        {
            try
            {
                // Run loan audit
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningDocSetEndpoint}/{orderId}/documents";

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

                ProcessId process = await _http.PostAsync<ProcessId>(url, CommonFields.EncompassHeaders, JsonSerializer.Serialize(addToDocSetRequest));

                return new ProcessResult<string>(true)
                {
                    Result = process.id
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to add to opening doc set for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string>(false, $"Failed to add to opening doc set for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<List<Document>>> GetOpeningDocSet(string orgId, string loanId, string orderId, string jobId)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningDocSetEndpoint}/{orderId}/documents/{jobId}";


                DocumentOrders documentOrders = await _http.GetAsync<DocumentOrders>(url, CommonFields.EncompassHeaders, maxRetries: 5, delayMilliseconds: 10000);

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
                int totalPageCount = filePageCounts.Last().Value;
                List<string> keys = filePageCounts.Keys.ToList();

                foreach (Document doc in documents ?? new List<Document>())
                {
                    // Get current item in page count dictionary
                    KeyValuePair<string, int> currentItem = filePageCounts.First(item => item.Key.Replace(".pdf", "") == doc.name.Replace("/", "_"));
                    int indexOfCurrentItem = keys.ToList().IndexOf(currentItem.Key);
                    // This page number is what page the current document starts from
                    int beginningPageNumber = 0;

                    if(indexOfCurrentItem == 0)
                    {
                        beginningPageNumber = 0;
                    }
                    else if((indexOfCurrentItem - 1) != 0)
                    {
                        beginningPageNumber = filePageCounts.ElementAt(indexOfCurrentItem - 1).Value;
                    }

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
                        if(signer.partyId.ToLower().Contains("originator"))
                        {
                            // Skip originator
                            continue;
                        }

                        string signerName = signer.partyId.Replace("Borrower-", "");

                        // Find recipient by the partyId
                        Models.Docusign.Recipient recipient = draftEditRequest.Recipients.Find(rec => rec.RecipientName == signerName);

                        // There is a bug with the doc set api where they have the borrower and co-borrower credit disclosure forms mixed up
                        if (doc.name == "Credit Score Disclosure")
                        {
                            recipient = draftEditRequest.Recipients[0];
                        }
                        else if(doc.name == "Credit Score Disclosure, 2")
                        {
                            recipient = draftEditRequest.Recipients[1];
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

        public ProcessResult<Dictionary<string, int>> MergePdfs(string downloadPathPrefix)
        {
            try
            {
                Dictionary<string, int> filePageCounts = new Dictionary<string, int>();

                int currentPageCount = 0;

                // Get all PDF files in the folder
                string[] inputFiles = Directory.GetFiles(downloadPathPrefix, "*.pdf");

                using (PdfDocument outputDocument = new PdfDocument())
                {
                    foreach (string file in inputFiles)
                    {
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
                    outputDocument.Save($"{downloadPathPrefix}/{_fileConfig.DisclosuresFileName}");
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
    }

    public interface IEncompassService
    {
        Task<ProcessResult<string>> GetResourceLock(string orgId, string loanId);
        Task<ProcessResult> RemoveResourceLock(string orgId, string loanId, string lockId);
        Task<ProcessResult<string>> GetApplicationIdFromLoan(string orgId, string loanId);

        /// <summary>
        /// Creates the initial audit job for an opening order, which is typical for initial disclosures. 
        /// This will run asynchronously until it has completed. 
        /// A Loan Audit ID is returned in the response and a webhook notification will be generated when complete.
        /// Borrower Id is also the application Id in Encompass.
        /// </summary>
        /// <param name="loanId"></param>
        /// <param name="applicationId"></param>
        /// <returns>Audit Id - 202 Accepted</returns>
        Task<ProcessResult<string>> CreateLoanAuditForOpeningOrder(string orgId, string loanId, string applicationId);

        /// <summary>
        /// Returns the status of an audit request for an opening order.
        /// This tells the user needs to fill out information.
        /// </summary>
        /// <param name="auditId">The unique identifier of the audit to retrieve. Cannot be null or empty.</param>
        /// <returns>Audit results</returns>
        Task<ProcessResult> GetDocumentAuditForOpeningOrder(string orgId, string loanId, string auditId);

        /// <summary>
        /// Generates an Opening doc set for the package (Initial Disclosures).
        /// </summary>
        /// <param name="auditId"></param>
        /// <returns>Order Id - 202 Accepted</returns>
        Task<ProcessResult<string>> GenerateOpeningDocSet(string orgId, string loanId, string auditId);

        /// <summary>
        /// Retrieves doc status and details for the specified order (orderID).
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns>Order Details</returns>
        Task<ProcessResult<List<Document>>> GetOrderStatus(string orgId, string loanId, string orderId);

        Task<ProcessResult<List<string>>> GetEFolderDocuments(string orgId, string loanId, Dictionary<string, bool> dictAttachmentNamesToFind);

        Task<ProcessResult<string>> AddToOpeningDocSet(string orgId, string loanId, string orderId, string applicationId, List<string> templateNames);

        Task<ProcessResult<List<Document>>> GetOpeningDocSet(string orgId, string loanId, string orderId, string jobId);

        ProcessResult CheckIfAllDocumentsExist(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents);

        ProcessResult<DraftEditRequest> MapToDraftEditRequest(CandidIncomingRequest request, List<Document> documents, Dictionary<string, int> filePageCounts);

        ProcessResult<Dictionary<string, int>> MergePdfs(string downloadPathPrefix);

        List<Document> RemoveUnneededDocuments(Dictionary<string, bool> dictAttachmentNamesToFind, List<Document> documents);

        /*        /// <summary>
                /// Commit and send an Opening Package (Initial Disclosures) to a recipient.
                /// todo Finish other params
                /// </summary>
                /// <param name="orderId"></param>
                /// <returns>Job Id - 202 Accepted</returns>
                Task SendOpeningPackage(string orderId);

                /// <summary>
                /// Retrieves delivery details for an Opening Package.
                /// todo figure out response
                /// </summary>
                /// <param name="orderId"></param>
                /// <param name="jobId"></param>
                /// <returns></returns>
                Task GetOpeningPackageDeliveryDetails(string orderId, string jobId);*/
    }
}
