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

using Quartz;
using Quartz.Impl;
using Quartz.Impl.Triggers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
/****************************
* [Author] 张强
* [Date] 2018-11-23
* [Describe] Quartz作业调度工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Quartz作业调度工具类
    /// </summary>
    public class QuartzHelper
    {
        #region StdSchedulerFactory
        /// <summary>
        /// 调度工厂
        /// </summary>
        private static ISchedulerFactory schedulerFactory;

        /// <summary>
        /// 调度工厂配置
        /// </summary>
        private static readonly QuartzSchedulerFactoryOptions options;
        #endregion

        #region QuartzHelper
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static QuartzHelper()
        {
            //appsettings获取调度工厂配置
            options = new QuartzSchedulerFactoryOptions();
            ConfigHelper.Bind("QuartzNet", options);

            //初始化参数
            var props = new NameValueCollection
            {
                /***
                * misfireThreshold是用来设置调度引擎对触发器超时的忍耐时间，简单来说misfireThreshold=60000(单位毫秒)默认值。
                * 那么它的意思说当一个触发器超时时间如果大于misfireThreshold的值 就认为这个触发器真正的超时(也叫Misfires)。
                * 如果一个触发器超时时间 小于misfireThreshold的值， 那么调度引擎则不认为触发器超时。也就是说调度引擎可以忍受这个超时的时间。
                */
                ["quartz.jobStore.misfireThreshold"] = options.MisfireThreshold
            };

            //实例名称
            if (!options.SchedulerName.IsNullOrEmpty())
            {
                props["quartz.scheduler.instanceName"] = options.SchedulerName;
            }

            //时区转换插件
            if (!options.TimeZoneConverterPlugin.IsNullOrEmpty())
            {
                props["quartz.plugin.timeZoneConverter.type"] = options.TimeZoneConverterPlugin;
            }

            //是否启用持久化
            if (options.EnablePersistence)
            {
                //存储类型
                props["quartz.jobStore.type"] = options.PersistenceType;
                //序列化类型
                props["quartz.serializer.type"] = options.SerializerType;
                //数据库表名称前缀
                props["quartz.jobStore.tablePrefix"] = options.TablePrefix;
                //驱动委托类型：SqlServerDelegate、PostgreSQLDelegate、MySQLDelegate、FirebirdDelegate、OracleDelegate、SQLiteDelegate
                props["quartz.jobStore.driverDelegateType"] = options.DriverDelegateType;
                //数据源名称
                props["quartz.jobStore.dataSource"] = options.DataSource;
                //连接字符串
                props[$"quartz.dataSource.{options.DataSource}.connectionString"] = options.ConnectionString;
                //数据库驱动类型：SqlServer、Npgsql、MySql、Firebird、OracleODPManaged、SQLite
                props[$"quartz.dataSource.{options.DataSource}.provider"] = options.Provider;
                //数据库最大链接数
                props[$"quartz.dataSource.{options.DataSource}.maxConnections"] = options.MaxConnections;
                //是否使用JobDataMap
                props["quartz.jobStore.useProperties"] = options.UseProperties;
                //是否集群
                props["quartz.jobStore.clustered"] = options.Clustered;
                //集群id，要保证唯一，设为AUTO
                props["quartz.scheduler.instanceId"] = options.InstanceId;
                //并发线程数
                props["quartz.threadPool.threadCount"] = options.ThreadCount;
            }

            //实例化工厂
            schedulerFactory = new StdSchedulerFactory(props);
        }
        #endregion

        #region SetStdSchedulerFactoryProps
        /// <summary>
        /// 自定义调度工厂参数
        /// </summary>
        /// <param name="props">参数集</param>
        public static void SetStdSchedulerFactoryProps(NameValueCollection props)
        {
            if (props == null)
                throw new ArgumentException("参数不能为空！");

            if (props["quartz.jobStore.misfireThreshold"].IsNullOrEmpty())
                props["quartz.jobStore.misfireThreshold"] = "100";

            schedulerFactory = new StdSchedulerFactory(props);
        }
        #endregion

        #region QuartzJobAttribute
        /// <summary>
        /// 获取IJob实例特性
        /// </summary>
        /// <param name="type">IJob实例Type类型</param>
        /// <returns></returns>
        public static QuartzJobAttribute GetQuartzJobAttribute(Type type)
        {
            var attribute = type.GetAttribute<QuartzJobAttribute>();
            return attribute ?? new QuartzJobAttribute
            {
                JobName = $"{type.Name}_Job",
                JobGroupName = $"{type.Name}_JobGroup",
                TriggerName = $"{type.Name}_Trigger",
                TriggerGroupName = $"{type.Name}_TriggerGroup"
            };
        }

        /// <summary>
        /// 获取IJob实例特性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static QuartzJobAttribute GetQuartzJobAttribute<T>() where T : IJob
        {
            return GetQuartzJobAttribute(typeof(T));
        }
        #endregion

        #region Scheduler
        /// <summary>
        /// 获取所有的调度器
        /// </summary>
        /// <returns></returns>
        public static async Task<IReadOnlyList<IScheduler>> GetAllSchedulersAsync()
        {
            return await schedulerFactory.GetAllSchedulers();
        }

        /// <summary>
        /// 获取调度器，会根据配置调度名称获取，没有配置名称获取默认调度
        /// </summary>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IScheduler> GetSchedulerAsync(string schedName = null)
        {
            if (schedName.IsNullOrEmpty())
                return await schedulerFactory.GetScheduler();
            else
                return await schedulerFactory.GetScheduler(schedName);
        }

        /// <summary>
        /// 启动调度器
        /// </summary>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task StartAsync(string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.Start();
        }

        /// <summary>
        /// 延迟启动调度器
        /// </summary>
        /// <param name="dely">延迟时间</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task StartDelayedAsync(TimeSpan dely, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.StartDelayed(dely);
        }

        /// <summary>
        /// 关闭调度器
        /// </summary>
        /// <param name="schedName"></param>
        /// <returns></returns>
        public static async Task ShutdownAsync(string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);

            if (!sched.IsShutdown)
                await sched.Shutdown();
        }

        /// <summary>
        /// 关闭调度器
        /// </summary>
        /// <param name="waitForJobsToComplete">是否等待Job执行完成</param>
        /// <param name="schedName"></param>
        /// <returns></returns>
        public static async Task ShutdownAsync(bool waitForJobsToComplete, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);

            if (!sched.IsShutdown)
                await sched.Shutdown(waitForJobsToComplete);
        }
        #endregion

        #region Job
        #region GetJobAsync
        /// <summary>
        /// 获取一个任务
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IJobDetail> GetJobAsync<T>(string schedName = null) where T : IJob
        {
            return await GetJobAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 获取一个任务
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IJobDetail> GetJobAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await GetJobAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 获取一个任务
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IJobDetail> GetJobAsync(string jobName, string jobGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.GetJobDetail(new JobKey(jobName, jobGroupName));
        }
        #endregion

        #region AddJobAsync
        /// <summary>
        /// 添加一个定时任务，注意：一个Job可以对应多个Trigger，但一个Trigger只能对应一个Job
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="jobDescription">Job描述</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="jobData">自定义Job数据</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddJobAsync<T>(
            string cron,
            string jobDescription = null,
            string triggerDescription = null,
            Dictionary<string, object> jobData = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
            where T : IJob
        {
            await AddJobAsync(typeof(T), cron, jobDescription, triggerDescription, jobData, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个定时任务，注意：一个Job可以对应多个Trigger，但一个Trigger只能对应一个Job
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="jobDescription">Job描述</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="jobData">自定义Job数据</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddJobAsync(
            Type type,
            string cron,
            string jobDescription = null,
            string triggerDescription = null,
            Dictionary<string, object> jobData = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await AddJobAsync(type, attribute.JobName, attribute.JobGroupName, attribute.TriggerName, attribute.TriggerGroupName, cron, jobDescription ?? attribute.JobDescription, triggerDescription ?? attribute.TriggerDescription, jobData, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个定时任务，注意：一个Job可以对应多个Trigger，但一个Trigger只能对应一个Job
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="jobDescription">Job描述</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="jobData">自定义Job数据</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddJobAsync<T>(
            string jobName,
            string jobGroupName,
            string triggerName,
            string triggerGroupName,
            string cron,
            string jobDescription = null,
            string triggerDescription = null,
            Dictionary<string, object> jobData = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
            where T : IJob
        {
            await AddJobAsync(typeof(T), jobName, jobGroupName, triggerName, triggerGroupName, cron, jobDescription, triggerDescription, jobData, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个定时任务，注意：一个Job可以对应多个Trigger，但一个Trigger只能对应一个Job
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="jobDescription">Job描述</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="jobData">自定义Job数据</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddJobAsync(
            Type type,
            string jobName,
            string jobGroupName,
            string triggerName,
            string triggerGroupName,
            string cron,
            string jobDescription = null,
            string triggerDescription = null,
            Dictionary<string, object> jobData = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            //获取调度器
            var sched = await GetSchedulerAsync(schedName);

            //Job自定义数据
            var jobDataMap = new JobDataMap();
            if (jobData?.Count > 0)
            {
                foreach (var item in jobData)
                {
                    jobDataMap[item.Key] = item.Value;
                }
            }
            //创建Job
            var jobDetail = JobBuilder
                                .Create(type)
                                .WithIdentity(jobName, jobGroupName)
                                .WithDescription(jobDescription ?? "")
                                .UsingJobData(jobDataMap)
                                .Build();

            //Trigger自定义数据
            var triggerDataMap = new JobDataMap();
            if (triggerData?.Count > 0)
            {
                foreach (var item in triggerData)
                {
                    triggerDataMap[item.Key] = item.Value;
                }
            }
            //创建Trigger
            var trigger = TriggerBuilder
                            .Create()
                            .WithIdentity(new TriggerKey(triggerName, triggerGroupName))
                            .WithSchedule(CronScheduleBuilder
                                .CronSchedule(cron)
                                .WithMisfireHandlingInstructionDoNothing())//对错过的内容不再执行
                            .WithDescription(triggerDescription ?? "")
                            .UsingJobData(triggerDataMap)
                            .Build();

            //调度容器设置JobDetail和Trigger
            await sched.ScheduleJob(jobDetail, trigger);

            //启动  
            if (!sched.IsShutdown && !sched.IsStarted && (start ?? options.JobOrTriggerAutoStart ?? true))
                await sched.Start();
        }
        #endregion

        #region RemoveJobAsync
        /// <summary>
        /// 移除一个任务
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveJobAsync<T>(string schedName = null) where T : IJob
        {
            return await RemoveJobAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 移除一个任务
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveJobAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await RemoveJobAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 移除一个任务
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveJobAsync(string jobName, string jobGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.DeleteJob(new JobKey(jobName, jobGroupName));
        }

        /// <summary>
        /// 移除多个任务
        /// </summary>
        /// <param name="jobs">任务名和任务组名字典集合</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveJobsAsync(Dictionary<string, string> jobs, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.DeleteJobs(jobs.Select(o => new JobKey(o.Key, o.Value)).ToList());
        }
        #endregion

        #region UpdateJobAsync
        /// <summary>
        /// 更新一个任务的触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task UpdateJobAsync<T>(
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
            where T : IJob
        {
            await UpdateJobAsync(typeof(T), cron, triggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 更新一个任务的触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task UpdateJobAsync(
            Type type,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await UpdateJobAsync(attribute.JobName, attribute.JobGroupName, attribute.TriggerName, attribute.TriggerGroupName, cron, triggerDescription ?? attribute.TriggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 更新一个任务的触发器
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="triggerName">新触发器名</param>
        /// <param name="triggerGroupName">新触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task UpdateJobAsync(
            string jobName,
            string jobGroupName,
            string triggerName,
            string triggerGroupName,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            var triggers = await sched.GetTriggersOfJob(new JobKey(jobName, jobGroupName));
            if (triggers?.Count > 0)
            {
                var cronTrigger = triggers.Select(x => (CronTriggerImpl)x).FirstOrDefault(x => x.Name == triggerName && x.Group == triggerGroupName);
                if (cronTrigger != null)
                {
                    //Trigger自定义数据
                    var jobDataMap = cronTrigger.JobDataMap ?? new JobDataMap();
                    if (triggerData?.Count > 0)
                    {
                        foreach (var item in triggerData)
                        {
                            jobDataMap[item.Key] = item.Value;
                        }
                    }

                    //创建Trigger 
                    var trigger = TriggerBuilder
                                        .Create()
                                        .WithIdentity(triggerName, triggerGroupName)
                                        .WithSchedule(CronScheduleBuilder
                                            .CronSchedule(cron)
                                            .WithMisfireHandlingInstructionDoNothing())
                                        .WithDescription(triggerDescription ?? cronTrigger.Description)
                                        .UsingJobData(jobDataMap)
                                        .Build();

                    //重新绑定Trigger
                    await sched.RescheduleJob(cronTrigger.Key, trigger);

                    // 启动 
                    if (!sched.IsShutdown && !sched.IsStarted && (start ?? options.JobOrTriggerAutoStart ?? true))
                        await sched.Start();
                }
            }
        }
        #endregion

        #region PauseJobAsync
        /// <summary>
        /// 暂停一个任务
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseJobAsync<T>(string schedName = null) where T : IJob
        {
            await PauseJobAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 暂停一个任务
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseJobAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await PauseJobAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 暂停一个任务
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseJobAsync(string jobName, string jobGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.PauseJob(new JobKey(jobName, jobGroupName));
        }
        #endregion

        #region ResumeJobAsync
        /// <summary>
        /// 恢复一个任务
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeJobAsync<T>(string schedName = null) where T : IJob
        {
            await ResumeJobAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 恢复一个任务
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeJobAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await ResumeJobAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 恢复一个任务
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeJobAsync(string jobName, string jobGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.ResumeJob(new JobKey(jobName, jobGroupName));
        }
        #endregion

        #region CheckJobExistsAsync
        /// <summary>
        /// 检查一个任务是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> CheckJobExistsAsync<T>(string schedName = null) where T : IJob
        {
            var attribute = GetQuartzJobAttribute<T>();
            return await CheckJobExistsAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 检查一个任务是否存在
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroup">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> CheckJobExistsAsync(string jobName, string jobGroup, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.CheckExists(new JobKey(jobName, jobGroup));
        }
        #endregion

        #region IsJobGroupPausedAsync
        /// <summary>
        /// 判断任务组是否处于暂停状态
        /// </summary>
        /// <param name="groupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> IsJobGroupPausedAsync(string groupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.IsJobGroupPaused(groupName);
        }
        #endregion

        #region ShutdownAllAsync
        /// <summary>
        /// 关闭所有任务，并清除所有资源
        /// </summary>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ShutdownAllAsync(string schedName = null)
        {
            await ShutdownAsync(schedName);
        }
        #endregion

        #region PauseAllAsync
        /// <summary>
        /// 暂停所有任务
        /// </summary>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseAllAsync(string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.PauseAll();
        }
        #endregion

        #region ResumeAllAsync
        /// <summary>
        /// 恢复所有任务
        /// </summary>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeAllAsync(string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.ResumeAll();
        }
        #endregion
        #endregion

        #region Trigger
        #region GetTriggerStateAsync
        /// <summary>
        /// 获取指定触发器的状态
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<TriggerState> GetTriggerStateAsync<T>(string schedName = null) where T : IJob
        {
            return await GetTriggerStateAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 获取指定触发器的状态
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<TriggerState> GetTriggerStateAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await GetTriggerStateAsync(attribute.TriggerName, attribute.TriggerGroupName, schedName);
        }

        /// <summary>
        /// 获取指定触发器的状态
        /// </summary>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<TriggerState> GetTriggerStateAsync(string triggerName, string triggerGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.GetTriggerState(new TriggerKey(triggerName, triggerGroupName));
        }

        /// <summary>
        /// 获取指定任务的所有触发器状态
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<List<TriggerState>> GetTriggerStatesAsync<T>(string schedName = null) where T : IJob
        {
            return await GetTriggerStatesAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 获取指定任务的所有触发器状态
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<List<TriggerState>> GetTriggerStatesAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await GetTriggerStatesAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 获取指定任务的所有触发器状态
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<List<TriggerState>> GetTriggerStatesAsync(string jobName, string jobGroupName, string schedName = null)
        {
            var list = new List<TriggerState>();
            var sched = await GetSchedulerAsync(schedName);
            var triggers = await sched.GetTriggersOfJob(new JobKey(jobName, jobGroupName));
            foreach (var trigger in triggers)
            {
                list.Add(await sched.GetTriggerState(trigger.Key));
            }
            return list;
        }
        #endregion

        #region GetTriggerAsync
        /// <summary>
        /// 获取一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<ITrigger> GetTriggerAsync<T>(string schedName = null) where T : IJob
        {
            return await GetTriggerAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 获取一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<ITrigger> GetTriggerAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await GetTriggerAsync(attribute.TriggerName, attribute.TriggerGroupName, schedName);
        }

        /// <summary>
        /// 获取一个触发器
        /// </summary>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<ITrigger> GetTriggerAsync(string triggerName, string triggerGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.GetTrigger(new TriggerKey(triggerName, triggerGroupName));
        }

        /// <summary>
        /// 获取指定任务对应的所有触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IReadOnlyCollection<ITrigger>> GetTriggersAsync<T>(string schedName = null) where T : IJob
        {
            return await GetTriggersAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 获取指定任务对应的所有触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IReadOnlyCollection<ITrigger>> GetTriggersAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await GetTriggersAsync(attribute.JobName, attribute.JobGroupName, schedName);
        }

        /// <summary>
        /// 获取指定任务对应的所有触发器
        /// </summary>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<IReadOnlyCollection<ITrigger>> GetTriggersAsync(string jobName, string jobGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.GetTriggersOfJob(new JobKey(jobName, jobGroupName));
        }
        #endregion

        #region AddTriggerAsync
        /// <summary>
        /// 添加一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddTriggerAsync<T>(
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
            where T : IJob
        {
            await AddTriggerAsync(typeof(T), cron, triggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddTriggerAsync(
            Type type,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            await AddTriggerAsync(type, null, null, cron, triggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddTriggerAsync<T>(
            string triggerName,
            string triggerGroupName,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
            where T : IJob
        {
            await AddTriggerAsync(typeof(T), triggerName, triggerGroupName, cron, triggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddTriggerAsync(
            Type type,
            string triggerName,
            string triggerGroupName,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await AddTriggerAsync(type, attribute.JobName, attribute.JobGroupName, triggerName, triggerGroupName, cron, triggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddTriggerAsync<T>(
            string jobName,
            string jobGroupName,
            string triggerName,
            string triggerGroupName,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
            where T : IJob
        {
            await AddTriggerAsync(typeof(T), jobName, jobGroupName, triggerName, triggerGroupName, cron, triggerDescription, triggerData, schedName, start);
        }

        /// <summary>
        /// 添加一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="jobName">任务名</param>
        /// <param name="jobGroupName">任务组名</param>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="cron">时间设置，参考quartz说明文档</param>
        /// <param name="triggerDescription">Trigger描述</param>
        /// <param name="triggerData">自定义Trigger数据</param>
        /// <param name="schedName">调度器名称</param>
        /// <param name="start">是否启用调度器Start</param>
        /// <returns></returns>
        public static async Task AddTriggerAsync(
            Type type,
            string jobName,
            string jobGroupName,
            string triggerName,
            string triggerGroupName,
            string cron,
            string triggerDescription = null,
            Dictionary<string, object> triggerData = null,
            string schedName = null,
            bool? start = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            var triggers = (await sched.GetTriggersOfJob(new JobKey(jobName, jobGroupName))).ToList();
            if (triggers?.Count > 0)
            {
                //获取原有Job
                var job = (await sched.GetJobDetail(new JobKey(jobName, jobGroupName)));
                //删除原有job
                await sched.DeleteJob(new JobKey(jobName, jobGroupName));

                //创建新触发器  
                var triggerBuilder = TriggerBuilder.Create();
                //判断触发器是否为空
                if (!triggerName.IsNullOrEmpty() && !triggerGroupName.IsNullOrEmpty())
                {
                    triggerBuilder = triggerBuilder.WithIdentity(triggerName, triggerGroupName);
                }

                //Trigger自定义数据
                var jobDataMap = new JobDataMap();
                if (triggerData?.Count > 0)
                {
                    foreach (var item in triggerData)
                    {
                        jobDataMap[item.Key] = item.Value;
                    }
                }
                //创建Trigger
                var triggerNew = triggerBuilder
                                    .WithSchedule(CronScheduleBuilder
                                        .CronSchedule(cron)
                                        .WithMisfireHandlingInstructionDoNothing())
                                    .WithDescription(triggerDescription ?? "")
                                    .UsingJobData(jobDataMap)
                                    .Build();
                //添加Trigger到Triggers
                triggers.Add(triggerNew);

                //重新创建job
                var jobDetail = JobBuilder
                                    .Create(type)
                                    .WithIdentity(jobName, jobGroupName)
                                    .WithDescription(job.Description ?? "")
                                    .UsingJobData(job.JobDataMap ?? new JobDataMap())
                                    .Build();

                //调度容器设置JobDetail和Trigger
                await sched.ScheduleJob(jobDetail, triggers, true);

                //启动  
                if (!sched.IsShutdown && !sched.IsStarted && (start ?? options.JobOrTriggerAutoStart ?? true))
                    await sched.Start();
            }
        }
        #endregion

        #region RemoveTriggerAsync
        /// <summary>
        /// 移除一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveTriggerAsync<T>(string schedName = null) where T : IJob
        {
            return await RemoveTriggerAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 移除一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveTriggerAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            return await RemoveTriggerAsync(attribute.TriggerName, attribute.TriggerGroupName, schedName);
        }

        /// <summary>
        /// 移除一个触发器
        /// </summary>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveTriggerAsync(string triggerName, string triggerGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.UnscheduleJob(new TriggerKey(triggerName, triggerGroupName));
        }

        /// <summary>
        /// 移除多个触发器
        /// </summary>
        /// <param name="triggers">触发器名和触发器组名字典集合</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> RemoveTriggersAsync(Dictionary<string, string> triggers, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.UnscheduleJobs(triggers.Select(o => new TriggerKey(o.Key, o.Value)).ToList());
        }
        #endregion

        #region PauseTriggerAsync
        /// <summary>
        /// 暂停一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseTriggerAsync<T>(string schedName = null) where T : IJob
        {
            await PauseTriggerAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 暂停一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseTriggerAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await PauseTriggerAsync(attribute.TriggerName, attribute.TriggerGroupName, schedName);
        }

        /// <summary>
        /// 暂停一个触发器
        /// </summary>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task PauseTriggerAsync(string triggerName, string triggerGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.PauseTrigger(new TriggerKey(triggerName, triggerGroupName));
        }
        #endregion

        #region ResumeTriggerAsync
        /// <summary>
        /// 恢复一个触发器
        /// </summary>
        /// <typeparam name="T">IJob实现类</typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeTriggerAsync<T>(string schedName = null) where T : IJob
        {
            await ResumeTriggerAsync(typeof(T), schedName);
        }

        /// <summary>
        /// 恢复一个触发器
        /// </summary>
        /// <param name="type">IJob实现类Type</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeTriggerAsync(Type type, string schedName = null)
        {
            var attribute = GetQuartzJobAttribute(type);
            await ResumeTriggerAsync(attribute.TriggerName, attribute.TriggerGroupName, schedName);
        }

        /// <summary>
        /// 恢复一个触发器
        /// </summary>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroupName">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task ResumeTriggerAsync(string triggerName, string triggerGroupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            await sched.ResumeTrigger(new TriggerKey(triggerName, triggerGroupName));
        }
        #endregion

        #region CheckTriggerExistsAsync
        /// <summary>
        /// 检查一个触发器是否存在
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> CheckTriggerExistsAsync<T>(string schedName = null) where T : IJob
        {
            var attribute = GetQuartzJobAttribute<T>();
            return await CheckTriggerExistsAsync(attribute.TriggerName, attribute.TriggerGroupName, schedName);
        }

        /// <summary>
        /// 检查一个触发器是否存在
        /// </summary>
        /// <param name="triggerName">触发器名</param>
        /// <param name="triggerGroup">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> CheckTriggerExistsAsync(string triggerName, string triggerGroup, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.CheckExists(new TriggerKey(triggerName, triggerGroup));
        }
        #endregion

        #region IsTriggerGroupPausedAsync
        /// <summary>
        /// 判断任务组是否处于暂停状态
        /// </summary>
        /// <param name="groupName">触发器组名</param>
        /// <param name="schedName">调度器名称</param>
        /// <returns></returns>
        public static async Task<bool> IsTriggerGroupPausedAsync(string groupName, string schedName = null)
        {
            var sched = await GetSchedulerAsync(schedName);
            return await sched.IsTriggerGroupPaused(groupName);
        }
        #endregion
        #endregion
    }

    /// <summary>
    /// IJob实例特性，用于设置：任务名、任务组名、触发器名、触发器组名
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class QuartzJobAttribute : Attribute
    {
        /// <summary>
        /// 任务名
        /// </summary>
        public string JobName { get; set; }

        /// <summary>
        /// 任务组名
        /// </summary>
        public string JobGroupName { get; set; }

        /// <summary>
        /// 任务描述
        /// </summary>
        public string JobDescription { get; set; }

        /// <summary>
        /// 触发器名
        /// </summary>
        public string TriggerName { get; set; }

        /// <summary>
        /// 触发器组名
        /// </summary>
        public string TriggerGroupName { get; set; }

        /// <summary>
        /// 触发器描述
        /// </summary>
        public string TriggerDescription { get; set; }
    }

    /// <summary>
    /// Quartz调度工厂配置
    /// </summary>
    public class QuartzSchedulerFactoryOptions
    {
        /// <summary>
        /// misfireThreshold是用来设置调度引擎对触发器超时的忍耐时间，简单来说misfireThreshold=60000(单位毫秒)默认值。
        /// 那么它的意思说当一个触发器超时时间如果大于misfireThreshold的值 就认为这个触发器真正的超时(也叫Misfires)。
        /// 如果一个触发器超时时间 小于misfireThreshold的值， 那么调度引擎则不认为触发器超时。也就是说调度引擎可以忍受这个超时的时间。
        /// </summary>
        public string MisfireThreshold { get; set; } = "100";

        /// <summary>
        /// 实例名称
        /// </summary>
        public string SchedulerName { get; set; }

        /// <summary>
        /// 时区转换插件，如：Quartz.Plugin.TimeZoneConverter.TimeZoneConverterPlugin, Quartz.Plugins.TimeZoneConverter
        /// </summary>
        public string TimeZoneConverterPlugin { get; set; }

        /// <summary>
        /// 是否持久化
        /// </summary>
        public bool EnablePersistence { get; set; } = false;

        /// <summary>
        /// 存储类型
        /// </summary>
        public string PersistenceType { get; set; } = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz";

        /// <summary>
        /// 序列化类型，如：json、binary
        /// </summary>
        public string SerializerType { get; set; } = "json";

        /// <summary>
        /// 数据库表名称前缀
        /// </summary>
        public string TablePrefix { get; set; } = "Quartz_";

        /// <summary>
        /// 驱动委托类型：SqlServerDelegate、PostgreSQLDelegate、MySQLDelegate、FirebirdDelegate、OracleDelegate、SQLiteDelegate
        /// </summary>
        public string DriverDelegateType { get; set; }

        /// <summary>
        /// 数据源名称
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 数据库驱动类型：SqlServer、Npgsql、MySql、Firebird、OracleODPManaged、SQLite
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// 数据库最大链接数
        /// </summary>
        public string MaxConnections { get; set; } = "20";

        /// <summary>
        /// 是否使用JobDataMap
        /// </summary>
        public string UseProperties { get; set; } = "true";

        /// <summary>
        /// 是否集群
        /// </summary>
        public string Clustered { get; set; } = "true";

        /// <summary>
        /// 集群id，要保证唯一，设为AUTO
        /// </summary>
        public string InstanceId { get; set; } = "AUTO";

        /// <summary>
        /// 并发线程数
        /// </summary>
        public string ThreadCount { get; set; } = "20";

        /// <summary>
        /// 任务或者触发器是否自动Start
        /// </summary>
        public bool? JobOrTriggerAutoStart { get; set; }
    }
}
