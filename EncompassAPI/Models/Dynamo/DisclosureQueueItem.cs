using Amazon.DynamoDBv2.DataModel;

namespace EncompassAPI.Models.Dynamo
{
    [DynamoDBTable("DisclosuresQueue")]
    public class DisclosureQueueItem
    {
        /// <summary>
        /// This is the disclosure tracking log id
        /// </summary>
        [DynamoDBHashKey]
        public string id { get; set; }

        public string json_request { get; set; }

        public int RetryCount { get; set; }

        public string status { get; set; }

        [DynamoDBGlobalSecondaryIndexRangeKey("Date_GSI")]
        public DateTime created_date { get; set; }

        public DateTime modified_date { get; set; }

        [DynamoDBGlobalSecondaryIndexHashKey("Date_GSI")]
        public string GSI_PK { get; set; } = "GLOBAL_DISCLOSURES";
    }
}
