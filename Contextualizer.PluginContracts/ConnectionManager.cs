using Contextualizer.PluginContracts;
using Microsoft.Data.SqlClient;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Concurrent;
using System.Data;

namespace Contextualizer.PluginContracts
{
    /// <summary>
    /// Manages database connections to prevent multiple connection pools for the same database
    /// </summary>
    public static class ConnectionManager
    {
        private static readonly ConcurrentDictionary<string, string> _optimizedConnectionStrings = new();

        /// <summary>
        /// Gets an optimized connection string, reusing the same optimized string for identical base connections
        /// This ensures connection pooling works properly across multiple handlers
        /// </summary>
        public static string GetOptimizedConnectionString(string baseConnectionString, string connector, HandlerConfig? config = null)
        {
            // Create a cache key based on base connection + connector + optimization settings
            string cacheKey = CreateCacheKey(baseConnectionString, connector, config);
            
            return _optimizedConnectionStrings.GetOrAdd(cacheKey, _ => 
                OptimizeConnectionString(baseConnectionString, connector, config));
        }

        /// <summary>
        /// Creates a database connection using the optimized connection string
        /// </summary>
        public static IDbConnection CreateConnection(string baseConnectionString, string connector, HandlerConfig? config = null)
        {
            string optimizedConnectionString = GetOptimizedConnectionString(baseConnectionString, connector, config);
            
            return connector.ToLowerInvariant() switch
            {
                "mssql" => new SqlConnection(optimizedConnectionString),
                "plsql" => new OracleConnection(optimizedConnectionString),
                _ => throw new NotSupportedException($"Connector type '{connector}' is not supported.")
            };
        }

        private static string CreateCacheKey(string baseConnectionString, string connector, HandlerConfig? config)
        {
            // Create a unique key based on connection parameters that affect pooling
            var keyParts = new[]
            {
                connector.ToLowerInvariant(),
                baseConnectionString,
                config?.DisablePooling?.ToString() ?? "null",
                config?.MaxPoolSize?.ToString() ?? "null", 
                config?.MinPoolSize?.ToString() ?? "null",
                config?.ConnectionTimeoutSeconds?.ToString() ?? "null"
            };
            
            return string.Join("|", keyParts);
        }

        private static string OptimizeConnectionString(string baseConnectionString, string connector, HandlerConfig? config)
        {
            try
            {
                var connectionStringBuilder = new System.Data.Common.DbConnectionStringBuilder
                {
                    ConnectionString = baseConnectionString
                };

                // Add connection timeout if configured
                if (config?.ConnectionTimeoutSeconds.HasValue == true)
                {
                    try
                    {
                        connectionStringBuilder["Connection Timeout"] = config.ConnectionTimeoutSeconds.Value.ToString();
                    }
                    catch
                    {
                        // Ignore if connection timeout is not supported
                    }
                }

                // Handle pooling settings
                if (config?.DisablePooling == true)
                {
                    try
                    {
                        // Explicitly disable pooling (useful for development/testing)
                        connectionStringBuilder["Pooling"] = "false";
                    }
                    catch
                    {
                        // Ignore if pooling control is not supported
                    }
                }
                else if (config?.MaxPoolSize.HasValue == true || config?.MinPoolSize.HasValue == true)
                {
                    try
                    {
                        // User specified pool sizes - enable pooling with custom settings
                        connectionStringBuilder["Pooling"] = "true";
                        
                        if (config.MaxPoolSize.HasValue)
                            connectionStringBuilder["Max Pool Size"] = config.MaxPoolSize.Value.ToString();
                        
                        if (config.MinPoolSize.HasValue)
                            connectionStringBuilder["Min Pool Size"] = config.MinPoolSize.Value.ToString();
                    }
                    catch
                    {
                        // If pooling settings fail, return original connection string
                        return baseConnectionString;
                    }
                }
                else
                {
                    // If no pooling configuration is provided, use conservative defaults
                    // This prevents session exhaustion issues in development environments
                    try
                    {
                        connectionStringBuilder["Pooling"] = "true";
                        connectionStringBuilder["Max Pool Size"] = "10";  // Conservative limit
                        connectionStringBuilder["Min Pool Size"] = "1";   // Keep some connections ready
                    }
                    catch
                    {
                        // If pooling settings fail, use original connection string
                    }
                }

                return connectionStringBuilder.ConnectionString;
            }
            catch
            {
                // Safe fallback - return original connection string if anything fails
                return baseConnectionString;
            }
        }

        /// <summary>
        /// Clears the connection string cache (useful for testing or configuration changes)
        /// </summary>
        public static void ClearCache()
        {
            _optimizedConnectionStrings.Clear();
        }
    }
}
