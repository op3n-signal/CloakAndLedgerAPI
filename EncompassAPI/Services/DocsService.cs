namespace EncompassAPI.Services
{
    public class DocsService
    {
    }

    public interface IEncompassService
    {
        /// <summary>
        /// Creates the initial audit job for an opening order, which is typical for initial disclosures. 
        /// This will run asynchronously until it has completed. 
        /// A Loan Audit ID is returned in the response and a webhook notification will be generated when complete.
        /// Borrower Id is also the application Id in Encompass.
        /// </summary>
        /// <param name="loanId"></param>
        /// <param name="borrowerId"></param>
        /// <returns>Audit Id - 202 Accepted</returns>
        Task<string> CreateLoanAuditForOpeningOrder(string loanId, string borrowerId);

        /// <summary>
        /// Returns the status of an audit request for an opening order.
        /// This tells the user needs to fill out information.
        /// </summary>
        /// <param name="auditId">The unique identifier of the audit to retrieve. Cannot be null or empty.</param>
        /// <returns>Audit results</returns>
        Task GetDocumentAuditForOpeningOrder(string auditId);

        /// <summary>
        /// Generates an Opening doc set for the package (Initial Disclosures).
        /// </summary>
        /// <param name="auditId"></param>
        /// <returns>Order Id - 202 Accepted</returns>
        Task GenerateOpeningDocSet(string auditId);

        /// <summary>
        /// Retrieves doc status and details for the specified order (orderID).
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns>Order Details</returns>
        Task GetOrderStatus(string orderId);

        /// <summary>
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
        Task GetOpeningPackageDeliveryDetails(string orderId, string jobId);
    }
}
