namespace EncompassAPI.Models.Encompass
{
    public class DocumentOrders
    {
        public string id { get; set; }
        public string status { get; set; }
        public List<Document> documents { get; set; }
        public object[] failedDocuments { get; set; }
        public Party[] parties { get; set; }
        public string printMode { get; set; }
        public string auditId { get; set; }
    }

    public class Document
    {
        public string id { get; set; }
        public string signatureType { get; set; }
        public string type { get; set; }
        public string name { get; set; }
        public Signer[] signers { get; set; }
        public Pdf pdf { get; set; }
        public string difference { get; set; }
        public string documentSet { get; set; }
        public bool isNotarizable { get; set; }
        public Reviewer[] reviewers { get; set; }
    }

    public class Pdf
    {
        public string url { get; set; }
        public string authorizationHeader { get; set; }
    }

    public class Signer
    {
        public Field[] fields { get; set; }
        public string partyId { get; set; }
    }

    public class Field
    {
        public string name { get; set; }
        public int page { get; set; }
        public float left { get; set; }
        public float right { get; set; }
        public float top { get; set; }
        public float bottom { get; set; }
        public string type { get; set; }
        public float scale { get; set; }
    }

    public class Reviewer
    {
        public string partyId { get; set; }
    }

    public class Party
    {
        public string id { get; set; }
        public string type { get; set; }
        public string fullName { get; set; }
        public Contact contact { get; set; }
        public Entity loanEntity { get; set; }
    }

    public class Contact
    {
        public string email { get; set; }
        public string emailSecondary { get; set; }
        public string phoneHome { get; set; }
        public string phoneBusiness { get; set; }
        public string phoneCell { get; set; }
    }
}
