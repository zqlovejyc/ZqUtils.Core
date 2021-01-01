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

using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Confluent.Kafka;
using FreeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Scrutor;
using System;
using System.Linq;
using System.Reflection;
using ZqUtils.Core.Attributes;
using ZqUtils.Core.Helpers;
/****************************
* [Author] 张强
* [Date] 2018-05-17
* [Describe] IServiceCollection扩展类
* **************************/
namespace ZqUtils.Core.Extensions
{
    /// <summary>
    /// IServiceCollection扩展类
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        #region Private Static Field
        /// <summary>
        /// 私有静态字段
        /// </summary>
        private static readonly ContainerBuilder container = new ContainerBuilder();
        #endregion      

        #region RegisterAutofac
        /// <summary>
        /// 注册Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="modules"></param>
        public static void RegisterAutofac(this IServiceCollection services, params IModule[] modules)
        {
            if (modules?.Length > 0)
            {
                foreach (var module in modules)
                {
                    container.RegisterModule(module);
                }
            }
        }

        /// <summary>
        /// 注册Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies"></param>
        public static void RegisterAutofac(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies?.Length > 0)
            {
                container
                   .RegisterAssemblyTypes(assemblies)
                   .AsImplementedInterfaces()
                   .PropertiesAutowired()
                   .AsSelf();
            }
        }

        /// <summary>
        /// 注册Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="predicate"></param>
        /// <param name="assemblies"></param>
        public static void RegisterAutofac(this IServiceCollection services, Func<Type, bool> predicate, params Assembly[] assemblies)
        {
            if (assemblies?.Length > 0)
            {
                container
                   .RegisterAssemblyTypes(assemblies)
                   .Where(predicate)
                   .AsImplementedInterfaces()
                   .PropertiesAutowired()
                   .AsSelf();
            }
        }

        /// <summary>
        /// 注册Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="modules"></param>
        /// <param name="assemblies"></param>
        public static void RegisterAutofac(this IServiceCollection services, IModule[] modules, Assembly[] assemblies)
        {
            if (modules?.Length > 0)
            {
                foreach (var module in modules)
                {
                    container.RegisterModule(module);
                }
            }
            if (assemblies?.Length > 0)
            {
                container
                    .RegisterAssemblyTypes(assemblies)
                    .AsImplementedInterfaces()
                    .PropertiesAutowired()
                    .AsSelf();
            }
        }

        /// <summary>
        /// 注册Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="modules"></param>
        /// <param name="assemblies"></param>
        /// <param name="predicate"></param>
        public static void RegisterAutofac(this IServiceCollection services, IModule[] modules, Assembly[] assemblies, Func<Type, bool> predicate)
        {
            if (modules?.Length > 0)
            {
                foreach (var module in modules)
                {
                    container.RegisterModule(module);
                }
            }
            if (assemblies?.Length > 0)
            {
                container
                    .RegisterAssemblyTypes(assemblies)
                    .Where(predicate)
                    .AsImplementedInterfaces()
                    .PropertiesAutowired()
                    .AsSelf();
            }
        }
        #endregion

        #region AddAutofac
        /// <summary>
        /// 添加Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceProvider AddAutofac(this IServiceCollection services)
        {
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        /// <summary>
        /// 添加Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="modules"></param>
        /// <returns></returns>
        public static IServiceProvider AddAutofac(this IServiceCollection services, params IModule[] modules)
        {
            if (modules?.Length > 0)
            {
                foreach (var module in modules)
                {
                    container.RegisterModule(module);
                }
            }
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        /// <summary>
        /// 添加Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static IServiceProvider AddAutofac(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (assemblies?.Length > 0)
            {
                container
                   .RegisterAssemblyTypes(assemblies)
                   .AsImplementedInterfaces()
                   .PropertiesAutowired()
                   .AsSelf();
            }
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        /// <summary>
        /// 添加Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="predicate"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static IServiceProvider AddAutofac(this IServiceCollection services, Func<Type, bool> predicate, params Assembly[] assemblies)
        {
            if (assemblies?.Length > 0)
            {
                container
                   .RegisterAssemblyTypes(assemblies)
                   .Where(predicate)
                   .AsImplementedInterfaces()
                   .PropertiesAutowired()
                   .AsSelf();
            }
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        /// <summary>
        /// 添加Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="modules"></param>
        /// <param name="assemblies"></param>
        /// <returns></returns>
        public static IServiceProvider AddAutofac(this IServiceCollection services, IModule[] modules, Assembly[] assemblies)
        {
            if (modules?.Length > 0)
            {
                foreach (var module in modules)
                {
                    container.RegisterModule(module);
                }
            }
            if (assemblies?.Length > 0)
            {
                container
                       .RegisterAssemblyTypes(assemblies)
                       .AsImplementedInterfaces()
                       .PropertiesAutowired()
                       .AsSelf();
            }
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }

        /// <summary>
        /// 添加Autofac依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="modules"></param>
        /// <param name="assemblies"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IServiceProvider AddAutofac(this IServiceCollection services, IModule[] modules, Assembly[] assemblies, Func<Type, bool> predicate)
        {
            if (modules?.Length > 0)
            {
                foreach (var module in modules)
                {
                    container.RegisterModule(module);
                }
            }
            if (assemblies?.Length > 0)
            {
                container
                    .RegisterAssemblyTypes(assemblies)
                    .Where(predicate)
                    .AsImplementedInterfaces()
                    .PropertiesAutowired()
                    .AsSelf();
            }
            container.Populate(services);
            return new AutofacServiceProvider(container.Build());
        }
        #endregion

        #region AddFromAssembly
        /// <summary>
        /// 扫描程序集自动注入
        /// </summary>
        /// <param name="this">服务集合</param>
        /// <param name="baseType">基类型，如：typof(IDependency)</param>
        /// <param name="assemblyFilter">程序集过滤器</param>
        /// <param name="typeFilter">程序集中Type过滤器</param>
        /// <param name="lifeTime">生命周期，默认：Transient，其他生命周期可选值：Singleton、Scoped</param>
        /// <returns></returns>
        public static IServiceCollection AddFromAssembly(
            this IServiceCollection @this,
            Type baseType,
            Func<string, bool> assemblyFilter = null,
            Func<Type, bool> typeFilter = null,
            ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            //扫描程序集获取指定条件下的类型集合
            var types = AssemblyHelper.GetTypesFromAssembly(filter: assemblyFilter);

            //获取基接口所有继承者
            var inherits = types.Where(x => baseType.IsAssignableFrom(x) && x != baseType).Distinct();
            if (typeFilter.IsNotNull())
                inherits = inherits.Where(typeFilter);

            //获取所有实现类
            var implementationTypes = inherits?.Where(x => x.IsClass);
            if (implementationTypes.IsNotNullOrEmpty())
            {
                foreach (var implementationType in implementationTypes)
                {
                    //获取继承接口
                    var serviceTypes = implementationType.GetInterfaces()?.Where(x => x != baseType);
                    if (serviceTypes.IsNotNullOrEmpty())
                    {
                        foreach (var serviceType in serviceTypes)
                        {
                            switch (lifeTime)
                            {
                                case ServiceLifetime.Singleton:
                                    @this.AddSingleton(serviceType, implementationType);
                                    break;
                                case ServiceLifetime.Transient:
                                    @this.AddTransient(serviceType, implementationType);
                                    break;
                                case ServiceLifetime.Scoped:
                                    @this.AddScoped(serviceType, implementationType);
                                    break;
                                default:
                                    @this.AddTransient(serviceType, implementationType);
                                    break;
                            }
                        }
                    }
                }
            }

            return @this;
        }

        /// <summary>
        /// 扫描程序集自动注入
        /// </summary>
        /// <param name="this"></param>
        /// <param name="typeFilter">程序集中Type过滤器</param>
        /// <param name="assemblyFilter">程序集过滤器</param>
        /// <param name="lifeTime"></param>
        /// <returns></returns>
        public static IServiceCollection AddFromAssembly(
            this IServiceCollection @this,
            Func<IImplementationTypeFilter, IImplementationTypeFilter> typeFilter,
            Func<string, bool> assemblyFilter = null,
            ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            //获取程序集
            var assemblies = AssemblyHelper.GetAssemblies(filter: assemblyFilter);

            //扫描程序集注入接口及实现类
            return @this.Scan(scan => scan
                            .FromAssemblies(assemblies)
                            .AddClasses(classes => typeFilter(classes))
                            .AsImplementedInterfaces()
                            .AsSelf()
                            .WithLifetime(lifeTime));
        }

        /// <summary>
        /// 扫描程序集自动注入DependsOn特性的服务
        /// </summary>
        /// <param name="this"></param>
        /// <param name="assemblyFilter"></param>
        /// <param name="lifeTime"></param>
        /// <returns></returns>
        public static IServiceCollection AddDependsOnFromAssembly(
            this IServiceCollection @this,
            Func<string, bool> assemblyFilter = null,
            ServiceLifetime lifeTime = ServiceLifetime.Transient)
        {
            var types = AssemblyHelper.GetTypesFromAssembly(filter: assemblyFilter);
            if (types.IsNotNullOrEmpty())
            {
                var dependsOnTypes = types.Where(x => x.Has<DependsOnAttribute>()).Distinct();
                if (dependsOnTypes.IsNotNullOrEmpty())
                {
                    foreach (var type in dependsOnTypes)
                    {
                        var dependedTypes = type.GetCustomAttributes<DependsOnAttribute>(false).Select(x => x.DependedType).Distinct();
                        foreach (var dependedType in dependedTypes)
                        {
                            switch (lifeTime)
                            {
                                case ServiceLifetime.Singleton:
                                    @this.AddSingleton(dependedType, type);
                                    break;
                                case ServiceLifetime.Scoped:
                                    @this.AddScoped(dependedType, type);
                                    break;
                                case ServiceLifetime.Transient:
                                    @this.AddTransient(dependedType, type);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            return @this;
        }
        #endregion

        #region AddFreeRedis
        /// <summary>
        /// 注入FreeRedis
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddFreeRedis(this IServiceCollection @this, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection("Redis:ConnectionStrings").Get<string[]>();

            @this.AddSingleton(x =>
            {
                RedisClient client;
                if (connectionStrings.Length == 1)
                    //普通模式
                    client = new RedisClient(connectionStrings[0]);
                else
                    //集群模式
                    client = new RedisClient(connectionStrings.Select(v => ConnectionStringBuilder.Parse(v)).ToArray());

                //配置序列化和反序列化
                client.Serialize = obj => JsonConvert.SerializeObject(obj);
                client.Deserialize = (json, type) => JsonConvert.DeserializeObject(json, type);

                return client;
            });

            return @this;
        }
        #endregion

        #region AddStackExchangeRedis
        /// <summary>
        /// 注入RedisHelper、IConnectionMultiplexer
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddStackExchangeRedis(this IServiceCollection @this, IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("Redis:ConnectionStrings");
            if (connectionString.IsNullOrEmpty())
                connectionString = configuration.GetSection("Redis:ConnectionStrings").Get<string[]>()?.FirstOrDefault();

            if (connectionString.IsNullOrEmpty())
                throw new ArgumentNullException("Redis连接字符串配置为null");

            @this.AddTransient(x => new RedisHelper(connectionString));

            @this.AddSingleton(x => x.GetRequiredService<RedisHelper>().IConnectionMultiplexer);

            return @this;
        }
        #endregion

        #region AddMongoDb
        /// <summary>
        /// 注入MongoDb
        /// </summary>
        /// <param name="this"></param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDb(
            this IServiceCollection @this,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new MongodbHelper());
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new MongodbHelper());
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new MongodbHelper());
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入MongoDb
        /// </summary>
        /// <param name="this"></param>
        /// <param name="databaseName">数据库</param>
        /// <param name="settings">MongoClientSettings配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDb(
            this IServiceCollection @this,
            string databaseName,
            MongoClientSettings settings,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new MongodbHelper(databaseName, settings));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new MongodbHelper(databaseName, settings));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new MongodbHelper(databaseName, settings));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入MongoDb
        /// </summary>
        /// <param name="this"></param>
        /// <param name="databaseName">数据库</param>
        /// <param name="connectionString">链接字符串</param>
        /// <param name="isMongoClientSettings">是否为MongoClientSettings连接字符串，默认：false</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDb(
            this IServiceCollection @this,
            string databaseName,
            string connectionString,
            bool isMongoClientSettings = false,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new MongodbHelper(databaseName, connectionString, isMongoClientSettings));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new MongodbHelper(databaseName, connectionString, isMongoClientSettings));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new MongodbHelper(databaseName, connectionString, isMongoClientSettings));
                    break;
                default:
                    break;
            }
            return @this;
        }
        #endregion

        #region AddMongoClient
        /// <summary>
        /// 注入MongoClient
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration">appsettiongs配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection @this,
            IConfiguration configuration,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton<IMongoClient>(new MongoClient(configuration.GetValue<string>("Mongodb:ConnectionString")));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped<IMongoClient>(x => new MongoClient(configuration.GetValue<string>("Mongodb:ConnectionString")));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient<IMongoClient>(x => new MongoClient(configuration.GetValue<string>("Mongodb:ConnectionString")));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入MongoClient
        /// </summary>
        /// <param name="this"></param>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection @this,
            string connectionString,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton<IMongoClient>(new MongoClient(connectionString));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped<IMongoClient>(x => new MongoClient(connectionString));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient<IMongoClient>(x => new MongoClient(connectionString));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入MongoClient
        /// </summary>
        /// <param name="this"></param>
        /// <param name="url">MongoUrl</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection @this,
            MongoUrl url,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton<IMongoClient>(new MongoClient(url));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped<IMongoClient>(x => new MongoClient(url));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient<IMongoClient>(x => new MongoClient(url));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入MongoClient
        /// </summary>
        /// <param name="this"></param>
        /// <param name="settings">MongoClient配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection @this,
            MongoClientSettings settings,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton<IMongoClient>(new MongoClient(settings));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped<IMongoClient>(x => new MongoClient(settings));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient<IMongoClient>(x => new MongoClient(settings));
                    break;
                default:
                    break;
            }
            return @this;
        }
        #endregion

        #region AddRabbitMq
        /// <summary>
        /// 注入RabbitMq
        /// </summary>
        /// <param name="this"></param>
        /// <param name="factory">连接工厂配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection @this,
            ConnectionFactory factory,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new RabbitMqHelper(factory));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new RabbitMqHelper(factory));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new RabbitMqHelper(factory));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入RabbitMq
        /// </summary>
        /// <param name="this"></param>
        /// <param name="config">连接配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection @this,
            MqConfig config,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new RabbitMqHelper(config));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new RabbitMqHelper(config));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new RabbitMqHelper(config));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入RabbitMq
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration">json配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddRabbitMq(
            this IServiceCollection @this,
            IConfiguration configuration,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new RabbitMqHelper(configuration.GetSection("RabbitMq").Get<MqConfig>()));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new RabbitMqHelper(configuration.GetSection("RabbitMq").Get<MqConfig>()));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new RabbitMqHelper(configuration.GetSection("RabbitMq").Get<MqConfig>()));
                    break;
                default:
                    break;
            }
            return @this;
        }
        #endregion

        #region AddKafka
        /// <summary>
        /// 注入Kafka
        /// </summary>
        /// <param name="this"></param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddKafka(
            this IServiceCollection @this,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new KafkaHelper());
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new KafkaHelper());
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new KafkaHelper());
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入Kafka
        /// </summary>
        /// <param name="this"></param>
        /// <param name="config">连接配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddKafka(
            this IServiceCollection @this,
            KafkaConfig config,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new KafkaHelper(config));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new KafkaHelper(config));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new KafkaHelper(config));
                    break;
                default:
                    break;
            }
            return @this;
        }

        /// <summary>
        /// 注入Kafka
        /// </summary>
        /// <param name="this"></param>
        /// <param name="producerConfig">生产者连接配置</param>
        /// <param name="consumerConfig">消费者连接配置</param>
        /// <param name="lifeTime">生命周期，默认：单例模式</param>
        /// <returns></returns>
        public static IServiceCollection AddKafka(
            this IServiceCollection @this,
            ProducerConfig producerConfig,
            ConsumerConfig consumerConfig,
            ServiceLifetime lifeTime = ServiceLifetime.Singleton)
        {
            switch (lifeTime)
            {
                case ServiceLifetime.Singleton:
                    @this.AddSingleton(new KafkaHelper(producerConfig, consumerConfig));
                    break;
                case ServiceLifetime.Scoped:
                    @this.AddScoped(x => new KafkaHelper(producerConfig, consumerConfig));
                    break;
                case ServiceLifetime.Transient:
                    @this.AddTransient(x => new KafkaHelper(producerConfig, consumerConfig));
                    break;
                default:
                    break;
            }
            return @this;
        }
        #endregion
    }
}