����Ŀ�Ǵ�Kugar.Core��������з������,��������

�����ṩasp.net mvc5 / asp.net core mvc ��һЩ�������

�ɵ�git��ʷ ,���Բ鿴 https://gitee.com/kugar/Kugar.Core/tree/master/UI/Kugar.Core.Web
                   https://gitee.com/kugar/Kugar.Core/tree/master/UI/Kugar.Core.Web.NetCore

[![NuGet](https://img.shields.io/nuget/v/Kugar.Core.Web.NetCore)](https://nuget.org/packages/Kugar.Core.Web.NetCore)

���õ�����

1.����webapi��,ʹ��json��ʽpost����,Ȼ����action��,ʹ�ú��������ķ�ʽ���н���,ʡȥ����ҪΪ��ͬ��action������ͬ��model��������,���Բ����ṩ����У��Ĺ���
  ```
    1) ��start.cs��:
        core 2.1 ��:
            services.AddMvc().EnableJsonValueModelBinder(); //����json��ʽ��ModelBinder
        core 3.0 ��:
            services.AddControllersWithViews().EnableJsonValueModelBinder(); //����json��ʽ��ModelBinder
    2) �� controller��
        [FromBodyJson()]  //���ϸ�����,��ʶ��action����json�󶨵ķ�ʽ,�������趨�Ƿ���ƥ������ʱ,���Դ�Сд
        public async Task<IActionResult> ApiTest(
                string keyword="",
                [Required](int productID,int qty)[] productlst, // ֧������/������ʽ��ValueTuple��
                [MinValue(1)]int pageIndex=1,
                [Range(10,100)]int pageSize=20)

    3) Http post json�ķ�ʽ�ύ���¸�ʽ����:
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

        ע�����content-typeһ��Ҫ Ϊapplication/json ����text/json
  ```
2. ImageActionResult ����һ��ͼƬ������ΪActionResult

3. QrCodeActionResult ����һ����ά��ͼƬ��ActionResult,����string�Զ����ɶ�Ӧ�Ķ�ά��ͼƬ,�󷵻ظ��ͻ���

4. ValidateCodeResult ����һ�������֤��ͼƬActionResult

5. MyRequest���ṩ��һϵ�ж�Request��GetXXXϵ�к����Լ�����ͨ�ò�������

6. HttpContext���ṩ�˾�̬���ʵ�ǰHttpContext�Ĺ���,��������Asp.net Core��ʹ��

    1)ʹ��ʱ,��start.cs�м���
    ```
        app.UseStaticHttpContext()
    ```
    2)ʹ��ʱ: HttpContext.Current

7. ApplicationBuilderExtMethod �����ṩһЩ���õĺ���
    1) AddPhysicalStaticFiles ��չ����,���ڱȽϷ�������һ�������ļ���ӳ��
    ```
        app.AddPhysicalStaticFiles("uploads","uploads");  //��uploads�ļ������ⲿ����
    ```
8. RequestLocal�����ṩÿ�����Ӳ��е�����,������ ThreadLocal ����,ֻ�����÷�Χ��һ��Request��Χ��

9. ��View�ķ�ʽ���Json,���Զ�����Swagger�ĵ�
    ���������Json���Զ������,���ҿɸ���AddProperty�ķ�ʽ,����Jsonֵ����ķ�ʽ��Swagger�ĵ�����Ҫ������.Swagger��������ǰ���ṩ�ӿ��ĵ�
    ���ѡ�������,�Դ��ĵ�,��ɶ�ȡ�������Ӧ������summary�ε�����
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
                            x => (x.order.AwardAmount / x.order.Qty) * (x.order.Qty - x.order.RefundQty), "������")
                        .AddProperty("buyerUserID", x => x.buyer.UserID, "�¼�ID")
                        .AddProperty("buyerNickName", x => x.buyer.NickName, "�¼��ǳ�")
                        .AddProperty(x => x.buyer.HeaderPortraitUrl, "�¼�ͷ��")
                        .AddProperty(x => x.activity.ActivityID, x => x.activity.Title,x=>x.activity.MainImageUrl)
                        ;
                }
            }
            
        }
    }
    ```

10.ModelStateValidActionResult ���ڸ�ʽ�����ModelState�еĴ�����Ϣ 

    ```
    //��Action��
    return new ModelStateValidActionResult(ModelState);
    ```

11. TimerHostedService:�����ں�ִ̨��һ����ʱ����,������Asp.net Core��ʹ�ø����滻TimerEx  

    ����ȡ��TimeEx,��asp.net core�Ļ�����ʹ��,�̳и����,
    ʹ�� services.AddHostedService&lt;��ʱ��������&gt;();��,�Զ��ں�̨������ǰ��ʱ����,���ҿ���ʹ�õ�Ioc��ע�����
  
12. BackgroundTaskQueue ���ں�̨ʹ�õĶ��д����� 

    ��Start��
    ```
    services.UseQueuedTaskService()
    ```

    ��Action��:
    ```
    public async Task<IActionResult> xx([FromService]BackgroundTaskQueue queue)
    {
        queue.QueueBackgroundWorkItem(������)  //����һ�������������
    }
    ```

13. ScheduledTaskService ���ڼ������ͨ��Cron���ʽ���üƻ�ִ�е�����,�����޷�����ʱ�޸�Cron,����Enabled�����޸�Ϊfalse��,����ֹͣ,�����޷����¿���,ע�᷽ʽ�ο�TimerHostedService