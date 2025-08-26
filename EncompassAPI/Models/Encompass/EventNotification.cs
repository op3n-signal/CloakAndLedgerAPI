namespace EncompassAPI.Models.Encompass
{
    public class EventNotification
    {
        public string id { get; set; }
        public string type { get; set; }
        public DateTime time { get; set; }
        public Event _event { get; set; }
    }

    public class Event
    {
        public string instanceId { get; set; }
        public string clientId { get; set; }
        public string eventId { get; set; }
        public DateTime eventTime { get; set; }
        public string eventType { get; set; }
        /// <summary>
        /// If the resource is a loan, this will be the loan ID.
        /// </summary>
        public string resourceId { get; set; }
        public string resourceType { get; set; }
        public DateTime ingressTime { get; set; }
        public string subscriptionId { get; set; }
        public string status { get; set; }
        public string statusDetails { get; set; }
        public string deliveryStatus { get; set; }
    }

}
