using Dapper;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.PluginContracts
{
    public class DapperRepository
    {
        public HandlerConfig HandlerConfig { get; }

        public DapperRepository(HandlerConfig handlerConfig)
        {
            HandlerConfig = handlerConfig ?? throw new ArgumentNullException(nameof(handlerConfig));
        }

        protected IDbConnection CreateConnection()
        {
            return HandlerConfig.Connector.ToLowerInvariant() switch
            {
                "mssql" => new SqlConnection(HandlerConfig.ConnectionString),
                "plsql" => new OracleConnection(HandlerConfig.ConnectionString),
                _ => throw new NotSupportedException($"Connector type '{HandlerConfig.Connector}' is not supported.")
            };
        }

        protected async Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, Task<TResult>> action)
        {
            using var conn = CreateConnection();
            return await action(conn);
        }

        public Task<T?> GetAsync<T>(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.QueryFirstOrDefaultAsync<T>(sql, parameters));

        public Task<IEnumerable<T>> GetAllAsync<T>(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.QueryAsync<T>(sql, parameters));

        public Task<int> ExecuteAsync(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.ExecuteAsync(sql, parameters));

        public Task<T?> ExecuteScalarAsync<T>(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.ExecuteScalarAsync<T>(sql, parameters));

        public Task<dynamic?> GetAsync(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.QueryFirstOrDefaultAsync(sql, parameters));

        public Task<IEnumerable<dynamic>> GetAllAsync(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.QueryAsync(sql, parameters));

        public Task<object?> ExecuteScalarAsync(string sql, DynamicParameters? parameters = null) =>
            WithConnectionAsync(conn => conn.ExecuteScalarAsync(sql, parameters));
    }
}
