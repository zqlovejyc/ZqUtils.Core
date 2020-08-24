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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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
        #region GetIpAddressAsync
        /// <summary>
        /// 获取本地的IP地址
        /// </summary>
        /// <param name="ipv4">是否ipv4</param>
        /// <param name="hostNameOrAddress">主机名称或者地址</param>
        /// <returns></returns>
        public static async Task<string> GetIpAddressAsync(bool ipv4 = true, string hostNameOrAddress = null)
        {
            var client = new LookupClient();
            var hostEntry = await client.GetHostEntryAsync(hostNameOrAddress ?? Dns.GetHostName());
            IPAddress ipAddress = null;
            if (ipv4)
            {
                ipAddress = hostEntry
                                .AddressList
                                .Where(ip => !IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                                .FirstOrDefault();
            }
            else
            {
                ipAddress = hostEntry
                                .AddressList
                                .Where(ip => !IPAddress.IsLoopback(ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
                                .FirstOrDefault();
            }
            return ipAddress?.ToString();
        }

        /// <summary>
        /// 根据域名获取对应的IP地址
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="type">请求类型</param>
        /// <returns></returns>
        public static async Task<string> GetIpAddressAsync(string domain, QueryType type = QueryType.ANY)
        {
            var lookup = new LookupClient();
            var result = await lookup.QueryAsync(domain, type);
            var record = result.Answers.ARecords().FirstOrDefault();
            return record?.Address?.ToString();
        }
        #endregion
    }
}
