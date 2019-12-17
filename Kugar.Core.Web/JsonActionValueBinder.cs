using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using System.Web.Http.ModelBinding.Binders;
using System.Web.Http.ValueProviders;
using System.Web.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using IModelBinder = System.Web.Http.ModelBinding.IModelBinder;
using IValueProvider = System.Web.Http.ValueProviders.IValueProvider;
using ModelBinderProvider = System.Web.Http.ModelBinding.ModelBinderProvider;
using ValueProviderResult = System.Web.Http.ValueProviders.ValueProviderResult;

namespace Kugar.Core.Web
{
    /// <summary>
    /// 用于WebApi处理当Post传入的Body数据是Json时,对函数参数进行绑定赋值<br></br>
    /// 使用方法为: 在WebApiConfig中 调用config.Services.Replace(typeof(IActionValueBinder),new JsonActionValueBinder());<br></br>
    /// 然后在需要使用Json绑定参数的Action上加上 【FromBodyJson】特性
    /// </summary>
    public class JsonActionValueBinder : DefaultActionValueBinder
    {
        public JsonActionValueBinder()
        {

        }

        public static void Register(HttpConfiguration config)
        {
            config.Services.Replace(typeof(IActionValueBinder), new JsonActionValueBinder());
        }

        public override HttpActionBinding GetBinding(HttpActionDescriptor actionDescriptor)
        {
            var action = (ReflectedHttpActionDescriptor) actionDescriptor;

            var atr = action.MethodInfo.GetCustomAttribute<FromBodyJsonAttribute>();

            if (atr != null)
            {
                JsonValueActionBinding actionBinding = new JsonValueActionBinding();

                HttpParameterDescriptor[] parameters = actionDescriptor.GetParameters().ToArray();
                HttpParameterBinding[] binders = Array.ConvertAll(parameters, p => DetermineBinding(actionBinding, p));

                actionBinding.ActionDescriptor = actionDescriptor;
                actionBinding.ParameterBindings = binders;

                return actionBinding;
            }
            else if (action.MethodInfo.GetCustomAttribute<FromFormDataAttribute>() != null)
            {
                var a = base.GetBinding(actionDescriptor);
                HttpParameterDescriptor[] parameters = actionDescriptor.GetParameters().ToArray();

                a.ParameterBindings = parameters.Select(x => DefaultBinding(a, x)).ToArray();

                return a;
            }
            else
            {
                var a = base.GetBinding(actionDescriptor);

                return a;
            }
        }

        protected override HttpParameterBinding GetParameterBinding(HttpParameterDescriptor parameter)
        {
            return base.GetParameterBinding(parameter);
        }

        private HttpParameterBinding DefaultBinding(HttpActionBinding actionBinding, HttpParameterDescriptor parameter)
        {
            HttpConfiguration config = parameter.Configuration;

            var attr = new ModelBinderAttribute(); // use default settings

            ModelBinderProvider provider = attr.GetModelBinderProvider(config);
            IModelBinder binder = provider.GetBinder(config, parameter.ParameterType);

            //if (binder is ComplexModelBinder)
            //{
            //    binder=new CompositeModelBinder();
            //}

            //var  binder=new CompositeModelBinder();

            // Alternatively, we could put this ValueProviderFactory in the global config.
            List<ValueProviderFactory> vpfs = new List<ValueProviderFactory>(attr.GetValueProviderFactories(config));
            //vpfs.Add(new BodyValueProviderFactory());
            vpfs.Add(new BodyValueProviderFactory());

            return new ModelBinderParameterBinding(parameter, binder, vpfs);
        }

        private HttpParameterBinding DetermineBinding(JsonValueActionBinding actionBinding,
            HttpParameterDescriptor parameter)
        {
            HttpConfiguration config = parameter.Configuration;

            var attr = new ModelBinderAttribute(); // use default settings

            IModelBinder binder = null;

            if (parameter.ParameterType.IsClass && !parameter.ParameterType.IsPrimitive &&
                parameter.ParameterType != typeof(string))
            {
                binder = new CompositeModelBinder(new System.Web.Http.ModelBinding.Binders.TypeConverterModelBinder(),
                    new System.Web.Http.ModelBinding.Binders.TypeMatchModelBinder());
            }
            else
            {
                ModelBinderProvider provider = attr.GetModelBinderProvider(config);
                binder = provider.GetBinder(config, parameter.ParameterType);
            }



            //var  binder=new CompositeModelBinder();

            // Alternatively, we could put this ValueProviderFactory in the global config.
            List<ValueProviderFactory> vpfs = new List<ValueProviderFactory>(attr.GetValueProviderFactories(config));
            vpfs.Add(new JsonValueProviderFactory());

            return new ModelBinderParameterBinding(parameter, binder, vpfs);
        }

        // Derive from ActionBinding so that we have a chance to read the body once and then share that with all the parameters.
        private class JsonValueActionBinding : HttpActionBinding
        {
            // Read the body upfront , add as a ValueProvider
            public override async Task ExecuteBindingAsync(HttpActionContext actionContext,
                CancellationToken cancellationToken)
            {
                HttpRequestMessage request = actionContext.ControllerContext.Request;
                HttpContent content = request.Content;
                if (content != null)
                {

                    var jsonStr = await content.ReadAsStringAsync();

                    try
                    {
                        var json = JObject.Parse(jsonStr);

                        request.Properties["JsonValue"] = new JsonValueProvider(json);
                    }
                    catch (Exception)
                    {

                    }
                }

                await base.ExecuteBindingAsync(actionContext, cancellationToken);
            }
        }

        private class JsonValueProvider : IValueProvider
        {
            private JObject _json = null;

            public JsonValueProvider(JObject json)
            {
                _json = json;
            }

            public bool ContainsPrefix(string prefix)
            {
                //return false;

                JToken j;


                return _json.TryGetValue(prefix, StringComparison.CurrentCultureIgnoreCase, out j);
            }

            public ValueProviderResult GetValue(string key)
            {
                JToken value;

                if (_json.TryGetValue(key, StringComparison.CurrentCultureIgnoreCase, out value))
                {


                    return new JsonValueProviderResult(key,value, value.ToString(Formatting.None),
                        CultureInfo.CurrentCulture);
                }
                else
                {
                    return null;
                }

                //throw new NotImplementedException();
            }
        }

        public class JsonValueProviderResult : ValueProviderResult
        {
            private JToken _rawValue;
            private string _key;

            public JsonValueProviderResult(string key, JToken rawValue,
                string attemptedValue,
                CultureInfo culture) : base(rawValue, attemptedValue, culture)
            {
                _rawValue = rawValue;
                _key = key;
            }

            public override object ConvertTo(Type type, CultureInfo culture)
            {
                try
                {
                    return _rawValue.ToObject(type);
                }
                catch (Exception)
                {
                    throw new Exception($"参数{_key}的数值类型转换失败,请检查输入的数据");
                }

                
            }

            
        }

        //private class JsonModelBinderProvider : ModelBinderProvider
        //{
        //    public override IModelBinder GetBinder(HttpConfiguration configuration, Type modelType)
        //    {
        //        if (modelType.IsClass)
        //        {
        //            return new 
        //        }
        //    }
        //}

        private class JsonValueProviderFactory : ValueProviderFactory
        {
            public override IValueProvider GetValueProvider(HttpActionContext actionContext)
            {
                object vp;

                if (actionContext.Request.Properties.TryGetValue("JsonValue", out vp))
                {
                    return (IValueProvider)vp; 
                }
                else
                {
                    return null;
                }               
            }

            
        }

        private class BodyValueProviderFactory : ValueProviderFactory
        {
            public override IValueProvider GetValueProvider(HttpActionContext actionContext)
            {
                return new BodyValueProvider(actionContext);
            }

            private class BodyValueProvider : IValueProvider
            {
                private HttpActionContext _actionContext;

                public BodyValueProvider(HttpActionContext actionContext)
                {
                    _actionContext = actionContext;
                }

                public bool ContainsPrefix(string prefix)
                {
                    return true;
                }

                public ValueProviderResult GetValue(string key)
                {
                    var value = HttpContext.Current.Request.Form[key];

                    return new ValueProviderResult(value,value,CultureInfo.CurrentCulture);
                }
            }
        }
    }

    /// <summary>
    /// 在Action上,增加该特性,可以在Body中传入Json对象时,对参数进行赋值
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,AllowMultiple = false)]
    public class FromBodyJsonAttribute : Attribute
    {
        
    }

     /// <summary>
    /// 在Action上,增加该特性,可以在Body中传入Form参数时,对参数进行赋值
    /// </summary>
    [AttributeUsage(AttributeTargets.Method,AllowMultiple = false)]
    public class FromFormDataAttribute : Attribute
    {
        
    }
}
