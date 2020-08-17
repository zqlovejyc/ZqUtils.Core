using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Quartz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ZqUtils.Core.Extensions;
using ZqUtils.Core.Helpers;
using SysConsole = System.Console;

namespace ZqUtils.Core.Console
{
    /// <summary>
    /// 单例测试实体
    /// </summary>
    public class TestObject
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public TestObject()
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="age"></param>
        public TestObject(string name, int age)
        {
            SysConsole.WriteLine($"姓名：{name}，年龄：{age}");
        }

        /// <summary>
        /// 单例静态实例
        /// </summary>
        public static TestObject Instance => SingletonHelper<TestObject>.GetInstance("张三", 20);
    }

    /// <summary>
    /// 测试实体
    /// </summary>
    public class TestEntity
    {
        /// <summary>
        /// 姓名
        /// </summary>
        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [Required(ErrorMessage = "地址不能为空")]
        public string Address { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 对象池实体
    /// </summary>
    public class ObjectPoolEntity
    {
        /// <summary>
        /// 对象池
        /// </summary>
        private static readonly ConcurrentDictionary<string, Lazy<ObjectPoolHelper<ObjectPoolEntity>>> _pool = new ConcurrentDictionary<string, Lazy<ObjectPoolHelper<ObjectPoolEntity>>>();

        /// <summary>
        /// 对象池获取实例
        /// </summary>
        /// <returns></returns>
        public ObjectPoolEntity GetInstance(string key = "defaultkey")
        {
            if (!_pool.ContainsKey(key))
            {
                var objectPool = new Lazy<ObjectPoolHelper<ObjectPoolEntity>>(() => new ObjectPoolHelper<ObjectPoolEntity>(() => new ObjectPoolEntity()));
                _pool.GetOrAdd(key, objectPool);
                return objectPool.Value.GetObject();
            }
            else
            {
                return _pool[key].Value.GetObject();
            }
        }
    }

    /// <summary>
    /// 自定义对象池策略
    /// </summary>
    public class CustomPooledObjectPolicy : IPooledObjectPolicy<TestEntity>
    {
        /// <summary>
        /// 创建对象
        /// </summary>
        /// <returns></returns>
        public TestEntity Create()
        {
            return new TestEntity
            {
                Name = "测试名称",
                Address = "测试地址"
            };
        }

        /// <summary>
        /// 是否允许归返对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Return(TestEntity obj)
        {
            return true;
        }
    }

    /// <summary>
    /// 自定义映射配置文件
    /// </summary>
    public class MapperProfile : Profile
    {
        public MapperProfile()
        {
            //映射字典类型到实体类型
            CreateMap<IDictionary<string, object>, TestEntity>()
                .ConstructUsing(x => x.ToJson().ToObject<TestEntity>(true))
                .ReverseMap();
        }
    }

    /// <summary>
    /// 测试Job
    /// </summary>
    [QuartzJob(
        JobName = "MyTestJob",
        JobGroupName = "MyTestJobGroup",
        JobDescription = "Job描述",
        TriggerName = "MyTestTrigger",
        TriggerGroupName = "MyTestTriggerGroup",
        TriggerDescription = "Trigger描述"
     )]
    [DisallowConcurrentExecution]
    public class TestJob : IJob
    {
        /// <summary>
        /// 异步执行Job
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Execute(IJobExecutionContext context)
        {
            LogHelper.Info($"任务名 : {context.JobDetail.Key.Name}，描述：{context.JobDetail.Description}，自定义Trigger数据 : {context.Trigger.JobDataMap.GetString("trigger")}，时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}，下次执行时间：{context.NextFireTimeUtc.Value.ConvertTime(TimeZoneInfo.Local):yyyy-MM-dd HH:mm:ss}");
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Program
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            #region Kafka
            //var kafka = new KafkaHelper(new KafkaConfig(new Dictionary<string, string> 
            //{
            //    ["group.id"] = "test-consumer-group"
            //}) 
            //{ 
            //    Servers = "localhost:9092"
            //});

            //var messages = new List<Message<Null, string>>();
            //for (int i = 0; i < 100; i++)
            //{
            //    messages.Add(new Message<Null, string> { Value = "Hello Kafka!" + (i + 1) });
            //}
            //var r = kafka.Publish("test-topic", messages);
            //SysConsole.WriteLine(r);

            //var r = kafka.Publish("test-topic", new Message<Null, string> { Value = "Hello Kafka!" });
            //SysConsole.WriteLine(r);

            //kafka.Subscribe<Ignore, string>("test-topic", receiveHandler: x =>
            //{
            //    SysConsole.WriteLine(x.Message.Value);
            //});

            //SysConsole.ReadLine();
            #endregion

            #region ToDateTime
            //解析自定义格式的日期字符串
            var str = "Mon Mar 16 13:08:45 2020";
            //en-GB、en-US都可以
            var time2 = str.ToDateTime("ddd MMM dd HH:mm:ss yyyy", CultureInfo.CreateSpecificCulture("en-GB"));
            var time = "2020-06-06 15:26".ToDateTime("yyyy-MM-dd HH:mm");

            //时间戳转换
            var timeSpan = DateTime.UtcNow.ToTimeStamp();
            var time3 = timeSpan.ToDateTime().Value.AddHours(8);

            //GMT
            var gmt = DateTime.Now.ToGMT();
            var time4 = gmt.ToDateTime();
            #endregion

            #region Job
            //先初始化日志
            LogHelper.Info("Job开始执行");

            //测试JobDataMap序列化和反序列化
            var jobDataMap = new JobDataMap
            {
                ["测试key"] = "测试123"
            };
            //序列化
            var bytes = jobDataMap.Serialize();
            //反序列化
            var data = bytes.Deserialize<JobDataMap>();

            #region 手动初始化调度工厂
            ////配置作业调度工厂，持久化
            //QuartzHelper.SetStdSchedulerFactoryProps(new NameValueCollection
            //{
            //    /***
            //     * misfireThreshold是用来设置调度引擎对触发器超时的忍耐时间，简单来说misfireThreshold=60000(单位毫秒)默认值。
            //     * 那么它的意思说当一个触发器超时时间如果大于misfireThreshold的值 就认为这个触发器真正的超时(也叫Misfires)。
            //     * 如果一个触发器超时时间 小于misfireThreshold的值， 那么调度引擎则不认为触发器超时。也就是说调度引擎可以忍受这个超时的时间。
            //     */
            //    ["quartz.jobStore.misfireThreshold"] = "100",
            //    //实例名称
            //    ["quartz.scheduler.instanceName"] = "quartz.scheduler.zqutils",
            //    //存储类型
            //    ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
            //    //序列化类型
            //    ["quartz.serializer.type"] = "json",
            //    //数据库表名称前缀
            //    ["quartz.jobStore.tablePrefix"] = "Quartz_",
            //    //驱动委托类型：SqlServerDelegate、PostgreSQLDelegate、MySQLDelegate、FirebirdDelegate、OracleDelegate、SQLiteDelegate
            //    ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.OracleDelegate, Quartz",
            //    //数据源名称
            //    ["quartz.jobStore.dataSource"] = "gitea",
            //    //连接字符串
            //    [$"quartz.dataSource.gitea.connectionString"] = "",
            //    //数据库驱动类型：SqlServer、Npgsql、MySql、Firebird、OracleODPManaged、SQLite
            //    [$"quartz.dataSource.gitea.provider"] = "OracleODPManaged",
            //    //数据库最大链接数
            //    ["quartz.dataSource.gitea.maxConnections"] = "20",
            //    //是否使用JobDataMap
            //    ["quartz.jobStore.useProperties"] = "true",
            //    //是否集群
            //    ["quartz.jobStore.clustered"] = "true",
            //    //集群id，要保证唯一，设为AUTO
            //    ["quartz.scheduler.instanceId"] = "AUTO",
            //    //并发线程数
            //    ["quartz.threadPool.threadCount"] = "20"
            //});
            #endregion

            //Quartz内部采用当前NLog日志
            await QuartzHelper.AddJobAsync<TestJob>("0/3 * * * * ?", triggerData: new Dictionary<string, object> { ["trigger"] = "trigger1" });
            await QuartzHelper.AddTriggerAsync<TestJob>("0/2 * * * * ?", triggerData: new Dictionary<string, object> { ["trigger"] = "trigger2" });
            await QuartzHelper.AddTriggerAsync<TestJob>("0/1 * * * * ?", triggerData: new Dictionary<string, object> { ["trigger"] = "trigger3" });
            //await QuartzHelper.RemoveJobAsync<TestJob>();

            await QuartzHelper.StartDelayedAsync(TimeSpan.FromSeconds(10));

            SysConsole.ReadLine();
            #endregion

            #region Jwt
            //生产token
            var secret = "GQDstcKsx0NHjPOuXOYg5MbeJ1XT0uFiwDVvVBrk";
            var token = JwtTokenHelper.CreateToken(new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub,"zhang"),//Subject,
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),//JWT ID,JWT的唯一标识
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(), ClaimValueTypes.Integer64),//Issued At，JWT颁发的时间，采用标准unix时间，用于验证过期
            }, 3600, secret);
            //读取token
            try
            {
                var (securityToken, principal) = JwtTokenHelper.ReadToken(token, secret);
            }
            catch (Exception ex)
            {
                SysConsole.WriteLine(ex.Message);
            }
            #endregion

            #region AutoMapper
            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MapperProfile>()).CreateMapper();
            var dic = new Dictionary<string, object>
            {
                ["Name"] = "姓名",
                ["Address"] = "地址",
                ["CreateDate"] = DateTime.Now
            };
            var test1 = mapper.Map<TestEntity>(dic);

            //采用扩展方法进行自定义映射
            var test2 = ((object)dic).MapTo<TestEntity>(new MapperConfiguration(x => x.AddProfile<MapperProfile>()));

            //匿名对象映射
            var test3 = new { Name = "姓名", Address = "地址", CreateDate = DateTime.Now }.MapTo<TestEntity>();
            #endregion

            #region 二维码/条形码/验证码
            //var str = CodeHelper.CreateQrBase64("zhangq", @"D:\Utils.Png");
            //SysConsole.WriteLine(str);
            //var res = CodeHelper.ReadCode(@"D:\qr1.png");
            //var code = $"data:image/png;base64,{Convert.ToBase64String(CodeHelper.CreateValidateGraphic("zhangq"))}";
            #endregion

            #region Zip文件解压缩
            //压缩
            //ZipHelper.Zip(@"D:\月度考核表", @"D:\月度考核表.zip");

            //解压缩
            //ZipHelper.UnZip(@"C:\月度考核表.zip", @"C:\");
            #endregion

            #region 实体验证
            var entity = new List<TestEntity>
            {
                new TestEntity
                {
                    Name = "张三",
                    Address = ""
                }
            };
            var (isValid, validationResults) = entity.IsValidWithResult();
            if (!isValid)
            {
                SysConsole.WriteLine(string.Join("；", validationResults.Select(x => x.ErrorMessage)));
            }
            #endregion

            #region 对象池
            var instance = new ObjectPoolEntity().GetInstance();
            instance = new ObjectPoolEntity().GetInstance();
            SysConsole.WriteLine(instance.ToJson());
            #endregion

            #region 微软对象池
            //方法一
            //var policy = new DefaultPooledObjectPolicy<TestEntity>();
            var policy = new CustomPooledObjectPolicy();
            //初始化对象池，并设置大小为10
            var pool1 = new DefaultObjectPool<TestEntity>(policy, 10);
            //对象池获取对象
            var testEntity1 = pool1.Get();
            pool1.Return(testEntity1);
            var testEntity2 = pool1.Get();
            pool1.Return(testEntity2);
            SysConsole.WriteLine(testEntity1 == testEntity2);

            //方法二
            var services = new ServiceCollection();
            //设置线程池大小
            services.AddSingleton<ObjectPoolProvider>(new DefaultObjectPoolProvider { MaximumRetained = 10 });
            services.AddSingleton(s =>
            {
                var provider = s.GetRequiredService<ObjectPoolProvider>();
                return provider.Create(new CustomPooledObjectPolicy());
            });
            var serviceProvider = services.BuildServiceProvider();
            var pool2 = serviceProvider.GetService<ObjectPool<TestEntity>>();
            //对象池获取对象
            testEntity1 = pool2.Get();
            pool2.Return(testEntity1);
            testEntity2 = pool2.Get();
            pool2.Return(testEntity2);
            SysConsole.WriteLine(testEntity1 == testEntity2);

            //方法三，采用封装过后的微软对象池
            var pool = new MicrosoftObjectPool<TestEntity>(creater: () => new TestEntity { Name = "张三", Address = "测试地址" });
            var entity1 = pool.Get();
            pool.Return(entity1);
            var entity2 = pool.Get();
            pool.Return(entity2);
            SysConsole.WriteLine(entity1 == entity2);
            #endregion

            #region 单例
            var o = TestObject.Instance;
            o = TestObject.Instance;
            #endregion

            SysConsole.ReadLine();
        }
    }
}
