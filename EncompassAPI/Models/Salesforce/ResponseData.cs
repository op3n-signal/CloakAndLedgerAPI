namespace EncompassAPI.Models.Salesforce
{
    public class ResponseData
    {
        public string response { get; set; }
        public string message { get; set; }
        public List<Document> itemsNeeded { get; set; }
        public List<Document> pendingReview { get; set; }
        public List<Document> completed { get; set; }
    }
}
