namespace EncompassAPI.Models.Encompass
{
    public class AuditRequest
    {
        public Entity entity { get; set; }
        public Entity scope { get; set; }
    }

    public class Entity
    {
        public string entityid { get; set; }
        public string entitytype { get; set; }
    }
}
