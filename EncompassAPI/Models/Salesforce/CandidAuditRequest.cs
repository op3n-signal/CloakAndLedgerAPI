namespace EncompassAPI.Models.Salesforce
{
    public class CandidAuditRequest
    {
        public string orgId { get; set; }

        public string loanId { get; set; }

        public bool econsentReceivedB { get; set; }

        public bool econsentReceivedCo { get; set; }
    }
}
