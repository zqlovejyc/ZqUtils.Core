#region License
/***
 * Copyright © 2018-2022, 张强 (943620963@qq.com).
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

using StackExchange.Redis;
using System;

namespace ZqUtils.Core.Redis
{
    /// <summary>
    /// The service who handles the Redis connection pool.
    /// </summary>
    public interface IRedisConnectionPoolManager : IDisposable
    {
        /// <summary>
        /// Get the Redis connection
        /// </summary>
        /// <returns>Returns an instance of<see cref="IConnectionMultiplexer"/>.</returns>
        IConnectionMultiplexer GetConnection();

        /// <summary>
        /// Gets the information about the connection pool
        /// </summary>
        ConnectionPoolInformation GetConnectionInformations();
    }
}
