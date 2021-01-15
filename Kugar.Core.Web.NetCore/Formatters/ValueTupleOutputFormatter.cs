using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using Newtonsoft.Json;

namespace Kugar.Core.Web.Formatters
{
    public class ValueTupleOutputFormatter : TextOutputFormatter
    {
        private static ConcurrentDictionary<Type, bool> _canHandleType = new ConcurrentDictionary<Type, bool>();  //缓存一个Type是否能处理,提高性能,不用每次都判断
        private static ConcurrentDictionary<MethodInfo, JsonSerializerSettings> _cacheSettings = new ConcurrentDictionary<MethodInfo, JsonSerializerSettings>(); //用于缓存不同的函数的JsonSerializerSettings,各自定义,避免相互冲突

        private Action<ValueTupleContractResolver> _resolverConfigFunc = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolverConfigFunc">用于在注册Formatter的时候对ContractResolver进行配置修改,比如属性名的大小写之类的</param>
        public ValueTupleOutputFormatter(Action<ValueTupleContractResolver> resolverConfigFunc = null)
        {
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("text/json");
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);

            _resolverConfigFunc = resolverConfigFunc;
        }

        protected override bool CanWriteType(Type type)
        {
            return _canHandleType.GetOrAdd(type, t =>
            {
                return type.GetProperties()  //判断该类是否包含有ValueTuple的属性
                    .Where(x => x.CanRead && (CustomAttributeExtensions.GetCustomAttribute<TupleElementNamesAttribute>((MemberInfo) x) != null || x.PropertyType.IsValueTuple()))
                    .Any();
            });
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var acce = (IActionContextAccessor)context.HttpContext.RequestServices.GetService(typeof(IActionContextAccessor));

#if NETCOREAPP2_1
            var ac = acce.ActionContext.ActionDescriptor as ControllerActionDescriptor;
#endif
#if NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0
            var endpoint = acce.ActionContext.HttpContext.GetEndpoint();
            var ac = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();   //用来获取当前Action对应的函数信息
#endif

            var settings = _cacheSettings.GetOrAdd(ac.MethodInfo, m =>  //这里主要是为了配置settings,每个methodinfo对应一个自己的settings,当然也就是每个MethodInfo一个CustomContractResolver,防止相互冲突
            {
                var orgSettings = JsonConvert.DefaultSettings?.Invoke();  //获取默认的JsonSettings
                var tmp = orgSettings != null ? cloneSettings(orgSettings) : new JsonSerializerSettings();  //如果不存在默认的,则new一个,如果已存在,则clone一个新的
                var resolver = new ValueTupleContractResolver(m, tmp.ContractResolver is CompositeContractResolver ? null : tmp.ContractResolver); //创建自定义ContractResolver,传入函数信息

                _resolverConfigFunc?.Invoke(resolver);  //调用配置函数

                if (tmp.ContractResolver != null)  //如果已定义过ContractResolver,则使用CompositeContractResolver进行合并
                {
                    if (tmp.ContractResolver is CompositeContractResolver c)  //如果定义的是CompositeContractResolver,则直接插入到最前
                    {
                        c.Insert(0, resolver);
                    }
                    else
                    {
                        tmp.ContractResolver = new CompositeContractResolver()
                        {
                            resolver,
                            tmp.ContractResolver
                        };
                    }
                }
                else
                {
                    tmp.ContractResolver = new CompositeContractResolver()
                    {
                        resolver
                    };
                }

                return tmp;
            });

            var json = JsonConvert.SerializeObject(context.Object, Formatting.None, settings);  //调用序列化器进行序列化
            await context.HttpContext.Response.Body.WriteAsync(selectedEncoding.GetBytes(json));
        }

        private JsonSerializerSettings cloneSettings(JsonSerializerSettings settings)
        {
            var tmp = new JsonSerializerSettings();

            var properties = settings.GetType().GetProperties();

            foreach (var property in properties)
            {
                var pvalue = property.GetValue(settings);

                if (pvalue is ICloneable p2)
                {
                    property.SetValue(tmp, p2.Clone());
                }
                else
                {
                    property.SetValue(tmp, pvalue);
                }
            }

            return tmp;
        }

    }
}