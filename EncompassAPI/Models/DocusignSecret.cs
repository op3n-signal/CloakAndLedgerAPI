namespace EncompassAPI.Models
{
    public class DocusignSecret
    {
        public string jwt_token { get; set; }

        public string redirect {  get; set; }

        public string linkForEditorEndpoint { get; set; }

        public string requestDraftForEditingEndpoint { get; set; }

        public string requestDraftForEditingEndpointV4 { get; set; }
        public string requestDraftForEditingEndpointV5 { get; set; }
    }
}
