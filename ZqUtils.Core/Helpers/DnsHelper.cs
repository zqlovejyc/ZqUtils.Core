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

using DnsClient;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
/****************************
* [Author] 张强
* [Date] 2018-08-24
* [Describe] Dns工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// Dns工具类
    /// </summary>
    public class DnsHelper
    {
        /// <summary>
        /// 获取本地的IP地址
        /// </summary>
        /// <param name="ipv4">是否ipv4，否则ipv6，默认：ipv4</param>
        /// <param name="wifi">是否无线网卡，默认：有线网卡</param>
        /// <returns></returns>
        public static string GetIpAddress(bool ipv4 = true, bool wifi = false)
        {
            return NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(x => (wifi ?
                            x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ://WIFI
                            x.NetworkInterfaceType == NetworkInterfaceType.Ethernet) && //有线网
                            x.OperationalStatus == OperationalStatus.Up)
                        .Select(p => p.GetIPProperties())
                        .SelectMany(p => p.UnicastAddresses)
                        .Where(p => (ipv4 ?
                            p.Address.AddressFamily == AddressFamily.InterNetwork :
                            p.Address.AddressFamily == AddressFamily.InterNetworkV6) &&
                            !IPAddress.IsLoopback(p.Address))
                        .FirstOrDefault()?
                        .Address
                        .ToString();
        }

        /// <summary>
        /// 根据域名获取对应的IP地址
        /// </summary>
        /// <param name="domain">域名，如：baidu.com</param>
        /// <param name="type">请求类型</param>
        /// <returns></returns>
        public static async Task<List<string>> GetIpAddressAsync(string domain, QueryType type = QueryType.ANY)
        {
            var lookup = new LookupClient();
            var result = await lookup.QueryAsync(domain, type);
            return result.Answers
                         .ARecords()?
                         .Select(x => x.Address?.ToString())
                         .ToList();
        }

        /// <summary>
        /// <list type="bullet">
        ///     <item>获取远程客户端IP地址</item>
        /// </list>
        /// <list type="number">
        ///     <item>注意ConfigureServices里面必须要注入：services.TryAddSingleton&lt;IHttpContextAccessor, HttpContextAccessor&gt;();</item>
        ///     <item>注意Configure里面调用：app.UseHttpContext();</item>
        ///     <item>如果Jexus反代AspNetCore的话，从http头“X-Forwarded-For”可以得到客户端IP地址；</item>
        ///     <item>如果是使用Jexus的AppHost驱动Asp.Net Core应用，可以从HTTP头“X-Real-IP”或“X-Original-For”等头域中得到客户端IP</item>
        /// </list>
        /// </summary>
        /// <returns></returns>
        public static string GetClientRemoteIpAddress()
        {
            string res;
            var ip = HttpContextHelper.Current.Connection.RemoteIpAddress;
            //判断是否为回环地址
            if (ip.IsNotNull() && !IPAddress.IsLoopback(ip))
            {
                res = ip.ToString();
            }
            else
            {
                //Jexus反向代理Asp.Net Core
                res = HttpContextHelper.Current.Request.Headers["X-Forwarded-For"];
                if (res.IsNullOrEmpty() || IPAddress.IsLoopback(IPAddress.Parse(res)))
                {
                    //使用Jexus的AppHost驱动Asp.Net Core应用
                    res = HttpContextHelper.Current.Request.Headers["X-Real-IP"];
                    if (res.IsNullOrEmpty())
                        res = HttpContextHelper.Current.Request.Headers["X-Original-For"];
                }
            }
            return res;
        }
    }
}
