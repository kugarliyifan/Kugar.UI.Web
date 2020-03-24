using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kugar.Core.Web
{
    public class FileIOController : ControllerBase
    {
        public FileIOController(IOptions<FileIOOption> option)
        {
            if (option==null)
            {
                throw new ArgumentNullException(nameof(option));
            }

            Option = option.Value;
        }

        [Route("WebCore/FileIO/Upload/{type}")]
        [Route("WebCore/FileIO/Upload")]
        [HttpPost]
        public async Task<IActionResult> Upload(string type="")
        {
            var re = Option.onRequestInternal(HttpContext, type);

            if (re!=null)
            {
                return re;
            }

            if (type==null)
            {
                return NotFound();
            }

            if (Option.TypeMappings.TryGetValue(type,out var path))
            {
                var file = Request.GetFile();
                var fileName = Option.onGenerateFileName(file, type);

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = file.GetRandomName();
                }

                var newPath = Path.Combine(path, DateTime.Now.ToString("yyyyMMdd"), fileName);

                var result=await file.SaveAsExAsync(Path.Combine(Request.GetWebsitePath(), newPath.Trim('/')));

                if (result.IsSuccess)
                {
                    if (Option.IncludeHost)
                    {
                        newPath =$"{(Request.IsHttps?"https":"http")}://{Request.Host.ToStringEx()}{newPath}";
                    }

                    return new JsonResult(new SuccessResultReturn(newPath));
                }
                else
                {
                    return StatusCode(500, result.Message);
                }
            }
            else
            {
                return NotFound("不存在指定类型的映射");
            }
        }

        public FileIOOption Option {private set; get; }
    }

    public class FileIOOption
    {
        /// <summary>
        /// 传递不同类型,映射存储的路径,key为type,value为路径,路径必须是以 / 开头,会在该路径后加上日期,再加上文件名
        /// </summary>
        public IReadOnlyDictionary<string,string> TypeMappings { set; get; }

        /// <summary>
        /// 用于映射不同类型的type,key为type,,使用不用类型的授权方式,,如果多个type使用同一种授权,则type可以使用逗号分隔
        /// </summary>
        public IReadOnlyDictionary<string, AuthorizeAttribute> AuthroizeMappigns { set; get; } 

        internal IActionResult onRequestInternal(Microsoft.AspNetCore.Http.HttpContext context, string type)
        {
            return OnRequest?.Invoke(context,type);
            
        }

        internal string onGenerateFileName(IFormFile file, string type)
        {
            return GenerateFileName?.Invoke(file,type);
        }

        /// <summary>
        /// 对请求进行处理,比如授权检查等,,如需要中断请求,则返回对应处理的IActionResult,如果无需要中断,请返回null
        /// </summary>
        public event RequestHandler OnRequest;

        /// <summary>
        /// 对文件名生成的事件,如无需额外处理,请返回null或者空字符串
        /// </summary>
        public event GenerateFileName GenerateFileName;

        /// <summary>
        /// 上传后的链接,是否包含域名
        /// </summary>
        public bool IncludeHost { set; get; }
    }

    //public static class FileIOGlobalExtented
    //{
    //    public static IMvcBuilder AddFileIOAuthroizeMapping(this IMvcBuilder builder)
    //    {
    //        builder.Services.AddSingleton<Convention>();

    //        return builder;
    //    }

    //    public class Convention : IApplicationModelConvention
    //    {
    //        private FileIOOption _fileOption;

    //        public Convention(IOptions<FileIOOption> fileOption)
    //        {
                
    //        }

    //        public void Apply(ApplicationModel application)
    //        {
    //            var c = application.Controllers.First(x => x is FileIOController);

    //            if (_fileOption.AuthroizeMappigns.HasData())
    //            {
    //                foreach (var mapping in _fileOption.AuthroizeMappigns)
    //                {
    //                    c.
    //                }
    //            }
                

    //            ClassUsedByConventionOptions.ClassUsedByConventionAction(ClassUsedByConvention);
    //            ClassUsedByConvention.Apply(application);
    //        }
    //    }

    //    public class MyConfigureOptions<MvcOptions> : IConfigureOptions<MvcOptions>
    //    {
    //        private readonly TConvention _convention;
    //        public MyConfigureOptions(TConvention convention)
    //        {
    //            _convention = convention;
    //        }

    //        public void Configure(MvcOptions options) => options.Conventions.Add(_convention);
    //    }
    //}

    public delegate string GenerateFileName(IFormFile file,string type);

    public delegate IActionResult RequestHandler(Microsoft.AspNetCore.Http.HttpContext context, string type);
}
