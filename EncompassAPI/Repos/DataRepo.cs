using Dapper;
using System.Data;

namespace EncompassAPI.Repos
{
    public class DataRepo : IDataRepo
    {
        private IDbConnection _connection;

        public DataRepo(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<T>> Select<T>(string sql)
        {
            return await _connection.QueryAsync<T>(sql);
        }

        public async Task<IEnumerable<T>> Select<T>(string sql, object objParams, CommandType commandType = CommandType.Text, int commandTimeout = 30)
        {
            return await _connection.QueryAsync<T>(sql, objParams, commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<T> SelectSingle<T>(string sql, object objParams, CommandType commandType = CommandType.Text, int commandTimeout = 30)
        {
            return await _connection.QuerySingleAsync<T>(sql, objParams, commandType: commandType, commandTimeout: commandTimeout);
        }

        public async Task<int> Insert(string sql, object objParams, CommandType commandType = CommandType.Text)
        {
            return await _connection.ExecuteAsync(sql, objParams, commandType: commandType);
        }

        public async Task<int> Delete(string sql)
        {
            return await _connection.ExecuteAsync(sql);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }

    public interface IDataRepo : IDisposable
    {
        Task<IEnumerable<T>> Select<T>(string sql);
        Task<IEnumerable<T>> Select<T>(string sql, object objParams, CommandType commandType = CommandType.Text, int commandTimeout = 30);
        Task<T> SelectSingle<T>(string sql, object objParams, CommandType commandType = CommandType.Text, int commandTimeout = 30);
        Task<int> Insert(string sql, object objParams, CommandType commandType = CommandType.Text);
        Task<int> Delete(string sql);
    }
}
