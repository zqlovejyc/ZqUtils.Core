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

using Microsoft.FSharp.Core;
using Pulsar.Client.Api;
using Pulsar.Client.Common;
using Pulsar.Client.Transaction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
/****************************
* [Author] 张强
* [Date] 2021-10-12
* [Describe] Pulsar工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Pulsar工具类
    /// </summary>
    public class PulsarHelper
    {
        #region 私有字段
        /// <summary>
        /// 线程对象，线程锁使用
        /// </summary>
        private static readonly object _locker = new();

        /// <summary>
        /// Pulsar客户端
        /// </summary>
        private readonly PulsarClient _client;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceUrl">Pulsar服务地址</param>
        /// <param name="action">PulsarClientBuilder委托，用于Pulsar客户端自定义配置</param>
        /// <remarks>
        ///     <code>
        ///         var serviceUrl = "pulsar+ssl://my-pulsar-cluster:6651";
        ///         
        ///         var ca = new X509Certificate2(@"path-to-ca.crt");
        ///         
        ///         var userTls = AuthenticationFactory.Tls(@"path-to-user.pfx");
        ///         
        ///         var pulsar=new PulsarHelper(
        ///             serviceUrl,
        ///             builder => builder
        ///                 .EnableTls(true)
        ///                 .TlsTrustCertificate(ca)
        ///                 .Authentication(userTls));
        ///     </code>
        /// </remarks>
        public PulsarHelper(string serviceUrl, Action<PulsarClientBuilder> action = null)
        {
            if (_client != null)
                return;

            lock (_locker)
            {
                var builder = new PulsarClientBuilder().ServiceUrl(serviceUrl);

                action?.Invoke(builder);

                _client = builder
                    .BuildAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }
        #endregion

        #region 公有属性
        /// <summary>
        /// Pulsar客户端
        /// </summary>
        public PulsarClient PulsarClient => _client;
        #endregion

        #region 生产者
        /// <summary>
        /// 创建生产者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="schema"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IProducer<T>> CreateProducerAsync<T>(string topic, ISchema<T> schema, Action<ProducerBuilder<T>> action = null)
        {
            var builder = _client.NewProducer(schema).Topic(topic);

            action?.Invoke(builder);

            return await builder.CreateAsync();
        }

        /// <summary>
        /// 创建string生产者
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IProducer<string>> CreateStringProducerAsync(string topic, Action<ProducerBuilder<string>> action = null)
        {
            return await this.CreateProducerAsync(topic, Schema.STRING(), action);
        }

        /// <summary>
        /// 创建byte生产者
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IProducer<byte[]>> CreateByteProducerAsync(string topic, Action<ProducerBuilder<byte[]>> action = null)
        {
            return await this.CreateProducerAsync(topic, Schema.BYTES(), action);
        }
        #endregion

        #region 消费者
        /// <summary>
        /// 创建消费者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schema"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IConsumer<T>> CreateConsumerAsync<T>(ISchema<T> schema, Action<ConsumerBuilder<T>> action = null)
        {
            var builder = _client.NewConsumer(schema);

            action?.Invoke(builder);

            return await builder.SubscribeAsync();
        }

        /// <summary>
        /// 创建string消费者
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IConsumer<string>> CreateStringConsumerAsync(string topic, Action<ConsumerBuilder<string>> action = null)
        {
            return await this.CreateConsumerAsync(Schema.STRING(), builder =>
            {
                builder.Topic(topic);

                action?.Invoke(builder);
            });
        }

        /// <summary>
        /// 创建string消费者
        /// </summary>
        /// <param name="topics"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IConsumer<string>> CreateStringConsumerAsync(IEnumerable<string> topics, Action<ConsumerBuilder<string>> action = null)
        {
            return await this.CreateConsumerAsync(Schema.STRING(), builder =>
            {
                builder.Topics(topics);

                action?.Invoke(builder);
            });
        }

        /// <summary>
        /// 创建byte消费者
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IConsumer<byte[]>> CreateByteConsumerAsync(string topic, Action<ConsumerBuilder<byte[]>> action = null)
        {
            return await this.CreateConsumerAsync(Schema.BYTES(), builder =>
            {
                builder.Topic(topic);

                action?.Invoke(builder);
            });
        }

        /// <summary>
        /// 创建byte消费者
        /// </summary>
        /// <param name="topics"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IConsumer<byte[]>> CreateByteConsumerAsync(IEnumerable<string> topics, Action<ConsumerBuilder<byte[]>> action = null)
        {
            return await this.CreateConsumerAsync(Schema.BYTES(), builder =>
            {
                builder.Topics(topics);

                action?.Invoke(builder);
            });
        }
        #endregion

        #region 读取者
        /// <summary>
        /// 创建读取者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schema"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IReader<T>> CreateReaderAsync<T>(ISchema<T> schema, Action<ReaderBuilder<T>> action = null)
        {
            var builder = _client.NewReader(schema);

            action?.Invoke(builder);

            return await builder.CreateAsync();
        }

        /// <summary>
        /// 创建读取者
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic"></param>
        /// <param name="schema"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IReader<T>> CreateReaderAsync<T>(string topic, ISchema<T> schema, Action<ReaderBuilder<T>> action = null)
        {
            return await this.CreateReaderAsync(schema, builder =>
            {
                builder.Topic(topic);

                action?.Invoke(builder);
            });
        }

        /// <summary>
        /// 创建string读取者
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IReader<string>> CreateStringReaderAsync(string topic, Action<ReaderBuilder<string>> action = null)
        {
            return await this.CreateReaderAsync(Schema.STRING(), builder =>
            {
                builder.Topic(topic);

                action?.Invoke(builder);
            });
        }

        /// <summary>
        /// 创建byte读取者
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<IReader<byte[]>> CreateByteReaderAsync(string topic, Action<ReaderBuilder<byte[]>> action = null)
        {
            return await this.CreateReaderAsync(Schema.BYTES(), builder =>
            {
                builder.Topic(topic);

                action?.Invoke(builder);
            });
        }
        #endregion

        #region 事务
        /// <summary>
        /// 创建事务
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<Transaction> CreateTransactionAsync(Action<TransactionBuilder> action = null)
        {
            var builder = _client.NewTransaction();

            action?.Invoke(builder);

            return await builder.BuildAsync();
        }
        #endregion

        #region 发送消息
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<MessageId> SendMessageAsync<T>(IProducer<T> producer, T message)
        {
            return await producer.SendAsync(message);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer"></param>
        /// <param name="messageBuilder"></param>
        /// <returns></returns>
        public async Task<MessageId> SendMessageAsync<T>(IProducer<T> producer, MessageBuilder<T> messageBuilder)
        {
            return await producer.SendAsync(messageBuilder);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<Unit> SendForgetMessageAsync<T>(IProducer<T> producer, T message)
        {
            return await producer.SendAndForgetAsync(message);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="producer"></param>
        /// <param name="messageBuilder"></param>
        /// <returns></returns>
        public async Task<Unit> SendForgetMessageAsync<T>(IProducer<T> producer, MessageBuilder<T> messageBuilder)
        {
            return await producer.SendAndForgetAsync(messageBuilder);
        }
        #endregion

        #region 接收消息
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public async Task<Message<T>> ReceiveMessageAsync<T>(IConsumer<T> consumer)
        {
            return await consumer.ReceiveAsync();
        }

        /// <summary>
        /// 批量接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <returns></returns>
        public async Task<Messages<T>> BatchReceiveMessageAsync<T>(IConsumer<T> consumer)
        {
            return await consumer.BatchReceiveAsync();
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="receiveHandler"></param>
        /// <param name="retry"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public void ReceiveMessage<T>(IConsumer<T> consumer, Func<Message<T>, bool> receiveHandler = null, int retry = 5, Action<Message<T>, int, Exception> exceptionHandler = null)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var numberOfRetries = 0;
                    Exception exception = null;
                    bool? result = false;

                    var message = await this.ReceiveMessageAsync(consumer);

                    while (numberOfRetries <= retry)
                    {
                        try
                        {
                            if (message == null)
                                continue;

                            result = receiveHandler?.Invoke(message);

                            //异常置空
                            exception = null;

                            break;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            exceptionHandler?.Invoke(message, numberOfRetries, ex);
                            numberOfRetries++;
                        }
                    }

                    if (!(result == true) || exception != null)
                        await this.NegativeAcknowledgeAsync(consumer, message.MessageId);
                    else
                        await this.AcknowledgeAsync(consumer, message.MessageId);

                    await Task.Delay(10);
                }
            });
        }

        /// <summary>
        /// 批量接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="receiveHandler"></param>
        /// <param name="retry"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public void BatchReceiveMessage<T>(IConsumer<T> consumer, Func<Messages<T>, bool> receiveHandler = null, int retry = 5, Action<Messages<T>, int, Exception> exceptionHandler = null)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var numberOfRetries = 0;
                    Exception exception = null;
                    bool? result = false;

                    var messages = await this.BatchReceiveMessageAsync(consumer);

                    while (numberOfRetries <= retry)
                    {
                        try
                        {
                            if (messages == null)
                                continue;

                            result = receiveHandler?.Invoke(messages);

                            //异常置空
                            exception = null;

                            break;
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                            exceptionHandler?.Invoke(messages, numberOfRetries, ex);
                            numberOfRetries++;
                        }
                    }

                    if (!(result == true) || exception != null)
                        await this.NegativeAcknowledgeAsync(consumer, messages);
                    else
                        await this.AcknowledgeAsync(consumer, messages);

                    await Task.Delay(10);
                }
            });
        }
        #endregion

        #region 确认消息
        /// <summary>
        /// 确认消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task<Unit> AcknowledgeAsync<T>(IConsumer<T> consumer, MessageId messageId)
        {
            return await consumer.AcknowledgeAsync(messageId);
        }

        /// <summary>
        /// 确认消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="messageId"></param>
        /// <param name="txn"></param>
        /// <returns></returns>
        public async Task<Unit> AcknowledgeAsync<T>(IConsumer<T> consumer, MessageId messageId, Transaction txn)
        {
            return await consumer.AcknowledgeAsync(messageId, txn);
        }

        /// <summary>
        /// 确认消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public async Task<Unit> AcknowledgeAsync<T>(IConsumer<T> consumer, Messages<T> messages)
        {
            return await consumer.AcknowledgeAsync(messages);
        }

        /// <summary>
        /// 确认消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public async Task<Unit> AcknowledgeAsync<T>(IConsumer<T> consumer, IEnumerable<MessageId> messages)
        {
            return await consumer.AcknowledgeAsync(messages);
        }
        #endregion

        #region 否认消息
        /// <summary>
        /// 否认消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task<Unit> NegativeAcknowledgeAsync<T>(IConsumer<T> consumer, MessageId messageId)
        {
            return await consumer.NegativeAcknowledge(messageId);
        }

        /// <summary>
        /// 否认消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="consumer"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        public async Task<Unit> NegativeAcknowledgeAsync<T>(IConsumer<T> consumer, Messages<T> messages)
        {
            return await consumer.NegativeAcknowledge(messages);
        }
        #endregion

        #region 读取消息
        /// <summary>
        /// 读取消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public async Task<Message<T>> ReaderMessageAsync<T>(IReader<T> reader)
        {
            return await reader.ReadNextAsync();
        }
        #endregion
    }
}
