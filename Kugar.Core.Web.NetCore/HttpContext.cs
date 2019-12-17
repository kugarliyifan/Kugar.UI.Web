using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Kugar.Core.Web
{
    public static class HttpContext
    {
        private static Microsoft.AspNetCore.Http.IHttpContextAccessor m_httpContextAccessor;


        internal static void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            m_httpContextAccessor = httpContextAccessor;
            ApplicationBuilder = app;
        }

        public static IApplicationBuilder ApplicationBuilder { get; private set; }

        public static Microsoft.AspNetCore.Http.HttpContext Current
        {
            get
            {
                if (m_httpContextAccessor == null)
                {
                    throw new Exception("未初始化,请使用app.UseStaticHttpContext()");
                }
                return m_httpContextAccessor.HttpContext;
            }
        }
    }

    public static class HttpContextExt
    {
        /// <summary>
        /// 配置HttpContext.Current类
        /// </summary>
        /// <param name="app"></param>
        public static void UseStaticHttpContext(this IApplicationBuilder app)
        {
            HttpContext.Configure(app,app.ApplicationServices.
                GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
        }

        /// <summary>
        /// 设置一个对象,必须是可序列化为json的
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        /// <summary>
        /// 返回一个对象
        /// </summary>
        /// <typeparam name="T">必须是可序列化为json的类型</typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetObject<T>(this ISession session, string key)
        {
            return GetObject<T>(session, key, default(T));
        }

        /// <summary>
        /// 返回一个对象
        /// </summary>
        /// <typeparam name="T">必须是可序列化为json的类型</typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetObject<T>(this ISession session, string key,T defaultValue)
        {
            if (session.TryGetValue(key, out var data))
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
            }
            else
            {
                return defaultValue;
            }
            //var value = session.GetString(key);
            
        }
    }
}
