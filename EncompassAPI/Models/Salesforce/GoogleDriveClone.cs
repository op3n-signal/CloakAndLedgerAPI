using Newtonsoft.Json;

namespace EncompassAPI.Models.Salesforce
{
    public class GoogleDriveClone
    {
        [JsonProperty("CANDIDOEM__AWS_Access_Key_Id__c")]
        public string AWS_Access_Key_Id__c { get; set; }

        [JsonProperty("CANDIDOEM__AWS_Secret_Access_Key__c")]
        public string AWS_Secret_Access_Key__c { get; set; }

        [JsonProperty("CANDIDOEM__Root_Folder_Name__c")]
        public string Root_Folder_Name__c { get; set; }
    }
}
