#region License
/***
 * Copyright © 2018-2025, 张强 (943620963@qq.com).
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

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
/****************************
* [Author] 张强
* [Date] 2021-09-01
* [Describe] IConfiguration扩展类
* **************************/
namespace ZqUtils.Core.Extensions
{
    /// <summary>
    /// IConfiguration扩展类
    /// </summary>
    public static class IConfigurationExtensions
    {
        /// <summary>
        /// ReadInt32
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int? ReadInt32(this IConfiguration configuration, string name)
        {
            return configuration[name] is string value ? int.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// ReadDouble
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static double? ReadDouble(this IConfiguration configuration, string name)
        {
            return configuration[name] is string value ? double.Parse(value, CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// ReadTimeSpan
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TimeSpan? ReadTimeSpan(this IConfiguration configuration, string name)
        {
            // Format "c" => [-][d'.']hh':'mm':'ss['.'fffffff]. 
            // You also can find more info at https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier
            return configuration[name] is string value ? TimeSpan.ParseExact(value, "c", CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// ReadUri
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Uri ReadUri(this IConfiguration configuration, string name)
        {
            return configuration[name] is string value ? new Uri(value) : null;
        }

        /// <summary>
        /// ReadEnum
        /// </summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static TEnum? ReadEnum<TEnum>(this IConfiguration configuration, string name) where TEnum : struct
        {
            if (configuration[name] is string value)
            {
                if (Enum.TryParse<TEnum>(value, true, out var result))
                    return result;
            }

            return null;
        }

        /// <summary>
        /// ReadBool
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool? ReadBool(this IConfiguration configuration, string name)
        {
            return configuration[name] is string value ? bool.Parse(value) : null;
        }

        /// <summary>
        /// ReadVersion
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Version ReadVersion(this IConfiguration configuration, string name)
        {
            return configuration[name] is string value && !string.IsNullOrEmpty(value) ? Version.Parse(value + (value.Contains('.') ? "" : ".0")) : null;
        }

        /// <summary>
        /// ReadStringDictionary
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static IReadOnlyDictionary<string, string> ReadStringDictionary(this IConfigurationSection section)
        {
            if (section.GetChildren() is var children && !children.Any())
            {
                return null;
            }

            return new ReadOnlyDictionary<string, string>(children.ToDictionary(s => s.Key, s => s.Value, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// ReadStringArray
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public static string[] ReadStringArray(this IConfigurationSection section)
        {
            if (section.GetChildren() is var children && !children.Any())
            {
                return null;
            }

            return children.Select(s => s.Value).ToArray();
        }
    }
}
