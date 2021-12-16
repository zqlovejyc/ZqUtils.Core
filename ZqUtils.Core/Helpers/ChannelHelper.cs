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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
/****************************
* [Author] 张强
* [Date] 2021-02-08
* [Describe] Channel工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Channel工具类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ChannelHelper<T> : IDisposable
    {
        #region 私有字段
        /// <summary>
        /// Task集合
        /// </summary>
        private readonly List<Task> _tasks = new();

        /// <summary>
        /// 取消令牌源
        /// </summary>
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// 是否已释放
        /// </summary>
        private bool _disposed;
        #endregion

        #region 公有属性
        /// <summary>
        /// Channel
        /// </summary>
        public Channel<T> ThreadChannel { get; private set; }
        #endregion

        #region 构造函数
        #region Unbounded
        /// <summary>
        /// 构造函数Unbounded
        /// </summary>
        public ChannelHelper()
        {
            ThreadChannel = Channel.CreateUnbounded<T>();
        }

        /// <summary>
        /// 构造函数Unbounded
        /// </summary>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(Action<T> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateUnbounded<T>();

            Subscribe(subscriber, threadCount);
        }

        /// <summary>
        /// 构造函数Unbounded
        /// </summary>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(Func<T, Task> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateUnbounded<T>();

            Subscribe(subscriber, threadCount);
        }

        /// <summary>
        /// 构造函数Unbounded
        /// </summary>
        /// <param name="options">Unbounded Channel配置</param>
        public ChannelHelper(UnboundedChannelOptions options)
        {
            ThreadChannel = Channel.CreateUnbounded<T>(options);
        }

        /// <summary>
        /// 构造函数Unbounded
        /// </summary>
        /// <param name="options">Unbounded Channel配置</param>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(UnboundedChannelOptions options, Action<T> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateUnbounded<T>(options);

            Subscribe(subscriber, threadCount);
        }

        /// <summary>
        /// 构造函数Unbounded
        /// </summary>
        /// <param name="options">Unbounded Channel配置</param>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(UnboundedChannelOptions options, Func<T, Task> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateUnbounded<T>(options);

            Subscribe(subscriber, threadCount);
        }
        #endregion

        #region Bounded
        /// <summary>
        /// 构造函数Bounded
        /// </summary>
        /// <param name="capacity">Channel存储长度</param>
        public ChannelHelper(int capacity)
        {
            ThreadChannel = Channel.CreateBounded<T>(capacity);
        }

        /// <summary>
        /// 构造函数Bounded
        /// </summary>
        /// <param name="capacity">Channel存储长度</param>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(int capacity, Action<T> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateBounded<T>(capacity);

            Subscribe(subscriber, threadCount);
        }

        /// <summary>
        /// 构造函数Bounded
        /// </summary>
        /// <param name="capacity">Channel存储长度</param>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(int capacity, Func<T, Task> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateBounded<T>(capacity);

            Subscribe(subscriber, threadCount);
        }

        /// <summary>
        /// 构造函数Bounded
        /// </summary>
        /// <param name="options">Bounded Channel配置</param>
        public ChannelHelper(BoundedChannelOptions options)
        {
            ThreadChannel = Channel.CreateBounded<T>(options);
        }

        /// <summary>
        /// 构造函数Bounded
        /// </summary>
        /// <param name="options">Bounded Channel配置</param>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(BoundedChannelOptions options, Action<T> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateBounded<T>(options);

            Subscribe(subscriber, threadCount);
        }

        /// <summary>
        /// 构造函数Bounded
        /// </summary>
        /// <param name="options">Bounded Channel配置</param>
        /// <param name="subscriber">订阅处理委托</param>
        /// <param name="threadCount">线程数量</param>
        public ChannelHelper(BoundedChannelOptions options, Func<T, Task> subscriber, int threadCount = 1)
        {
            ThreadChannel = Channel.CreateBounded<T>(options);

            Subscribe(subscriber, threadCount);
        }
        #endregion
        #endregion

        #region 发布消息
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <returns></returns>
        public bool Publish(T message)
        {
            return ThreadChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <returns></returns>
        public async Task PublishAsync(T message)
        {
            await ThreadChannel.Writer.WriteAsync(message, _cts.Token);
        }
        #endregion

        #region 订阅消息
        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="handler">自定义处理委托</param>
        /// <param name="threadCount">线程数量</param>
        /// <returns></returns>
        public void Subscribe(Action<T> handler, int threadCount = 1)
        {
            try
            {
                var tasks = Enumerable.Range(0, threadCount).Select(x =>
                    Task.Run(async () =>
                    {
                        while (await ThreadChannel.Reader.WaitToReadAsync(_cts.Token))
                        {
                            if (ThreadChannel.Reader.TryRead(out var message))
                            {
                                if (handler != null)
                                {
                                    try
                                    {
                                        handler(message);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogHelper.Error(ex, $"`{handler.Method}` -> Message({typeof(T).Name}) -> {message.ToJson()} Subscribe Exception:{ex.Message}");
                                    }
                                }
                            }
                        }
                    }, _cts.Token));

                _tasks.AddRange(tasks);

                Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, $"Subscribe(Action<T> handler, int threadCount = 1)");
            }
        }

        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="handler">自定义处理委托</param>
        /// <param name="threadCount">线程数量</param>
        /// <returns></returns>
        public void Subscribe(Func<T, Task> handler, int threadCount = 1)
        {
            try
            {
                var tasks = Enumerable.Range(0, threadCount).Select(x =>
                    Task.Run(async () =>
                    {
                        while (await ThreadChannel.Reader.WaitToReadAsync(_cts.Token))
                        {
                            if (ThreadChannel.Reader.TryRead(out var message))
                            {
                                if (handler != null)
                                {
                                    try
                                    {
                                        await handler(message);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogHelper.Error(ex, $"`{handler.Method}` -> Message({typeof(T).Name}) -> {message.ToJson()} Subscribe Exception:{ex.Message}");
                                    }
                                }
                            }
                        }
                    }, _cts.Token));

                _tasks.AddRange(tasks);

                Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, $"Subscribe(Func<T, Task> handler, int threadCount = 1)");
            }
        }
        #endregion

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _cts.Cancel();

                //延迟等待task取消任务，否则下面task释放会抛异常
                Thread.Sleep(50);

                foreach (var task in _tasks)
                {
                    if (!task.IsCompleted)
                        task.Wait();

                    task.Dispose();
                }

                _cts.Dispose();

                _disposed = true;
            }
        }
        #endregion
    }
}
