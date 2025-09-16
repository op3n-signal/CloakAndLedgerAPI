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

        public string documentComments { get; set; }

        public string documentModifiedDate { get; set; }

        public string googleDriveFileId { get; set; }

        public string question { get; set; }

        public string questionAnswer { get; set; }

        public string ipAddress { get; set; }

        public string device { get; set; }

        public string browser { get; set; }

        public string documentCategory { get; set; }

        public object contactInformation { get; set; }

        public object creditCardInformation { get; set; }

        public string documentAssignedToName { get; set; }

        public string documentAssignToInitials { get; set; }

        public string contactInformationFor {  get; set; }

        public string dueDate {  get; set; }

        public bool newMessage {  get; set; }

        public string rinNameWithFirstComment {  get; set; }

        public string accountHolderName {  get; set; }

        public string accountNumber {  get; set; }

        public decimal statementHistoryDuration {  get; set; }

        public string esignStatus { get; set; }
    }
}
