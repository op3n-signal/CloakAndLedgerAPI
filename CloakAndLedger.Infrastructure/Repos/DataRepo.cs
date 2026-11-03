using CloakAndLedger.Application.Interfaces;
using CloakAndLedger.Infrastructure.Db;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CloakAndLedger.Infrastructure.Repos
{
    public class DataRepo : IDataRepo
    {
        private readonly DapperDbContext _dbContext;

        public DataRepo(DapperDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<T> SelectFirst<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _dbContext.DbConnection.QueryFirstAsync<T>(sql, objParams, commandType: commandType);
        }

        public async Task<T> SelectSingle<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _dbContext.DbConnection.QuerySingleAsync<T>(sql, objParams, commandType: commandType);
        }

        public async Task<IEnumerable<T>> Select<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _dbContext.DbConnection.QueryAsync<T>(sql, objParams, commandType: commandType);
        }

        public async Task<int> Execute(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _dbContext.DbConnection.ExecuteAsync(sql, objParams, commandType: commandType);
        }
    }
}
