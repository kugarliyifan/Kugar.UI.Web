该项目是从Kugar.Core解决方案中分离出来,独立管理

用于提供asp.net mvc5 / asp.net core mvc 的一些常用类库

旧的git历史 ,可以查看 https://gitee.com/kugar/Kugar.Core/tree/master/UI/Kugar.Core.Web
                   https://gitee.com/kugar/Kugar.Core/tree/master/UI/Kugar.Core.Web.NetCore

[![NuGet](https://img.shields.io/nuget/v/Kugar.Core.Web.NetCore)](https://nuget.org/packages/Kugar.Core.Web.NetCore)

常用的类有

1.用于webapi中,使用json方式post数据,然后在action中,使用函数参数的方式进行接收,省去了需要为不同的action建立不同的model接收数据,并对参数提供数据校验的功能
  ```
    1) 在start.cs中:
        core 2.1 中:
            services.AddMvc().EnableJsonValueModelBinder(); //启用json方式的ModelBinder
        core 3.0 中:
            services.AddControllersWithViews().EnableJsonValueModelBinder(); //启用json方式的ModelBinder
    2) 在 controller中
        [FromBodyJson()]  //加上该特性,标识该action启用json绑定的方式,并可以设定是否在匹配名称时,忽略大小写
        public async Task<IActionResult> ApiTest(
                string keyword="",
                [Required](int productID,int qty)[] productlst, // 支持数组/单个形式的ValueTuple绑定
                [MinValue(1)]int pageIndex=1,
                [Range(10,100)]int pageSize=20)

    3) Http post json的方式提交如下格式数据:
        {
            keyword:"",
            pageIndex:1,
            pageSize:2,
            productlst:[
                {
                    productID:10,
                    qty:20
                }
            ]
        }

        注意的是content-type一定要 为application/json 或者text/json
  ```
2. ImageActionResult 构建一个图片数据作为ActionResult

3. QrCodeActionResult 构建一个二维码图片的ActionResult,传入string自动生成对应的二维码图片,后返回给客户端

4. ValidateCodeResult 构建一个随机验证码图片ActionResult

5. MyRequest类提供了一系列对Request的GetXXX系列函数以及其他通用操作函数

6. HttpContext类提供了静态访问当前HttpContext的功能,不建议在Asp.net Core中使用

    1)使用时,在start.cs中加入
    ```
        app.UseStaticHttpContext()
    ```
    2)使用时: HttpContext.Current

7. ApplicationBuilderExtMethod 用于提供一些公用的函数
    1) AddPhysicalStaticFiles 扩展函数,用于比较方便的添加一个物理文件的映射
    ```
        app.AddPhysicalStaticFiles("uploads","uploads");  //将uploads文件开放外部访问
    ```
8. RequestLocal类用提供每个链接才有的数据,类似于 ThreadLocal 功能,只是作用范围是一个Request范围内

9. 以View的方式输出Json,并自动生成Swagger文档
    用于在输出Json的自定义输出,并且可根据AddProperty的方式,生成Json值输出的方式和Swagger文档所需要的内容.Swagger可用于向前端提供接口文档
    如果选择的属性,自带文档,则可读取输入类对应的属性summary段的内容
    ```
    public class XXX:StaticJsonTemplateActionResult<ResultReturn<VM_PagedList<(business_Order order,business_UserCoinLog log,base_WxUser buyer,base_Activity activity)>>>
    {
        protected override void BuildSchema()
        {
            using (var r=this.AddReturnResult(Model))
            {
                using (var p=r.AddPagedList(x=>x))
                {
                    p.AddProperty(x => x.order.OrderDt, x => x.order.OrderCode, x => x.log.UnfreezeDt )
                        .AddProperty("AwardAmount",
                            x => (x.order.AwardAmount / x.order.Qty) * (x.order.Qty - x.order.RefundQty), "红包金额")
                        .AddProperty("buyerUserID", x => x.buyer.UserID, "下级ID")
                        .AddProperty("buyerNickName", x => x.buyer.NickName, "下级昵称")
                        .AddProperty(x => x.buyer.HeaderPortraitUrl, "下级头像")
                        .AddProperty(x => x.activity.ActivityID, x => x.activity.Title,x=>x.activity.MainImageUrl)
                        ;
                }
            }
            
        }
    }
    ```

10.ModelStateValidActionResult 用于格式化输出ModelState中的错误信息 

    ```
    //在Action中
    return new ModelStateValidActionResult(ModelState);
    ```

11. TimerHostedService:用于在后台执行一个定时任务,建议在Asp.net Core下使用该类替换TimerEx  

    用于取代TimeEx,在asp.net core的环境下使用,继承该类后,
    使用 services.AddHostedService&lt;定时器处理类&gt;();后,自动在后台启动当前定时任务,并且可以使用到Ioc中注册的类
  
12. BackgroundTaskQueue 用于后台使用的队列处理类 

    在Start中
    ```
    services.UseQueuedTaskService()
    ```

    在Action中:
    ```
    public async Task<IActionResult> xx([FromService]BackgroundTaskQueue queue)
    {
        queue.QueueBackgroundWorkItem(任务类)  //加入一个待处理的任务
    }
    ```

13. ScheduledTaskService 用于简单情况下通过Cron表达式设置计划执行的周期,该类无法运行时修改Cron,并且Enabled属性修改为false后,任务将停止,并且无法重新开启,注册方式参考TimerHostedService