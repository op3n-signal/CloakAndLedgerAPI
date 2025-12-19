using System.ComponentModel.DataAnnotations;

namespace EncompassAPI.Models.Docusign
{
    public class DraftEditRequestV4 : DraftEditRequest
    {
      
    }

    public class DraftEditRequestV5 : DraftEditRequest
    {
        public Dictionary<string, string> DocumentIdMap { get; set; }
        public string ProspectId { get; set; }
    }

    public class Recipient
    {
        public string RecipientName { get; set; } 
        public string RecipientEmail { get; set; } 
        public int RecipientId { get; set; }
        public Tabs Tabs { get; set; } = new Tabs();
    }

    public class Tabs
    {
        public List<SignHere> SignHereTabs { get; set; } = new List<SignHere>();
        public List<InitialHere> InitialHereTabs { get; set; } = new List<InitialHere>();
        public List<DateSigned> DateSignedTabs { get; set; } = new List<DateSigned>();
    }

    public class SignHere
    {
        public string DocumentId { get; set; }
        public string PageNumber { get; set; }
        public string RecipientId { get; set; }
        public string TabLabel { get; set; }
        public string XPosition { get; set; }
        public string YPosition { get; set; }
        public string ScaleValue { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string AnchorString { get; set; }
        public string AnchorXOffset { get; set; }
        public string AnchorYOffset { get; set; }
        public string AnchorIgnoreIfNotPresent { get; set; }
        public string AnchorUnits { get; set; }
        public string AnchorCaseSensitive { get; set; }
        public string AnchorMatchWholeWord { get; set; }
    }

    public class InitialHere
    {
        public string DocumentId { get; set; }
        public string PageNumber { get; set; }
        public string RecipientId { get; set; }
        public string TabLabel { get; set; }
        public string XPosition { get; set; }
        public string YPosition { get; set; }
        public string ScaleValue { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string AnchorString { get; set; }
        public string AnchorXOffset { get; set; }
        public string AnchorYOffset { get; set; }
        public string AnchorIgnoreIfNotPresent { get; set; }
        public string AnchorUnits { get; set; }
        public string AnchorCaseSensitive { get; set; }
        public string AnchorMatchWholeWord { get; set; }
    }

    public class DateSigned
    {
        public string DocumentId { get; set; }
        public string PageNumber { get; set; }
        public string RecipientId { get; set; }
        public string TabLabel { get; set; }
        public string XPosition { get; set; }
        public string YPosition { get; set; }
        public string ScaleValue { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public string AnchorString { get; set; }
        public string AnchorXOffset { get; set; }
        public string AnchorYOffset { get; set; }
        public string AnchorIgnoreIfNotPresent { get; set; }
        public string AnchorUnits { get; set; }
        public string AnchorCaseSensitive { get; set; }
        public string AnchorMatchWholeWord { get; set; }
    }
}
