using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

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
    }
}
