namespace EncompassAPI.Models.Encompass
{
    public class AddToDocSetRequest
    {
        public string documentType { get; set; }
        public Forms forms { get; set; }
    }

    public class Forms
    {
        public Entity scope { get; set; }
        public string[] templates { get; set; }
        public string printMode { get; set; }
    }
}
