using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
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
        public static void AddPhysicalStaticFiles(this IApplicationBuilder app, string folder, string requestPath,TimeSpan? maxAge=null)
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

            if (!Path.IsPathFullyQualified(folder))
            {
                var env = (IHostingEnvironment)app.ApplicationServices.GetService(typeof(IHostingEnvironment));


                if (folder.StartsWith('/'))
                {
                    folder = env.ContentRootPath + folder;
                }
                else
                {
                    folder = Path.Combine(env.ContentRootPath, folder);
                }
            }

            if (folder[folder.Length-1]!='/')
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
                          RequestPath = requestPath,
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

            app.UseStaticFiles(opt);
        }

        /// <summary>
        /// 简单的使用Logger记录request的信息,不建议在生产环境中使用,生产环境使用请使用HttpReports之类的监控,使用该功能时,请检查是否有注入ILogger,未注入的,无法写入日志
        /// </summary>
        /// <param name="app"></param>
        /// <param name="logFilter">过滤一条连接是否记录日志,返回true,则记录,false不记录</param>
        public static void UseRequestLog(this IApplicationBuilder app,Func<Microsoft.AspNetCore.Http.HttpContext,bool> logFilter=null)
        {
            app.Use(async (context, next) =>
            {
                Exception error = null;

                var data = "";

                var needForLog = logFilter?.Invoke(context)??true;

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

                        if (logger!=null)
                        {
                            var headers = context.Request.Headers.Select(x => $"{x.Key}={x.Value}").JoinToString('\n');

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
                                logger.Log(LogLevel.Error,error, $"接收到请求:url:{context.Request.GetDisplayUrl()} \n body:{data} \n header: {headers}");
                            }
                            else
                            {
                                logger.Log(LogLevel.Error, $"接收到请求:url:{context.Request.GetDisplayUrl()} \n body:{data} \n header: {headers}", error);

                            }
                        }
                    }
                }

            });
        }
    }
}
