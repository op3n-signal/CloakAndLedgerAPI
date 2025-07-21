using AWS.Logger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EncompassAPI.Models;

namespace EncompassAPITests
{
    public class TestHelperBase
    {
        protected IConfiguration _configuration { get; private set; }
        protected ILoggerFactory LoggerFactory { get; private set; }

        public TestHelperBase()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            // Get CloudWatch logging configuration
            var cloudWatchConfig = GetCloudWatchLoggingConfig().Value;

            // Configure LoggerFactory with AWS CloudWatch
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddAWSProvider(new AWSLoggerConfig
                    {
                        LogGroup = cloudWatchConfig.LogGroup,
                        LogStreamName = $"{cloudWatchConfig.LogStream}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}",
                        Region = cloudWatchConfig.Region,
                        BatchSizeInBytes = 1024,
                        BatchPushInterval = TimeSpan.FromMilliseconds(1)
                    });
                    builder.SetMinimumLevel(LogLevel.Information);
                })
                .BuildServiceProvider();

            LoggerFactory = serviceProvider.GetService<ILoggerFactory>();
        }

        public TestOptionsSnapshot<CloudWatchLoggingConfig> GetCloudWatchLoggingConfig()
        {
            // Bind the AWSConfig object to the configuration section
            var cloudWatchLoggingConfig = new CloudWatchLoggingConfig();
            _configuration.GetSection("Logging").Bind(cloudWatchLoggingConfig);

            return new TestOptionsSnapshot<CloudWatchLoggingConfig>(cloudWatchLoggingConfig);
        }

        public TestOptionsSnapshot<LogSettingsConfig> GetCoreAppSettingsConfig()
        {
            var coreAppSettingsConfig = new LogSettingsConfig();
            _configuration.GetSection("LogSettingsConfig").Bind(coreAppSettingsConfig);

            return new TestOptionsSnapshot<LogSettingsConfig>(coreAppSettingsConfig);
        }

        //This is just here so we can ensure all logging happens in the test
        //PROD setup will not need the Thread.Sleep
        public void Dispose()
        {
            if (LoggerFactory is IDisposable disposableLoggerFactory)
            {
                disposableLoggerFactory.Dispose(); // Simulate application shutdown
            }

            // Delay to allow logs to flush
            // Ensure logs are pushed to CloudWatch
            // If an error occurs in the test it won't log app runs to fast
            Thread.Sleep(500); // 0.5 seconds seems to work smh
        }
    }

    public class TestOptionsSnapshot<T> : IOptionsSnapshot<T> where T : class, new()
    {
        public TestOptionsSnapshot(T currentValue)
        {
            Value = currentValue;
        }

        public T Get(string name)
        {
            return Value;
        }

        public T Value { get; }
    }
}