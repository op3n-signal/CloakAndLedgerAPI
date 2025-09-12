using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;

namespace EncompassAPI.AppLayer
{
    public class DisclosureAuditAppService : IDisclosureAuditAppService
    {
        private readonly IDisclosureAuditService _disclosureAuditService;

        public DisclosureAuditAppService(IDisclosureAuditService disclosureAuditService)
        {
            _disclosureAuditService = disclosureAuditService;
        }

        public async Task<ProcessResult> UpdateDisclosureIntentToProceed(CandidAuditRequest candidAuditRequest, string token)
        {
            ProcessResult processResult = new ProcessResult(true);

            ProcessResult<IEnumerable<DisclosureTrackingLog>> prDisclosureTrackingLogs = await _disclosureAuditService.GetDisclosureTrackingLogs(candidAuditRequest.orgId, candidAuditRequest.loanId, token);

            if (!prDisclosureTrackingLogs.Success)
            {
                processResult.Success = false;
                processResult.Message = prDisclosureTrackingLogs.Message;
                return processResult;
            }

            ProcessResult<DisclosureTrackingLog> prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLogByReceivedDate(candidAuditRequest.orgId, candidAuditRequest.loanId, prDisclosureTrackingLogs.Result);

            if (!prDisclosureTrackingLog.Success)
            {
                processResult.Success = false;
                processResult.Message = prDisclosureTrackingLog.Message;
                return processResult;
            }

            bool blContainsLE = prDisclosureTrackingLog.Result.contents.Any(item => item.ToLower().Contains("le"));

            ProcessResult prUpdate = await _disclosureAuditService.UpdateIntentToProceedInDisclosureLog(candidAuditRequest.orgId, candidAuditRequest.loanId, prDisclosureTrackingLog.Result.id, blContainsLE, token);

            if (!prUpdate.Success)
            {
                processResult.Success = false;
                processResult.Message = prUpdate.Message;
                return processResult;
            }

            return processResult;
        }
    }

    public interface IDisclosureAuditAppService
    {
        Task<ProcessResult> UpdateDisclosureIntentToProceed(CandidAuditRequest candidAuditRequest, string token);
    } 
}
