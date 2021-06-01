using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CrystalQuartz.AspNetCore;
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZqUtils.Core.Extensions;
using ZqUtils.Core.Helpers;

namespace ZqUtils.Core.Web
{
    public interface IDenpendency { }

    public interface IUserService : IDenpendency
    {
        Task<string> TestAsync();
    }

    public class UserService : IUserService
    {
        public async Task<string> TestAsync()
        {
            return await Task.FromResult("ok");
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddControllers();

            services.AddFromAssembly(
                type => type
                    .AssignableTo<IDenpendency>()
                    .Where(x => x.Namespace == "ZqUtils.Core.Web"),
                assembly => assembly
                    .StartsWith("ZqUtils.Core"));

            services
               .AddHttpClient("default")
               .ConfigurePrimaryHttpMessageHandler(() =>
                   new HttpClientHandler { ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) => true });

            //services.AddFromAssembly(typeof(IDenpendency),
            //    assembly => assembly
            //        .StartsWith("ZqUtils.Core"),
            //    type => type
            //        .Namespace == "ZqUtils.Core.Web");

            //注入IJob和IJobFactory
            services.AddJobAndJobFactory(x => x.StartsWith("ZqUtils.Core"));

            //注入Redis
            services.AddFreeRedis(Configuration);

            //允许同步IO操作
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //使用Ioc作业调度工厂
            var scheduler = app.UseIocJobFactory();

            //使用QuartzUI
            app.UseCrystalQuartz(() => scheduler);

            //配置HttpContext
            app.UseHttpContext();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            //配置Exceptionless
            ExceptionlessClient.Default.Configuration.ApiKey = Configuration.GetSection("Exceptionless:ApiKey").Value;
            ExceptionlessClient.Default.Configuration.ServerUrl = Configuration.GetSection("Exceptionless:ServerUrl").Value;
            app.UseExceptionless();
        }
    }
}
