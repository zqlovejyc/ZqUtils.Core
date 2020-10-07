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
/****************************
* [Author] 张强
* [Date] 2020-10-07
* [Describe] 基于AspectCore和Polly实现的重试、熔断等拦截特性
* **************************/
namespace ZqUtils.Core.Attributes
{
    /// <summary>
    /// 基于AspectCore和Polly实现的重试、熔断等拦截特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HystrixAttribute : AbstractInterceptorAttribute
    {
        /// <summary>
        /// policies
        /// </summary>
        private static readonly ConcurrentDictionary<MethodInfo, AsyncPolicy> policies =
           new ConcurrentDictionary<MethodInfo, AsyncPolicy>();

        /// <summary>
        /// memory cache
        /// </summary>
        private static readonly IMemoryCache memoryCache =
            new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// fallback method
        /// </summary>
        public string FallBackMethod { get; set; }

        /// <summary>
        ///  0 means no retry
        /// </summary>
        public int MaxRetryTimes { get; set; } = 0;

        /// <summary>
        /// retry interval millisecond
        /// </summary>
        public int RetryIntervalMilliseconds { get; set; } = 200;

        /// <summary>
        /// enable circuit breaker
        /// </summary>
        public bool EnableCircuitBreaker { get; set; }

        /// <summary>
        /// exceptions allowed before breaking times
        /// </summary>
        public int ExceptionsAllowedBeforeBreaking { get; set; } = 3;

        /// <summary>
        /// count by milliseconds
        /// </summary>
        public int DurationOfBreak { get; set; } = 3000;

        /// <summary>
        /// count by milliseconds.0 means don't check timeout
        /// </summary>
        public int TimeOut { get; set; } = 0;

        /// <summary>
        ///  count by milliseconds
        /// </summary>
        public int CacheTtl { get; set; } = 0;

        /// <summary>
        /// logger
        /// </summary>
        [FromServiceContext]
        public ILogger<HystrixAttribute> Logger { get; set; }

        /// <summary>
        /// Invoke
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            /*
             * share one policy in a instance,because CircuitBreaker require so.
             *
             * the MethodInfo gotten by reflect is always same one, 
             * however the instance gotten by reflect is different every time.
             * so we save it in policies with MethodInfo as its key.  
             */

            policies.TryGetValue(context.ServiceMethod, out var policy);

            //current method is possible to be called by more than one thread.
            lock (policies)
            {
                if (policy == null)
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

                    var policyFallBack = Policy
                        .Handle<Exception>()
                        .FallbackAsync(async (ctx, t) =>
                        {
                            var aspectContext = (AspectContext)ctx["aspectContext"];
                            var fallBackMethod = context.ImplementationMethod.DeclaringType?.GetMethod(FallBackMethod);
                            var fallBackResult = fallBackMethod?.Invoke(context.Implementation, context.Parameters);
                            aspectContext.ReturnValue = fallBackResult;
                            await Task.CompletedTask;
                        }, async (ex, t) =>
                        {
                            Logger?.LogError(ex, ex.Message);
                            await Task.CompletedTask;
                        });

                    policy = policyFallBack.WrapAsync(policy);
                    policies.TryAdd(context.ServiceMethod, policy);
                }
            }

            //transfer local AspectContext to Polly,especially FallbackAsync.this way can avoid troubles in closure
            var pollyCtx = new Context { ["aspectContext"] = context };

            if (CacheTtl > 0)
            {
                //use assembly.class.method+parameters as cache key
                var cacheKey =
                    $"HystrixMethodCacheManager_Key_{context.ImplementationMethod.DeclaringType.FullName}.{context.ImplementationMethod}{string.Join("_", context.Parameters)}";

                //try to get result from cache firstly.If success return it directly
                if (memoryCache.TryGetValue(cacheKey, out var cacheValue))
                    context.ReturnValue = cacheValue;
                else
                {
                    //it's not cached currently,just execute the method 
                    await policy.ExecuteAsync(ctx => next(context), pollyCtx);
                    //cache it
                    using (var cacheEntry = memoryCache.CreateEntry(cacheKey))
                    {
                        cacheEntry.Value = context.ReturnValue;
                        cacheEntry.AbsoluteExpiration = DateTime.Now + TimeSpan.FromMilliseconds(CacheTtl);
                    }
                }
            }
            else //disabled cache,just execute the method directly
                await policy.ExecuteAsync(ctx => next(context), pollyCtx);
        }
    }
}