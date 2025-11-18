using EncompassAPI.Models.Encompass;

namespace EncompassAPI.Models
{
    public class MapRequest
    {
        public string LoanId { get; set; }

        public string OrgId { get; set; }

        public string DisclosureStage { get; set; }
    }
}
