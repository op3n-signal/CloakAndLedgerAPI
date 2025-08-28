namespace EncompassAPI.Models.Encompass
{
    public class EFolderDocument
    {
        public string id { get; set; }
        public string name { get; set; }
        public string documentType { get; set; }
        public string source { get; set; }
        public int daysDue { get; set; }
        public int daysTillExpire { get; set; }
        public string signatureType { get; set; }
        public string[] accessibleTo { get; set; }
    }

}
