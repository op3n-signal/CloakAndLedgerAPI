using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Models.Encompass;
using Microsoft.Extensions.Options;
using Shared.Models;
using System.Text.Json;

namespace EncompassAPI.Services
{
    public class DisclosureAuditService : IDisclosureAuditService
    {
        private const string ReceivedMethod = "eDisclosure";

        private readonly LoggerService<DisclosureAuditService> _logger;
        private readonly EncompassConfig _encompassConfig;
        private readonly IEncompassHttpService _http;

        public DisclosureAuditService(LoggerService<DisclosureAuditService> logger, IOptionsSnapshot<EncompassConfig> encompassOptions, IEncompassHttpService http)
        {
            _logger = logger;
            _encompassConfig = encompassOptions.Value;
            _http = http;
        }

        public async Task<ProcessResult> UpdateIntentToProceedInDisclosureLog(string orgId, string loanId, string disclosureLogId, bool containsLE, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(UpdateIntentToProceedInDisclosureLog)}");

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
            _logger.Base.LogInformation($"Entering {nameof(GetDisclosureTrackingLogs)}");

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs";

                _logger.LogVerbose("token: " + token);

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
            _logger.Base.LogInformation($"Entering {nameof(GetLastestDisclosureTrackingLog)}");

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

                // Let's only proceed if the actual received date is not set
                if (latestLog.disclosedMethod == "Other")
                {
                    _logger.Base.LogError($"Disclosed Method is set to {latestLog.disclosedMethod} and description {latestLog.disclosedMethodDescription} in the latest disclosure log {latestLog.id} for loan {loanId} in org {orgId}.");
                    return new ProcessResult<DisclosureTrackingLog>(false, $"Disclosed Method is set to {latestLog.disclosedMethod} and description {latestLog.disclosedMethodDescription} in the latest disclosure log {latestLog.id} for loan {loanId} in org {orgId}.");
                }

                _logger.LogVerbose("disclosureType: " + latestLog.disclosureType);
                _logger.LogVerbose("disclosureType: " + JsonSerializer.Serialize(latestLog.documents.forms));

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

        public ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLogByReceivedDate(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs)
        {
            _logger.Base.LogInformation($"Entering {nameof(GetLastestDisclosureTrackingLogByReceivedDate)}");

            try
            {
                // Order logs by created date descending
                IOrderedEnumerable<DisclosureTrackingLog> orderedLogs = ieDisclosureLogs.OrderByDescending(log => log.disclosureCreatedDate);

                // Get the latest log
                DisclosureTrackingLog latestLog = orderedLogs.First();

                if (string.IsNullOrEmpty(latestLog.disclosureRecipients[0].actualReceivedDate))
                {
                    _logger.Base.LogError($"No actual received date found in the latest disclosure log {latestLog.id} for loan {loanId} in org {orgId}.");
                    return new ProcessResult<DisclosureTrackingLog>(false, $"No actual received date found in the latest disclosure log {latestLog.id} for loan {loanId} in org {orgId}.");
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

        public async Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token)
        {
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

        public async Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool isLatestLog, DisclosureStatus status, bool includeInTimeline, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(UpdateDisclosureTrackingLog)}");

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLogId}";

                dynamic payload = new System.Dynamic.ExpandoObject();
                payload.includedInTimeline = includeInTimeline;

                if (isLatestLog) {
                    payload.disclosedMethod = CommonFields.DisclosedMethod;
                    payload.disclosedMethodDescription = status.ToString();
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

        public async Task<ProcessResult> UpdateReceivedDateInTrackingLog(string orgId, string loanId, DisclosureTrackingLog disclosureLog, string token)
        {
            _logger.Base.LogInformation($"Entering {nameof(UpdateReceivedDateInTrackingLog)}");

            try
            {
                // Get Disclosure tracking logs from Encompass
                string url = $"{_encompassConfig.BaseUrl}{_encompassConfig.LoanEndpointV3}/{loanId}/disclosureTracking2015Logs/{disclosureLog.id}";
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                List<object> disclosureRecipients = new List<object>();
                disclosureRecipients.Add(new
                {
                    id = disclosureLog.disclosureRecipients[0].id,
                    actualReceivedDate = currentDate,
                    disclosedMethod = "Other",
                    disclosedMethodDescription = CommonFields.AppName
                });

                if (disclosureLog.disclosureRecipients.Length > 1)
                {
                    disclosureRecipients.Add(new
                    {
                        id = disclosureLog.disclosureRecipients[1].id,
                        actualReceivedDate = currentDate,
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

    public interface IDisclosureAuditService
    {
        Task<ProcessResult> UpdateIntentToProceedInDisclosureLog(string orgId, string loanId, string disclosureLogId, bool containsLE, string token);
        Task<ProcessResult<IEnumerable<DisclosureTrackingLog>>> GetDisclosureTrackingLogs(string orgId, string loanId, string token);
        ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLog(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs);
        Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLogs(string orgId, string loanId, List<string> logIds, string latestLogId, DisclosureStatus status, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateDisclosureTrackingLog(string orgId, string loanId, string disclosureLogId, bool isLatestLog, DisclosureStatus status, bool includeInTimeline, string token);
        Task<ProcessResult> UpdateReceivedDateInTrackingLog(string orgId, string loanId, DisclosureTrackingLog disclosureLog, string token);
        ProcessResult<DisclosureTrackingLog> GetLastestDisclosureTrackingLogByReceivedDate(string orgId, string loanId, IEnumerable<DisclosureTrackingLog> ieDisclosureLogs);
    }
}
