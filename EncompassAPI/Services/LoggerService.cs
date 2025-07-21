using Microsoft.Extensions.Options;
using EncompassAPI.Models;

namespace EncompassAPI.Services
{
    public class LoggerService<T>
    {
        private ILogger<T> _logger;
        private LogSettingsConfig _logSettings;

        public LoggerService(ILogger<T> logger, IOptionsSnapshot<LogSettingsConfig> logOptions)
        {
            _logger = logger;
            _logSettings = logOptions.Value;
        }

        public void LogVerbose(string message)
        {
            if (_logSettings.VerboseLogging == false)
                return;

            _logger.LogInformation(message);
        }

        public ILogger<T> Base => _logger;
    }
}
