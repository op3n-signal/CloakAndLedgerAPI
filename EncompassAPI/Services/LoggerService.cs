using Microsoft.Extensions.Options;
using EncompassAPI.Models.Configs;

namespace EncompassAPI.Services
{
    public class LoggerService<T>
    {
        private ILogger<T> _logger;
        private LoggingConfig _loggingConfig;

        public LoggerService(ILogger<T> logger, IOptionsSnapshot<LoggingConfig> loggingOptions)
        {
            _logger = logger;
            _loggingConfig = loggingOptions.Value;
        }

        public void LogVerbose(string message)
        {
            if (_loggingConfig.VerboseLogging == false)
                return;

            _logger.LogInformation(message);
        }

        public ILogger<T> Base => _logger;
    }
}
