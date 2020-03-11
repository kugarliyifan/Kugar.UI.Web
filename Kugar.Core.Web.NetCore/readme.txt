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