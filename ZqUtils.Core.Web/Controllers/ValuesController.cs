using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Exceptionless;
using Exceptionless.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Quartz;
using ZqUtils.Core.Helpers;
using ZqUtils.Core.Utilities;

namespace ZqUtils.Core.Web.Controllers
{
    public interface ITestService : IDenpendency
    {
        Task<string> PrintAsync();
    }

    public class TestService : ITestService
    {
        public async Task<string> PrintAsync()
        {
            return await Task.FromResult("ok");
        }
    }

    /// <summary>
    /// 测试Job
    /// </summary>
    [DisallowConcurrentExecution]
    public class TestJob : IJob
    {
        private readonly ILogger<TestJob> _logger;
        public TestJob(ILogger<TestJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"执行Job完成，时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}，下一次执行时间：{context.NextFireTimeUtc:yyyy-MM-dd HH:mm:ss}");
            await Task.CompletedTask;
        }
    }

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IUserService _userService;
        //private readonly ITestService _testService;

        public ValuesController(IUserService userService
            //, ITestService testService
            )
        {
            _userService = userService;
            //_testService = testService;
        }
        // GET api/values/download
        [HttpGet]
        [ActionName("Download")]
        public async Task<ActionResult> DownloadAsync()
        {
            #region FileHelper
            //await FileHelper.GetFileOfZipAsync(new[] { @"D:\1.pptx", @"D:\2.docx" }, "chorme.zip");
            //await FileHelper.GetFileAsync(@"D:\月度考核表.zip", "月度考核表.zip", "application/zip", true);
            var bytes = await FileHelper.GetFileOfZipAsync(new[] { @"D:\2.docx", @"D:\1.pptx" });
            return File(bytes, "1.zip".GetContentType(), "1.zip");
            #endregion

            #region ExcelHelper
            /*
            DataTable tblDatas = new DataTable("Datas");
            DataColumn dc = null;
            dc = tblDatas.Columns.Add("ID", Type.GetType("System.Int32"));
            dc.AutoIncrement = true;//自动增加
            dc.AutoIncrementSeed = 1;//起始为1
            dc.AutoIncrementStep = 1;//步长为1
            dc.AllowDBNull = false;//

            dc = tblDatas.Columns.Add("Product", Type.GetType("System.String"));
            dc = tblDatas.Columns.Add("Version", Type.GetType("System.String"));
            dc = tblDatas.Columns.Add("Description", Type.GetType("System.String"));

            DataRow newRow;
            newRow = tblDatas.NewRow();
            newRow["Product"] = "大话西游";
            newRow["Version"] = "2.0";
            newRow["Description"] = "我很喜欢";
            tblDatas.Rows.Add(newRow);

            newRow = tblDatas.NewRow();
            newRow["Product"] = "梦幻西游";
            newRow["Version"] = "3.0";
            newRow["Description"] = "比大话更幼稚";
            tblDatas.Rows.Add(newRow);
            ExcelHelper.EPPlusExportExcel(tblDatas, DateTime.Now.ToString("yyyyMMddHHmmss"));
            */
            #endregion
        }

        // GET api/values/download1
        [HttpGet]
        [ActionName("Download1")]
        public ActionResult Download1()
        {
            return File(System.IO.File.ReadAllBytes(@"E:\软件\67.0.3396.62_chrome_installer.exe"), "application/octet-stream");
        }

        // GET api/values/get
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> Get()
        {
            var res = await _userService.TestAsync();
            //res = await _testService.PrintAsync();

            return new[] { res };

            ////写入Cookie
            //CookieHelper.Set("cookie1", "zhagnq");
            ////ExceptionlessClient.Default.SubmitLog("测试信息info", LogLevel.Info);
            ////ExceptionlessClient.Default.CreateLog("测试信息tags").AddTags("tags1").Submit();
            //try
            //{
            //    throw new Exception("测试异常");
            //}
            //catch (Exception ex)
            //{
            //    ex.ToExceptionless().Submit();
            //}
            //return new string[] { "value1", "value2" };
        }

        // GET api/values/get/1
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> Get(int id)
        {
            await QuartzHelper.AddJobAsync<TestJob>("0/3 * * * * ?");

            //获取redis缓存值
            var res = await RedisHelper.GetAsync("LRADMSlearun_adms_area_0");

            //读取cookie
            //var cookie = CookieHelper.Get("cookie1");

            return res;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
