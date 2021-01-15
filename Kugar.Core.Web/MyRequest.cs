using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Web;



#if NETCOREAPP
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
#endif


namespace Kugar.Core.Web
{
    public static partial class MyRequest
    {
        private static Regex _regex = new Regex(@"data:image/(?<type>.+?);base64,(?<data>.+)", RegexOptions.Compiled);

#if Net45

        public static JObject GetJson(this HttpRequestBase request, string name, JObject defaultValue = null)
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return JObject.Parse(jstr);
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
                
            }
        }

        public static int? GetIntNullable(this System.Web.HttpRequestBase request, string keyName)
        {
            return GetIntNullable(request, keyName, null);
        }

        public static T GetIntEnum<T>(this HttpRequestBase request, string name, T defaultValue)
        {
            var v = GetInt(request, name, (int)Convert.ToInt32(defaultValue));

            if (Enum.IsDefined(typeof(T), v))
            {
                return (T)Enum.ToObject(typeof(T), v);
            }
            else
            {
                return defaultValue;
            }
        }

        public static int? GetIntNullable(this System.Web.HttpRequestBase request, string keyName, int? defaultValue)
        {
            return request[keyName].ToIntNullable(defaultValue);
        }

        public static string GetString(this System.Web.HttpRequestBase request, string name)
        {
            return GetString(request, name, "", true);
        }

        public static string GetString(this System.Web.HttpRequestBase request, string name, string defaultValue)
        {
            return GetString(request, name, defaultValue, true);
        }

        public static string GetString(this System.Web.HttpRequestBase request, string name, string defaultValue, bool autoDecode)
        {
            try
            {
                var s = request[name];

                if (string.IsNullOrWhiteSpace(s))
                {
                    return defaultValue;
                }
                else
                {
                    if (autoDecode)
                    {
                        return HttpUtility.UrlDecode(s,request.ContentEncoding);
                    }
                    else
                    {
                        return s;
                    }
                }
            }
            catch (Exception)
            {
                return defaultValue;
            }


        }


        /// <summary>
        /// 获取指定字段的图片二进制数据，该字段需为data:image/格式，并返回扩展名，如果指定字段格式为未图片数据或者解码错误，则返回null
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ImageDataUrl GetImageRawFromDataUrl(this HttpRequestBase request, string name)
        {
            var data = request.GetString(name, "", false);

            if (string.IsNullOrEmpty(data))
            {
                return null;
            }
            else
            {
                var result = _regex.Match(data);

                if (!result.Success)
                {
                    return null;
                }
                else
                {
                    data = result.Groups["data"].Value;
                    var type = result.Groups["type"].Value;
                    try
                    {
                        return new ImageDataUrl()
                               {
                                   Data = Convert.FromBase64String(data),
                                   FileExtension = $".{type}"
                               };
                    }
                    catch (Exception e)
                    {
                        return null;
                    }


                }
            }
        }

        public static bool GetBool(this System.Web.HttpRequestBase request, string name, bool defaultValue = false)
        {
            try
            {
                var s = request[name];

                if (string.IsNullOrWhiteSpace(s))
                {
                    return defaultValue;
                }
                else
                {
                    var s1 = HttpUtility.UrlDecode(s).Trim();

                    if (string.Compare(s1, "true", true) == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return defaultValue;
            }


        }

        public static int GetInt(this System.Web.HttpRequestBase request, string name, int defaultValue = 0)
        {
            var s = GetString(request, name);

            var i = 0;

            if (int.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static decimal GetDecimal(this System.Web.HttpRequestBase request, string name, decimal defaultValue = 0m)
        {
            var s = GetString(request, name);

            decimal i;

            if (decimal.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static decimal? GetDecimalNullable(this System.Web.HttpRequestBase request, string name, decimal? defaultValue =null)
        {
            var s = GetString(request, name);

            decimal i;

            if (decimal.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static long GetLong(this HttpRequestBase request, string name, long defaultValue = 0)
        {
            var s = GetString(request, name);

            long i = 0;

            if (long.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static long? GetLongNullable(this HttpRequestBase request, string name, long? defaultValue = null)
        {
            var s = GetString(request, name);

            long i = 0;

            if (long.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static DateTime? GetDateTimeNullable(this HttpRequestBase request, string name, string formatStr,
            DateTime? defaultValue)
        {
            var s = GetString(request, name);

            long i = 0;

            return s.ToDateTimeNullable(formatStr, defaultValue);
        }

        public static DateTime GetDateTime(this HttpRequestBase request, string name, string formatStr,
            DateTime defaultValue)
        {
            var s = GetString(request, name);

            long i = 0;

            return s.ToDateTime(formatStr, defaultValue);
        }


        public static string GetString(this HttpRequest request, string name)
        {
            return GetString(request, name, "", true);
        }



        public static string GetString(this HttpRequest request, string name, string defaultValue)
        {
            return GetString(request, name, defaultValue, true);
        }

        public static bool IsFileExist(this HttpRequest request, string key)
        {
            if (request.Files.Count > 0 && request.Files.AllKeys.Contains(key))
            {
                var file = request.Files.Get(key);

                if (file != null && file.ContentLength > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsFileExist(this HttpRequestBase request, string key)
        {
            if (request.Files.Count > 0 && request.Files.AllKeys.Contains(key))
            {
                var file = request.Files.Get(key);

                if (file != null && file.ContentLength > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetString(this HttpRequest request, string name, string defaultValue, bool autoDecode)
        {
            try
            {
                var s = request[name];

                if (string.IsNullOrWhiteSpace(s))
                {
                    return defaultValue;
                }
                else
                {
                    if (autoDecode)
                    {
                        return HttpUtility.UrlDecode(s);
                    }
                    else
                    {
                        return s;
                    }
                }
            }
            catch (Exception)
            {
                return defaultValue;
            }


        }

        public static string GetRandomName(this HttpPostedFileBase file)
        {
            return
                $"{DateTime.Now.ToString("yyyyMMddHHmmssffff")}{RandomEx.Next(100, 999)}{Path.GetExtension(file.FileName)}";
        }

        public static string GetRandomName(this HttpPostedFile file)
        {
            return
                $"{DateTime.Now.ToString("yyyyMMddHHmmssffff")}{RandomEx.Next(100, 999)}{Path.GetExtension(file.FileName)}";
        }

        public static HttpPostedFileBase GetFile(this HttpRequestBase request)
        {
            return GetFile(request, "");
        }

        public static HttpPostedFileBase GetFile(this HttpRequestBase request, string key)
        {
            if (request.Files.Count > 0 )
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return request.Files.Get(0);
                }
                else
                {
                    var file = request.Files.Get(key);

                    if (file != null && file.ContentLength > 0)
                    {
                        return file;
                    }                    
                }


            }

            return null;
        }

        public static HttpPostedFile GetFile(this HttpRequest request)
        {
            return GetFile(request, "");
        }

        public static HttpPostedFile GetFile(this HttpRequest request, string key)
        {
            if (request.Files.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    return request.Files.Get(0);
                }
                else
                {
                    var file = request.Files.Get(key);

                    if (file != null && file.ContentLength > 0)
                    {
                        return file;
                    }                    
                }
            }

            return null;
        }

            /// <summary>
        /// 保存HttpPost的文件的扩展方式,,自动创建文件夹,同名文件存在时,自动删除
        /// </summary>
        /// <param name="file"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ResultReturn SaveAsEx(this HttpPostedFile file, string path)
        {
            if (file == null || file.ContentLength <= 0)
            {
                return new FailResultReturn(new ArgumentOutOfRangeException(nameof(file)));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return new FailResultReturn(new ArgumentNullException(nameof(path)));
            }
            
            try
            {
                string fullPath = "";

                if (!IsFileFullPath(path))
                {
                    fullPath = HttpContext.Current.Server.MapPath(path);
                }

                var dirPath = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                file.SaveAs(fullPath);

                return new SuccessResuleReturn();
            }
            catch (Exception ex)
            {
                return new FailResultReturn(ex);
            }

        }

        /// <summary>
        /// 保存HttpPost的文件的扩展方式,,自动创建文件夹,同名文件存在时,自动删除
        /// </summary>
        /// <param name="file"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ResultReturn SaveAsEx(this HttpPostedFileBase file, string path)
        {
            if (file == null || file.ContentLength <= 0)
            {
                return new FailResultReturn(new ArgumentOutOfRangeException(nameof(file)));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return new FailResultReturn(new ArgumentNullException(nameof(path)));
            }

            try
            {
                string fullPath = "";

                if (!IsFileFullPath(path))
                {
                    fullPath = HttpContext.Current.Server.MapPath(path);
                }

                var dirPath = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                file.SaveAs(fullPath);

                return new SuccessResuleReturn();
            }
            catch (Exception ex)
            {
                return new FailResultReturn(ex);
            }

        }

        public static JObject[] GetJsonJObjects(this HttpRequestBase request, string name, JObject[] defaultValue = null)
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return JArray.Parse(jstr).Select(x => (JObject)x).ToArrayEx();
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
                

                //return jstr.DeserializeFromJsonString(defaultValue);
            }
        }

    
        public static JArray GetJArray(this HttpRequestBase request, string name, JArray defaultValue = null)
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return JArray.Parse(jstr);
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
            }
        }

    
        public static T GetJson<T>(this HttpRequestBase request, string name, T defaultValue = default(T))
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                return jstr.DeserializeToObject<T>(defaultValue);
            }
        }

                /// <summary>
        /// 返回当前客户端的IP地址
        /// </summary>
        public static string GetClientIPAddress(this HttpRequestBase request)
        {

            string ip = null;

            if (request.ServerVariables["HTTP_VIA"] != null) // using proxy
            {
                ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToStringEx();  // Return real client IP.
            }
            else// not using proxy or can't get the Client IP
            {
                ip = request.ServerVariables["REMOTE_ADDR"].ToStringEx(); //While it can't get the Client IP, it will return proxy IP.
            }

            return ip;
        }

        /// <summary>
        /// 返回当前客户端的IP地址
        /// </summary>
        public static string GetClientIPAddress(this HttpRequest request)
        {

            string ip = null;

            if (request.ServerVariables["HTTP_VIA"] != null) // using proxy
            {
                ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"].ToStringEx();  // Return real client IP.
            }
            else// not using proxy or can't get the Client IP
            {
                ip = request.ServerVariables["REMOTE_ADDR"].ToStringEx(); //While it can't get the Client IP, it will return proxy IP.
            }

            return ip;
        }

    
        public static bool IsMobileBrowser(this HttpRequest request)
        {
            string u = request.ServerVariables["HTTP_USER_AGENT"];

            return (b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)));
        }

        public static bool IsMobileBrowser(this HttpRequestBase request)
        {
            string u = request.ServerVariables["HTTP_USER_AGENT"];

            return (b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)));
        }

#endif


        /// <summary>
        /// 获取指定字段的图片二进制数据，该字段需为data:image/格式，并返回扩展名，如果指定字段格式为未图片数据或者解码错误，则返回null
        /// </summary>
        /// <param name="request"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static ImageDataUrl GetImageRawFromDataUrl(this HttpRequest request, string name)
        {
            var data = request.GetString(name, "", false);

            if (string.IsNullOrEmpty(data))
            {
                return null;
            }
            else
            {
                var result = _regex.Match(data);

                if (!result.Success)
                {
                    return null;
                }
                else
                {
                    data = result.Groups["data"].Value;
                    var type = result.Groups["type"].Value;
                    try
                    {
                        return new ImageDataUrl()
                               {
                                   Data = Convert.FromBase64String(data),
                                   FileExtension = $".{type}"
                               };
                    }
                    catch (Exception e)
                    {
                        return null;
                    }


                }
            }
        }


        public static bool GetBool(this HttpRequest request, string name, bool defaultValue = false)
        {
            try
            {
                var s = GetString(request,name,string.Empty);

                if (string.IsNullOrWhiteSpace(s))
                {
                    return defaultValue;
                }
                else
                {
                    var s1 = HttpUtility.UrlDecode(s).Trim();

                    if (string.Compare(s1, "true", true) == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return defaultValue;
            }


        }

        public static int GetInt(this HttpRequest request, string name, int defaultValue = 0)
        {
            var s = GetString(request, name);

            var i = 0;

            if (int.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static int? GetIntNullable(this HttpRequest request, string name, int? defaultValue = null)
        {
            return request.GetString(name).ToIntNullable(defaultValue);
        }

        public static decimal GetDecimal(this HttpRequest request, string name, decimal defaultValue = 0m)
        {
            var s = GetString(request, name);

            decimal i;

            if (decimal.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static decimal? GetDecimalNullable(this HttpRequest request, string name, decimal? defaultValue =null)
        {
            var s = GetString(request, name);

            decimal i;

            if (decimal.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static long GetLong(this HttpRequest request, string name, long defaultValue = 0)
        {
            var s = GetString(request, name);

            long i = 0;

            if (long.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static long? GetLongNullable(this HttpRequest request, string name, long? defaultValue = null)
        {
            var s = GetString(request, name);

            long i = 0;

            if (long.TryParse(s, out i))
            {
                return i;
            }
            else
            {
                return defaultValue;
            }
        }

        public static DateTime? GetDateTimeNullable(this HttpRequest request, string name, string formatStr = "yyyy-MM-dd",
                                                    DateTime? defaultValue=null)
        {
            var s = GetString(request, name);

            long i = 0;

            return s.ToDateTimeNullable(formatStr, defaultValue);
        }

        public static DateTime GetDateTime(this HttpRequest request, string name, string formatStr , DateTime defaultValue)
        {
            var s = GetString(request, name);

            long i = 0;

            return s.ToDateTime(formatStr, defaultValue);

            //if (long.TryParse(s, out i))
            //{
            //    return i;
            //}
            //else
            //{
            //    return defaultValue;
            //}
        }

        public static DateTime GetDateTime(this HttpRequest request, string name, string formatStr = "yyyy-MM-dd")
        {
            return GetDateTime(request, name, formatStr, DateTime.Now);
        }

        public static T GetIntEnum<T>(this HttpRequest request, string name, T defaultValue)
        {
            var v = GetInt(request, name, (int)Convert.ToInt32(defaultValue));

            if (Enum.IsDefined(typeof(T), v))
            {
                return (T)Enum.ToObject(typeof(T), v);
            }
            else
            {
                return defaultValue;
            }
        }

        public static T? GetIntEnumNullable<T>(this HttpRequest request, string name, T defaultValue) where T:struct
        {
#if NETCOREAPP
            
            if (!request.Form.ContainsKey(name) && !request.Query.ContainsKey(name))
            {
                return null;
            }
#endif
#if Net45
            if (!request.Form.AllKeys.Any(x=>x==name) && !request.QueryString.AllKeys.Any(x=>x==name))
            {
                return null;
            }
#endif
            
            
            

            var v = GetInt(request, name, (int)Convert.ToInt32(defaultValue));

            if (Enum.IsDefined(typeof(T), v))
            {
                return (T)Enum.ToObject(typeof(T), v);
            }
            else
            {
                return defaultValue;
            }
        }


        public static JObject GetJson(this HttpRequest request, string name, JObject defaultValue = null)
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return JObject.Parse(jstr);
                }
                catch (Exception e)
                {
                    return defaultValue;
                }

            }
        }



        public static JObject[] GetJsonJObjects(this HttpRequest request, string name, JObject[] defaultValue = null)
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return JArray.Parse(jstr).Select(x => (JObject)x).ToArrayEx();
                }
                catch (Exception e)
                {
                    return defaultValue;
                }

                //return jstr.DeserializeFromJsonString(defaultValue);
            }
        }


        public static JArray GetJArray(this HttpRequest request, string name, JArray defaultValue = null)
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                try
                {
                    return JArray.Parse(jstr);
                }
                catch (Exception e)
                {
                    return defaultValue;
                }
            }
        }


        public static T GetJson<T>(this HttpRequest request, string name, T defaultValue = default(T))
        {
            var jstr = request.GetString(name, "");

            if (string.IsNullOrWhiteSpace(jstr))
            {
                return defaultValue;
            }
            else
            {
                return jstr.DeserializeToObject<T>(defaultValue);
            }
        }
        

        

        private static readonly Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);


        private static Regex _re_file = new Regex(@"^[A-Z]:\\{1,2}(([^\\/:\*\?<>\|]+)\\{1,2})+([^\\/:\*\?<>\|]+)(\.[A-Z]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex _re_shared_file = new Regex(@"^\\{2}(([^\\/:\*\?<>\|]+)\\{1,2})+([^\\/:\*\?<>\|]+)(\.[A-Z]+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsFileFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            //// ???????? (???D:\\Test.gif)
            //Regex dirRegex = new Regex(@"^[A-Z]:\\{1,2}(([^\\/:\*\?<>\|]+)\\{1,2})+([^\\/:\*\?<>\|]+)(\.[A-Z]+)$", RegexOptions.IgnoreCase);
            //// ????????(???\\MyComputer\Test.gif)
            //Regex sharedDirRegex = new Regex(@"^\\{2}(([^\\/:\*\?<>\|]+)\\{1,2})+([^\\/:\*\?<>\|]+)(\.[A-Z]+)$", RegexOptions.IgnoreCase);

            return _re_file.IsMatch(path) || _re_shared_file.IsMatch(path);
        }



        public class ImageDataUrl
        {
            public byte[] Data { set; get; }

            /// <summary>
            /// 图片数据对应的扩展名，如 ".jepg"
            /// </summary>
            public string FileExtension { set; get; }
        }

#region WebApi

        //public static object GetFile(this HttpRequestMessage request)
        //{
        //    if (!request.Content.IsMimeMultipartContent())
        //    {
        //        //throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

        //        return null;
        //    }

        //    Dictionary<string, string> dic = new Dictionary<string, string>();
        //    string root = HttpContext.Current.Server.MapPath("~/App_Data");//指定要将文件存入的服务器物理位置  
        //    var provider = new MultipartMemoryStreamProvider();

        //    var p = new MultipartFormDataContent();



        //    try
        //    {
        //        // Read the form data.  
        //        var s= request.Content.ReadAsMultipartAsync(provider).Result;

        //        foreach (var file in provider.Contents)
        //        {
        //            if (file != null)
        //            {
        //                var fileName = file.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
        //            }

        //            var filename = file.Headers.ContentDisposition.FileName.Trim('\"');
        //            var buffer = await file.ReadAsByteArrayAsync();
        //            //Do whatever you want with filename and its binaray data.
        //        }


        //        //// This illustrates how to get the file names.  
        //        //foreach (MultipartFileData file in provider.FileData)
        //        //{//接收文件  
        //        //    Trace.WriteLine(file.Headers.ContentDisposition.FileName);//获取上传文件实际的文件名  
        //        //    Trace.WriteLine("Server file path: " + file.LocalFileName);//获取上传文件在服务上默认的文件名  
        //        //}//TODO:这样做直接就将文件存到了指定目录下，暂时不知道如何实现只接收文件数据流但并不保存至服务器的目录下，由开发自行指定如何存储，比如通过服务存到图片服务器  
        //        //foreach (var key in provider.FormData.AllKeys)
        //        //{//接收FormData  
        //        //    dic.Add(key, provider.FormData[key]);
        //        //}
        //    }
        //    catch
        //    {
        //        throw;
        //    }
        //    return dic;
        //}

#endregion
    }


}
