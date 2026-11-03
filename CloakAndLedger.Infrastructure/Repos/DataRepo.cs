using CloakAndLedger.Application.Interfaces;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CloakAndLedger.Infrastructure.Repos
{
    public class DataRepo : IDataRepo
    {
        private IDbConnection _connection;

        public DataRepo(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<T> SelectFirst<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _connection.QueryFirstAsync<T>(sql, objParams, commandType: commandType);
        }

        public async Task<T> SelectSingle<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _connection.QuerySingleAsync<T>(sql, objParams, commandType: commandType);
        }

        public async Task<IEnumerable<T>> Select<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _connection.QueryAsync<T>(sql, objParams, commandType: commandType);
        }

        public async Task<int> Execute(string sql, object objParams = null, CommandType commandType = CommandType.Text)
        {
            return await _connection.ExecuteAsync(sql, objParams, commandType: commandType);
        }
    }
}
