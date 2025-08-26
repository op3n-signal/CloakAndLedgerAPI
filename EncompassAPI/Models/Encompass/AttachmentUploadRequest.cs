using EncompassAPI.Models.Salesforce;

namespace EncompassAPI.Models.Encompass
{
    public class AttachmentUploadRequest
    {
        public string bucketName { get; set; }

        public string key { get; set; }

        public string orgId { get; set; }

        public Document document { get; set; }

        public AttachmentUrl attachment { get; set; }
    }

    public class AttachmentUrls
    {
        public AttachmentUrl[] attachments { get; set; }
    }

    public class AttachmentUrl
    {
        public string id { get; set; }
        public string url { get; set; }
        public string authorizationHeader { get; set; }
        public string contentType { get; set; }
    }

}
