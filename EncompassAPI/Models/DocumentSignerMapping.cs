using EncompassAPI.Models.Encompass;

namespace EncompassAPI.Models
{
    public class DocumentSignerMapping
    {
        public string Id { get; set; }
        public string DocumentName { get; set; }
        public string LoanType { get; set; }
        public string State { get; set; }
        public int BorrowerCount { get; set; }
        public Signer[] Signers { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public DocumentSignerMapping()
        {
            Id = Guid.NewGuid().ToString().ToLower();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
