using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web.WebApi
{
    public static class WebApiRequestExt
    {
        public static HttpPostedFile GetFile(this HttpRequestMessage request, string key)
        {
            return HttpContext.Current.Request.GetFile(key);
        }

        public static IEnumerable<HttpPostedFile> GetFiles(this HttpRequestMessage request)
        {
            var files = HttpContext.Current.Request.Files;

            if (files.HasData())
            {
                foreach (HttpPostedFile file in files)
                {
                    yield return file;
                }
            }
        }

        public static bool IsContainFiles(this HttpRequestMessage request)
        {
            return HttpContext.Current.Request.Files.AllKeys.Any();
        }

        public static string GetString(this HttpRequestHeaders headers, string key, string defaultValue = "")
        {
            if (headers.Contains(key))
            {
                return headers.GetValues(key).FirstOrDefault();
            }
            else
            {
                return defaultValue;
            }
        }


    }

    public static class WebApiHttpConfigurationExt
    {
        /// <summary>
        /// 注册Json解析器,用于在body中传入json数据,并自动绑定到函数的参数上
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HttpConfiguration UseWebApiJsonValueBinder(this HttpConfiguration config)
        {
            JsonActionValueBinder.Register(config);

            return config;
        }

        /// <summary>
        /// 使用newtonsoft的json序列化器替代原有webapi自带的json序列化器
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HttpConfiguration UseWebApiNewtonJsonTextFormatter(this HttpConfiguration config)
        {
            NewtonJsonTextFormatter.Register(config);

            return config;
        }

        /// <summary>
        /// 使用支持区分域名的ControlerSelector,在路由默认值中,加入 @namespace="xxxxxxx",即可
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static HttpConfiguration UseWebApiNamespaceController(this HttpConfiguration config)
        {
            NamespaceHttpControllerSelector.Register(config);

            return config;
        }
    }
}
