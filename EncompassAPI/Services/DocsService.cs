using Amazon.CloudWatchLogs;
using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DocsService : IEncompassService
    {
        private readonly LoggerService<DocsService> _logger;
        private readonly EncompassConfig _encompassConfig;
        private readonly IHttpService _http;

        public DocsService(LoggerService<DocsService> logger, IOptionsSnapshot<EncompassConfig> encompassOptions, IEncompassHttpService http) 
        { 
            _logger = logger;
            _encompassConfig = encompassOptions.Value;
            _http = http;
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


                AuditResult auditResult = await _http.GetAsync<AuditResult>(url, CommonFields.EncompassHeaders);

                if(auditResult.status.ToLower() != "succeeded")
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

        public async Task<ProcessResult<Document[]>> GetOrderStatus(string orgId, string loanId, string orderId)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.OpeningDocSetEndpoint}/{orderId}";


                DocumentOrders documentOrders = await _http.GetAsync<DocumentOrders>(url, CommonFields.EncompassHeaders);

                if (documentOrders.status.ToLower() != "succeeded")
                {
                    _logger.Base.LogError("The document orders response has errors"); //todo
                    return new ProcessResult<Document[]>(false, "The document orders response has errors");
                }

                return new ProcessResult<Document[]>(true)
                {
                    Result = documentOrders.documents
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document orders for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<Document[]>(false, $"Failed to retrieve document orders for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string[]>> GetEFolderDocuments(string orgId, string loanId)
        {
            try
            {
                // Get loan audit result
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.GetEFolderDocsEndpoint}";


                EFolderDocument[] efolderDocuments = await _http.GetAsync<EFolderDocument[]>(url, CommonFields.EncompassHeaders);

                if (efolderDocuments == null || efolderDocuments.Length == 0)
                {
                    _logger.Base.LogError("No docs found"); //todo
                    return new ProcessResult<string[]>(false, "No docs found");
                }

                return new ProcessResult<string[]>(true)
                {
                    Result = efolderDocuments.Select(doc => doc.name).ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve document from e-folder for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<string[]>(false, $"Failed to retrieve document from e-folder for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<string>> AddToOpeningDocSet(string orgId, string loanId, string orderId, string applicationId, string[] templateNames)
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
    }

    public interface IEncompassService
    {
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
        Task<ProcessResult<Document[]>> GetOrderStatus(string orgId, string loanId, string orderId);

        Task<ProcessResult<string[]>> GetEFolderDocuments(string orgId, string loanId);

        Task<ProcessResult<string>> AddToOpeningDocSet(string orgId, string loanId, string orderId, string applicationId, string[] templateNames);

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
