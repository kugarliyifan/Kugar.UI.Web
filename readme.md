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

6. HttpContext���ṩ�˾�̬���ʵ�ǰHttpContext�Ĺ���

    1)ʹ��ʱ,��start.cs�м���
    ```
        app.UseStaticHttpContext()
    ```
    2)ʹ��ʱ: HttpContext.Current

7. ApplicationBuilderExtMethod �����ṩһЩ���õĺ���
    1) AddPhysicalStaticFiles ��չ����,���ڱȽϷ��������һ�������ļ���ӳ��
    ```
        app.AddPhysicalStaticFiles("uploads","uploads");  //��uploads�ļ������ⲿ����
    ```
8. RequestLocal�����ṩÿ�����Ӳ��е�����,������ ThreadLocal ����,ֻ�����÷�Χ��һ��Request��Χ��