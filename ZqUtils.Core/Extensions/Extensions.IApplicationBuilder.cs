using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using ZqUtils.Core.Redis;
/****************************
* [Author] 张强
* [Date] 2021-05-12
* [Describe] IApplicationBuilder扩展类
* **************************/
namespace ZqUtils.Core.Extensions
{
    /// <summary>
    /// IApplicationBuilder扩展类
    /// </summary>
    public static class IApplicationBuilderExtensions
    {
        #region UseIf
        /// <summary>
        /// 根据条件配置Application
        /// </summary>
        /// <param name="this"></param>
        /// <param name="condition"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIf(
            this IApplicationBuilder @this,
            bool condition,
            Action<IApplicationBuilder> action)
        {
            if (condition && action != null)
                action(@this);

            return @this;
        }

        /// <summary>
        /// 根据条件配置Application
        /// </summary>
        /// <param name="this"></param>
        /// <param name="condition"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseIf(
            this IApplicationBuilder @this,
            bool condition,
            Action<IServiceProvider, IApplicationBuilder> action)
        {
            if (condition && action != null)
                action(@this.ApplicationServices, @this);

            return @this;
        }
        #endregion

        #region UseRedisInformation
        /// <summary>
        /// 使用Reids信息中间件
        /// </summary>
        /// <param name="this"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRedisInformation(
            this IApplicationBuilder @this,
            IConfiguration configuration)
        {
            return @this.Use(async (context, next) =>
            {
                var pathPrefix = configuration.GetValue<string>("Redis:RedisInformationPathPrefix");

                if (context.Request.Method.EqualIgnoreCase("get") &&
                    context.Request.Path.HasValue &&
                    context.Request.Path.Value.EqualIgnoreCase($"{pathPrefix}/redis/info", $"{pathPrefix}/redis/connectionInfo"))
                {
                    var services = @this.ApplicationServices;

                    var poolManger = services.GetService<IRedisConnectionPoolManager>();

                    if (poolManger != null)
                    {
                        //connectionInfo
                        if (context.Request.Path.Value.EqualIgnoreCase($"{pathPrefix}/redis/connectionInfo"))
                        {
                            var data = poolManger.GetConnectionInformations();

                            await context.Response.WriteAsync(data.ToJson(), Encoding.UTF8);
                        }

                        //info
                        if (context.Request.Path.Value.EqualIgnoreCase($"{pathPrefix}/redis/info"))
                        {
                            var database = configuration.GetValue<int?>("Redis:Database") ?? 0;

                            var redisDatabase = poolManger.GetConnection().GetDatabase(database);

                            var data = (await redisDatabase.ScriptEvaluateAsync("return redis.call('INFO')").ConfigureAwait(false)).ToString();

                            context.Response.ContentType = "text/html; charset=utf-8";
                            await context.Response.WriteAsync(data.Replace("\r\n", "<br/>"), Encoding.UTF8);
                        }

                        return;
                    }
                }

                await next();
            });
        }
        #endregion
    }
}
