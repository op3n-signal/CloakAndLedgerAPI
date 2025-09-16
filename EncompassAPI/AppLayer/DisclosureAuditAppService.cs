using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Newtonsoft.Json.Linq;

namespace EncompassAPI.AppLayer
{
    public class DisclosureAuditAppService : IDisclosureAuditAppService
    {
        private readonly IDisclosureAuditService _disclosureAuditService;
        private readonly IEncompassService _docsService;

        public DisclosureAuditAppService(IDisclosureAuditService disclosureAuditService, IEncompassService docsService)
        {
            _disclosureAuditService = disclosureAuditService;
            _docsService = docsService;
        }

        public async Task<ProcessResult> UpdateDisclosureIntentToProceed(CandidAuditRequest candidAuditRequest, string token)
        {
            ProcessResult processResult = new ProcessResult(true);

            // Get the resource lock id from loan
            ProcessResult<string> prRetrievedLockId = await _docsService.GetResourceLock(candidAuditRequest.orgId, candidAuditRequest.loanId, token);

            if (prRetrievedLockId.Success)
            {
                // Remove resource lock from loan
                ProcessResult prLockRemoved = await _docsService.RemoveResourceLock(candidAuditRequest.orgId, candidAuditRequest.loanId, prRetrievedLockId.Result, token);

                if (!prLockRemoved.Success)
                {
                    // If we can't remove the lock, we can't proceed
                    processResult.Success = false;
                    processResult.Message = prLockRemoved.Message;
                    return processResult;
                }
            }

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
