using System.ComponentModel.DataAnnotations;

namespace EncompassAPI.Models.Docusign
{
    public class DraftEditRequest
    {
        public string OrganizationId { get; set; }

        public List<Recipient> Recipients { get; set; }

        public string S3RootFolderName { get; set; }

        public string S3Path { get; set; }

        public string DocumentName { get; set; }

        public string EmailSubject { get; set; }

        public string EmailMessage { get; set; }

        public string ReplyToEmail { get; set; }

        public string ReplyToName { get; set; } 

        public string Status { get; set; }
    }
}
