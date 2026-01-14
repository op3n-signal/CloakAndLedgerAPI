namespace EncompassAPI.Models
{
    public class CandidIncomingRequest
    {
        public string LoanId { get; set; }

        public string OrgId { get; set; }

        public string LoId { get; set; }

        public string ProspectId { get; set; }

        public List<CandidRecipient> Recipients { get; set; }

        public string ReplyToName { get; set; }

        public string ReplyToEmail { get; set; }

        public string Path { get; set; }

        public string AssignedTo { get; set; }

        public string BorrowerValue { get; set; }
        public bool AutoIndexSignedDisclosures { get; set; }
    }

    public class CandidRecipient
    {
        public string RecipientName { get; set; }

        public string RecipientEmail { get; set; }

        public int RecipientId { get; set; }
    }
}
