namespace CloakAndLedger.Api.Models.Configs
{
    public class CloudWatchLoggingConfig
    {
        public string LogGroup { get; set; }
        public string LogStream { get; set; }
        public string Region { get; set; }
    }
}
