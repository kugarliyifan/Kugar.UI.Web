using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Authentications;
using Kugar.Core.Web.Convention;

#if NETCOREAPP3_1  || NET5_0 || NET6_0
using Kugar.Storage;
#endif

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;


namespace Kugar.Core.Web
{
    /// <summary>
    /// 
    /// </summary>
    [ApiExplorerSettings()]
    public class FileIOController : ControllerBase
    {
        /// <summary>
        /// 多文件上传,返回 returnData=[{isSuccess:true,orgFileName:'原上传文件的文件名',uploadFileName:'新上传后的文件路径',message:'该文件上传后的提示文本'}]
        /// </summary>
        /// <param name="type">上传类型,由AddFileUpload().AddOption()参数中的type决定</param>
        /// <param name="configManager"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        [Authorize("corefileIO")]
        [Route("WebCore/FileIO/Uploads/{type}")]
        [Route("WebCore/FileIO/Uploads")]
        [HttpPost]
        public async Task<IActionResult> Uploads([FromQuery] string type = "",
            [FromServices] FileIOConfigManager configManager = null, [FromServices] IStorage storage = null)
        {
            var option = configManager.GetByKey(type);

            if (option.Events.OnRequestHandler != null)
            {
                var result = await option.Events.OnRequestHandler.Invoke(HttpContext, type);

                if (result != null)
                {
                    return result;
                }
            }

            if (option.Storage == null && storage == null)
            {
                throw new Exception("options.Storage或全局注入的storage,必须拥有一个");
            }

            var files = Request.GetFiles();

            if (files.Length<=0)
            {
                throw new Exception("请上传文件");
            }

            if (!option.MutileFileUpload && files.Length > 1)
            {
                throw new Exception("当前上传参数不允许同时上传多个文件");
            }


            var lst = new JArray();

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                var fileName = await option.Events.OnGenerateFileName.Invoke(HttpContext, file, type);

                await using var fileStream = file.OpenReadStream();

                var ret = await (option.Storage ?? storage)?.StorageFileAsync(fileName, fileStream)!;

                lst.Add(new JObject()
                {
                    ["orgFileName"] = file.FileName,
                    ["uploadFileName"] = ret.ReturnData,
                    ["isSuccess"] = ret.IsSuccess,
                    ["message"] = ret.Message
                });

            }


            return new JsonResult(new SuccessResultReturn(lst));


            //if (Option.TypeMappings.TryGetValue(type,out var path))
            //{
            //    var file = Request.GetFile();
            //    var fileName = Option.onGenerateFileName(file, type);

            //    if (string.IsNullOrWhiteSpace(fileName))
            //    {
            //        fileName = file.GetRandomName();
            //    }

            //    var newPath = Path.Combine(path, DateTime.Now.ToString("yyyyMMdd"), fileName);

            //    var result=await file.SaveAsExAsync(Path.Combine(Request.GetWebsitePath(), newPath.Trim('/')));

            //    if (result.IsSuccess)
            //    {
            //        if (Option.IncludeHost)
            //        {
            //            newPath =$"{(Request.IsHttps?"https":"http")}://{Request.Host.ToStringEx()}{newPath}";
            //        }

            //        return new JsonResult(new SuccessResultReturn(newPath));
            //    }
            //    else
            //    {
            //        return StatusCode(500, result.Message);
            //    }
            //}
            //else
            //{
            //    return NotFound("不存在指定类型的映射");
            //}
        }

        /// <summary>
        /// 单文件上传,返回 returnData=新上传后的文件路径
        /// </summary>
        /// <param name="type">上传类型,由AddFileUpload().AddOption()参数中的type决定</param>
        /// <param name="configManager"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        [Authorize("corefileIO")]
        [Route("WebCore/FileIO/Upload/{type}")]
        [Route("WebCore/FileIO/Upload")]
        [HttpPost]
        public async Task<IActionResult> Upload([FromQuery] string type = "",
            [FromServices] FileIOConfigManager configManager = null, [FromServices] IStorage storage = null)
        {
            var option = configManager.GetByKey(type);

            if (option.Events.OnRequestHandler != null)
            {
                var result = await option.Events.OnRequestHandler.Invoke(HttpContext, type);

                if (result != null)
                {
                    return result;
                }
            }

            if (option.Storage == null && storage == null)
            {
                throw new Exception("options.Storage或全局注入的storage,必须拥有一个");
            }

            var files = Request.GetFiles();

            if (files.Length > 1)
            {
                throw new Exception("请使用uploads上传多文件");
            }

            if (files.Length <= 0)
            {
                throw new ArgumentOutOfRangeException("file");
            }

            var file = files[0];

            var fileName = await option.Events.OnGenerateFileName.Invoke(HttpContext, file, type);

            await using var fileStream = file.OpenReadStream();

            var ret = await (option.Storage ?? storage)?.StorageFileAsync(fileName, fileStream)!;
            
            return new JsonResult(ret);

        }
    }

    public class FileIOOption : WebJWTOptionBase
    {
        /// <summary>
        /// 用于处理文件实际存储的接口,顺序为 属性Storage中类->全局容器中的IStorage接口->存入本地/uploads/目录下,实际的存放路径,参考Events.OnGenerateFileName,如需修改,请赋值该属性
        /// </summary>
        public IStorage Storage { set; get; }

        public FileUploadEvents GlobalEvents { set; get; }

        /// <summary>
        /// 用于根据Type获取不同的Token,默认使用Authorization或者{type}_Authorization,获取顺序:Headers->Cookie,如需自定义,使用该属性进行自定义
        /// </summary>
        public FileUploadMessageReceived OnTokenMessageReceived { set; get; }

    }

    public static class FileIOExt
    {
        public static AuthenticationBuilder AddFileUpload(this AuthenticationBuilder builder,
            FileIOOption options = null
            )
        {
            options = new FileIOOption()
            {
                Audience = "api",
                Issuer = "api",
                AuthenticationScheme = "corefileio",
                TokenEncKey = "oPvU7#2XQNzfke7M",
            };

            builder.Services.AddSingleton<FileIOConfigManager>();

            builder.Services.AddOptions().Configure<FileIOOption>("corefileIO", opt =>
            {

                //opt.Cookie = options.Cookie;
                //opt.AuthenticationScheme = authenticationScheme;
                //opt.OnTokenValidated = options.OnTokenValidated;
                //opt.Issuer = options.Issuer;
                //opt.Audience = options.Audience;
                //opt.TokenEncKey = options.TokenEncKey;
                //opt.LoginService = options.LoginService;
                //opt.ExpireTimeSpan = options.ExpireTimeSpan;
                //opt.OnChallenge = options.OnChallenge;
                //opt.LoginUrl = options.LoginUrl;

                options.CopyValue(opt);

                //options.CopyValue(opt);
            });

            builder.AddJwtBearer("corefileIO", (opt) =>
            {
                opt.Events = opt.Events ?? new JwtBearerEvents();

                opt.Events.OnMessageReceived = async (context) =>
                {
                    var authName = context.Scheme.Name;

                    var tmp = (OptionsManager<FileIOOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<FileIOOption>));

                    context.HttpContext.Items.Remove("SchemeName");
                    context.HttpContext.Items.Add("SchemeName", authName);//.TryGetValue("SchemeName", "")

                    var option = tmp.Get(authName);


                    if (context.HttpContext.Request.Cookies.TryGetValue(string.IsNullOrEmpty(option.Cookie?.Name) ? $"jwt.{authName}" : option.Cookie?.Name,
                        out var v))
                    {
                        context.Token = v;
                    }

                    if (string.IsNullOrEmpty(context.Token) && context.Request.Headers.ContainsKey("Authorization"))
                    {
                        context.Token = context.Request.Headers.TryGetValue("Authorization").FirstOrDefault();
                    }

                    if (string.IsNullOrEmpty(context.Token) && context.Request.Query.ContainsKey("access_token"))
                    {
                        context.Token = context.Request.Query["access_token"];
                    }

                    if (string.IsNullOrWhiteSpace(context.Token))
                    {
                        context.Fail("未包含token");
                    }
                    //兼容Bearer开头
                    if (context.Token != null && context.Token.StartsWith("Bearer ", StringComparison.CurrentCultureIgnoreCase))
                    {
                        context.Token = context.Token.Substring(7);
                    }



                };


                opt.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer ?? options.AuthenticationScheme,
                    ValidateAudience = true,
                    ValidAudience = options.Audience ?? options.AuthenticationScheme,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(options.ActualEncKey),
                    TokenDecryptionKey = new SymmetricSecurityKey(options.ActualEncKey),
                };

                opt.Events.OnTokenValidated = async (context) =>
                {

                    var authName = context.Scheme.Name;

                    var t = (OptionsManager<FileIOOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<FileIOOption>));

                    var tmpOpt = t.Get(authName);

                    if (tmpOpt.LoginService != null)
                    {

                        var userName = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier).Trim();
                        var pw = context.Principal.FindFirstValue("k").DesDecrypt(tmpOpt.TokenEncKey.Left(8)).Trim();

                        var type = context.HttpContext.Request.GetString("type");

                        var ret = await tmpOpt.LoginService.Login(context.HttpContext, userName, pw, new[]
                        {
                            new KeyValuePair<string, string>("type",type),
                            new KeyValuePair<string, string>("schemeName",authName)
                        });

                        if (!ret.IsSuccess)
                        {
                            context.Fail("身份校验错误");

                            return;
                        }
                        else
                        {
                            var permissionFactoryService =
                                (IUserPermissionFactoryService)context.HttpContext.RequestServices.GetService(typeof(IUserPermissionFactoryService));

                            HashSet<string> permissions = null;

                            if (permissionFactoryService != null)
                            {
                                var tmp =
                                    await permissionFactoryService.GetUserPermissions(context.HttpContext,
                                        ret.ReturnData);

                                permissions = new HashSet<string>(tmp);

                                context.HttpContext.Items["___CurrentUserPermisions"] = permissions;
                            }

                            context.Principal.AddClaim("userID", ret.ReturnData);

                            var fileIOConfigManager =
                                context.HttpContext.RequestServices.GetService<FileIOConfigManager>();

                            var option = fileIOConfigManager.GetByKey(type);

                            if (option.PermissionCodes.HasData())
                            {
                                if (!permissions.HasData())
                                {
                                    context.Fail("当前用户无权限操作");
                                    return;
                                }

                                foreach (var code in option.PermissionCodes)
                                {
                                    if (!permissions.Contains(code))
                                    {
                                        context.Fail("当前用户无权限操作");
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    context.HttpContext.Items.Remove("SchemeName");
                    context.HttpContext.Items.Add("SchemeName", authName);//.TryGetValue("SchemeName", "")


                    //HttpContext.Current.Items.Add("SchemeName", authenticationScheme);//.TryGetValue("SchemeName", "")

                    //if (tmpOpt.OnTokenValidated != null)
                    //{
                    //    await tmpOpt.OnTokenValidated(context);
                    //}
                };

                opt.Events.OnChallenge = async (context) =>
                {
                    var authName = context.Scheme.Name;

                    var t = (OptionsManager<FileIOOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<FileIOOption>));

                    var tmpOpt = t.Get(authName);

                    context.HttpContext.Items.Remove("SchemeName");
                    context.HttpContext.Items.Add("SchemeName", authName);//.TryGetValue("SchemeName", "")

                    context.Response.StatusCode = 401;
                    //if (tmpOpt.OnChallenge != null)
                    //{
                    //    context.Response.StatusCode = 200;
                    //    await tmpOpt.OnChallenge(context);
                    //}

                    //if (!context.Handled)
                    //{
                    //    if (string.IsNullOrWhiteSpace(tmpOpt.LoginUrl))
                    //    {
                    //        context.Response.StatusCode = 401;

                    //        //context.Response.Redirect($"/AdminCore/Logout/{authenticationScheme}?backurl=" + context.Request.GetDisplayUrl());
                    //    }
                    //    else
                    //    {
                    //        //context.Response.StatusCode=302;
                    //        context.Response.Redirect($"{tmpOpt.LoginUrl}?backurl={context.Request.GetDisplayUrl()}");
                    //    }

                    //    await context.Response.WriteAsync(jsonser)

                    //    context.HandleResponse();
                    //}


                };

                opt.Events.OnAuthenticationFailed = async (context) =>
                {
                    context.Response.StatusCode = 401;
                };
            });

            return builder;

        }
    }

    public class FileIOConfigManager
    {
        private Dictionary<string, FileIOOptionConfig> _fileConfigs = new Dictionary<string, FileIOOptionConfig>(StringComparer.CurrentCultureIgnoreCase);

        private string _schemeName = "";

        public FileIOConfigManager(string schemeName)
        {
            _schemeName = schemeName;
        }

        public FileIOConfigManager AddOption(string type, FileIOOptionConfig option)
        {
            option.Type = type;
            option.SchemeName = _schemeName;

            _fileConfigs.Add(type, option);

            return this;
        }

        public FileIOOptionConfig GetByKey(string type) => _fileConfigs.TryGetValue(type);
    }

    public class FileIOOptionConfig
    {
        public FileIOOptionConfig()
        {
            Events = new FileUploadEvents(this);
            PermissionCodes = Array.Empty<string>();
        }

        public string Type { internal set; get; }

        public decimal FileSizeLimit { set; get; } = -1;

        public bool MutileFileUpload { set; get; } = false;

        public string SchemeName { internal set; get; }

        public IStorage Storage { set; get; }

        public IEnumerable<string> PermissionCodes { set; get; }

        public FileUploadEvents Events { get; }

    }

    public class FileUploadEvents
    {
        private FileIOOptionConfig _option = null;
        private GenerateFileName _onGenerateFileName;

        public FileUploadEvents(FileIOOptionConfig option)
        {
            _option = option;

            OnGenerateFileName = internalGenerateFileName;
            OnRequestHandler = internalRequestHandler;
        }

        /// <summary>
        /// 用于根据传入的type和context,返回
        /// </summary>
        [NotNull]
        public GenerateFileName OnGenerateFileName
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _onGenerateFileName = value;
            }
            get => _onGenerateFileName;
        }

        /// <summary>
        /// 触发请求时,回调该函数,如需特殊处理的,返回IActionResult,则直接返回处理结果忽略原有的存储步骤,如无需处理,则返回null,一般用于对请求进行额外校验
        /// </summary>
        public RequestHandler OnRequestHandler { set; get; }

        private Task<string> internalGenerateFileName(Microsoft.AspNetCore.Http.HttpContext contex, IFormFile file,
            string type)
        {
            var fileName = file.GetRandomName();

            fileName = $"{type}/{DateTime.Now:yyyyMM}/{fileName}";

            return Task.FromResult(fileName);
        }

        private Task<IActionResult> internalRequestHandler(Microsoft.AspNetCore.Http.HttpContext context, string type)
        {
            return null;
        }


    }

    /// <summary>
    /// 用于获取
    /// </summary>
    /// <param name="context"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate Task<string> FileUploadMessageReceived(MessageReceivedContext context, string type);

    /// <summary>
    /// 用于返回基于基地址的路径及文件名,默认存在 基地址/{type}/{当前日期}/{随机文件名}.{扩展名}
    /// </summary>
    /// <param name="contex"></param>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate Task<string> GenerateFileName(Microsoft.AspNetCore.Http.HttpContext contex, IFormFile file, string type);

    public delegate Task<IActionResult> RequestHandler(Microsoft.AspNetCore.Http.HttpContext context, string type);

}
