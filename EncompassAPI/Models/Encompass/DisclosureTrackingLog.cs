namespace EncompassAPI.Models.Encompass
{
    public class DisclosureTrackingLog
    {
        public string id { get; set; }
        public string provider { get; set; }
        public bool includedInTimeline { get; set; }
        public string status { get; set; }
        public Discloseddate disclosedDate { get; set; }
        public Disclosedby disclosedBy { get; set; }
        public string disclosedMethod { get; set; }
        public Disclosurerecipient[] disclosureRecipients { get; set; }
        public Discloseddailyinterest disclosedDailyInterest { get; set; }
        public Disclosedapr disclosedApr { get; set; }
        public Disclosedfinancecharge disclosedFinanceCharge { get; set; }
        public string loanProgram { get; set; }
        public float loanAmount { get; set; }
        public DateTime disclosureCreatedDate { get; set; }
        public string applicationDate { get; set; }
        public Propertyaddress propertyAddress { get; set; }
        public Tracking tracking { get; set; }
        public Documents documents { get; set; }
    }

    public class Discloseddate
    {
        public DateTime computedValue { get; set; }
        public bool useUserValue { get; set; }
    }

    public class Disclosedby
    {
        public string computedName { get; set; }
        public bool useUserValue { get; set; }
        public string computedValue { get; set; }
    }

    public class Discloseddailyinterest
    {
        public bool useUserValue { get; set; }
        public float computedValue { get; set; }
    }

    public class Disclosedapr
    {
        public bool useUserValue { get; set; }
        public float computedValue { get; set; }
    }

    public class Disclosedfinancecharge
    {
        public bool useUserValue { get; set; }
        public float computedValue { get; set; }
    }

    public class Propertyaddress
    {
        public string street1 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
    }

    public class Tracking
    {
        public string[] disclosurePackageTypes { get; set; }
        public DateTime packageCreatedDate { get; set; }
        public string packageId { get; set; }
        public string docPackageId { get; set; }
    }

    public class Documents
    {
        public string viewableFormsFile { get; set; }
        public List<Form2> forms { get; set; }
    }

    public class Form2
    {
        public string formName { get; set; }
        public string formType { get; set; }
        public string signatureType { get; set; }
        public string viewableFormFile { get; set; }
    }

    public class Disclosurerecipient
    {
        public string id { get; set; }
        public string recipientType { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string disclosedMethod { get; set; }
        public Role2 role { get; set; }
    }

    public class Role2
    {
        public string entityId { get; set; }
        public string entityType { get; set; }
        public string legacyId { get; set; }
        public Application2 application { get; set; }
        public string entityDescription { get; set; }
    }

    public class Application2
    {
        public string entityId { get; set; }
        public string entityType { get; set; }
        public string legacyId { get; set; }
    }

}
