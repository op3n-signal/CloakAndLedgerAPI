using Amazon.Runtime.Internal;
using EncompassAPI.Models;
using EncompassAPI.Models.Dynamo;
using EncompassAPI.Models.Encompass;
using EncompassAPI.Models.Salesforce;
using EncompassAPI.Services;
using Newtonsoft.Json.Linq;
using Shared.Models;
using System.Text.Json;

namespace EncompassAPI.AppLayer
{
    public class DisclosureAuditAppService : IDisclosureAuditAppService, IAsyncDisposable
    {
        private readonly IDisclosureAuditServiceV3 _disclosureAuditService;
        private readonly IEncompassService _docsService;
        private readonly IDisclosuresQueueService _disclosuresQueueService;
        private readonly IDocumentService _documentService;

        private DisclosureStatus _status;
        private string _disclosureLogId;
        private string _token;

        public DisclosureAuditAppService(IDisclosureAuditServiceV3 disclosureAuditService, IEncompassService docsService, IDisclosuresQueueService disclosuresQueueService, IDocumentService documentService)
        {
            _disclosureAuditService = disclosureAuditService;
            _docsService = docsService;
            _disclosuresQueueService = disclosuresQueueService;
            _documentService = documentService;
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

            // Check if there is a disclosure queue item for this disclosure log id
            ProcessResult<DisclosureQueueItem> prDisclosureQueue = await _disclosuresQueueService.GetDisclosureQueueItemById(prDisclosureTrackingLog.Result.id);

            if (prDisclosureQueue.Success)
            {
                Enum.TryParse(prDisclosureQueue.Result.status, out DisclosureStatus status);

                if ((status == DisclosureStatus.PostProcess_Locked || status == DisclosureStatus.PostProcess_Failed) && !string.IsNullOrEmpty(prDisclosureTrackingLog.Result.id))
                {
                    // Set status to retrying
                    // This method is called multiple time, so in order to get around this we want to make sure that _latestLogId is not null or empty
                    // _latestLogId being not null or empty means this is a retry attempt from the lambda retry mechanism
                    await _disclosuresQueueService.UpdateStatus(prDisclosureTrackingLog.Result.id, DisclosureStatus.PostProcess_Retrying);
                }
                else if (status == DisclosureStatus.Retrieved)
                {
                    // Set status to processing
                    ProcessResult prUpsertDisclosureQueue = await _disclosuresQueueService.UpsertDisclosureQueue(prDisclosureTrackingLog.Result.id, _token, JsonSerializer.Serialize(candidAuditRequest), DisclosureStatus.PostProcess_Processing, true);

                    if (!prUpsertDisclosureQueue.Success)
                    {
                        _status = DisclosureStatus.PostProcess_Failed;
                        processResult.Success = false;
                        processResult.Message = prUpsertDisclosureQueue.Message;
                        return processResult;
                    }
                }
                else
                {
                    // exit out dont let dispose happen
                    processResult.Success = false;
                    processResult.Message = "doesn't meet criteria to retry";
                    return processResult;
                }
            }

            _disclosureLogId = prDisclosureTrackingLog.Result.id;

            // Get the resource lock id from loan
            ProcessResult<string> prRetrievedLockId = await _docsService.GetResourceLock(candidAuditRequest.orgId, candidAuditRequest.loanId, _token);

            if (prRetrievedLockId.Success)
            {
                _status = DisclosureStatus.PostProcess_Locked;
                return new ProcessResult(false, prRetrievedLockId.Message);
            }

            // Manually set econsent received to true since this is post signature
            ProcessResult prUpdateLogActualReceivedDate = await _disclosureAuditService.UpdateReceivedDateInTrackingLog(candidAuditRequest.orgId, candidAuditRequest.loanId, candidAuditRequest.econsentReceivedB, candidAuditRequest.econsentReceivedBDate, candidAuditRequest.econsentReceivedCo, candidAuditRequest.econsentReceivedCoDate, prDisclosureTrackingLog.Result, _token);

            if (!prUpdateLogActualReceivedDate.Success)
            {
                _status = DisclosureStatus.PostProcess_Failed;
                return new ProcessResult(false, prUpdateLogActualReceivedDate.Message); ;
            }

            bool blContainsIntentToProceed = prDisclosureTrackingLog.Result.documents.forms.Any(f => f.formName.ToLower().Contains("intent to proceed"));

            if (blContainsIntentToProceed)
            {
                ProcessResult prUpdate = await _disclosureAuditService.UpdateIntentToProceedInDisclosureLog(candidAuditRequest.orgId, candidAuditRequest.loanId, _disclosureLogId, _token);

                if (!prUpdate.Success)
                {
                    _status = DisclosureStatus.PostProcess_Failed;
                    processResult.Success = false;
                    processResult.Message = prUpdate.Message;
                    return processResult;
                }
            }

            if (candidAuditRequest.autoIndexSignedDisclosures == true)
            {
                ProcessResult prAutoIndex = await _documentService.AutoIndex(candidAuditRequest, _token);

                if (!prAutoIndex.Success)
                {
                    _status = DisclosureStatus.PostProcess_Failed;
                    processResult.Success = false;
                    processResult.Message = prAutoIndex.Message;
                    return processResult;
                }
            }

            _status = DisclosureStatus.Completed;
            return processResult;
        }

        public async ValueTask DisposeAsync()
        {
            if (_status != DisclosureStatus.None)
            {
                await _disclosuresQueueService.UpdateStatus(_disclosureLogId, _status);
            }
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
