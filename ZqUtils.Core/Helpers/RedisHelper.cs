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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
using ZqUtils.Core.Redis;
/****************************
* [Author] 张强
* [Date] 2018-03-21
* [Describe] Redis工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Redis工具类，支持Redis单例连接和连接池两种模式
    /// </summary>
    public class RedisHelper
    {
        #region 私有字段
        /// <summary>
        /// 线程锁对象
        /// </summary>
        private static readonly object _lock = new();

        /// <summary>
        /// 线程锁
        /// </summary>
        private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        /// <summary>
        /// redis单例连接对象
        /// </summary>
        private static IConnectionMultiplexer _singleConnection;

        /// <summary>
        /// redis连接池
        /// </summary>
        private readonly IRedisConnectionPoolManager _poolManager;

        /// <summary>
        /// redis连接池创建的连接对象
        /// </summary>
        private readonly IConnectionMultiplexer _poolConnection;
        #endregion

        #region 公有属性
        /// <summary>
        /// 静态单例
        /// </summary>
        public static RedisHelper Instance => SingletonHelper<RedisHelper>.GetInstance();

        /// <summary>
        /// 当前Redis连接对象
        /// </summary>
        public IConnectionMultiplexer RedisConnection => _poolConnection ?? _singleConnection;

        /// <summary>
        /// Redis连接池
        /// </summary>
        public IRedisConnectionPoolManager ConnectionPoolManager => _poolManager;

        /// <summary>
        /// 数据库
        /// </summary>
        public IDatabase Database { get; set; }

        /// <summary>
        /// RedisKey的前缀
        /// </summary>
        public string KeyPrefix { get; set; } = ConfigHelper.GetValue<string>("Redis:KeyPrefix");
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connection">redis连接对象</param>
        public RedisHelper(
            IConnectionMultiplexer connection) =>
            Database = (_singleConnection = connection).GetDatabase();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultDatabase">数据库索引</param>
        /// <param name="connection">redis连接对象</param>
        public RedisHelper(
            int defaultDatabase,
            IConnectionMultiplexer connection) =>
            Database = (_singleConnection = connection).GetDatabase(defaultDatabase);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        public RedisHelper(
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            Database = GetConnection(action, log).GetDatabase();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultDatabase">数据库索引</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        public RedisHelper(
            int defaultDatabase,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            Database = GetConnection(action, log).GetDatabase(defaultDatabase);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="redisConnectionString">redis连接字符串</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        public RedisHelper(
            string redisConnectionString,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            Database = GetConnection(redisConnectionString, action, log).GetDatabase();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="redisConnectionString">redis连接字符串</param>
        /// <param name="defaultDatabase">数据库索引</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        public RedisHelper(
            string redisConnectionString,
            int defaultDatabase,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            Database = GetConnection(redisConnectionString, action, log).GetDatabase(defaultDatabase);

        /// <summary>
        /// 构造函数
        /// </summary>        
        /// <param name="configurationOptions">连接配置</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        public RedisHelper(
            ConfigurationOptions configurationOptions,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            Database = GetConnection(configurationOptions, action, log).GetDatabase();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="poolManager">redis连接池</param>
        public RedisHelper(
            IRedisConnectionPoolManager poolManager) =>
            Database = (_poolConnection = (_poolManager = poolManager).GetConnection()).GetDatabase();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="defaultDatabase">数据库索引</param>
        /// <param name="poolManager">redis连接池</param>
        public RedisHelper(
            int defaultDatabase,
            IRedisConnectionPoolManager poolManager) =>
            Database = (_poolConnection = (_poolManager = poolManager).GetConnection()).GetDatabase(defaultDatabase);
        #endregion 构造函数

        #region 连接对象
        #region GetConnection
        /// <summary>
        /// 获取redis连接对象
        /// </summary>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns>返回IConnectionMultiplexer</returns>
        public static IConnectionMultiplexer GetConnection(
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null)
        {
            var connectionStr = ConfigHelper.GetConnectionString("RedisConnectionStrings");

            if (connectionStr.IsNullOrEmpty())
                connectionStr = ConfigHelper.GetValue<string>("Redis:ConnectionStrings");

            if (connectionStr.IsNullOrEmpty())
                connectionStr = ConfigHelper.Get<string[]>("Redis:ConnectionStrings")?.FirstOrDefault();

            if (connectionStr.IsNullOrEmpty())
                throw new Exception("Redis连接字符串配置为null");

            return GetConnection(connectionStr, action, log);
        }

        /// <summary>
        /// 获取redis连接对象
        /// </summary>
        /// <param name="redisConnectionString">redis连接字符串</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns>返回IConnectionMultiplexer</returns>
        public static IConnectionMultiplexer GetConnection(
            string redisConnectionString,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null)
        {
            if (_singleConnection != null && _singleConnection.IsConnected)
                return _singleConnection;

            lock (_lock)
            {
                if (_singleConnection != null && _singleConnection.IsConnected)
                    return _singleConnection;

                _singleConnection = ConnectionMultiplexer.Connect(redisConnectionString, log);

                _singleConnection.IncludeDetailInExceptions = true;

                action?.Invoke(_singleConnection);

                RegisterEvents(_singleConnection);
            }

            return _singleConnection;
        }

        /// <summary>
        /// 获取redis连接对象
        /// </summary>
        /// <param name="configurationOptions">连接配置</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns>返回IConnectionMultiplexer</returns>
        public static IConnectionMultiplexer GetConnection(
            ConfigurationOptions configurationOptions,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null)
        {
            if (_singleConnection != null && _singleConnection.IsConnected)
                return _singleConnection;

            lock (_lock)
            {
                if (_singleConnection != null && _singleConnection.IsConnected)
                    return _singleConnection;

                _singleConnection = ConnectionMultiplexer.Connect(configurationOptions, log);

                _singleConnection.IncludeDetailInExceptions = true;

                action?.Invoke(_singleConnection);

                RegisterEvents(_singleConnection);
            }

            return _singleConnection;
        }
        #endregion

        #region GetConnectionAsync
        /// <summary>
        /// 获取redis连接对象
        /// </summary>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns>返回IConnectionMultiplexer</returns>
        public static async Task<IConnectionMultiplexer> GetConnectionAsync(
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null)
        {
            var connectionStr = ConfigHelper.GetConnectionString("RedisConnectionStrings");

            if (connectionStr.IsNullOrEmpty())
                connectionStr = ConfigHelper.GetValue<string>("Redis:ConnectionStrings");

            if (connectionStr.IsNullOrEmpty())
                connectionStr = ConfigHelper.Get<string[]>("Redis:ConnectionStrings")?.FirstOrDefault();

            if (connectionStr.IsNullOrEmpty())
                throw new Exception("Redis连接字符串配置为null");

            return await GetConnectionAsync(connectionStr, action, log);
        }

        /// <summary>
        /// 获取redis连接对象
        /// </summary>
        /// <param name="redisConnectionString">redis连接字符串</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns>返回IConnectionMultiplexer</returns>
        public static async Task<IConnectionMultiplexer> GetConnectionAsync(
            string redisConnectionString,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null)
        {
            if (_singleConnection != null && _singleConnection.IsConnected)
                return _singleConnection;

            try
            {
                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

                if (_singleConnection != null && _singleConnection.IsConnected)
                    return _singleConnection;

                _singleConnection = await ConnectionMultiplexer.ConnectAsync(redisConnectionString, log);

                _singleConnection.IncludeDetailInExceptions = true;

                action?.Invoke(_singleConnection);

                RegisterEvents(_singleConnection);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return _singleConnection;
        }

        /// <summary>
        /// 获取redis连接对象
        /// </summary>
        /// <param name="configurationOptions">连接配置</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns>返回IConnectionMultiplexer</returns>
        public static async Task<IConnectionMultiplexer> GetConnectionAsync(
            ConfigurationOptions configurationOptions,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null)
        {
            if (_singleConnection != null && _singleConnection.IsConnected)
                return _singleConnection;

            try
            {
                await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

                if (_singleConnection != null && _singleConnection.IsConnected)
                    return _singleConnection;

                _singleConnection = await ConnectionMultiplexer.ConnectAsync(configurationOptions, log);

                _singleConnection.IncludeDetailInExceptions = true;

                action?.Invoke(_singleConnection);

                RegisterEvents(_singleConnection);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return _singleConnection;
        }
        #endregion

        #region SetConnectionAsync
        /// <summary>
        /// 设置IConnectionMultiplexer
        /// </summary>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns></returns>
        public static async Task SetConnectionAsync(
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            _singleConnection = await GetConnectionAsync(action, log);

        /// <summary>
        /// 设置IConnectionMultiplexer
        /// </summary>
        /// <param name="redisConnectionString">连接字符串</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns></returns>
        public static async Task SetConnectionAsync(
            string redisConnectionString,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            _singleConnection = await GetConnectionAsync(redisConnectionString, action, log);

        /// <summary>
        /// 设置IConnectionMultiplexer
        /// </summary>
        /// <param name="configurationOptions">连接配置</param>
        /// <param name="action">自定义委托</param>
        /// <param name="log">记录redis连接日志</param>
        /// <returns></returns>
        public static async Task SetConnectionAsync(
            ConfigurationOptions configurationOptions,
            Action<IConnectionMultiplexer> action = null,
            TextWriter log = null) =>
            _singleConnection = await GetConnectionAsync(configurationOptions, action, log);
        #endregion
        #endregion

        #region Redis事务
        /// <summary>
        /// redis事务
        /// </summary>
        /// <returns>返回ITransaction</returns>
        public ITransaction GetTransaction()
        {
            return Database.CreateTransaction();
        }
        #endregion

        #region 注册事件
        /// <summary>
        /// 注册IConnectionMultiplexer事件
        /// </summary>
        private static void RegisterEvents(IConnectionMultiplexer connection)
        {
            if (ConfigHelper.GetValue<bool>("Redis:RegisterEvent"))
            {
                var hashCode = connection.GetHashCode();

                //物理连接恢复时
                connection.ConnectionRestored +=
                    (s, e) => LogHelper.Error(e.Exception, $"Redis(hash:{hashCode}) -> `ConnectionRestored`: {e.Exception?.Message}");

                //物理连接失败时
                connection.ConnectionFailed +=
                    (s, e) => LogHelper.Error(e.Exception, $"Redis(hash:{hashCode}) -> `ConnectionFailed`: {e.Exception?.Message}");

                //发生错误时
                connection.ErrorMessage +=
                    (s, e) => LogHelper.Error($"Redis(hash:{hashCode}) -> `ErrorMessage`: {e.Message}");

                //配置更改时
                connection.ConfigurationChanged +=
                    (s, e) => LogHelper.Info($"Redis(hash:{hashCode}) -> `ConfigurationChanged`: {e.EndPoint}");

                //更改集群时
                connection.HashSlotMoved +=
                    (s, e) => LogHelper.Info($"Redis(hash:{hashCode}) -> `HashSlotMoved`: {nameof(e.OldEndPoint)}-{e.OldEndPoint} To {nameof(e.NewEndPoint)}-{e.NewEndPoint}, ");

                //发生内部错误时（主要用于调试）
                connection.InternalError +=
                    (s, e) => LogHelper.Error(e.Exception, $"Redis(hash:{hashCode}) -> `InternalError`: {e.Exception?.Message}");

                //重新配置广播时（通常意味着主从同步更改）
                connection.ConfigurationChangedBroadcast +=
                    (s, e) => LogHelper.Info($"Redis(hash:{hashCode}) -> `ConfigurationChangedBroadcast`: {e.EndPoint}");
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 添加key的前缀
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>返回添加前缀后的key</returns>
        private string AddKeyPrefix(string key)
        {
            return KeyPrefix.IsNullOrWhiteSpace() ? key : $"{KeyPrefix}:{key}";
        }
        #endregion

        #region 公有方法
        #region 切换IDatabase
        /// <summary>
        /// 切换数据库，注意单例对象慎用
        /// </summary>
        /// <param name="database">数据库索引</param>
        /// <returns></returns>
        public RedisHelper UseDatabase(int database)
        {
            Database = RedisConnection.GetDatabase(database);

            return this;
        }
        #endregion

        #region 重置RedisKey前缀
        /// <summary>
        /// 重置RedisKey前缀，注意单例对象慎用
        /// </summary>
        /// <param name="keyPrefix"></param>
        /// <returns></returns>
        public RedisHelper ResetKeyPrefix(string keyPrefix = null)
        {
            KeyPrefix = keyPrefix;

            return this;
        }
        #endregion

        #region String操作
        #region 同步方法
        #region StringSet
        /// <summary>
        /// 保存字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="redisValue">字符串value</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public bool StringSet(
            string key,
            string redisValue,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.StringSet(key, redisValue, expiry, when, flags);
        }

        /// <summary>
        /// 保存一组字符串
        /// </summary>
        /// <param name="keyValuePairs">字符串集合</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public bool StringSet(
            IEnumerable<KeyValuePair<string, string>> keyValuePairs,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            var pairs = keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key), x.Value));
            return Database.StringSet(pairs.ToArray(), when, flags);
        }

        /// <summary>
        /// 保存对象为字符串
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">字符串key</param>
        /// <param name="redisValue">字符串value</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public bool StringSet<T>(
            string key,
            T redisValue,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return StringSet(key, redisValue.ToOrDefault<string>(), expiry, when, flags);

            return StringSet(key, redisValue.ToJson(), expiry, when, flags);
        }
        #endregion

        #region StringGet
        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串值</returns>
        public string StringGet(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.StringGet(key, flags);
        }

        /// <summary>
        /// 获取序反列化后的对象
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">字符串key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象</returns>
        public T StringGet<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return StringGet(key, flags).ToObject<T>();
        }
        #endregion

        #region StringIncrement
        /// <summary>
        /// 递增字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递增值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递增后的值</returns>
        public long StringIncrement(
            string key,
            long value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.StringIncrement(key, value, flags);
        }

        /// <summary>
        /// 递增字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递增值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递增后的值</returns>
        public double StringIncrement(
            string key,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.StringIncrement(key, value, flags);
        }
        #endregion

        #region StringDecrement
        /// <summary>
        /// 递减字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递减后的值</returns>
        public long StringDecrement(
            string key,
            long value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.StringDecrement(key, value, flags);
        }

        /// <summary>
        /// 递减字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递减后的值</returns>
        public double StringDecrement(
            string key,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.StringDecrement(key, value, flags);
        }
        #endregion
        #endregion

        #region 异步方法
        #region StringSetAsync
        /// <summary>
        /// 保存字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="redisValue">字符串value</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public async Task<bool> StringSetAsync(
            string key,
            string redisValue,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.StringSetAsync(key, redisValue, expiry, when, flags);
        }

        /// <summary>
        /// 保存一组字符串
        /// </summary>
        /// <param name="keyValuePairs">字符串集合</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public async Task<bool> StringSetAsync(
            IEnumerable<KeyValuePair<string, string>> keyValuePairs,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            var pairs = keyValuePairs.Select(x => new KeyValuePair<RedisKey, RedisValue>(AddKeyPrefix(x.Key), x.Value));
            return await Database.StringSetAsync(pairs.ToArray(), when, flags);
        }

        /// <summary>
        /// 保存对象为字符串
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">字符串key</param>
        /// <param name="redisValue">字符串value</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public async Task<bool> StringSetAsync<T>(
            string key,
            T redisValue,
            TimeSpan? expiry = null,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return await StringSetAsync(key, redisValue.ToOrDefault<string>(), expiry, when, flags);

            return await StringSetAsync(key, redisValue.ToJson(), expiry, when, flags);
        }
        #endregion

        #region StringGetAsync
        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串值</returns>
        public async Task<string> StringGetAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.StringGetAsync(key, flags);
        }

        /// <summary>
        /// 获取序反列化后的对象
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">字符串key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象</returns>
        public async Task<T> StringGetAsync<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return (await StringGetAsync(key, flags)).ToObject<T>();
        }
        #endregion

        #region StringIncrementAsync
        /// <summary>
        /// 递增字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递增值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递增后的值</returns>
        public async Task<long> StringIncrementAsync(
            string key,
            long value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.StringIncrementAsync(key, value, flags);
        }

        /// <summary>
        /// 递增字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递增值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递增后的值</returns>
        public async Task<double> StringIncrementAsync(
            string key,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.StringIncrementAsync(key, value, flags);
        }
        #endregion

        #region StringDecrementAsync
        /// <summary>
        /// 递减字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递减后的值</returns>
        public async Task<long> StringDecrementAsync(
            string key,
            long value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.StringDecrementAsync(key, value, flags);
        }

        /// <summary>
        /// 递减字符串
        /// </summary>
        /// <param name="key">字符串key</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回字符串递减后的值</returns>
        public async Task<double> StringDecrementAsync(
            string key,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.StringDecrementAsync(key, value, flags);
        }
        #endregion
        #endregion
        #endregion

        #region Hash操作
        #region 同步方法
        #region HashSet
        /// <summary>
        /// 保存hash字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="fieldValue">hash字段value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public bool HashSet(
            string key,
            string hashField,
            string fieldValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashSet(key, hashField, fieldValue, when, flags);
        }

        /// <summary>
        /// 保存hash字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashFields">hash字段key-value集合</param>
        /// <param name="flags">命令标志</param>
        public void HashSet(
            string key,
            IEnumerable<KeyValuePair<string, string>> hashFields,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var entries = hashFields.Select(x => new HashEntry(x.Key, x.Value));
            Database.HashSet(key, entries.ToArray(), flags);
        }

        /// <summary>
        /// 保存对象到hash中
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="fieldValue">hash字段value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public bool HashSet<T>(
            string key,
            string hashField,
            T fieldValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return HashSet(key, hashField, fieldValue.ToOrDefault<string>(), when, flags);

            return HashSet(key, hashField, fieldValue.ToJson(), when, flags);
        }
        #endregion

        #region HashGet
        /// <summary>
        /// 获取hash字段值
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回hash字段值</returns>
        public string HashGet(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashGet(key, hashField, flags);
        }

        /// <summary>
        /// 获取hash中反序列化后的对象
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象</returns>
        public T HashGet<T>(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            return HashGet(key, hashField, flags).ToObject<T>();
        }

        /// <summary>
        /// 获取hash字段值反序列化后的对象集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象集合</returns>
        public IEnumerable<T> HashGet<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            var hashFields = HashKeys(key, flags);
            return HashGet<T>(key, hashFields, flags);
        }

        /// <summary>
        /// 获取hash字段值反序列化后的对象集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="hashFields">hash字段key集合</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象集合</returns>
        public IEnumerable<T> HashGet<T>(
            string key,
            IEnumerable<string> hashFields,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var fields = hashFields.Select(x => (RedisValue)x);
            return Database.HashGet(key, fields.ToArray(), flags).Select(o => o.ToString().ToObject<T>());
        }
        #endregion

        #region HashDelete
        /// <summary>
        /// 从hash中移除指定字段
        /// </summary>
        /// <param name="key">redis存储key</param>        
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否删除成功</returns>
        public long HashDelete(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            var hashFields = HashKeys(key, flags);
            return HashDelete(key, hashFields, flags);
        }

        /// <summary>
        /// 从hash中移除指定字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否删除成功</returns>
        public bool HashDelete(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashDelete(key, hashField, flags);
        }

        /// <summary>
        /// 从hash中移除指定字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashFields">hash字段key集合</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否删除成功</returns>
        public long HashDelete(
            string key,
            IEnumerable<string> hashFields,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var fields = hashFields?.Select(x => (RedisValue)x);
            if (fields.IsNotNullOrEmpty())
                return Database.HashDelete(key, fields.ToArray(), flags);

            return 0;
        }
        #endregion

        #region HashExists
        /// <summary>
        /// 判断键值是否在hash中
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否存在</returns>
        public bool HashExists(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashExists(key, hashField, flags);
        }
        #endregion

        #region HashKeys
        /// <summary>
        /// 获取hash中指定key的所有字段key
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回hash字段key集合</returns>
        public IEnumerable<string> HashKeys(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashKeys(key, flags).Select(o => o.ToString());
        }
        #endregion

        #region HashValues
        /// <summary>
        /// 获取hash中指定key的所有字段value
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回hash字段value集合</returns>
        public IEnumerable<string> HashValues(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashValues(key, flags).Select(o => o.ToString());
        }
        #endregion

        #region HashLength
        /// <summary>
        /// 获取hash长度
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public long HashLength(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashLength(key, flags);
        }
        #endregion

        #region HashScan
        /// <summary>
        /// hash扫描
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="pattern">模式匹配key</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public IEnumerable<HashEntry> HashScan(
            string key,
            string pattern,
            int pageSize,
            CommandFlags flags)
        {
            key = AddKeyPrefix(key);
            return Database.HashScan(key, pattern, pageSize, flags);
        }

        /// <summary>
        /// hash扫描
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="pattern">模式匹配key</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="cursor">起始位置</param>
        /// <param name="pageOffset">起始偏移量</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public IEnumerable<HashEntry> HashScan(
            string key,
            string pattern,
            int pageSize = 250,
            long cursor = 0L,
            int pageOffset = 0,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashScan(key, pattern, pageSize, cursor, pageOffset, flags);
        }
        #endregion
        #endregion

        #region 异步方法
        #region HashSetAsync
        /// <summary>
        /// 保存hash字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="fieldValue">hash字段value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public async Task<bool> HashSetAsync(
            string key,
            string hashField,
            string fieldValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.HashSetAsync(key, hashField, fieldValue, when, flags);
        }

        /// <summary>
        /// 保存hash字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashFields">hash字段key-value集合</param>
        /// <param name="flags">命令标志</param>
        public async Task HashSetAsync(
            string key,
            IEnumerable<KeyValuePair<string, string>> hashFields,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var entries = hashFields.Select(x => new HashEntry(AddKeyPrefix(x.Key), x.Value));
            await Database.HashSetAsync(key, entries.ToArray(), flags);
        }

        /// <summary>
        /// 保存对象到hash中
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="fieldValue">hash字段value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否保存成功</returns>
        public async Task<bool> HashSetAsync<T>(
            string key,
            string hashField,
            T fieldValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return await HashSetAsync(key, hashField, fieldValue.ToOrDefault<string>(), when, flags);

            return await HashSetAsync(key, hashField, fieldValue.ToJson(), when, flags);
        }
        #endregion

        #region HashGetAsync
        /// <summary>
        /// 获取hash字段值
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回hash字段值</returns>
        public async Task<string> HashGetAsync(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.HashGetAsync(key, hashField, flags);
        }

        /// <summary>
        /// 获取hash中反序列化后的对象
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象</returns>
        public async Task<T> HashGetAsync<T>(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            return (await HashGetAsync(key, hashField, flags)).ToObject<T>();
        }

        /// <summary>
        /// 获取hash字段值反序列化后的对象集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象集合</returns>
        public async Task<IEnumerable<T>> HashGetAsync<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            var hashFields = await HashKeysAsync(key, flags);
            return await HashGetAsync<T>(key, hashFields, flags);
        }

        /// <summary>
        /// 获取hash字段值反序列化后的对象集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="hashFields">hash字段key集合</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回反序列化后的对象集合</returns>
        public async Task<IEnumerable<T>> HashGetAsync<T>(
            string key,
            IEnumerable<string> hashFields,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var fields = hashFields.Select(x => (RedisValue)x);
            var result = (await Database.HashGetAsync(key, fields.ToArray(), flags)).Select(o => o.ToString());
            return result.Select(o => o.ToString().ToObject<T>());
        }
        #endregion

        #region HashDeleteAsync
        /// <summary>
        /// 从hash中移除指定字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否删除成功</returns>
        public async Task<long> HashDeleteAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            var hashFields = await HashKeysAsync(key, flags);
            return await HashDeleteAsync(key, hashFields, flags);
        }

        /// <summary>
        /// 从hash中移除指定字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否删除成功</returns>
        public async Task<bool> HashDeleteAsync(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.HashDeleteAsync(key, hashField, flags);
        }

        /// <summary>
        /// 从hash中移除指定字段
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashFields">hash字段key集合</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否删除成功</returns>
        public async Task<long> HashDeleteAsync(
            string key,
            IEnumerable<string> hashFields,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var fields = hashFields?.Select(x => (RedisValue)x);
            if (fields.IsNotNullOrEmpty())
                return await Database.HashDeleteAsync(key, fields.ToArray(), flags);

            return 0;
        }
        #endregion

        #region HashExistsAsync
        /// <summary>
        /// 判断键值是否在hash中
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="hashField">hash字段key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否存在</returns>
        public async Task<bool> HashExistsAsync(
            string key,
            string hashField,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.HashExistsAsync(key, hashField, flags);
        }
        #endregion

        #region HashKeysAsync
        /// <summary>
        /// 获取hash中指定key的所有字段key
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回hash字段key集合</returns>
        public async Task<IEnumerable<string>> HashKeysAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return (await Database.HashKeysAsync(key, flags)).Select(o => o.ToString());
        }
        #endregion

        #region HashValuesAsync
        /// <summary>
        /// 获取hash中指定key的所有字段value
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回hash字段value集合</returns>
        public async Task<IEnumerable<string>> HashValuesAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return (await Database.HashValuesAsync(key, flags)).Select(o => o.ToString());
        }
        #endregion

        #region HashLengthAsync
        /// <summary>
        /// 获取hash长度
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public async Task<long> HashLengthAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.HashLengthAsync(key, flags);
        }
        #endregion

        #region HashScanAsync
        /// <summary>
        /// hash扫描
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="pattern">模式匹配key</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public IAsyncEnumerable<HashEntry> HashScanAsync(
            string key,
            string pattern,
            int pageSize,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashScanAsync(key, pattern, pageSize, flags: flags);
        }

        /// <summary>
        /// hash扫描
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="pattern">模式匹配key</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="cursor">起始位置</param>
        /// <param name="pageOffset">起始偏移量</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public IAsyncEnumerable<HashEntry> HashScanAsync(
            string key,
            string pattern,
            int pageSize = 250,
            long cursor = 0L,
            int pageOffset = 0,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.HashScanAsync(key, pattern, pageSize, cursor, pageOffset, flags);
        }
        #endregion
        #endregion
        #endregion

        #region List操作
        #region 同步方法
        #region ListLeft
        /// <summary>
        /// 在列表头部插入值，如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public long ListLeftPush(
            string key,
            string redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListLeftPush(key, redisValue, when, flags);
        }

        /// <summary>
        /// 在列表头部插入值，如果键不存在，先创建再插入值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public long ListLeftPush<T>(
            string key,
            T redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return ListLeftPush(key, redisValue.ToOrDefault<string>(), when, flags);

            return ListLeftPush(key, redisValue.ToJson(), when, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public string ListLeftPop(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListLeftPop(key, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public T ListLeftPop<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return ListLeftPop(key, flags).ToObject<T>();
        }
        #endregion

        #region ListRight
        /// <summary>
        /// 在列表尾部插入值，如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public long ListRightPush(
            string key,
            string redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListRightPush(key, redisValue, when, flags);
        }

        /// <summary>
        /// 在列表尾部插入值，如果键不存在，先创建再插入值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public long ListRightPush<T>(
            string key,
            T redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return ListRightPush(key, redisValue.ToOrDefault<string>(), when, flags);

            return ListRightPush(key, redisValue.ToJson(), when, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public string ListRightPop(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListRightPop(key, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public T ListRightPop<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return ListRightPop(key, flags).ToObject<T>();
        }
        #endregion

        #region ListRemove
        /// <summary>
        /// 移除列表指定键上与该值相同的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的数量</returns>
        public long ListRemove(
            string key,
            string redisValue,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListRemove(key, redisValue, flags: flags);
        }
        #endregion

        #region ListLength
        /// <summary>
        /// 返回列表上该键的长度，如果不存在，返回 0
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回列表的长度</returns>
        public long ListLength(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListLength(key, flags);
        }
        #endregion

        #region ListRange
        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引位置</param>
        /// <param name="stop">结束索引位置</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围内的元素集合</returns>
        public IEnumerable<string> ListRange(
            string key,
            long start = 0,
            long stop = -1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.ListRange(key, start, stop, flags).Select(o => o.ToString());
        }
        #endregion

        #region Queue
        /// <summary>
        /// 队列入队
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回入队后队列的长度</returns>
        public long EnqueueItemOnList(
            string key,
            string redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return ListRightPush(key, redisValue, when, flags);
        }

        /// <summary>
        /// 队列入队
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回入队后队列的长度</returns>
        public long EnqueueItemOnList<T>(
            string key,
            T redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return ListRightPush(key, redisValue, when, flags);
        }

        /// <summary>
        /// 队列出队
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回出队元素的值</returns>
        public string DequeueItemFromList(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return ListLeftPop(key, flags);
        }

        /// <summary>
        /// 队列出队
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回出队元素的值</returns>
        public T DequeueItemFromList<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return ListLeftPop<T>(key, flags);
        }

        /// <summary>
        /// 获取队列长度
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回队列的长度</returns>
        public long GetQueueLength(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return ListLength(key, flags);
        }
        #endregion
        #endregion

        #region 异步方法
        #region ListLeftAsync
        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public async Task<long> ListLeftPushAsync(
            string key,
            string redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.ListLeftPushAsync(key, redisValue, when, flags);
        }

        /// <summary>
        /// 在列表头部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public async Task<long> ListLeftPushAsync<T>(
            string key,
            T redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return await ListLeftPushAsync(key, redisValue.ToOrDefault<string>(), when, flags);

            return await ListLeftPushAsync(key, redisValue.ToJson(), when, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public async Task<string> ListLeftPopAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.ListLeftPopAsync(key, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的第一个元素
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public async Task<T> ListLeftPopAsync<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return (await ListLeftPopAsync(key, flags)).ToObject<T>();
        }
        #endregion

        #region ListRightAsync
        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public async Task<long> ListRightPushAsync(
            string key,
            string redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.ListRightPushAsync(key, redisValue, when, flags);
        }

        /// <summary>
        /// 在列表尾部插入值。如果键不存在，先创建再插入值
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回插入后列表的长度</returns>
        public async Task<long> ListRightPushAsync<T>(
            string key,
            T redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return await ListRightPushAsync(key, redisValue.ToOrDefault<string>(), when, flags);

            return await ListRightPushAsync(key, redisValue.ToJson(), when, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public async Task<string> ListRightPopAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.ListRightPopAsync(key, flags);
        }

        /// <summary>
        /// 移除并返回存储在该键列表的最后一个元素
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的值</returns>
        public async Task<T> ListRightPopAsync<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return (await ListRightPopAsync(key, flags)).ToObject<T>();
        }
        #endregion

        #region ListRemoveAsync
        /// <summary>
        /// 移除列表指定键上与该值相同的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素的数量</returns>
        public async Task<long> ListRemoveAsync(
            string key,
            string redisValue,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.ListRemoveAsync(key, redisValue, flags: flags);
        }
        #endregion

        #region ListLengthAsync
        /// <summary>
        /// 返回列表上该键的长度，如果不存在，返回 0
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回列表的长度</returns>
        public async Task<long> ListLengthAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.ListLengthAsync(key, flags);
        }
        #endregion

        #region ListRangeAsync
        /// <summary>
        /// 返回在该列表上键所对应的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引位置</param>
        /// <param name="stop">结束索引位置</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围内的元素集合</returns>
        public async Task<IEnumerable<string>> ListRangeAsync(
            string key,
            long start = 0,
            long stop = -1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var query = await Database.ListRangeAsync(key, start, stop, flags);
            return query.Select(x => x.ToString());
        }
        #endregion

        #region QueueAsync
        /// <summary>
        /// 队列入队
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回入队后队列的长度</returns>
        public async Task<long> EnqueueItemOnListAsync(
            string key,
            string redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return await ListRightPushAsync(key, redisValue, when, flags);
        }

        /// <summary>
        /// 队列入队
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="redisValue">redis存储value</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回入队后队列的长度</returns>
        public async Task<long> EnqueueItemOnListAsync<T>(
            string key,
            T redisValue,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            return await ListRightPushAsync(key, redisValue, when, flags);
        }

        /// <summary>
        /// 队列出队
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回出队元素的值</returns>
        public async Task<string> DequeueItemFromListAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return await ListLeftPopAsync(key, flags);
        }

        /// <summary>
        /// 队列出队
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回出队元素的值</returns>
        public async Task<T> DequeueItemFromListAsync<T>(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return await ListLeftPopAsync<T>(key, flags);
        }

        /// <summary>
        /// 获取队列长度
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回队列的长度</returns>
        public async Task<long> GetQueueLengthAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            return await ListLengthAsync(key, flags);
        }
        #endregion
        #endregion
        #endregion

        #region SortedSet操作
        #region 同步方法
        #region SortedSetAdd
        /// <summary>
        /// 新增元素到有序集合
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="score">score</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>若值已存在则返回false且score被更新，否则添加成功返回true</returns>
        public bool SortedSetAdd(
            string key,
            string member,
            double score,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetAdd(key, member, score, when, flags);
        }

        /// <summary>
        /// 新增元素到有序集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="score">score</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>若值已存在则返回false且score被更新，否则添加成功返回true</returns>
        public bool SortedSetAdd<T>(
            string key,
            T member,
            double score,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return SortedSetAdd(key, member.ToOrDefault<string>(), score, when, flags);

            return SortedSetAdd(key, member.ToJson(), score, when, flags);
        }
        #endregion

        #region SortedSetRemove
        /// <summary>
        /// 移除有序集合中指定key-value的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public bool SortedSetRemove(
            string key,
            string member,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRemove(key, member, flags);
        }

        /// <summary>
        /// 移除有序集合中指定key-value的元素
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public bool SortedSetRemove<T>(
            string key,
            T member,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return SortedSetRemove(key, member.ToOrDefault<string>(), flags);

            return SortedSetRemove(key, member.ToJson(), flags);
        }

        /// <summary>
        /// 根据起始索引位置移除有序集合中的指定范围的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引位置</param>
        /// <param name="stop">结束索引位置</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素数量</returns>
        public long SortedSetRemoveRangeByRank(
            string key,
            long start,
            long stop,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRemoveRangeByRank(key, start, stop, flags);
        }

        /// <summary>
        /// 根据score起始值移除有序集合中的指定score范围的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="exclude">要排除的开始和停止中的哪一个</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素数量</returns>
        public long SortedSetRemoveRangeByScore(
            string key,
            double start,
            double stop,
            Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRemoveRangeByScore(key, start, stop, exclude, flags);
        }

        /// <summary>
        /// 根据value最大和最小值移除有序集合中的指定value范围的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="min">value最小值</param>
        /// <param name="max">value最大值</param>
        /// <param name="exclude">要排除的最小只和最大值中的哪一个</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素数量</returns>
        public long SortedSetRemoveRangeByValue(
            string key,
            string min,
            string max,
            Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRemoveRangeByValue(key, min, max, exclude, flags);
        }
        #endregion

        #region SortedSetIncrement
        /// <summary>
        /// 按增量增加按键存储的有序集合中成员的score
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">增量值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public double SortedSetIncrement(
            string key,
            string member,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetIncrement(key, member, value, flags);
        }

        /// <summary>
        /// 按增量增加按键存储的有序集合中成员的score
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">增量值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public double SortedSetIncrement<T>(
            string key,
            T member,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return Database.SortedSetIncrement(key, member.ToOrDefault<string>(), value, flags);

            return Database.SortedSetIncrement(key, member.ToJson(), value, flags);
        }
        #endregion

        #region SortedSetDecrement
        /// <summary>
        /// 通过递减递减存储在键处的有序集中的成员的score
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public double SortedSetDecrement(
            string key,
            string member,
            double value,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetDecrement(key, member, value, flags);
        }

        /// <summary>
        /// 通过递减递减存储在键处的有序集中的成员的score
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public double SortedSetDecrement<T>(
            string key,
            T member,
            double value,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return Database.SortedSetDecrement(key, member.ToOrDefault<string>(), value, flags);

            return Database.SortedSetDecrement(key, member.ToJson(), value, flags);
        }
        #endregion

        #region SortedSetLength
        /// <summary>
        /// 获取有序集合的长度
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回有序集合的长度</returns>
        public long SortedSetLength(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetLength(key, flags: flags);
        }
        #endregion

        #region SortedSetRank
        /// <summary>
        /// 获取集合中的索引位置，从0开始
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回索引位置</returns>
        public long? SortedSetRank(
            string key,
            string member,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRank(key, member, order, flags);
        }

        /// <summary>
        /// 获取集合中的索引位置，从0开始
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回索引位置</returns>
        public long? SortedSetRank<T>(
            string key,
            T member,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return Database.SortedSetRank(key, member.ToOrDefault<string>(), order, flags);

            return Database.SortedSetRank(key, member.ToJson(), order, flags);
        }
        #endregion

        #region SortedSetScore
        /// <summary>
        /// 获取有序集合中指定元素的score
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="memebr">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定元素的score</returns>
        public double? SortedSetScore(
            string key,
            string memebr,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetScore(key, memebr, flags);
        }

        /// <summary>
        /// 获取有序集合中指定元素的score
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="memebr">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定元素的score</returns>
        public double? SortedSetScore<T>(
            string key,
            T memebr,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return Database.SortedSetScore(key, memebr.ToOrDefault<string>(), flags);

            return Database.SortedSetScore(key, memebr.ToJson(), flags);
        }
        #endregion

        #region SortedSetRange
        /// <summary>
        /// 获取有序集合中指定索引范围的元素value，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public IEnumerable<string> SortedSetRangeByRank(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRangeByRank(key, start, stop, order, flags).Select(x => x.ToString());
        }

        /// <summary>
        /// 获取有序集合中指定索引范围的元素value，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>     
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public IEnumerable<T> SortedSetRangeByRank<T>(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return SortedSetRangeByRank(key, start, stop, order, flags).Select(o => o.ToObject<T>());
        }

        /// <summary>
        /// 获取有序集合中指定索引范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>        
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public Dictionary<string, double> SortedSetRangeByRankWithScores(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var result = Database.SortedSetRangeByRankWithScores(key, start, stop, order, flags);
            return result.Select(x => new { x.Score, Value = x.Element.ToString() }).ToDictionary(x => x.Value, x => x.Score);
        }

        /// <summary>
        /// 获取有序集合中指定索引范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public Dictionary<T, double> SortedSetRangeByRankWithScores<T>(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return SortedSetRangeByRankWithScores(key, start, stop, order, flags).ToDictionary(o => o.Key.ToObject<T>(), o => o.Value);
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public IEnumerable<string> SortedSetRangeByScore(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRangeByScore(key, start, stop, order: order, skip: skip, take: take, flags: flags).Select(o => o.ToString());
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public IEnumerable<T> SortedSetRangeByScore<T>(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return SortedSetRangeByScore(key, start, stop, skip, take, order, flags).Select(o => o.ToObject<T>());
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public Dictionary<string, double> SortedSetRangeByScoreWithScores(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var result = Database.SortedSetRangeByScoreWithScores(key, start, stop, order: order, skip: skip, take: take, flags: flags);
            return result.Select(x => new { x.Score, Value = x.Element.ToString() }).ToDictionary(x => x.Value, x => x.Score);
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public Dictionary<T, double> SortedSetRangeByScoreWithScores<T>(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return SortedSetRangeByScoreWithScores(key, start, stop, skip, take, order, flags).ToDictionary(o => o.Key.ToObject<T>(), o => o.Value);
        }

        /// <summary>
        /// 获取有序集合中指定value最大最小范围的元素value，当有序集合中的score相同时，按照value从小到大进行排序
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="min">value最小值</param>
        /// <param name="max">value最大值</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public IEnumerable<string> SortedSetRangeByValue(
            string key,
            RedisValue min = default(RedisValue),
            RedisValue max = default(RedisValue),
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.SortedSetRangeByValue(key, min, max, order: order, skip: skip, take: take, flags: flags).Select(o => o.ToString());
        }

        /// <summary>
        /// 获取有序集合中指定value最大最小范围的元素value，当有序集合中的score相同时，按照value从小到大进行排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="min">value最小值</param>
        /// <param name="max">value最大值</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public IEnumerable<T> SortedSetRangeByValue<T>(
            string key,
            RedisValue min = default(RedisValue),
            RedisValue max = default(RedisValue),
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return SortedSetRangeByValue(key, min, max, skip, take, order, flags).Select(o => o.ToObject<T>());
        }
        #endregion
        #endregion

        #region 异步方法
        #region SortedSetAddAsync
        /// <summary>
        /// 新增元素到有序集合
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="score">score</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>若值已存在则返回false且score被更新，否则添加成功返回true</returns>
        public async Task<bool> SortedSetAddAsync(
            string key,
            string member,
            double score,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetAddAsync(key, member, score, when, flags);
        }

        /// <summary>
        /// 新增元素到有序集合
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="score">score</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>若值已存在则返回false且score被更新，否则添加成功返回true</returns>
        public async Task<bool> SortedSetAddAsync<T>(
            string key,
            T member,
            double score,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return await SortedSetAddAsync(key, member.ToOrDefault<string>(), score, when, flags);

            return await SortedSetAddAsync(key, member.ToJson(), score, when, flags);
        }
        #endregion

        #region SortedSetRemoveAsync
        /// <summary>
        /// 移除有序集合中指定key-value的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public async Task<bool> SortedSetRemoveAsync(
            string key,
            string member,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetRemoveAsync(key, member, flags);
        }

        /// <summary>
        /// 移除有序集合中指定key-value的元素
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public async Task<bool> SortedSetRemoveAsync<T>(
            string key,
            T member,
            CommandFlags flags = CommandFlags.None)
        {
            if (typeof(T) == typeof(string))
                return await SortedSetRemoveAsync(key, member.ToOrDefault<string>(), flags);

            return await SortedSetRemoveAsync(key, member.ToJson(), flags);
        }

        /// <summary>
        /// 根据起始索引位置移除有序集合中的指定范围的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引位置</param>
        /// <param name="stop">结束索引位置</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素数量</returns>
        public async Task<long> SortedSetRemoveRangeByRankAsync(
            string key,
            long start,
            long stop,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetRemoveRangeByRankAsync(key, start, stop, flags);
        }

        /// <summary>
        /// 根据score起始值移除有序集合中的指定score范围的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="exclude">要排除的开始和停止中的哪一个</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素数量</returns>
        public async Task<long> SortedSetRemoveRangeByScoreAsync(
            string key,
            double start,
            double stop,
            Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetRemoveRangeByScoreAsync(key, start, stop, exclude, flags);
        }

        /// <summary>
        /// 根据value最大和最小值移除有序集合中的指定value范围的元素
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="min">value最小值</param>
        /// <param name="max">value最大值</param>
        /// <param name="exclude">要排除的最小值和最大值中的哪一个</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回移除元素数量</returns>
        public async Task<long> SortedSetRemoveRangeByValueAsync(
            string key,
            string min,
            string max,
            Exclude exclude = Exclude.None,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetRemoveRangeByValueAsync(key, min, max, exclude, flags);
        }
        #endregion

        #region SortedSetIncrementAsync
        /// <summary>
        /// 按增量增加按键存储的有序集合中成员的score
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">增量值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public async Task<double> SortedSetIncrementAsync(
            string key,
            string member,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetIncrementAsync(key, member, value, flags);
        }

        /// <summary>
        /// 按增量增加按键存储的有序集合中成员的score
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">增量值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public async Task<double> SortedSetIncrementAsync<T>(
            string key,
            T member,
            double value = 1,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return await Database.SortedSetIncrementAsync(key, member.ToOrDefault<string>(), value, flags);

            return await Database.SortedSetIncrementAsync(key, member.ToJson(), value, flags);
        }
        #endregion

        #region SortedSetDecrementAsync
        /// <summary>
        /// 通过递减递减存储在键处的有序集中的成员的score
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public async Task<double> SortedSetDecrementAsync(
            string key,
            string member,
            double value,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetDecrementAsync(key, member, value, flags);
        }

        /// <summary>
        /// 通过递减递减存储在键处的有序集中的成员的score
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="value">递减值</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回新的score</returns>
        public async Task<double> SortedSetDecrementAsync<T>(
            string key,
            T member,
            double value,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return await Database.SortedSetDecrementAsync(key, member.ToOrDefault<string>(), value, flags);

            return await Database.SortedSetDecrementAsync(key, member.ToJson(), value, flags);
        }
        #endregion

        #region SortedSetLengthAsync
        /// <summary>
        /// 获取有序集合的长度
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回有序集合的长度</returns>
        public async Task<long> SortedSetLengthAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetLengthAsync(key, flags: flags);
        }
        #endregion

        #region SortedSetRankAsync
        /// <summary>
        /// 获取集合中的索引位置，从0开始
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回索引位置</returns>
        public async Task<long?> SortedSetRankAsync(
            string key,
            string member,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetRankAsync(key, member, order, flags);
        }

        /// <summary>
        /// 获取集合中的索引位置，从0开始
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="member">redis存储value</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回索引位置</returns>
        public async Task<long?> SortedSetRankAsync<T>(
            string key,
            T member,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return await Database.SortedSetRankAsync(key, member.ToOrDefault<string>(), order, flags);

            return await Database.SortedSetRankAsync(key, member.ToJson(), order, flags);
        }
        #endregion

        #region SortedSetScoreAsync
        /// <summary>
        /// 获取有序集合中指定元素的score
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="memebr">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定元素的score</returns>
        public async Task<double?> SortedSetScoreAsync(
            string key,
            string memebr,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.SortedSetScoreAsync(key, memebr, flags);
        }

        /// <summary>
        /// 获取有序集合中指定元素的score
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="memebr">redis存储value</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定元素的score</returns>
        public async Task<double?> SortedSetScoreAsync<T>(
            string key,
            T memebr,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);

            if (typeof(T) == typeof(string))
                return await Database.SortedSetScoreAsync(key, memebr.ToOrDefault<string>(), flags);

            return await Database.SortedSetScoreAsync(key, memebr.ToJson(), flags);
        }
        #endregion

        #region SortedSetRangeAsync
        /// <summary>
        /// 获取有序集合中指定索引范围的元素value，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public async Task<IEnumerable<string>> SortedSetRangeByRankAsync(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return (await Database.SortedSetRangeByRankAsync(key, start, stop, order, flags)).Select(o => o.ToString());
        }

        /// <summary>
        /// 获取有序集合中指定索引范围的元素value，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public async Task<IEnumerable<T>> SortedSetRangeByRankAsync<T>(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return (await SortedSetRangeByRankAsync(key, start, stop, order, flags)).Select(o => o.ToObject<T>());
        }

        /// <summary>
        /// 获取有序集合中指定索引范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public async Task<Dictionary<string, double>> SortedSetRangeByRankWithScoresAsync(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var result = await Database.SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags);
            return result.Select(x => new { x.Score, Value = x.Element.ToString() }).ToDictionary(x => x.Value, x => x.Score);
        }

        /// <summary>
        /// 获取有序集合中指定索引范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始索引</param>
        /// <param name="stop">结束索引</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public async Task<Dictionary<T, double>> SortedSetRangeByRankWithScoresAsync<T>(
            string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return (await SortedSetRangeByRankWithScoresAsync(key, start, stop, order, flags)).ToDictionary(o => o.Key.ToObject<T>(), o => o.Value);
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public async Task<IEnumerable<string>> SortedSetRangeByScoreAsync(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return (await Database.SortedSetRangeByScoreAsync(key, start, stop, order: order, skip: skip, take: take, flags: flags)).Select(o => o.ToString());
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public async Task<IEnumerable<T>> SortedSetRangeByScoreAsync<T>(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return (await SortedSetRangeByScoreAsync(key, start, stop, skip, take, order, flags)).Select(o => o.ToObject<T>());
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public async Task<Dictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            var result = await Database.SortedSetRangeByScoreWithScoresAsync(key, start, stop, order: order, skip: skip, take: take, flags: flags);
            return result.Select(x => new { x.Score, Value = x.Element.ToString() }).ToDictionary(x => x.Value, x => x.Score);
        }

        /// <summary>
        /// 获取有序集合中指定score起始范围的元素value-score，默认情况下从低到高
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="start">开始score</param>
        /// <param name="stop">结束score</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value-score</returns>
        public async Task<Dictionary<T, double>> SortedSetRangeByScoreWithScoresAsync<T>(
            string key,
            double start = double.NegativeInfinity,
            double stop = double.PositiveInfinity,
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return (await SortedSetRangeByScoreWithScoresAsync(key, start, stop, skip, take, order, flags)).ToDictionary(o => o.Key.ToObject<T>(), o => o.Value);
        }

        /// <summary>
        /// 获取有序集合中指定value最大最小范围的元素value，当有序集合中的score相同时，按照value从小到大进行排序
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="min">value最小值</param>
        /// <param name="max">value最大值</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public async Task<IEnumerable<string>> SortedSetRangeByValueAsync(
            string key,
            RedisValue min = default(RedisValue),
            RedisValue max = default(RedisValue),
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return (await Database.SortedSetRangeByValueAsync(key, min, max, order: order, skip: skip, take: take, flags: flags)).Select(o => o.ToString());
        }

        /// <summary>
        /// 获取有序集合中指定value最大最小范围的元素value，当有序集合中的score相同时，按照value从小到大进行排序
        /// </summary>
        /// <typeparam name="T">泛型类型</typeparam>
        /// <param name="key">redis存储key</param>
        /// <param name="min">value最小值</param>
        /// <param name="max">value最大值</param>
        /// <param name="skip">跳过元素数量</param>
        /// <param name="take">拿取元素数量</param>
        /// <param name="order">排序方式</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回指定范围的元素value</returns>
        public async Task<IEnumerable<T>> SortedSetRangeByValueAsync<T>(
            string key,
            RedisValue min = default(RedisValue),
            RedisValue max = default(RedisValue),
            long skip = 0,
            long take = -1,
            Order order = Order.Ascending,
            CommandFlags flags = CommandFlags.None)
        {
            return (await SortedSetRangeByValueAsync(key, min, max, skip, take, order, flags)).Select(o => o.ToObject<T>());
        }
        #endregion
        #endregion
        #endregion

        #region Key操作
        #region 同步方法
        #region Keys
        /// <summary>
        /// 模式匹配获取key
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="database"></param>
        /// <param name="configuredOnly"></param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public List<string> Keys(
            string pattern,
            int database = 0,
            bool configuredOnly = false,
            CommandFlags flags = CommandFlags.None)
        {
            var result = new List<string>();
            var points = RedisConnection.GetEndPoints(configuredOnly);
            if (points?.Length > 0)
            {
                foreach (var point in points)
                {
                    var server = RedisConnection.GetServer(point);
                    var keys = server.Keys(database: database, pattern: pattern, flags: flags);
                    result.AddRangeIfNotContains(keys.Select(x => (string)x).ToArray());
                }
            }

            return result;
        }
        #endregion

        #region KeyDelete
        /// <summary>
        /// 移除指定key
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public bool KeyDelete(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.KeyDelete(key, flags);
        }

        /// <summary>
        /// 移除指定key
        /// </summary>
        /// <param name="redisKeys">redis存储key集合</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public long KeyDelete(
            IEnumerable<string> redisKeys,
            CommandFlags flags = CommandFlags.None)
        {
            var keys = redisKeys?.Select(x => (RedisKey)AddKeyPrefix(x));
            if (keys.IsNotNullOrEmpty())
                return Database.KeyDelete(keys.ToArray(), flags);

            return 0;
        }

        /// <summary>
        /// 根据通配符*移除key
        /// </summary>
        /// <param name="pattern">匹配模式</param>
        /// <param name="database">数据库</param>
        /// <param name="configuredOnly">配置</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public long KeyDeleteByPattern(
            string pattern,
            int database = 0,
            bool configuredOnly = false,
            CommandFlags flags = CommandFlags.None)
        {
            var result = 0L;
            var points = RedisConnection.GetEndPoints(configuredOnly);
            if (points?.Length > 0)
            {
                var db = RedisConnection.GetDatabase(database);

                foreach (var point in points)
                {
                    var server = RedisConnection.GetServer(point);
                    var keys = server.Keys(database: database, pattern: pattern, flags: flags);

                    if (keys.IsNotNullOrEmpty())
                        result += db.KeyDelete(keys.ToArray(), flags);
                }
            }
            return result;
        }
        #endregion

        #region KeyExists
        /// <summary>
        /// 判断key是否存在
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否存在</returns>
        public bool KeyExists(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.KeyExists(key, flags);
        }
        #endregion

        #region KeyRename
        /// <summary>
        /// 重命名key
        /// </summary>
        /// <param name="key">redis存储旧key</param>
        /// <param name="redisNewKey">redis存储新key</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回重命名是否成功</returns>
        public bool KeyRename(
            string key,
            string redisNewKey,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.KeyRename(key, redisNewKey, when, flags);
        }
        #endregion

        #region KeyExpire
        /// <summary>
        /// 设置key过期时间
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否设置成功</returns>
        public bool KeyExpire(
            string key,
            TimeSpan? expiry,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.KeyExpire(key, expiry, flags);
        }
        #endregion

        #region KeyTtl
        /// <summary>
        /// 获取key过期时间
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public TimeSpan? KeyTtl(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.KeyTimeToLive(key, flags);
        }
        #endregion
        #endregion

        #region 异步方法
        #region KeysAsync
        /// <summary>
        /// 模式匹配获取key
        /// </summary>
        /// <param name="pattern">匹配模式</param>
        /// <param name="database">数据库</param>
        /// <param name="configuredOnly">是否仅返回显式配置的端点</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public async Task<List<string>> KeysAsync(
            string pattern,
            int database = 0,
            bool configuredOnly = false,
            CommandFlags flags = CommandFlags.None)
        {
            var result = new List<string>();
            var points = RedisConnection.GetEndPoints(configuredOnly);
            if (points?.Length > 0)
            {
                foreach (var point in points)
                {
                    var server = RedisConnection.GetServer(point);
                    var keys = server.KeysAsync(database: database, pattern: pattern, flags: flags);
                    await foreach (var key in keys)
                    {
                        result.AddIfNotContains(key);
                    }
                }
            }

            return result;
        }
        #endregion

        #region KeyDeleteAsync
        /// <summary>
        /// 移除指定key
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public async Task<bool> KeyDeleteAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.KeyDeleteAsync(key, flags);
        }

        /// <summary>
        /// 移除指定key
        /// </summary>
        /// <param name="redisKeys">redis存储key集合</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public async Task<long> KeyDeleteAsync(
            IEnumerable<string> redisKeys,
            CommandFlags flags = CommandFlags.None)
        {
            var keys = redisKeys.Select(x => (RedisKey)AddKeyPrefix(x));
            if (keys.IsNotNullOrEmpty())
                return await Database.KeyDeleteAsync(keys.ToArray(), flags);

            return 0;
        }

        /// <summary>
        /// 根据通配符*移除key
        /// </summary>
        /// <param name="pattern">匹配模式</param>
        /// <param name="database">数据库</param>
        /// <param name="configuredOnly">是否仅返回显式配置的端点</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否移除成功</returns>
        public async Task<long> KeyDeleteByPatternAsync(
            string pattern,
            int database = 0,
            bool configuredOnly = false,
            CommandFlags flags = CommandFlags.None)
        {
            var result = 0L;
            var points = RedisConnection.GetEndPoints(configuredOnly);
            if (points?.Length > 0)
            {
                var db = RedisConnection.GetDatabase(database);

                foreach (var point in points)
                {
                    var server = RedisConnection.GetServer(point);
                    var keys = server.KeysAsync(database: database, pattern: pattern, flags: flags);
                    var keyDeletes = new List<RedisKey>();
                    await foreach (var key in keys)
                    {
                        keyDeletes.Add(key);
                    }

                    if (keyDeletes.IsNotNullOrEmpty())
                        result += await db.KeyDeleteAsync(keyDeletes.ToArray(), flags);
                }
            }

            return result;
        }
        #endregion

        #region KeyExistsAsync
        /// <summary>
        /// 判断key是否存在
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否存在</returns>
        public async Task<bool> KeyExistsAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.KeyExistsAsync(key, flags);
        }
        #endregion

        #region KeyRenameAsync
        /// <summary>
        /// 重命名key
        /// </summary>
        /// <param name="key">redis存储旧key</param>
        /// <param name="redisNewKey">redis存储新key</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回重命名是否成功</returns>
        public async Task<bool> KeyRenameAsync(
            string key,
            string redisNewKey,
            When when = When.Always,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.KeyRenameAsync(key, redisNewKey, when, flags);
        }
        #endregion

        #region KeyExpireAsync
        /// <summary>
        /// 设置key过期时间
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回是否设置成功</returns>
        public async Task<bool> KeyExpireAsync(
            string key,
            TimeSpan? expiry,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.KeyExpireAsync(key, expiry, flags);
        }
        #endregion

        #region KeyTtlAsync
        /// <summary>
        /// 获取key过期时间
        /// </summary>
        /// <param name="key">redis存储key</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public async Task<TimeSpan?> KeyTtlAsync(
            string key,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.KeyTimeToLiveAsync(key, flags);
        }
        #endregion
        #endregion
        #endregion

        #region 清空缓存
        #region 同步方法
        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="configuredOnly">默认：false</param>
        public void Clear(bool configuredOnly = false)
        {
            var points = RedisConnection.GetEndPoints(configuredOnly);
            foreach (var point in points)
            {
                var server = RedisConnection.GetServer(point);
                server.FlushAllDatabases();
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        public void Clear(string host, int port)
        {
            var server = RedisConnection.GetServer(host, port);
            server.FlushAllDatabases();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <param name="database">数据库</param>
        /// <param name="flags">命令标志</param>
        public void Clear(
            string host,
            int port,
            int database,
            CommandFlags flags = CommandFlags.None)
        {
            var server = RedisConnection.GetServer(host, port);
            server.FlushDatabase(database, flags);
        }
        #endregion

        #region 异步方法
        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="configuredOnly">默认：false</param>
        public async Task ClearAsync(bool configuredOnly = false)
        {
            var points = RedisConnection.GetEndPoints(configuredOnly);
            foreach (var point in points)
            {
                var server = RedisConnection.GetServer(point);
                await server.FlushAllDatabasesAsync();
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        public async Task ClearAsync(string host, int port)
        {
            var server = RedisConnection.GetServer(host, port);
            await server.FlushAllDatabasesAsync();
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <param name="database">数据库</param>
        /// <param name="flags">命令标志</param>
        public async Task ClearAsync(
            string host,
            int port,
            int database,
            CommandFlags flags = CommandFlags.None)
        {
            var server = RedisConnection.GetServer(host, port);
            await server.FlushDatabaseAsync(database, flags);
        }
        #endregion
        #endregion

        #region 分布式锁
        #region 同步方法
        /// <summary>
        /// 获取redis分布式锁
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public bool LockTake(
            string key,
            string value,
            TimeSpan expiry,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.LockTake(key, value, expiry, flags);
        }

        /// <summary>
        /// 释放redis分布式锁
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public bool LockRelease(
            string key,
            string value,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return Database.LockRelease(key, value, flags);
        }
        #endregion

        #region 异步方法
        /// <summary>
        /// 获取redis分布式锁
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public async Task<bool> LockTakeAsync(
            string key,
            string value,
            TimeSpan expiry,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.LockTakeAsync(key, value, expiry, flags);
        }

        /// <summary>
        /// 释放redis分布式锁
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="flags">命令标志</param>
        /// <returns></returns>
        public async Task<bool> LockReleaseAsync(
            string key,
            string value,
            CommandFlags flags = CommandFlags.None)
        {
            key = AddKeyPrefix(key);
            return await Database.LockReleaseAsync(key, value, flags);
        }
        #endregion
        #endregion

        #region  发布/订阅
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="channel">通道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回收到消息的客户端数量</returns>
        public long Publish(
            string channel,
            string message,
            CommandFlags flags = CommandFlags.None)
        {
            var sub = RedisConnection.GetSubscriber();
            return sub.Publish(channel, message, flags);
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="channel">通道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>返回收到消息的客户端数量</returns>
        public async Task<long> PublishAsync(
            string channel,
            string message,
            CommandFlags flags = CommandFlags.None)
        {
            var sub = RedisConnection.GetSubscriber();
            return await sub.PublishAsync(channel, message, flags);
        }

        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="channelFrom">通道来源</param>
        /// <param name="subscribeFn">订阅处理委托</param>
        /// <param name="flags">命令标志</param>
        public void Subscribe(
            string channelFrom,
            Action<RedisValue> subscribeFn,
            CommandFlags flags = CommandFlags.None)
        {
            var sub = RedisConnection.GetSubscriber();
            sub.Subscribe(channelFrom, (channel, message) => subscribeFn?.Invoke(message), flags);
        }

        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="channelFrom">通道来源</param>
        /// <param name="subscribeFn">订阅处理委托</param>
        /// <param name="flags">命令标志</param>
        public async Task SubscribeAsync(
            string channelFrom,
            Action<RedisValue> subscribeFn,
            CommandFlags flags = CommandFlags.None)
        {
            var sub = RedisConnection.GetSubscriber();
            await sub.SubscribeAsync(channelFrom, (channel, message) => subscribeFn?.Invoke(message), flags);
        }
        #endregion
        #endregion
    }
}
