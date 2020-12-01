该类用于提供一些通用的Web功能

1.通用的文件上传Action
	services.Configure<FileIOOption>(opt =>
            {
                opt.TypeMappings=new Dictionary<string, string>()
                {
                    ["product"]="/uploads/adv/"
                };
                opt.OnRequest=xxx , //该属性用于对请求进行处理,比如身份认证之类的
                opt.GenerateFileName=xxx, //用于自定义文件名称的生成结果
            });

    上传的地址为: 
        /WebCore/FileIO/Upload?type={type} 或者 /WebCore/FileIO/Upload/{type} post 方式

    注意:
        1.如果传入的文件夹路径不是绝对路径的话,需使用以下代码进行注册:
            services.AddHttpContextAccessor(); 
            app.UseStaticHttpContext();  
        2.请预先把uploads目录新建好,并且赋予写权限

2.通用的校验码图形生成和获取图片的Action
    图片地址为:  WebCore/Verification/VerificationCode?type={type} 或者 WebCore/Verification/VerificationCode/{type} get 方式
    注意:
        1.type可以为不填,
        2.使用 Session.GetString("VerificationCode_{type}") 获取本次生成的校验码做校验,如果type为空,则使用 Session.GetString("VerificationCode") 获取
        3.本功能需配合Session使用,请自行加入 services.AddSession();app.UseSession(); 
3.通用的MsgBox系列Controller扩展函数,基于layer
    在Controller的Action中,使用
        this.MsgBoxXXXXX("")
    在view中:
        @(new LayerServerMsg().RenderJS())

4.以View的方式输出Json,并自动生成Swagger文档
    用于在输出Json的自定义输出,并且可根据AddProperty的方式,生成Json值输出的方式和Swagger文档所需要的内容.Swagger可用于向前端提供接口文档
    如果选择的属性,自带文档,则可读取输入类对应的属性summary段的内容

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