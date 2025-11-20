namespace EncompassAPI.Models.Encompass
{
    public class LoanInfo
    {
        public LoanApplication[] applications { get; set; }
        public HMDA hmda { get; set; }
        public Property property { get; set; }
        public int expectedBorrowerCount { get; set; }
    }

    public class LoanApplication
    {
        public string id { get; set; }

        public Borrower coborrower { get; set; }
    }

    public class Borrower
    {
        public string firstName { get; set; }
    }

    public class HMDA
    {
        public string loanType { get; set; }
    }

    public class Property
    {
        public string state { get; set; }
    }
}
