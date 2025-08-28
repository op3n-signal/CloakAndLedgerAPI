namespace EncompassAPI.Models.Encompass
{
    public class LoanInfo
    {
        public LoanApplication[] applications { get; set; }
    }

    public class LoanApplication
    {
        public string id { get; set; }
    }

}
