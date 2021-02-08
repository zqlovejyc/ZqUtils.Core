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
using System.Threading.Channels;
using System.Threading.Tasks;
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
    public class ChannelHelper<T>
    {
        /// <summary>
        /// Channel
        /// </summary>
        public Channel<T> ThreadChannel { get; private set; }

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
        /// <param name="options">Unbounded Channel配置</param>
        public ChannelHelper(UnboundedChannelOptions options)
        {
            ThreadChannel = Channel.CreateUnbounded<T>(options);
        }

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
        /// <param name="options">Bounded Channel配置</param>
        public ChannelHelper(BoundedChannelOptions options)
        {
            ThreadChannel = Channel.CreateBounded<T>(options);
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool Publish(T message)
        {
            return ThreadChannel.Writer.TryWrite(message);
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task PublishAsync(T message)
        {
            await ThreadChannel.Writer.WriteAsync(message);
        }

        /// <summary>
        /// 订阅消息，注意订阅消息需要单独开启线程，否则会阻塞主线程
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task SubscribeAsync(Action<T> handler)
        {
            while (await ThreadChannel.Reader.WaitToReadAsync())
            {
                if (ThreadChannel.Reader.TryRead(out var message))
                {
                    handler?.Invoke(message);
                }
            }
        }

        /// <summary>
        /// 订阅消息，注意订阅消息需要单独开启线程，否则会阻塞主线程
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task SubscribeAsync(Func<T, Task> handler)
        {
            while (await ThreadChannel.Reader.WaitToReadAsync())
            {
                if (ThreadChannel.Reader.TryRead(out var message))
                {
                    if (handler != null)
                        await handler(message);
                }
            }
        }
    }
}
