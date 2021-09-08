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

using Microsoft.Extensions.Configuration;
using NATS.Client;
using System;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
/****************************
* [Author] 张强
* [Date] 2021-09-08
* [Describe] NATS工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// NATS工具类
    /// </summary>
    public class NatsHelper : IDisposable
    {
        #region 私有字段
        /// <summary>
        /// 配置
        /// </summary>
        private readonly Options _options;

        /// <summary>
        /// 线程对象，线程锁使用
        /// </summary>
        private static readonly object _locker = new();
        #endregion

        #region 公有属性
        /// <summary>
        /// NATS连接对象
        /// </summary>
        public IConnection NatsConnection { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">appsettings配置</param>
        public NatsHelper(IConfiguration configuration)
        {
            if (NatsConnection != null)
                return;

            lock (_locker)
            {
                _options = configuration.GetSection("NatsConfig").Get<Options>();
                NatsConnection = new ConnectionFactory().CreateConnection(_options);
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connection">NATS连接对象</param>
        public NatsHelper(IConnection connection)
        {
            if (NatsConnection != null)
                return;

            lock (_locker)
            {
                NatsConnection = connection;
                _options = NatsConnection.Opts;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options">NATS连接配置</param>
        public NatsHelper(Options options)
        {
            if (NatsConnection != null)
                return;

            lock (_locker)
            {
                NatsConnection = new ConnectionFactory().CreateConnection(options);
                _options = NatsConnection.Opts;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">NATS连接字符串</param>
        public NatsHelper(string url)
        {
            if (NatsConnection != null)
                return;

            lock (_locker)
            {
                NatsConnection = new ConnectionFactory().CreateConnection(url);
                _options = NatsConnection.Opts;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">NATS连接字符串</param>
        /// <param name="credentialsPath">证书路径</param>
        public NatsHelper(string url, string credentialsPath)
        {
            if (NatsConnection != null)
                return;

            lock (_locker)
            {
                NatsConnection = new ConnectionFactory().CreateConnection(url, credentialsPath);
                _options = NatsConnection.Opts;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="url">NATS连接字符串</param>
        /// <param name="jwt">JWT凭证的路径</param>
        /// <param name="privateNkey">私钥Nkey</param>
        public NatsHelper(string url, string jwt, string privateNkey)
        {
            if (NatsConnection != null)
                return;

            lock (_locker)
            {
                NatsConnection = new ConnectionFactory().CreateConnection(url, jwt, privateNkey);
                _options = NatsConnection.Opts;
            }
        }
        #endregion

        #region NATS连接
        /// <summary>
        /// 确保NATS连接可用
        /// </summary>
        /// <param name="connection">NATS连接对象</param>
        /// <returns></returns>
        public IConnection EnsureAvailabled(IConnection connection)
        {
            if (connection == null)
                connection = new ConnectionFactory().CreateConnection(_options);

            if (connection.IsClosed())
                connection = new ConnectionFactory().CreateConnection(connection.Opts);

            return connection;
        }
        #endregion

        #region 发送请求
        #region Sync
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <param name="timeout">超时时长，单位：ms</param>
        /// <returns></returns>
        public Msg Request<T>(string subject, T data, int timeout) =>
            EnsureAvailabled(NatsConnection).Request(subject, data.SerializeToUtf8Bytes(), timeout);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <param name="offset">消息字节偏移量，从0开始</param>
        /// <param name="count">消息字节长度</param>
        /// <param name="timeout">超时时长，单位：ms</param>
        /// <returns></returns>
        public Msg Request<T>(string subject, T data, int offset, int count, int timeout) =>
            EnsureAvailabled(NatsConnection).Request(subject, data.SerializeToUtf8Bytes(), offset, count, timeout);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <returns></returns>
        public Msg Request<T>(string subject, T data) =>
            EnsureAvailabled(NatsConnection).Request(subject, data.SerializeToUtf8Bytes());

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <param name="offset">消息字节偏移量，从0开始</param>
        /// <param name="count">消息字节长度</param>
        /// <returns></returns>
        public Msg Request<T>(string subject, T data, int offset, int count) =>
            EnsureAvailabled(NatsConnection).Request(subject, data.SerializeToUtf8Bytes(), offset, count);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="message">请求消息</param>
        /// <returns></returns>
        public Msg Request(Msg message) =>
            EnsureAvailabled(NatsConnection).Request(message);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="message">请求消息</param>
        /// <param name="timeout">超时时长，单位：ms</param>
        /// <returns></returns>
        public Msg Request(Msg message, int timeout) =>
            EnsureAvailabled(NatsConnection).Request(message, timeout);
        #endregion

        #region Async
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <param name="timeout">超时时长，单位：ms</param>
        /// <returns></returns>
        public async Task<Msg> RequestAsync<T>(string subject, T data, int timeout) =>
            await EnsureAvailabled(NatsConnection).RequestAsync(subject, data.SerializeToUtf8Bytes(), timeout);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <param name="offset">消息字节偏移量，从0开始</param>
        /// <param name="count">消息字节长度</param>
        /// <param name="timeout">超时时长，单位：ms</param>
        /// <returns></returns>
        public async Task<Msg> RequestAsync<T>(string subject, T data, int offset, int count, int timeout) =>
            await EnsureAvailabled(NatsConnection).RequestAsync(subject, data.SerializeToUtf8Bytes(), offset, count, timeout);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <returns></returns>
        public async Task<Msg> RequestAsync<T>(string subject, T data) =>
            await EnsureAvailabled(NatsConnection).RequestAsync(subject, data.SerializeToUtf8Bytes());

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">请求数据</param>
        /// <param name="offset">消息字节偏移量，从0开始</param>
        /// <param name="count">消息字节长度</param>
        /// <returns></returns>
        public async Task<Msg> RequestAsync<T>(string subject, T data, int offset, int count) =>
            await EnsureAvailabled(NatsConnection).RequestAsync(subject, data.SerializeToUtf8Bytes(), offset, count);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="message">请求消息</param>
        /// <returns></returns>
        public async Task<Msg> RequestAsync(Msg message) =>
            await EnsureAvailabled(NatsConnection).RequestAsync(message);

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="message">请求消息</param>
        /// <param name="timeout">超时时长，单位：ms</param>
        /// <returns></returns>
        public async Task<Msg> RequestAsync(Msg message, int timeout) =>
            await EnsureAvailabled(NatsConnection).RequestAsync(message, timeout);
        #endregion
        #endregion

        #region 发布消息
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">消息数据</param>
        public void Publish<T>(string subject, T data) =>
            EnsureAvailabled(NatsConnection).Publish(subject, data.SerializeToUtf8Bytes());

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="data">消息数据</param>
        /// <param name="offset">消息字节偏移量，从0开始</param>
        /// <param name="count">消息字节长度</param>
        public void Publish<T>(string subject, T data, int offset, int count) =>
            EnsureAvailabled(NatsConnection).Publish(subject, data.SerializeToUtf8Bytes(), offset, count);

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        public void Publish(Msg msg) =>
            EnsureAvailabled(NatsConnection).Publish(msg);

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="reply">回复主题</param>
        /// <param name="data">消息数据</param>
        public void Publish<T>(string subject, string reply, T data) =>
            EnsureAvailabled(NatsConnection).Publish(subject, reply, data.SerializeToUtf8Bytes());

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="subject">消息主题</param>
        /// <param name="reply">回复主题</param>
        /// <param name="data">消息数据</param>
        /// <param name="offset">消息字节偏移量，从0开始</param>
        /// <param name="count">消息字节长度</param>
        public void Publish<T>(string subject, string reply, T data, int offset, int count) =>
            EnsureAvailabled(NatsConnection).Publish(subject, reply, data.SerializeToUtf8Bytes(), offset, count);
        #endregion

        #region 订阅消息
        /// <summary>
        /// 异步订阅消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <returns></returns>
        public IAsyncSubscription SubscribeAsync(string subject) =>
            EnsureAvailabled(NatsConnection).SubscribeAsync(subject);

        /// <summary>
        /// 异步订阅消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <param name="queue">队列组名称</param>
        /// <param name="handler">订阅消息事件委托</param>
        /// <returns></returns>
        public IAsyncSubscription SubscribeAsync(string subject, string queue, EventHandler<MsgHandlerEventArgs> handler) =>
            EnsureAvailabled(NatsConnection).SubscribeAsync(subject, queue, handler);

        /// <summary>
        /// 异步订阅消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <param name="queue">队列组名称</param>
        /// <returns></returns>
        public IAsyncSubscription SubscribeAsync(string subject, string queue) =>
            EnsureAvailabled(NatsConnection).SubscribeAsync(subject, queue);

        /// <summary>
        /// 异步订阅消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <param name="handler">订阅消息事件委托</param>
        /// <returns></returns>
        public IAsyncSubscription SubscribeAsync(string subject, EventHandler<MsgHandlerEventArgs> handler) =>
            EnsureAvailabled(NatsConnection).SubscribeAsync(subject, handler);

        /// <summary>
        /// 同步订阅消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <param name="queue">队列组名称</param>
        /// <returns></returns>
        public ISyncSubscription SubscribeSync(string subject, string queue) =>
             EnsureAvailabled(NatsConnection).SubscribeSync(subject, queue);

        /// <summary>
        /// 同步订阅消息
        /// </summary>
        /// <param name="subject">消息主题</param>
        /// <returns></returns>
        public ISyncSubscription SubscribeSync(string subject) =>
            EnsureAvailabled(NatsConnection).SubscribeSync(subject);
        #endregion

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() =>
            NatsConnection?.Dispose();
        #endregion
    }
}
