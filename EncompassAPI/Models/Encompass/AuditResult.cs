namespace EncompassAPI.Models.Encompass
{
    public class AuditResult
    {
        public Entity entity { get; set; }
        public Entity scope { get; set; }
        public Dataauditresult dataAuditResult { get; set; }
        public string id { get; set; }
        public Loanformprintruleresult[] loanFormPrintRuleResults { get; set; }
        public Plancodeconflictresult planCodeConflictResult { get; set; }
        public string status { get; set; }
    }

    public class Dataauditresult
    {
        public Auditrulemessage[] auditRuleMessages { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }

    public class Auditrulemessage
    {
        public string description { get; set; }
        public string[] fields { get; set; }
        public string severity { get; set; }
    }

    public class Plancodeconflictresult
    {
        public object[] conflictFields { get; set; }
        public int noOfConflict { get; set; }
        public string planCodeDescription { get; set; }
        public string planCodeID { get; set; }
    }

    public class Loanformprintruleresult
    {
        public Form form { get; set; }
        public bool printable { get; set; }
    }

    public class Form
    {
        public string name { get; set; }
        public string type { get; set; }
    }

}
