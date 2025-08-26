namespace EncompassAPI.Models.Encompass
{
    public class EncompassDocument
    {
        public string id { get; set; }
        public string title { get; set; }
        public bool isMarkedRemoved { get; set; }
        public string documentStatus { get; set; }
        public DateTime statusDate { get; set; }
        public string status { get; set; }
        public Receivedby receivedBy { get; set; }
        public DateTime receivedDate { get; set; }
        public Requestedby requestedBy { get; set; }
        public DateTime requestedDate { get; set; }
        public DateTime createdDate { get; set; }
        public Createdby createdBy { get; set; }
        public DateTime updatedDate { get; set; }
        public DateTime lastAttachmentDate { get; set; }
        public bool isProtected { get; set; }
        public Application application { get; set; }
        public Milestone milestone { get; set; }
        public Role[] roles { get; set; }
        public string[] accessibleTo { get; set; }
        public string[] documentTypes { get; set; }
        public Attachment[] attachments { get; set; }
        public int daysTillExpire { get; set; }
    }

    public class Receivedby
    {
        public string entityId { get; set; }
        public string entityType { get; set; }
    }

    public class Requestedby
    {
        public string entityId { get; set; }
        public string entityType { get; set; }
    }

    public class Createdby
    {
        public string entityId { get; set; }
        public string entityType { get; set; }
    }

    public class Application
    {
        public string entityId { get; set; }
        public string entityName { get; set; }
        public string entityType { get; set; }
        public string legacyId { get; set; }
    }

    public class Milestone
    {
        public string entityId { get; set; }
        public string entityName { get; set; }
        public string entityType { get; set; }
    }

    public class Role
    {
        public string entityId { get; set; }
        public string entityName { get; set; }
        public string entityType { get; set; }
        public string roleAbbr { get; set; }
    }

    public class Attachment
    {
        public string entityId { get; set; }
        public string entityName { get; set; }
        public string entityType { get; set; }
        public bool isActive { get; set; }
        public DateTime createdDate { get; set; }
        public int fileSize { get; set; }
    }

}
