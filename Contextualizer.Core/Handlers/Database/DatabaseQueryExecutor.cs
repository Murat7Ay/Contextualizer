using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Contextualizer.PluginContracts;
using Dapper;

namespace Contextualizer.Core.Handlers.Database
{
    internal static class DatabaseQueryExecutor
    {
        public static IDbConnection CreateConnection(
            string connectionString,
            string connector,
            HandlerConfig handlerConfig)
        {
            var baseConnectionString = HandlerContextProcessor.ReplaceDynamicValues(
                connectionString,
                new Dictionary<string, string>()
            );

            return Contextualizer.PluginContracts.ConnectionManager.CreateConnection(
                baseConnectionString,
                connector,
                handlerConfig);
        }

        public static async Task<IEnumerable<dynamic>> ExecuteQueryAsync(
            IDbConnection connection,
            string query,
            DynamicParameters parameters,
            int? commandTimeoutSeconds)
        {
            var resolvedQuery = HandlerContextProcessor.ReplaceDynamicValues(
                query,
                new Dictionary<string, string>()
            );

            if (!DatabaseSafetyValidator.IsSafeSqlQuery(resolvedQuery))
            {
                throw new InvalidOperationException($"Unsafe SQL query blocked: {resolvedQuery}");
            }

            int commandTimeout = commandTimeoutSeconds ?? 30;
            return await connection.QueryAsync(resolvedQuery, parameters, commandTimeout: commandTimeout);
        }
    }
}
