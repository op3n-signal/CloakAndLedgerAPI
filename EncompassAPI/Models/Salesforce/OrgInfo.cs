using System.Text.Json.Serialization;

namespace EncompassAPI.Models.Salesforce
{
    public class OrgInfo
    {
        [JsonPropertyName("orgid")]
        public string OrgId { get; set; }

        [JsonPropertyName("username_client")]
        public string Username_Client { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("password_token")]
        public string Password_Token { get; set; }

        [JsonPropertyName("client_id")]
        public string Client_Id { get; set; }

        [JsonPropertyName("client_secret")]
        public string Client_Secret { get; set; }

        [JsonPropertyName("token_url")]
        public string Token_Url { get; set; }
    }
}
