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

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using ZqUtils.Core.Attributes;
/****************************
* [Author] 张强
* [Date] 2020-08-27
* [Describe] IServiceProvider扩展类
* **************************/
namespace ZqUtils.Core.Extensions
{
    /// <summary>
    /// IServiceProvider扩展类
    /// </summary>
    public static class IServiceProviderExtensions
    {
        /// <summary>
        /// 根据注入时的唯一名称获取指定的服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this">IServiceProvider</param>
        /// <param name="name">注入时的唯一名称</param>
        /// <returns></returns>
        public static T GetDependsService<T>(this IServiceProvider @this, string name)
        {
            var services = @this.GetServices<T>();
            if (services.IsNullOrEmpty())
                return default;

            return services
                    .Where(o =>
                        o.GetType().HasAttribute<DependsOnAttribute>(x =>
                        x.Name.EqualIgnoreCase(name)))
                    .FirstOrDefault();
        }

        /// <summary>
        /// 根据注入时的唯一名称获取指定的服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this">服务实例集合</param>
        /// <param name="name">注入时的唯一名称</param>
        /// <returns></returns>
        public static T GetDependsService<T>(this IEnumerable<T> @this, string name)
        {
            if (@this.IsNullOrEmpty())
                return default;

            return @this
                    .Where(o =>
                        o.GetType().HasAttribute<DependsOnAttribute>(x =>
                        x.Name.EqualIgnoreCase(name)))
                    .FirstOrDefault();
        }

        /// <summary>
        /// 根据目标服务类型获取指定的服务
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this">IServiceProvider</param>
        /// <param name="type">目标服务类型</param>
        /// <returns></returns>
        public static T GetService<T>(this IServiceProvider @this, Type type)
        {
            var services = @this.GetServices<T>();
            if (services.IsNullOrEmpty())
                return default;

            return services.Where(x => x.GetType() == type).FirstOrDefault();
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="this"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static T CreateInstance<T>(this IServiceProvider @this, params object[] parameters)
        {
            return ActivatorUtilities.CreateInstance<T>(@this, parameters);
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        /// <param name="this"></param>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object CreateInstance(this IServiceProvider @this, Type type, params object[] parameters)
        {
            return ActivatorUtilities.CreateInstance(@this, type, parameters);
        }

        /// <summary>
        /// 获取服务或创建实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <returns></returns>
        public static T GetServiceOrCreateInstance<T>(this IServiceProvider @this)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance<T>(@this);
        }

        /// <summary>
        /// 获取服务或创建实例
        /// </summary>
        /// <param name="this"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetServiceOrCreateInstance(this IServiceProvider @this, Type type)
        {
            return ActivatorUtilities.GetServiceOrCreateInstance(@this, type);
        }
    }
}