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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
/****************************
* [Author] 张强
* [Date] 2018-10-18
* [Describe] HttpContext工具类
* **************************/
namespace ZqUtils.Core.Helpers
{
    /// <summary>
    /// HttpContext工具类
    /// </summary>
    public static class HttpContextHelper
    {
        /// <summary>
        /// IHttpContextAccessor
        /// </summary>
        private static IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 使用HttpContext，Startup中配置IHttpContextAccessor
        /// </summary>
        /// <param name="app"></param>
        /// <example>
        ///     <code>
        ///         public void ConfigureServices(IServiceCollection services)
        ///         {
        ///             services.TryAddSingleton$lt;IHttpContextAccessor, HttpContextAccessor&gt;();
        ///             services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        ///         }
        ///         
        ///         public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IServiceProvider svp)
        ///         {
        ///             loggerFactory.AddConsole(Configuration.GetSection("Logging"));
        ///             loggerFactory.AddDebug();
        ///             //配置HttpContext
        ///             app.UseHttpContext();
        ///         }
        ///     </code>
        /// </example>
        public static void UseHttpContext(this IApplicationBuilder app)
        {
            _httpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();
        }

        /// <summary>
        /// 当前HttpContext
        /// </summary>
        public static HttpContext Current => _httpContextAccessor.HttpContext;
    }
}
