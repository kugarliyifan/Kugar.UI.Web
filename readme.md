该项目是从Kugar.Core解决方案中分离出来,独立管理
用于提供asp.net mvc5 / asp.net core mvc 的一些常用类库
旧的git历史 ,可以查看 https://gitee.com/kugar/Kugar.Core/tree/master/UI/Kugar.Core.Web

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

6. HttpContext类提供了静态访问当前HttpContext的功能
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