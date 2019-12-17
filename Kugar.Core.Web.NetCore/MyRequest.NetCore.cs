using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;

namespace Kugar.Core.Web
{
    public static partial class MyRequest
    {
   
        public static string GetString(this HttpRequest request, string name, string defaultValue="", bool autoDecode=false)
        {
            try
            {
                string value;

                if (request.Method == "GET")
                {
                    value = request.Query[name];
                }
                else
                {
                    if (request.Form.ContainsKey(name))
                    {
                        value = request.Form[name];
                    }
                    else
                    {
                        value = request.Query[name];
                    }
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    return defaultValue;
                }
                else
                {
                    if (autoDecode)
                    {
                        return HttpUtility.UrlDecode(value);
                    }
                    else
                    {
                        return value;
                    }
                }
            }
            catch (Exception)
            {
                return defaultValue;
            }


        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsFileExist(this HttpRequest request, string key)
        {
            try
            {
                if (request.Form.Files.Count > 0 && request.Form.Files.Any(x=>x.Name.CompareTo(key,true)))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
            
        }

        /// <summary>
        /// 生成一个随机文件名,带扩展名
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetRandomName(this IFormFile file,bool containExtension=true)
        {
            return
                $"{DateTime.Now.ToString("yyyyMMddHHmmssffff")}{RandomEx.Next(100, 999)}{(containExtension? Path.GetExtension(file.FileName):"")}";
        }

        public static IFormFile GetFile(this HttpRequest request)
        {
            if (request.Form.Files.Count>0)
            {
                return request.Form.Files[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 根据naem,获取对应的file对象
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IFormFile GetFile(this HttpRequest request, string name)
        {
            if (IsFileExist(request, name))
            {
                return request.Form.Files.GetFile(name);
            }

            return null;
        }

        /// <summary>
        /// 获取相同name的多个file文件对象
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IFormFile[] GetFiles(this HttpRequest request, string name)
        {
            if (IsFileExist(request, name))
            {
                return request.Form.Files.GetFiles(name).ToArrayEx();
            }

            return null;
        }

        /// <summary>
        /// 获取本次上传的所有file文件对象
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IFormFile[] GetFiles(this HttpRequest request)
        {
            return request.Form.Files.ToArrayEx();
        }


        /// <summary>
        /// 将文件保存入磁盘,注意:path必须为完全路径,如非完整路径,请使用GetWebsitePath进行拼接或启用静态Httpcontext
        /// </summary>
        /// <param name="file"></param>
        /// <param name="path">保存的磁盘路径,必须为完全路径,如非完整路径,请使用GetWebsitePath进行拼接或启用静态Httpcontext</param>
        /// <returns></returns>
        public static ResultReturn<string> SaveAsEx(this IFormFile file, string path)
        {
            if (file == null || file.Length <= 0)
            {
                return new FailResultReturn<string>(new ArgumentOutOfRangeException(nameof(file)));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return new FailResultReturn<string>(new ArgumentNullException(nameof(path)));
            }

            try
            {
                //string fullPath = "";

                //if (!IsFileFullPath(path))
                //{
                //    fullPath = Path.Combine(GetWebsitePath(request), path);
                //}

                var filePath = path;

                if (!Path.IsPathFullyQualified(filePath))
                {
                    if (HttpContext.Current != null && HttpContext.Current.Request!=null)
                    {
                        var rootpath = GetWebsitePath(HttpContext.Current.Request);// HttpContext.Current.Request.GetWebsitePath();

                        if (path[0] == '/')
                        {
                            filePath = rootpath + filePath;
                        }
                        else
                        {
                            filePath = $"{rootpath}/{filePath}";
                        }
                    }
                    else
                    {
                        var rootPath = Directory.GetCurrentDirectory();

                        if (path[0] == '/')
                        {
                            filePath = rootPath + filePath;
                        }
                        else
                        {
                            filePath = $"{rootPath}/{filePath}";
                        }
                        
                    }
                }

                var dirPath = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (var diskFile = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite))
                using(var srcStream=file.OpenReadStream())
                {
                    srcStream.CopyTo(diskFile, 4096);
                }

                return new SuccessResultReturn<string>(path);
            }
            catch (Exception ex)
            {
                return new FailResultReturn<string>(ex);
            }

        }

        /// <summary>
        /// 将文件保存入磁盘,注意:path必须为完全路径,如非完整路径,请使用GetWebsitePath进行拼接或启用静态Httpcontext
        /// </summary>
        /// <param name="file"></param>
        /// <param name="path">保存的磁盘路径,必须为完全路径,如非完整路径,请使用GetWebsitePath进行拼接或启用静态Httpcontext</param>
        /// <returns></returns>
        public static async Task<ResultReturn<string>> SaveAsExAsync(this IFormFile file, string path)
        {
            if (file == null || file.Length <= 0)
            {
                return new FailResultReturn<string>(new ArgumentOutOfRangeException(nameof(file)));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return new FailResultReturn<string>(new ArgumentNullException(nameof(path)));
            }

            try
            {
                //string fullPath = "";

                //if (!IsFileFullPath(path))
                //{
                //    fullPath = Path.Combine(GetWebsitePath(request), path);
                //}

                var filePath = path;

                if (!Path.IsPathFullyQualified(filePath))
                {
                    if (HttpContext.Current != null)
                    {
                        var rootpath = HttpContext.Current.Request.GetWebsitePath();

                        if (path[0] == '/')
                        {
                            filePath = rootpath + filePath;
                        }
                        else
                        {
                            filePath = $"{rootpath}/{filePath}";
                        }
                    }
                    else
                    {
                        throw new ArgumentException("路径必须是绝对路径", nameof(path));
                    }
                }

                var dirPath = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                using (var diskFile = File.Open(filePath, FileMode.CreateNew, FileAccess.ReadWrite))
                using (var srcStream = file.OpenReadStream())
                {
                    await srcStream.CopyToAsync(diskFile, 4096);
                }

                return new SuccessResultReturn<string>(path);
            }
            catch (Exception ex)
            {
                return new FailResultReturn<string>(ex);
            }

        }

        //public static string GetClientIPAddress(this HttpRequest request)
        //{

        //    string ip = null;

        //    if (request.ServerVariables["HTTP_VIA"] != null) // using proxy
        //    {
        //        ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToStringEx();  // Return real client IP.
        //    }
        //    else// not using proxy or can't get the Client IP
        //    {
        //        ip = request.ServerVariables["REMOTE_ADDR"].ToStringEx(); //While it can't get the Client IP, it will return proxy IP.
        //    }

        //    return ip;
        //}

        /// <summary>
        /// 获取当前站点的宿主程序信息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IHostingEnvironment GetHostingEnvironment(this HttpRequest request)
        {
            var env = (IHostingEnvironment)request.HttpContext.RequestServices.GetService(typeof(IHostingEnvironment));

            return env;
        }

        /// <summary>
        /// 用于提供类似原先Asp.net 的Server.MapPath功能
        /// </summary>
        /// <param name="request"></param>
        /// <param name="path">目录</param>
        /// <returns></returns>
        public static string MapPath(this HttpRequest request, string path)
        {
            if (path[0]=='/')
            {
                return Path.Combine(request.GetWebsitePath(), path.Substring(1));
            }
            else
            {
                return Path.Combine(request.GetWebsitePath(), path);
            }
            
        }

        /// <summary>
        /// 获取当前站点的所在文件夹,站点的根目录
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetWebsitePath(this HttpRequest request)
        {
            var env = GetHostingEnvironment(request);

            return env.ContentRootPath;
        }

        /// <summary>
        /// 获取当前站点的useragent
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetUserAgent(this HttpRequest request)
        {
            return request.Headers["User-Agent"].FirstOrDefault();
        }

        /// <summary>
        /// 判断当前站点是否为移动端浏览器
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool IsMobileBrowser(this HttpRequest request)
        {
            string u = GetUserAgent(request);

            return (b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)));
        }

        /// <summary>
        /// 获取客户端的IP,如果有反向代理,也可以获取正确的客户端IP
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetClienIP(this HttpRequest request)
        {
            var ip = request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = request.HttpContext.Connection?.RemoteIpAddress?.ToStringEx();
            }
            return ip;
        }

        public static string GetBodyString(this HttpRequest request,Encoding encoding=null)
        {
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
            }
            
            //var oldPositon = request.Body.Position;

            request.Body.Position = 0;

            if (encoding==null)
            {
                encoding=Encoding.UTF8;
            }

            var data = request.Body.ReadToEnd(encoding);

            request.Body.Position = 0;

            return data;
        }
    }
}
