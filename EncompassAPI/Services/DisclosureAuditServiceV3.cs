using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DisclosureAuditServiceV3 : IDisclosureAuditServiceV3
    {
        private const string ReceivedMethod = "eDisclosure";

        private readonly LoggerService<DisclosureAuditService> _logger;
        private readonly EncompassConfig _encompassConfig;
        private readonly IEncompassHttpService _http;

        public DisclosureAuditServiceV3(LoggerService<DisclosureAuditService> logger, IOptionsSnapshot<EncompassConfig> encompassOptions, IEncompassHttpService http)
        {
            _logger = logger;
            _encompassConfig = encompassOptions.Value;
            _http = http;
        }

        public async Task<ProcessResult> UpdateIntentToProceedInDisclosureLog(string orgId, string loanId, string disclosureLogId, bool containsLE, string token)
        {
            _logger.Base.LogInformation(nameof(UpdateIntentToProceedInDisclosureLog));

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLogId}";
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                object intentToProceed = new
                {
                    intent = containsLE,
                    date = currentDate,
                    receivedMethod = "Other",
                    receivedMethodOther = "Candid",
                    receivedBy = new {
                        useUserValue = false
                    }
                };

                object payload = new
                {
                    intentToProceed = intentToProceed
                };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                HttpResponseMessage response = await _http.PatchAsync(url, headers, JsonSerializer.Serialize(payload));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Base.LogError($"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                    return new ProcessResult(false, $"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                }

                _logger.Base.LogInformation($"{nameof(UpdateIntentToProceedInDisclosureLog)} Succeeded");

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to update disclosure tracking log for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to update disclosure tracking log for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult<IEnumerable<DisclosureTrackingLog>>> GetDisclosureTrackingLogs(string orgId, string loanId, string token)
        {
            _logger.Base.LogInformation(nameof(GetDisclosureTrackingLogs));

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs";

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token }
                };

                IEnumerable<DisclosureTrackingLog> ieDisclosureLogs = await _http.GetAsync<DisclosureTrackingLog[]>(url, headers);

                if (!ieDisclosureLogs.Any())
                {
                    _logger.Base.LogError($"No disclosure tracking logs found for loan {loanId} in org {orgId}.");
                    return new ProcessResult<IEnumerable<DisclosureTrackingLog>>(false, $"No disclosure tracking logs found for loan {loanId} in org {orgId}.");
                }

                if (!ieDisclosureLogs.Any(log => log.includedInTimeline == true))
                {
                    _logger.Base.LogError($"No disclosure tracking logs found that are included in timeline for loan {loanId} in org {orgId}.");
                    return new ProcessResult<IEnumerable<DisclosureTrackingLog>>(false, $"No disclosure tracking logs found that are included in timeline for loan {loanId} in org {orgId}.");
                }

                // Get all logs that are included in timeline
                ieDisclosureLogs = ieDisclosureLogs.Where(log => log.includedInTimeline == true);

                return new ProcessResult<IEnumerable<DisclosureTrackingLog>>(true)
                {
                    Result = ieDisclosureLogs
                }
                ;
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve disclosure logs for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<IEnumerable<DisclosureTrackingLog>>(false, $"Failed to retrieve disclosure logs for loan {loanId} in org {orgId}.");
            }
        }

        public ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLog(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs)
        {
            _logger.Base.LogInformation(nameof(GetLastestDisclosureTrackingLog));

            try
            {
                // Order logs by created date descending
                IOrderedEnumerable<DisclosureTrackingLog> orderedLogs = ieDisclosureLogs.OrderByDescending(log => log.disclosureCreatedDate);

                // Get the latest log
                DisclosureTrackingLog latestLog = orderedLogs.First();

                if (latestLog.disclosureType.ToLower() == "revised")
                {
                    ieDisclosureLogs = orderedLogs.TakeWhile(d => d.disclosureType.ToLower() == "revised");

                    latestLog.revision = ieDisclosureLogs.Count();
                }

                return new ProcessResult<DisclosureTrackingLog>(true)
                {
                    Result = latestLog
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DisclosureTrackingLog>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLogById(string orgId, string loanId, string logId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs)
        {
            _logger.Base.LogInformation(nameof(GetLastestDisclosureTrackingLogById));

            try
            {
                // Order logs by created date descending
                IEnumerable<DisclosureTrackingLog> matchedLogs = ieDisclosureLogs.Where(log => log.id == logId);

                // Get the latest log
                DisclosureTrackingLog selectedLog = matchedLogs.First();

                if (selectedLog.disclosureType.ToLower() == "revised")
                {
                    ieDisclosureLogs = matchedLogs.TakeWhile(d => d.disclosureType.ToLower() == "revised");

                    selectedLog.revision = ieDisclosureLogs.Count();
                }

                // Let's only proceed if the actual received date is not set
                if (selectedLog.disclosedMethod == "Other" && selectedLog.disclosedMethodDescription == "Error")
                {
                    _logger.Base.LogError($"Disclosed Method is set to {selectedLog.disclosedMethod} and description {selectedLog.disclosedMethodDescription} in the latest disclosure log {selectedLog.id} for loan {loanId} in org {orgId}.");
                    return new ProcessResult<DisclosureTrackingLog>(false, $"Disclosed Method is set to {selectedLog.disclosedMethod} and description {selectedLog.disclosedMethodDescription} in the latest disclosure log {selectedLog.id} for loan {loanId} in org {orgId}.");
                }

                _logger.LogVerbose("disclosureType: " + selectedLog.disclosureType);
                _logger.LogVerbose("disclosureType: " + JsonSerializer.Serialize(selectedLog.documents.forms));

                return new ProcessResult<DisclosureTrackingLog>(true)
                {
                    Result = selectedLog
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DisclosureTrackingLog>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLogByReceivedDate(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs)
        {
            _logger.Base.LogInformation($"Entering {nameof(GetLastestDisclosureTrackingLogByReceivedDate)}");

            try
            {
                // Order logs by created date descending
                IOrderedEnumerable<DisclosureTrackingLog> orderedLogs = ieDisclosureLogs.OrderByDescending(log => log.disclosureCreatedDate);

                // Get the latest log
                DisclosureTrackingLog latestLog = orderedLogs.First();

                return new ProcessResult<DisclosureTrackingLog>(true)
                {
                    Result = latestLog
                };
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult<DisclosureTrackingLog>(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token)
        {
            _logger.Base.LogInformation(nameof(UpdateDisclosureTrackingLogs));
            try
            {
                foreach (var log in ieDisclosureLogs)
                {
                    ProcessResult result = await UpdateDisclosureTrackingLog(orgId, loanId, log.id, log.id == latestLogId, status, includeInTimeline, token);

                    if (!result.Success)
                    {
                        return result;
                    }
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to update disclosure tracking logs for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to update disclosure tracking logs for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, List<string> logIds, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token)
        {
            try
            {
                foreach (var id in logIds)
                {
                    ProcessResult result = await UpdateDisclosureTrackingLog(orgId, loanId, id, id == latestLogId, status, includeInTimeline, token);

                    if (!result.Success)
                    {
                        return result;
                    }
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to update disclosure tracking logs for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to update disclosure tracking logs for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, List<string> logIds, string latestLogId, string description, bool includeInTimeline, string token)
        {
            try
            {
                foreach (var id in logIds)
                {
                    ProcessResult result = await UpdateDisclosureTrackingLog(orgId, loanId, id, id == latestLogId, description, includeInTimeline, token);

                    if (!result.Success)
                    {
                        return result;
                    }
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to update disclosure tracking logs for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to update disclosure tracking logs for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool isLatestLog, DisclosureStatus status, bool includeInTimeline, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(UpdateDisclosureTrackingLog)}");

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLogId}";

                dynamic payload = new System.Dynamic.ExpandoObject();
                payload.includedInTimeline = includeInTimeline;

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                HttpResponseMessage response = await _http.PatchAsync(url, headers, JsonSerializer.Serialize(payload));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Base.LogError($"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                    return new ProcessResult(false, $"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool isLatestLog, string description, bool includeInTimeline, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(UpdateDisclosureTrackingLog)}");

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLogId}";

                dynamic payload = new System.Dynamic.ExpandoObject();
                payload.includedInTimeline = includeInTimeline;

                if (isLatestLog)
                {
                    payload.disclosedMethod = CommonFields.DisclosedMethod;
                    payload.disclosedMethodDescription = description;
                }

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                HttpResponseMessage response = await _http.PatchAsync(url, headers, JsonSerializer.Serialize(payload));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Base.LogError($"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                    return new ProcessResult(false, $"Failed to update disclosure tracking log {disclosureLogId} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to retrieve documents for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to retrieve documents for loan {loanId} in org {orgId}.");
            }
        }

        public async Task<ProcessResult> UpdateReceivedDateInTrackingLog(string orgId, string loanId, bool econsentReceivedB, DateTime econsentReceivedBDate, bool econsentReceivedCo, DateTime econsentReceivedCoDate, DisclosureTrackingLog disclosureLog, string token)
        {
            _logger.Base.LogInformation(nameof(UpdateReceivedDateInTrackingLog));

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLog.id}";
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                List<object> disclosureRecipients = new List<object>();

                if (econsentReceivedB && string.IsNullOrEmpty(disclosureLog.disclosureRecipients[0].actualReceivedDate))
                {
                    disclosureRecipients.Add(new
                    {
                        id = disclosureLog.disclosureRecipients[0].id,
                        actualReceivedDate = econsentReceivedBDate,
                        disclosedMethod = "Other",
                        disclosedMethodDescription = CommonFields.AppName
                    });
                }

                if (disclosureLog.disclosureRecipients.Length > 1 && string.IsNullOrEmpty(disclosureLog.disclosureRecipients[1].actualReceivedDate) && econsentReceivedCo)
                {
                    disclosureRecipients.Add(new
                    {
                        id = disclosureLog.disclosureRecipients[1].id,
                        actualReceivedDate = econsentReceivedCoDate,
                        disclosedMethod = "Other",
                        disclosedMethodDescription = CommonFields.AppName,
                    });
                }

                object payload = new
                {
                    disclosureRecipients = disclosureRecipients
                };

                Dictionary<string, string> headers = new Dictionary<string, string>()
                {
                    { "Authorization", "Bearer " + token },
                    { "Content-Type", CommonFields.EncompassHeaders["Content-Type"] }
                };

                HttpResponseMessage response = await _http.PatchAsync(url, headers, JsonSerializer.Serialize(payload));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Base.LogError($"Failed to update disclosure tracking log {disclosureLog.id} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                    return new ProcessResult(false, $"Failed to update disclosure tracking log {disclosureLog.id} for loan {loanId} in org {orgId}. Status code: {response.StatusCode}");
                }

                return new ProcessResult(true);
            }
            catch (Exception ex)
            {
                _logger.Base.LogError($"Failed to update disclosure tracking log for loan {loanId} in org {orgId}.{Environment.NewLine}Exception: {ex}");
                return new ProcessResult(false, $"Failed to update disclosure tracking log for loan {loanId} in org {orgId}.");
            }
        }
    }

    public interface IDisclosureAuditServiceV3
    {
        Task<ProcessResult> UpdateIntentToProceedInDisclosureLog(string orgId, string loanId, string disclosureLogId, bool containsLE, string token);
        Task<ProcessResult<IEnumerable<DisclosureTrackingLog>>> GetDisclosureTrackingLogs(string orgId, string loanId, string token);
        ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLog(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs);
        ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLogById(string orgId, string loanId, string logId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs);
        Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, List<string> logIds, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, List<string> logIds, string latestLogId, string description, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool isLatestLog, DisclosureStatus status, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool isLatestLog, string description, bool includeInTimeline, string token);
        /// <summary>
        /// When this method is used pre signature, we will update the actual received date if econsent was received for borrower and/or co-borrower respectively.
        /// When this method is used post signature, we will only update the actual received date if it is not already set.
        /// </summary>
        /// <param name="orgId"></param>
        /// <param name="loanId"></param>
        /// <param name="econsentReceivedB"></param>
        /// <param name="econsentReceivedCo"></param>
        /// <param name="disclosureLog"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<ProcessResult> UpdateReceivedDateInTrackingLog(string orgId, string loanId, bool econsentReceivedB, DateTime econsentReceivedBDate, bool econsentReceivedCo, DateTime econsentReceivedCoDate, DisclosureTrackingLog disclosureLog, string token);
        ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLogByReceivedDate(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs);
    }
}
