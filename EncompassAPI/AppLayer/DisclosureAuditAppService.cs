using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Dynamo;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Newtonsoft.Json.Linq;
using Shared.Models;

namespace EncompassAPI.AppLayer
{
    public class DisclosureAuditAppService : IDisclosureAuditAppService, IAsyncDisposable
    {
        private readonly IDisclosureAuditServiceV3 _disclosureAuditService;
        private readonly IEncompassService _docsService;
        private readonly IDisclosuresQueueService _disclosuresQueueService;

        private DisclosureStatus _status;
        private string _disclosureLogId;
        private string _token;

        public DisclosureAuditAppService(IDisclosureAuditServiceV3 disclosureAuditService, IEncompassService docsService, IDisclosuresQueueService disclosuresQueueService)
        {
            _disclosureAuditService = disclosureAuditService;
            _docsService = docsService;
            _disclosuresQueueService = disclosuresQueueService;
        }

        public void Init(string token)
        {
            _token = token;
        }

        public void Init(string token, string logId)
        {
            _token = token;
            _disclosureLogId = logId;
        }

        public async Task<ProcessResult> UpdatePostSignatureFields(CandidAuditRequest candidAuditRequest)
        {
            ProcessResult processResult = new ProcessResult(true);

            ProcessResult<IEnumerable<DisclosureTrackingLog>> prDisclosureTrackingLogs = await _disclosureAuditService.GetDisclosureTrackingLogs(candidAuditRequest.orgId, candidAuditRequest.loanId, _token);

            if (!prDisclosureTrackingLogs.Success)
            {
                processResult.Success = false;
                processResult.Message = prDisclosureTrackingLogs.Message;
                return processResult;
            }

            ProcessResult<DisclosureTrackingLog> prDisclosureTrackingLog;

            if (!string.IsNullOrEmpty(_disclosureLogId))
            {

                prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLogById(candidAuditRequest.orgId, candidAuditRequest.loanId, _disclosureLogId, prDisclosureTrackingLogs.Result);
            }
            else
            {
                // We need the latest disclosure tracking log. This is the log that is generated automatically from the print function
                prDisclosureTrackingLog = _disclosureAuditService.GetLastestDisclosureTrackingLogByReceivedDate(candidAuditRequest.orgId, candidAuditRequest.loanId, prDisclosureTrackingLogs.Result);
            }

            if (!prDisclosureTrackingLog.Success)
            {
                processResult.Success = false;
                processResult.Message = prDisclosureTrackingLog.Message;
                return processResult;
            }

            _disclosureLogId = prDisclosureTrackingLog.Result.id;

            ProcessResult prUpsertDisclosureQueue = await _disclosuresQueueService.UpsertDisclosureQueue(_disclosureLogId, _token, candidAuditRequest, DisclosureStatus.PostSignature_Processing);

            if(!prUpsertDisclosureQueue.Success)
            {
                _status = DisclosureStatus.PostSignature_Failed;
                processResult.Success = false;
                processResult.Message = prUpsertDisclosureQueue.Message;
                return processResult;
            }

            bool blContainsLE = prDisclosureTrackingLog.Result.contents.Any(item => item.ToLower().Contains("le"));

            // Get the resource lock id from loan
            ProcessResult<string> prRetrievedLockId = await _docsService.GetResourceLock(candidAuditRequest.orgId, candidAuditRequest.loanId, _token);

            if (prRetrievedLockId.Success)
            {
                _status = DisclosureStatus.PostSignature_Locked;
                return new ProcessResult(false, prRetrievedLockId.Message);
            }

            ProcessResult prUpdateLogActualReceivedDate = await _disclosureAuditService.UpdateReceivedDateInTrackingLog(candidAuditRequest.orgId, candidAuditRequest.loanId, prDisclosureTrackingLog.Result, _token);

            if (!prUpdateLogActualReceivedDate.Success)
            {
                _status = DisclosureStatus.PostSignature_Failed;
                return new ProcessResult(false, prUpdateLogActualReceivedDate.Message); ;
            }

            ProcessResult prUpdate = await _disclosureAuditService.UpdateIntentToProceedInDisclosureLog(candidAuditRequest.orgId, candidAuditRequest.loanId, _disclosureLogId, blContainsLE, _token);

            if (!prUpdate.Success)
            {
                _status = DisclosureStatus.PostSignature_Failed;
                processResult.Success = false;
                processResult.Message = prUpdate.Message;
                return processResult;
            }

            _status = DisclosureStatus.Completed;
            return processResult;
        }

        public async ValueTask DisposeAsync()
        {
            if (_status != DisclosureStatus.Dead)
                await _disclosuresQueueService.UpdateStatus(_disclosureLogId, _status);
        }
    }

    public interface IDisclosureAuditAppService
    {
        void Init(string token);
        void Init(string token, string logId);
        Task<ProcessResult> UpdatePostSignatureFields(CandidAuditRequest candidAuditRequest);
        ValueTask DisposeAsync();
    } 
}
