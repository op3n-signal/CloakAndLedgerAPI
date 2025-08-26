using System.Text.Json.Serialization;

namespace EncompassAPI.Models.Salesforce
{
    public class OrgRequestInfo
    {
        [JsonPropertyName("access_token")]
        public string Access_Token { get; set; }

        [JsonPropertyName("instance_url")]
        public string Instance_Url { get; set; }
    }
}
