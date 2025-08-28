using System.Text.Json.Serialization;

namespace EncompassAPI.Models.Salesforce
{
    public class QueryResult<T>
    {
        [JsonPropertyName("totalSize")]
        public int Total_Size { get; set; }

        [JsonPropertyName("records")]
        public T[] Records { get; set; }
    }
}
