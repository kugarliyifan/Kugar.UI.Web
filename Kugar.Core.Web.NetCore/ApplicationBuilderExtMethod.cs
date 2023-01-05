using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Kugar.Core.Web
{
    public static class ApplicationBuilderExtMethod
    {
        /// <summary>
        /// 添加一个静态的物理文件映射,如uploads文件夹,或者腾讯云进行SSL验证的时候,也需要开放文件访问
        /// </summary>
        /// <param name="app"></param>
        /// <param name="folder">物理文件夹路径,可以是相对路径或者绝对路径,相对路径为相对于IHostingEnvironment.ContentRootPath的文件夹</param>
        /// <param name="requestPath">请求的文件地址</param>
        /// <param name="maxAge">客户端缓存的文件时间,可为空</param>
        public static void AddPhysicalStaticFiles(this IApplicationBuilder app, string folder, string requestPath,TimeSpan? maxAge=null,Action<StaticFileOptions> optionFactory=null)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentNullException(nameof(folder));
            }

            if (requestPath==null)
            {
                throw new ArgumentNullException(nameof(requestPath));
            }

            //if (string.IsNullOrWhiteSpace(requestPath))
            //{
            //    throw new  ArgumentNullException(nameof(requestPath));
            //}
            var env = (IHostingEnvironment)app.ApplicationServices.GetService(typeof(IHostingEnvironment));

            //if (!Path.IsPathFullyQualified(folder))
            //{
            //    folder = Path.Join(env.ContentRootPath, folder);
            //}

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                folder = Path.Join(env.ContentRootPath, folder);
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!Path.IsPathFullyQualified(folder))
                {
                    folder = Path.Join(env.ContentRootPath, folder);
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && folder[folder.Length-1]!='/')
            {
                folder = folder + '/';
            }

            if (requestPath.Length<=0 || requestPath[0]!='/')
            {
                requestPath = "/" + requestPath;
            }

            if (requestPath[requestPath.Length-1]=='/')
            {
                requestPath = requestPath.Substring(0, requestPath.Length - 1);
            }

            if (!Directory.Exists(folder))
            {
                try
                {
                    Directory.CreateDirectory(folder);
                }
                catch (Exception e)
                {
                    throw new DirectoryNotFoundException($"创建文件夹失败,请确保{folder}文件夹存在");
                    return;
                }
            }



            var opt = new StaticFileOptions()
                      {
                          FileProvider = new PhysicalFileProvider(folder),
                          RequestPath = (PathString) requestPath,
                           
                      };
            
            if (maxAge.HasValue)
            {
                opt.OnPrepareResponse = (context) =>
                {
                    var headers = context.Context.Response.GetTypedHeaders();
                    headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
                                           {
                                               Public = true,
                                               MaxAge = maxAge.Value,
                                           };
                };
                
            }

            FileExtensionContentTypeProvider contentTypes = new FileExtensionContentTypeProvider();
            contentTypes.Mappings[".apk"] = "application/vnd.android.package-archive";

            var webConfigPath = Path.Join(env.WebRootPath, "web.config");

            if (File.Exists(webConfigPath))
            {
                var xmlDoc = new XmlDocument();

                using (var file = File.Open(webConfigPath, FileMode.Open, FileAccess.Read))
                {
                    xmlDoc.Load(file);
                }

                var node = xmlDoc.GetFirstElementsByTagName("staticContent");

                var mimeNodes = node.GetElementsByTagName("mimeMap");

                if (mimeNodes.HasData())
                {
                    foreach (var item in mimeNodes)
                    {
                        var ext = item.GetAttribute("fileExtension");
                        var mime = item.GetAttribute("mimeType");

                        if (!contentTypes.Mappings.ContainsKey(ext))
                        {
                            contentTypes.Mappings.Add(ext, mime);
                        }

                    }
                }
            }

            opt.ContentTypeProvider = contentTypes;

            if (optionFactory!=null)
            {
                optionFactory(opt);
            }

            app.UseStaticFiles(opt);
        }

        /// <summary>
        /// 简单的使用Logger记录request的信息,可以通过logFilter参数或在action上加入[RequestLog],不建议在生产环境中使用,生产环境使用请使用HttpReports之类的监控,使用该功能时,请检查是否有注入ILogger,未注入的,无法写入日志
        /// </summary>
        /// <param name="app"></param>
        /// <param name="logFilter">过滤一条连接是否记录日志,返回true,则记录,false不记录</param>
        public static void UseRequestLog(this IApplicationBuilder app,Func<Microsoft.AspNetCore.Http.HttpContext,bool> logFilter=null)
        {
            app.Use(async (context, next) =>
            {
                Exception error = null;

                var data = "";

                var executingEndpoint = context.GetEndpoint();

                var tag = "";
                
                // Get attributes on the executing action method and it's defining controller class
                var requestLog = executingEndpoint.Metadata.OfType<RequestLogAttribute>();

                var needForLog = requestLog.HasData();

                if (!needForLog) 
                {
                    needForLog = logFilter?.Invoke(context)??true;
                }
                else
                {
                    tag = requestLog.First()?.Tag??"";
                }

                if (needForLog)
                {
                    context.Request.EnableBuffering();
                }
                
                try
                {
                    await next();
                }
                catch (Exception e)
                {
                    error = e;
                    //logger.Log(LogLevel.Error,e,$"接收到请求:url:{context.Request.GetDisplayUrl()} \n body:{data} \n ", e);
                    throw;
                }
                finally
                {
                    if (needForLog)
                    {
                        var logger = (ILogger) context.RequestServices.GetService(typeof(ILogger));

                        if (logger==null)
                        {
                            var logFactory =
                                (ILoggerFactory) context.RequestServices.GetService(typeof(ILoggerFactory));

                            logger = logFactory.CreateLogger("TraceRequestLog");
                        }

                        if (logger==null)
                        {
                            var logProvider =
                                (ILoggerProvider) context.RequestServices.GetService(typeof(ILoggerProvider));

                            logger = logProvider.CreateLogger("TraceRequestLog");
                        }
                        
                        if (logger!=null)
                        {
                            var headers = context.Request.Headers?.Select(x => $"{x.Key}={x.Value.ToStringEx()}").JoinToString('\n');

                            if (context.Request.ContentLength < 20000)
                            {
                                //data = await context.Request.GetBodyString();
                                context.Request.Body.Position = 0L;

                                data = Encoding.UTF8.GetString(await context.Request.Body.ReadAllBytesAsync());
                                context.Request.Body.Position = 0L;
                            }
                            else
                            {
                                data = "内容超大,忽略记录";
                            }


                            if (error==null)
                            {
                                logger.Log(LogLevel.Debug, $"{tag}接收到请求:requestID:{context.TraceIdentifier }|url:{context.Request.GetDisplayUrl()} \n body:{data} \n header: {headers}");
                            }
                            else
                            {
                                logger.Log(LogLevel.Error,error, $"{tag}接收到请求:url:requestID:{context.TraceIdentifier }|{context.Request.GetDisplayUrl()} \n body:{data} \n header: {headers}", error);

                            }
                        }
                    }
                }

            });
        }
    }
}
