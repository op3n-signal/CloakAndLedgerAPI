using Amazon.S3;
using AWS.Logger;
using EncompassAPI.AppLayer;
using EncompassAPI.Models.Configs;
using EncompassAPI.Repos;
using EncompassAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EncompassAPITests
{
    public class TestHelperBase
    {
        protected IConfiguration _configuration { get; private set; }
        protected ILoggerFactory LoggerFactory { get; private set; }
        protected ServiceProvider ServiceProvider { get; private set; }

        public TestHelperBase()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            // Get CloudWatch logging configuration
            var cloudWatchConfig = GetCloudWatchLoggingConfig().Value;

            // Configure LoggerFactory with AWS CloudWatch
            ServiceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    var config = _configuration.GetAWSLoggingConfigSection();
                    builder.AddAWSProvider(config);
                    builder.SetMinimumLevel(LogLevel.Trace);
                })
                .AddSingleton<IHttpService, HttpService>()
                .AddScoped<IFileService, FileService>()
                .AddScoped<IeFolderService, eFolderService>()
                .AddScoped<ITokenRepo, TokenRepo>()
                .AddScoped(typeof(LoggerService<>), typeof(LoggerService<>))
                .AddScoped(typeof(S3Service))
                .AddScoped(typeof(AWSSecretService))
                .AddScoped<ICandidDataService, CandidDataService>()
                .AddScoped<ICandidDataRepo, CandidDataRepo>()
                .AddScoped<IUploadService, UploadService>()
                .AddAWSService<IAmazonS3>()
                .AddScoped<ISalesforceService, SalesforceService>()
                .Configure<AWSConfig>(options => _configuration.GetSection(nameof(AWSConfig)).Bind(options))
                .Configure<LoggingConfig>(options => _configuration.GetSection(nameof(LoggingConfig)).Bind(options))
                .Configure<FileConfig>(options => _configuration.GetSection(nameof(FileConfig)).Bind(options))
                .Configure<S3Config>(options => _configuration.GetSection(nameof(S3Config)).Bind(options))
                .Configure<EncompassConfig>(options => _configuration.GetSection(nameof(EncompassConfig)).Bind(options))
                .AddScoped<IDataService, DataService>(serviceProvider =>
                {
                    var awsOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AWSConfig>>();
                    var dataService = new DataService(awsOptions);
                    Task.Run(async () => await dataService.Init()).GetAwaiter().GetResult();
                    return dataService;
                })
                .AddScoped(f => f.GetRequiredService<IDataService>().GetConnection())
                .BuildServiceProvider();


            LoggerFactory = ServiceProvider.GetService<ILoggerFactory>();
        }

        public TestOptionsSnapshot<CloudWatchLoggingConfig> GetCloudWatchLoggingConfig()
        {
            // Bind the AWSConfig object to the configuration section
            var cloudWatchLoggingConfig = new CloudWatchLoggingConfig();
            _configuration.GetSection("Logging").Bind(cloudWatchLoggingConfig);

            return new TestOptionsSnapshot<CloudWatchLoggingConfig>(cloudWatchLoggingConfig);
        }

        public TestOptionsSnapshot<LoggingConfig> GetLoggingOptions()
        {
            // Bind the AWSConfig object to the configuration section
            var loggingConfig = new LoggingConfig();
            _configuration.GetSection("LoggingConfig").Bind(loggingConfig);

            return new TestOptionsSnapshot<LoggingConfig>(loggingConfig);
        }

        public TestOptionsSnapshot<FileConfig> GetFileOptions()
        {
            // Bind the AWSConfig object to the configuration section
            var fileConfig = new FileConfig();
            _configuration.GetSection("FileConfig").Bind(fileConfig);

            return new TestOptionsSnapshot<FileConfig>(fileConfig);
        }

        public TestOptionsSnapshot<LoggingConfig> GetCoreAppSettingsConfig()
        {
            var coreAppSettingsConfig = new LoggingConfig();
            _configuration.GetSection("LogSettingsConfig").Bind(coreAppSettingsConfig);

            return new TestOptionsSnapshot<LoggingConfig>(coreAppSettingsConfig);
        }

        public TestOptionsSnapshot<AWSConfig> GetAWSOptions()
        {
            var awsConfig = new AWSConfig();
            _configuration.GetSection("AWSConfig").Bind(awsConfig);

            return new TestOptionsSnapshot<AWSConfig>(awsConfig);
        }

        public TestOptionsSnapshot<EncompassConfig> GetEncompassOptions()
        {
            var encompassConfig = new EncompassConfig();
            _configuration.GetSection("EncompassConfig").Bind(encompassConfig);

            return new TestOptionsSnapshot<EncompassConfig>(encompassConfig);
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