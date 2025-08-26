namespace EncompassAPI.Models.Encompass
{
    public class EncompassIntrospection
    {
        public bool active { get; set; }
        public string scope { get; set; }
        public string client_id { get; set; }
        public string username { get; set; }
        public string token_type { get; set; }
        public long exp { get; set; }
        public string sub { get; set; }
        public string bearer_token { get; set; }
        public string encompass_instance_id { get; set; }
        public string user_name { get; set; }
        public string user_key { get; set; }
        public string encompass_user { get; set; }
        public string identity_type { get; set; }
        public string encompass_instance_type { get; set; }
        public string encompass_client_id { get; set; }
        public string realm_name { get; set; }
    }

}
