namespace Template.Models
{
    public class SecurityKeys
    {
        public string salt { get; set; }

        public string signing { get; set; }

        public string encryption { get; set; }
    }
}
