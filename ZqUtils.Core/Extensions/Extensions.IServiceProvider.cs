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

using Microsoft.Extensions.DependencyInjection;
using System;
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
            if (services.IsNotNullOrEmpty())
            {
                return services.Where(o => o
                                    .GetType()
                                    .GetAttributes<DependsOnAttribute>()
                                    .Select(x => x as DependsOnAttribute)
                                    .Where(x =>
                                        x.IsNotNull() &&
                                        x.DependedType == typeof(T) &&
                                        x.Name == name)
                                    .Any()
                                ).FirstOrDefault();
            }

            return default;
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
            if (services.IsNotNullOrEmpty())
            {
                return services.Where(x => x.GetType() == type).FirstOrDefault();
            }

            return default;
        }
    }
}