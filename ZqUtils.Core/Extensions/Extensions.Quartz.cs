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

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Simpl;
using Quartz.Spi;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ZqUtils.Core.Helpers;
/****************************
* [Author] 张强
* [Date] 2020-05-29
* [Describe] Quartz扩展类
* **************************/
namespace ZqUtils.Core.Extensions
{
    /// <summary>
    /// Quartz扩展类
    /// </summary>
    public static class QuartzExtensions
    {
        /// <summary>
        /// 注入Ioc作业调度工厂和自定义IJob
        /// </summary>
        /// <param name="this"></param>
        /// <param name="filter">Assembly过滤器</param>
        /// <param name="lifetime">生命周期</param>
        /// <returns></returns>
        public static IServiceCollection AddJobAndJobFactory(
            this IServiceCollection @this,
            Func<string, bool> filter = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
        {
            //扫描程序集
            var assemblies = AssemblyHelper.GetAssemblies(filter: filter).ToList();

            //判断是否包含ZqUtils.Core.dll
            var assembly = Assembly.Load("ZqUtils.Core");
            if (!assemblies.Contains(assembly))
                assemblies.Add(assembly);

            //注入IJobFactory和IJob
            @this.Scan(scan => scan
                .FromAssemblies(assemblies.ToArray())
                    .AddClasses(classes => classes.AssignableTo<IJobFactory>())
                        .AsImplementedInterfaces()
                        .AsSelf()
                        .WithSingletonLifetime()
                    .AddClasses(classes => classes.AssignableTo<IJob>())
                        .AsImplementedInterfaces()
                        .AsSelf()
                        .WithLifetime(lifetime));

            return @this;
        }

        /// <summary>
        /// 使用Ioc作业调度工厂
        /// </summary>
        /// <param name="this"></param>
        /// <param name="waitForJobsToComplete">是否等待Job执行完成</param>
        /// <returns></returns>
        public static IScheduler UseIocJobFactory(this IApplicationBuilder @this, bool? waitForJobsToComplete = null)
        {
            //获取IScheduler
            var scheduler = QuartzHelper.GetSchedulerAsync().GetAwaiter().GetResult();

            //设置JobFactory
            scheduler.JobFactory = @this.ApplicationServices.GetRequiredService<IJobFactory>();

            //应用程序生命周期
            var lifetime = @this.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

            //应用程序终止时，关闭Scheduler
            lifetime.ApplicationStopping.Register(() =>
            {
                if (!scheduler.IsShutdown)
                {
                    if (waitForJobsToComplete != null)
                        scheduler.Shutdown(waitForJobsToComplete.Value).Wait();
                    else
                        scheduler.Shutdown().Wait();
                }
            });

            //启动Scheduler
            if (!scheduler.IsShutdown && !scheduler.IsStarted)
            {
                scheduler.Start().Wait();
            }

            return scheduler;
        }

        /// <summary>
        /// 使用Ioc作业调度工厂
        /// </summary>
        /// <param name="this"></param>
        /// <param name="waitForJobsToComplete">是否等待Job执行完成</param>
        /// <returns></returns>
        public static IScheduler UseIocJobFactory(this IServiceProvider @this, bool? waitForJobsToComplete = null)
        {
            //获取IScheduler
            var scheduler = QuartzHelper.GetSchedulerAsync().GetAwaiter().GetResult();

            //设置JobFactory
            scheduler.JobFactory = @this.GetRequiredService<IJobFactory>();

            //应用程序生命周期
            var lifetime = @this.GetRequiredService<IHostApplicationLifetime>();

            //应用程序终止时，关闭Scheduler
            lifetime.ApplicationStopping.Register(() =>
            {
                if (!scheduler.IsShutdown)
                {
                    if (waitForJobsToComplete != null)
                        scheduler.Shutdown(waitForJobsToComplete.Value).Wait();
                    else
                        scheduler.Shutdown().Wait();
                }
            });

            //启动Scheduler
            if (!scheduler.IsShutdown && !scheduler.IsStarted)
            {
                scheduler.Start().Wait();
            }

            return scheduler;
        }

        /// <summary>
        /// 序列化为二进制数据，用于序列化对象为数据库中的BLOB类型数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <param name="serializerType">序列化工具类型：json或者binary</param>
        /// <returns></returns>
        public static byte[] Serialize<T>(this T @this, string serializerType = "json") where T : class
        {
            IObjectSerializer serializer;

            if (serializerType == "json")
                serializer = new JsonObjectSerializer();
            else
                serializer = new BinaryObjectSerializer();

            serializer?.Initialize();

            return serializer?.Serialize(@this);
        }

        /// <summary>
        /// 反序列化二进制数据，用于反序列化JOB_DATA这种BLOB类型数据；
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <param name="serializerType">序列化工具类型：json或者binary</param>
        /// <returns></returns>
        public static T Deserialize<T>(this byte[] @this, string serializerType = "json") where T : class
        {
            IObjectSerializer serializer;

            if (serializerType == "json")
                serializer = new JsonObjectSerializer();
            else
                serializer = new BinaryObjectSerializer();

            serializer?.Initialize();

            return serializer?.DeSerialize<T>(@this);
        }

        /// <summary>
        /// UtcTicks转换为本地时区的DateTimeOffset
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static DateTimeOffset? ToDateTimeOffsetFromUtcTicks(this long @this)
        {
            var ticks = Convert.ToInt64(@this, CultureInfo.CurrentCulture);
            if (ticks > 0)
            {
                var time = new DateTimeOffset(ticks, TimeSpan.Zero);
                return time.ConvertTime(TimeZoneInfo.Local);
            }
            return null;
        }
    }

    /// <summary>
    /// IJobFactory接口的Ioc实现类
    /// </summary>
    public class IocJobFactory : IJobFactory
    {
        /// <summary>
        /// IServiceProvider
        /// </summary>
        private readonly IServiceProvider _provider;

        /// <summary>
        /// IocJobFactory
        /// </summary>
        /// <param name="provider"></param>
        public IocJobFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Called by the scheduler at the time of the trigger firing, 
        /// in order to produce a Quartz.IJob instance on which to call Execute.
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return _provider.GetService(bundle.JobDetail.JobType) as IJob;
        }

        /// <summary>
        /// Allows the job factory to destroy/cleanup the job if needed.
        /// </summary>
        /// <param name="job"></param>
        public void ReturnJob(IJob job)
        {

        }
    }
}
