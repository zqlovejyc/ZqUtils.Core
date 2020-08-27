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

using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System;
using System.Linq;
using System.Reflection;
using ZqUtils.Core.Attributes;
using ZqUtils.Core.Helpers;
using RedisClient = CSRedis.CSRedisClient;
/****************************
* [Author] 张强
* [Date] 2018-05-17
* [Describe] IServiceCollection扩展类
* **************************/
namespace ZqUtils.Core.Extensions
{
    using Microsoft.Extensions.Configuration;

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
            string lifeTime = "Transient")
        {
            //扫描程序集获取指定条件下的类型集合
            var types = PathHelper.GetTypesFromAssembly(filter: assemblyFilter);

            //获取基接口所有继承者
            var inherits = types.Where(x => baseType.IsAssignableFrom(x) && x != baseType).Distinct();
            if (typeFilter != null)
                inherits = inherits.Where(typeFilter);

            //获取所有实现类
            var implementationTypes = inherits?.Where(x => x.IsClass);
            if (implementationTypes?.Count() > 0)
            {
                foreach (var implementationType in implementationTypes)
                {
                    //获取继承接口
                    var serviceTypes = implementationType.GetInterfaces()?.Where(x => x != baseType);
                    if (serviceTypes?.Count() > 0)
                    {
                        foreach (var serviceType in serviceTypes)
                        {
                            switch (lifeTime.ToLower())
                            {
                                case "singleton":
                                    @this.AddSingleton(serviceType, implementationType);
                                    break;
                                case "transient":
                                    @this.AddTransient(serviceType, implementationType);
                                    break;
                                case "scoped":
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
            var assemblies = PathHelper.GetAssemblies(filter: assemblyFilter);

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
            var types = PathHelper.GetTypesFromAssembly(filter: assemblyFilter);
            if (types.IsNotNullOrEmpty())
            {
                var dependsOnTypes = types.Where(x => x.ContainsAttribute<DependsOnAttribute>()).Distinct();
                if (dependsOnTypes.IsNotNullOrEmpty())
                {
                    foreach (var type in dependsOnTypes)
                    {
                        var dependsOns = type.GetCustomAttributes(typeof(DependsOnAttribute), false).Select(x => (DependsOnAttribute)x);
                        var dependedTypes = dependsOns.Select(x => x.DependedType).Distinct();
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

        #region AddCsRedis
        /// <summary>
        /// 初始化CSRedisCore，之后程序中可以直接使用RedisHelper的静态方法
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddCsRedis(this IServiceCollection @this, IConfiguration configuration)
        {
            var connectionStrings = configuration.GetSection("Redis:ConnectionStrings").Get<string[]>();

            RedisClient client;
            if (connectionStrings.Length == 1)
                //普通模式
                client = new RedisClient(connectionStrings[0]);
            else
                //集群模式
                //实现思路：根据key.GetHashCode() % 节点总数量，确定连向的节点
                //也可以自定义规则(第一个参数设置)
                client = new RedisClient(null, connectionStrings);

            //初始化 RedisHelper
            RedisHelper.Initialization(client);

            return @this;
        }
        #endregion

        #region AddRabbitMq
        /// <summary>
        /// 注入RabbitMq，单例模式
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddRabbitMq(this IServiceCollection @this, IConfiguration configuration)
        {
            return @this.AddSingleton(new RabbitMqHelper(configuration.GetSection("RabbitMq").Get<MqConfig>()));
        }
        #endregion

        #region AddMongoDb
        /// <summary>
        /// 注入MongoDb，单例模式
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static IServiceCollection AddMongoDb(this IServiceCollection @this)
        {
            return @this.AddSingleton(new MongodbHelper());
        }
        #endregion
    }
}
