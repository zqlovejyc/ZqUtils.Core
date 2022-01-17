#region License
/***
 * Copyright © 2018-2021, 张强 (943620963@qq.com).
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * without warranties or conditions of any kind, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using ZqUtils.Core.Extensions;

namespace ZqUtils.Core.Redis
{
    /// <summary>
    /// Redis connection pool.
    /// </summary>
    public class RedisConnectionPoolManager : IRedisConnectionPoolManager
    {
        private static readonly object _lock = new();
        private readonly IConnectionMultiplexer[] _connections;
        private readonly RedisConfiguration _redisConfiguration;
        private readonly ILogger<RedisConnectionPoolManager> _logger;
        private readonly Random random = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionPoolManager"/> class.
        /// </summary>
        /// <param name="redisConfiguration">The redis configuration.</param>
        /// <param name="logger">The logger.</param>
        public RedisConnectionPoolManager(RedisConfiguration redisConfiguration, ILogger<RedisConnectionPoolManager> logger = null)
        {
            this._redisConfiguration = redisConfiguration ?? throw new ArgumentNullException(nameof(redisConfiguration));
            this._logger = logger ?? NullLogger<RedisConnectionPoolManager>.Instance;

            if (this._connections.IsNullOrEmpty())
            {
                lock (_lock)
                {
                    if (this._connections.IsNullOrEmpty())
                    {
                        this._connections = new IConnectionMultiplexer[redisConfiguration.PoolSize];
                        this.EmitConnections();
                    }
                }
            }
        }

        /// <summary>
        /// Releasing resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releasing resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // free managed resources
                foreach (var connection in this._connections)
                    connection?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Get the Redis connection
        /// </summary>
        /// <returns>Returns an instance of<see cref="IConnectionMultiplexer"/>.</returns>
        public IConnectionMultiplexer GetConnection()
        {
            var connection = _redisConfiguration.ConnectionSelectionStrategy switch
            {
                ConnectionSelectionStrategy.Random => this._connections[random.Next(0, _redisConfiguration.PoolSize)],
                ConnectionSelectionStrategy.LeastLoaded => this._connections.OrderBy(x => x.GetCounters().TotalOutstanding).First(),
                _ => throw new Exception(nameof(_redisConfiguration.ConnectionSelectionStrategy))
            };

            _logger.LogDebug("Using connection {0} with {1} outstanding!", connection.GetHashCode(), connection.GetCounters().TotalOutstanding);

            return connection;
        }

        /// <summary>
        /// Gets the information about the connection pool
        /// </summary>
        public ConnectionPoolInformation GetConnectionInformations()
        {
            var activeConnections = 0;
            var invalidConnections = 0;

            var activeConnectionHashCodes = new List<int>();
            var invalidConnectionHashCodes = new List<int>();

            foreach (var connection in _connections)
            {
                if (!connection.IsConnected)
                {
                    invalidConnections++;
                    invalidConnectionHashCodes.Add(connection.GetHashCode());

                    continue;
                }

                activeConnections++;
                activeConnectionHashCodes.Add(connection.GetHashCode());
            }

            return new ConnectionPoolInformation()
            {
                RequiredPoolSize = _redisConfiguration.PoolSize,
                ActiveConnections = activeConnections,
                InvalidConnections = invalidConnections,
                ActiveConnectionHashCodes = activeConnectionHashCodes,
                InvalidConnectionHashCodes = invalidConnectionHashCodes
            };
        }

        /// <summary>
        /// Init all redis connections
        /// </summary>
        private void EmitConnections()
        {
            for (var i = 0; i < this._redisConfiguration.PoolSize; i++)
            {
                IConnectionMultiplexer connection = null;

                if (this._redisConfiguration.ConnectionString.IsNotNullOrEmpty())
                    connection = ConnectionMultiplexer.Connect(
                        this._redisConfiguration.ConnectionString,
                        this._redisConfiguration.ConnectLogger);

                if (this._redisConfiguration.ConfigurationOptions != null)
                    connection = ConnectionMultiplexer.Connect(
                        this._redisConfiguration.ConfigurationOptions,
                        this._redisConfiguration.ConnectLogger);

                if (connection == null)
                    throw new Exception($"Create the {i + 1} `IConnectionMultiplexer` connection fail");

                if (this._redisConfiguration.RegisterConnectionEvent)
                {
                    var hashCode = connection.GetHashCode();

                    connection.ConnectionFailed +=
                        (s, e) => _logger.LogError(e.Exception, $"Redis(hash:{hashCode}) connection error {e.FailureType}.");

                    connection.ConnectionRestored +=
                        (s, e) => _logger.LogError($"Redis(hash:{hashCode}) connection error restored.");

                    connection.InternalError +=
                        (s, e) => _logger.LogError(e.Exception, $"Redis(hash:{hashCode}) internal error {e.Origin}.");

                    connection.ErrorMessage +=
                        (s, e) => _logger.LogError($"Redis(hash:{hashCode}) error: {e.Message}");
                }

                connection.IncludeDetailInExceptions = true;

                this._redisConfiguration.Action?.Invoke(connection);

                this._connections[i] = connection;
            }
        }
    }
}
