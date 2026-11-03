using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CloakAndLedger.Application.Interfaces
{
    public interface IDataRepo
    {
        Task<T> SelectFirst<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text);
        Task<T> SelectSingle<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text);
        Task<IEnumerable<T>> Select<T>(string sql, object objParams = null, CommandType commandType = CommandType.Text);
        Task<int> Execute(string sql, object objParams = null, CommandType commandType = CommandType.Text);
    }
}
