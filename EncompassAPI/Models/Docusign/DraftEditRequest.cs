using System.ComponentModel.DataAnnotations;

namespace EncompassAPI.Models.Docusign
{
    public class DraftEditRequest
    {
        public string OrganizationId { get; set; }

        public List<Recipient> Recipients { get; set; }

        public string DocumentId { get; set; }

        public string S3RootFolderName { get; set; }

        public string S3Path { get; set; }

        public string DocumentName { get; set; }

        public string EmailSubject { get; set; }

        public string EmailMessage { get; set; }

        public string ReplyToEmail { get; set; } //need

        public string ReplyToName { get; set; } //need

        public string EmbeddedRecipientStartURL { get; set; }
    }

    public class Recipient
    {
        public string RecipientName { get; set; } //need
        public string RecipientEmail { get; set; } //need
        public int RecipientId { get; set; } //need
        public Tabs Tabs { get; set; }
    }

    public class Tabs
    {
        public List<SignHere> SignHereTabs { get; set; }
        public List<InitialHere> InitialHereTabs { get; set; }
        public List<DateSigned> DateSignedTabs { get; set; }
    }

    public class SignHere
    {
        public string DocumentId { get; set; }
        public string PageNumber { get; set; }
        public string RecipientId { get; set; }
        public string TabLabel { get; set; }
        public string XPosition { get; set; }
        public string YPosition { get; set; }
    }

    public class InitialHere
    {
        public string DocumentId { get; set; }
        public string PageNumber { get; set; }
        public string RecipientId { get; set; }
        public string TabLabel { get; set; }
        public string XPosition { get; set; }
        public string YPosition { get; set; }
    }

    public class DateSigned
    {
        public string DocumentId { get; set; }
        public string PageNumber { get; set; }
        public string RecipientId { get; set; }
        public string TabLabel { get; set; }
        public string XPosition { get; set; }
        public string YPosition { get; set; }
    }
}
