using System.ComponentModel;

namespace EncompassAPI.Models.Salesforce
{
    public class Document
    {
        public string documentId { get; set; }

        public string documentName { get; set; }

        public string documentStatus { get; set; }

        public string documentLastActivity { get; set; }

        public string documentAssignedTo { get; set; }

        public string googleDriveFileDownloadableURL { get; set; }

        public string prospectId { get; set; }

        public bool isTask { get; set; }

        public string taskType { get; set; }

        public string borrower { get; set; }

        public string category { get; set; }

        public string tasks { get; set; }

        public string ESign_Status__c { get; set; }
    }
}
