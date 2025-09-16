namespace EncompassAPI.Models.Docusign
{
    public class DocuSignEditLinkResponse : DocusignResponse
    {
        public string url { get; set; }
    }

    public class DocusignResponse
    {
        public bool success { get; set; }

        public string[] errors { get; set; }
    }
}
