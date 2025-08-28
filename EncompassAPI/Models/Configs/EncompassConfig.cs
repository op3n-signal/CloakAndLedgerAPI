namespace EncompassAPI.Models.Configs
{
    public class EncompassConfig
    {
        public string BaseUrl { get; set; }

        public string LoanEndpointV3 { get; set; }

        public string TokenEndpoint { get; set; }   

        public string IntrospectionEndpoint { get; set; }

        public string OpeningAuditEndpoint { get; set; }

        public string OpeningDocSetEndpoint { get; set; }

        public string OpeningLoanAuditEndpoint { get; set; }

        public string GetEFolderDocsEndpoint { get; set; }
    }
}
