using EncompassAPI.Models;
using EncompassAPI.Models.Configs;
using EncompassAPI.Repos;
using Microsoft.Extensions.Options;
using Shared;
using System.Data;
using System.Data.Common;

namespace EncompassAPI.Services
{
    public class DataService : IDataService, IDisposable
    {
        private AWSConfig _awsConfig;

        public string Connection { get; set; }

        public IDbConnection Conn { get; set; }

        public DataService(IOptionsSnapshot<AWSConfig> awsOptions)
        {
            _awsConfig = awsOptions.Value;
        }

        public async Task Init()
        {
            CandidDB candidDb = await SecretService.GetSecret<CandidDB>(_awsConfig.SecretPath + _awsConfig.CandidDbPath, "us-west-2");

            if (candidDb == null || string.IsNullOrWhiteSpace(candidDb.mySqlConnectionString))
                throw new Exception("No unify connection info found.");

            Connection = candidDb.mySqlConnectionString;
        }

        public IDataRepo GetConnection()
        {
            DbProviderFactories.RegisterFactory("MySql.Data.MySqlClient", MySqlConnector.MySqlConnectorFactory.Instance);
            DbProviderFactory factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
            Conn = factory.CreateConnection();
            Conn.ConnectionString = Connection;
            Conn.Open();
            return new DataRepo(Conn);
        }

        public void Dispose()
        {
            if (Conn != null)
            {
                Conn.Dispose();
                Conn = null;
            }
        }
    }

    public interface IDataService
    {
        string Connection { get; set; }
        Task Init();
        IDbConnection Conn { get; }
        IDataRepo GetConnection();
    }
}
