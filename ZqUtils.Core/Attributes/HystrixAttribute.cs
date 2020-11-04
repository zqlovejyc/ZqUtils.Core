#region License
/***
 * Copyright © 2018-2020, 张强 (943620963@qq.com).
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

using AspectCore.DependencyInjection;
using AspectCore.DynamicProxy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
/****************************
* [Author] 张强
* [Date] 2020-10-07
* [Describe] 基于AspectCore和Polly实现的重试、熔断、降级等拦截特性
* **************************/
namespace ZqUtils.Core.Attributes
{
    /// <summary>
    /// 基于AspectCore和Polly实现的重试、熔断、降级等拦截特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HystrixAttribute : AbstractInterceptorAttribute
    {
        /// <summary>
        /// Polly策略
        /// </summary>
        private static readonly ConcurrentDictionary<MethodInfo, AsyncPolicy> policies =
           new ConcurrentDictionary<MethodInfo, AsyncPolicy>();

        /// <summary>
        /// 缓存
        /// </summary>
        private static readonly IMemoryCache memoryCache =
            new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// 降级的方法
        /// </summary>
        public string FallBackMethod { get; set; }

        /// <summary>
        /// 最多重试几次，如果为0则不重试
        /// </summary>
        public int MaxRetryTimes { get; set; } = 0;

        /// <summary>
        /// 重试间隔的毫秒数
        /// </summary>
        public int RetryIntervalMilliseconds { get; set; } = 200;

        /// <summary>
        /// 是否启用熔断
        /// </summary>
        public bool EnableCircuitBreaker { get; set; }

        /// <summary>
        /// 熔断前出现允许错误几次
        /// </summary>
        public int ExceptionsAllowedBeforeBreaking { get; set; } = 3;

        /// <summary>
        /// 熔断多长时间（毫秒）
        /// </summary>
        public int DurationOfBreak { get; set; } = 3000;

        /// <summary>
        /// 执行超过多少毫秒则认为超时（0表示不检测超时）
        /// </summary>
        public int TimeOut { get; set; } = 0;

        /// <summary>
        /// 缓存多少毫秒（0表示不缓存），用“类名+方法名+所有参数ToString拼接”做缓存Key
        /// </summary>
        public int CacheTtl { get; set; } = 0;

        /// <summary>
        /// 日志
        /// </summary>
        [FromServiceContext]
        public ILogger<HystrixAttribute> Logger { get; set; }

        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            /*
             * 一个HystrixCommand中保持一个policy对象即可
             * 其实主要是CircuitBreaker要求对于同一段代码要共享一个policy对象
             * 根据反射原理，同一个方法的MethodInfo是同一个对象，但是对象上取出来的HystrixCommandAttribute
             * 每次获取的都是不同的对象，因此以MethodInfo为Key保存到policies中，确保一个方法对应一个policy实例
             */

            policies.TryGetValue(context.ServiceMethod, out var policy);

            //因为Invoke可能是并发调用，因此要确保policies赋值的线程安全
            lock (policies)
            {
                if (policy.IsNull())
                {
                    policy = Policy.NoOpAsync();

                    if (EnableCircuitBreaker)
                    {
                        policy = policy.WrapAsync(Policy.Handle<Exception>()
                            .CircuitBreakerAsync(ExceptionsAllowedBeforeBreaking,
                                TimeSpan.FromMilliseconds(DurationOfBreak)));
                    }

                    if (TimeOut > 0)
                    {
                        policy = policy.WrapAsync(Policy.TimeoutAsync(
                            () => TimeSpan.FromMilliseconds(TimeOut),
                            Polly.Timeout.TimeoutStrategy.Pessimistic));
                    }

                    if (MaxRetryTimes > 0)
                    {
                        policy = policy.WrapAsync(Policy.Handle<Exception>().WaitAndRetryAsync(MaxRetryTimes,
                            i => TimeSpan.FromMilliseconds(RetryIntervalMilliseconds)));
                    }

                    if (FallBackMethod.IsNotNullOrEmpty())
                    {
                        var policyFallBack = Policy
                        .Handle<Exception>()
                        .FallbackAsync(async (ctx, t) =>
                        {
                            var fallBackMethod = context.ImplementationMethod.DeclaringType?.GetMethod(FallBackMethod);
                            var fallBackResult = fallBackMethod?.Invoke(context.Implementation, context.Parameters);

                            var aspectContext = (AspectContext)ctx["aspectContext"];
                            if (aspectContext.IsNotNull())
                                aspectContext.ReturnValue = fallBackResult;

                            await Task.CompletedTask;
                        }, async (ex, t) =>
                        {
                            Logger?.LogError(ex, ex.Message);
                            await Task.CompletedTask;
                        });

                        policy = policyFallBack.WrapAsync(policy);
                    }

                    policies.TryAdd(context.ServiceMethod, policy);
                }
            }

            //把本地调用的AspectContext传递给Polly，主要给FallbackAsync中使用，避免闭包的坑
            var pollyCtx = new Context { ["aspectContext"] = context };

            if (CacheTtl > 0)
            {
                //用类名+方法名+参数的下划线连接起来作为缓存key
                var cacheKey = $"HystrixMethodCacheManager_Key_{context.ImplementationMethod.DeclaringType.FullName}.{context.ImplementationMethod}{string.Join("_", context.Parameters)}";

                //尝试去缓存中获取。如果找到了，则直接用缓存中的值做返回值
                if (memoryCache.TryGetValue(cacheKey, out var cacheValue))
                    context.ReturnValue = cacheValue;
                else
                {
                    //如果缓存中没有，则执行实际被拦截的方法
                    await policy.ExecuteAsync(ctx => next(context), pollyCtx);

                    //存入缓存中
                    using (var cacheEntry = memoryCache.CreateEntry(cacheKey))
                    {
                        cacheEntry.Value = context.ReturnValue;
                        cacheEntry.AbsoluteExpiration = DateTime.Now + TimeSpan.FromMilliseconds(CacheTtl);
                    }
                }
            }
            else //如果没有启用缓存，就直接执行业务方法
                await policy.ExecuteAsync(ctx => next(context), pollyCtx);
        }
    }
}