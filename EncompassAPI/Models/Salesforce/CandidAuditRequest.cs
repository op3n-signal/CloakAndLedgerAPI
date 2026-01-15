namespace EncompassAPI.Models.Salesforce
{
    public class CandidAuditRequest
    {
        public string orgId { get; set; }

        public string loanId { get; set; }

        public bool econsentReceivedB { get; set; }

        public bool econsentReceivedCo { get; set; }

        public string documentId { get; set; }

        public string envelopeId { get; set; }

        public bool autoIndexSignedDisclosures { get; set; }
    }
}
