using Microsoft.AspNetCore.Builder;
using System;
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
    }
}
