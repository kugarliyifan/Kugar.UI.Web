using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kugar.Core.Web
{
    public class JsonValueProviderFactory : IValueProviderFactory
    {
        private static JsonValueProvider _provider=new JsonValueProvider();
        private static ConcurrentDictionary<MethodInfo, FromBodyJsonAttribute> _cacheMethodIsJson = null;

        static JsonValueProviderFactory()
        {
            _cacheMethodIsJson=new ConcurrentDictionary<MethodInfo, FromBodyJsonAttribute>();
        }

        public async  Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            var d = (ControllerActionDescriptor)context.ActionContext.ActionDescriptor;

            if (context.ActionContext.HttpContext.Request.Method.CompareTo("get",true))
            {
                return;
            }

            //if (!context.ActionContext.HttpContext.Request.ContentType.Contains("json",true))
            //{
            //    return Task.CompletedTask;
            //}

            var contentType = context.ActionContext.HttpContext.Request.ContentType;

            if (!contentType.Contains("application/json",true) && !contentType.Contains("text/json",true))
            {
                return;
            }

            //var s = d.MethodInfo.GetCustomAttributes(typeof(FromBodyJsonAttribute), true);

            var attr = _cacheMethodIsJson.GetOrAdd(d.MethodInfo,
                x => (FromBodyJsonAttribute) d.MethodInfo.GetCustomAttributes(typeof(FromBodyJsonAttribute), true)
                    .FirstOrDefault());

            if (attr!=null)
            {
                context.ActionContext.HttpContext.Request.EnableBuffering();

                var inputStream= context.ActionContext.HttpContext.Request.Body;
                inputStream.Position = 0;

                var dataBytes=await inputStream.ReadAllBytesAsync();

                var jsonStr = Encoding.UTF8.GetString(dataBytes);

                JObject json = null;

                try
                {
                    json = JObject.Parse(jsonStr);
                }
                catch (Exception e)
                {
                    throw new Exception("request中的数据无法转换为json数据");
                }
                
                
                context.ActionContext.HttpContext.Items["__jsonData"] = json;
                inputStream.Position = 0;

                
                context.ValueProviders.Insert(0, _provider);
            }

        }



        internal class JsonModelBinderProvider : IModelBinderProvider
        {
            //private static JsonModelBinder _caseSensitivebinder = new JsonModelBinder();
            //private static JsonModelBinder _igoreCaseSensitive=new JsonModelBinder(false);

#if NETCOREAPP2_1 || Net45
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                var accessor = (IActionContextAccessor)context.Services.GetService(typeof(IActionContextAccessor));
                
                var ac = (accessor.ActionContext.ActionDescriptor as ControllerActionDescriptor);

                if (ac != null)
                {

                    var metaData = context.Metadata as DefaultModelMetadata;

                    if (metaData!=null)
                    {
                        //如果已经定义了别的绑定特性,则忽略,如FromQuery等
                        if (metaData.Attributes.ParameterAttributes!=null && metaData.Attributes.ParameterAttributes.Any(x=>x is IBindingSourceMetadata))
                        {
                            return null;
                        }
                    }
                }

                var attr = (FromBodyJsonAttribute)(accessor.ActionContext.ActionDescriptor as ControllerActionDescriptor).MethodInfo
                    .GetCustomAttributes(typeof(FromBodyJsonAttribute)).FirstOrDefault();

                if (attr!=null)
                {
                    if (attr != null)
                    {
                        var metaData = context.Metadata as DefaultModelMetadata;
                        
                        if (metaData?.Attributes?.ParameterAttributes?.Any(x => x is TupleElementNamesAttribute) == true)
                        {
                            var valueTupleAttr = (TupleElementNamesAttribute)metaData.Attributes.ParameterAttributes.FirstOrDefault(x =>
                                x is TupleElementNamesAttribute);

                            if (metaData.ModelType.IsIEnumerable())
                            {
                                return new JsonArrayValueTupleBinder(valueTupleAttr, metaData.ModelType,
                                    attr.IsCaseSensitive);
                            }

                            return new JsonValueTupleBinder(valueTupleAttr, metaData.ModelType, attr.IsCaseSensitive);
                        }
                        else
                        {
                            return new JsonModelBinder(attr.IsCaseSensitive);
                        }

                    }
                    else
                    {
                        return null;
                    }

                }

                return null;
            }
#endif

#if NETCOREAPP3_0 || NETCOREAPP3_1
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                var metaData = context.Metadata as DefaultModelMetadata;

                if (metaData != null)
                {
                    //如果已经定义了别的绑定特性,则忽略,如FromQuery等
                    if (metaData.Attributes.ParameterAttributes != null && metaData.Attributes.ParameterAttributes.Any(x => x is IBindingSourceMetadata))
                    {
                        return null;
                    }
                }

                var h = (IHttpContextAccessor)context.Services.GetService(typeof(IHttpContextAccessor));
                Endpoint endpoint = h.HttpContext.GetEndpoint();

                var attr = endpoint.Metadata.GetMetadata<FromBodyJsonAttribute>();

                if (attr!=null)
                {
                    if (metaData?.Attributes?.ParameterAttributes?.Any(x => x is TupleElementNamesAttribute) == true)
                    {
                        var valueTupleAttr = (TupleElementNamesAttribute)metaData.Attributes.ParameterAttributes.FirstOrDefault(x =>
                            x is TupleElementNamesAttribute);

                        if (metaData.ModelType.IsIEnumerable())
                        {
                            return new JsonArrayValueTupleBinder(valueTupleAttr, metaData.ModelType,
                                attr.IsCaseSensitive);
                        }

                        return new JsonValueTupleBinder(valueTupleAttr, metaData.ModelType,  attr.IsCaseSensitive);
                    }
                    else
                    {
                        return new JsonModelBinder(attr.IsCaseSensitive);
                    }

                }
                else
                {
                    return null;
                }

            }
#endif


        }

        private class JsonValueProvider : IValueProvider
        {
            private static ValueProviderResult _defaultValue = new ValueProviderResult();

            public bool ContainsPrefix(string prefix)
            {
                return false;
            }

            public ValueProviderResult GetValue(string key)
            {
                return _defaultValue;
            }
        }

    }

    internal abstract class JsonValueModelBinderBase : IModelBinder
    {
        private bool _isCaseSensitive = true;

        public JsonValueModelBinderBase(bool isCaseSensitive)
        {
            _isCaseSensitive = isCaseSensitive;
        }


        protected bool Validate(object value, ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelMetadata.ValidatorMetadata.Count > 0)
            {
                var validResults = new List<ValidationResult>(bindingContext.ModelMetadata.ValidatorMetadata.Count);

                var json = bindingContext.HttpContext.Items["__jsonData"];

                if (!Validator.TryValidateValue(value, new ValidationContext(json), validResults,
                    bindingContext.ModelMetadata.ValidatorMetadata.Select(x => (ValidationAttribute)x)))
                {
                    foreach (var result in validResults)
                    {
                        bindingContext.ModelState.AddModelError(bindingContext.FieldName, new ValidationException(result, null, value), bindingContext.ModelMetadata);
                    }

                    return false;
                }
                else
                {
                    return true;
                }
            }

            return true;
        }

        protected bool TryGetJsonValue(ModelBindingContext bindingContext, string fieldName, out JToken value)
        {
            if (bindingContext.HttpContext.Items.TryGetValue("__jsonData", out var item))
            {
                var json = (JObject)item;

                if (json?.ContainsKey(bindingContext.FieldName)!=true)
                {
                    value = null;
                    return false;
                }

                if (_isCaseSensitive)
                {
                    value = json?.GetValue(bindingContext.FieldName);
                }
                else
                {
                    value = json?.GetValue(bindingContext.FieldName, StringComparison.InvariantCultureIgnoreCase);
                }

                return true;
            }
            else
            {
                value = null;

                return false;
            }
        }


        public abstract Task BindModelAsync(ModelBindingContext bindingContext);

        protected bool IsCaseSensitive => _isCaseSensitive;
    }

    internal class JsonModelBinder : JsonValueModelBinderBase
    {
        

        public JsonModelBinder(bool isCaseSensitive):base(isCaseSensitive)
        {
            
        }

        

        public override async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (TryGetJsonValue(bindingContext, bindingContext.FieldName,out var jvalue) && jvalue!=null)
            {
                var value = jvalue?.ToObject(bindingContext.ModelType);

                Validate(value, bindingContext);

                bindingContext.ModelState.SetModelValue(bindingContext.FieldName, value, value.ToStringEx());
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }

        }
    }

    internal class JsonValueTupleBinder : JsonValueModelBinderBase
    {
        private TupleElementNamesAttribute _attr = null;
        private Type _modelType = null;
        private bool _isCaseSensitive = true;
        private Type[] _valueTypes = null;

        public JsonValueTupleBinder( TupleElementNamesAttribute attr,Type modelType, bool isCaseSensitive = true):base(isCaseSensitive)
        {
            _attr = attr;
            _modelType = modelType;
            _isCaseSensitive = isCaseSensitive;
            _valueTypes = modelType.GetGenericArguments();
        }

        protected object decodeJsonToValueTuple(JObject value,Type elementType)
        {
            var names = _attr.TransformNames;

            var values = new object[names.Count];

            for (int i = 0; i < names.Count; i++)
            {
                var type = GenericArgumentTypes[i];
                var name = names[i];

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                values[i]=value.GetValue(name)?.ToObject(type);
            }

            //Type genericType = Type.GetType("System.ValueTuple`" + values.Length);

            //Type specificType = genericType.MakeGenericType(_valueTypes);

            var o = Activator.CreateInstance(elementType, values);

            return o;
        }

        public override async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (TryGetJsonValue(bindingContext, bindingContext.FieldName,out var jvalue) && jvalue != null)
            {
                var value = decodeJsonToValueTuple((JObject) jvalue,_modelType);

                Validate(value, bindingContext);

                bindingContext.ModelState.SetModelValue(bindingContext.FieldName, value, value.ToStringEx());
                bindingContext.Result = ModelBindingResult.Success(value);
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
        }

        protected TupleElementNamesAttribute Attribute => _attr;

        protected Type ModelType
        {
            get => _modelType;
            set => _modelType = value;
        }

        protected Type[] GenericArgumentTypes
        {
            get => _valueTypes;
            set => _valueTypes = value;
        }
    }

    /// <summary>
    /// 用于解析数据类型的ValueTuple
    /// </summary>
    internal class JsonArrayValueTupleBinder : JsonValueTupleBinder
    {
        private bool _isArray = false;
        private bool _isList = false;
        private Type _elementType = null;

        public JsonArrayValueTupleBinder(TupleElementNamesAttribute attr, Type modelType, bool isCaseSensitive = true) : base(attr, modelType, isCaseSensitive)
        {
            if (modelType.IsArray)
            {
                _isArray = true;
                _elementType = modelType.GetElementType();
            }
            else 
            {
                if (modelType.IsImplementlInterface(typeof(IList<>)) || modelType.IsImplementlInterface(typeof(IEnumerable<>)))
                {
                    _elementType = modelType.GetGenericArguments()[0];
                    //ModelType = typeof(List<>).MakeGenericType(modelType.GetGenericArguments());
                    GenericArgumentTypes = modelType.GetGenericArguments()[0].GetGenericArguments();
                    _isList = true;
                }
            }
            

        }



        public override async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (TryGetJsonValue(bindingContext, bindingContext.FieldName, out var jvalue) && jvalue != null)
            {
                if (jvalue == null)
                {
                    bindingContext.ModelState.SetModelValue(bindingContext.ModelName,null, jvalue.ToStringEx());

                    bindingContext.Result = ModelBindingResult.Success(null);
                }
                else if (jvalue is JArray jarray)
                {
                    IList array = Array.CreateInstance(_elementType,jvalue.Count());

                    for (int i = 0; i < jarray.Count; i++)
                    {
                        var elementJson = jarray[i];

                        if (!(elementJson is JObject))
                        {
                            bindingContext.Result = ModelBindingResult.Failed();
                            return;
                        }

                        var value = decodeJsonToValueTuple((JObject)elementJson, _elementType);

                        array[i] = value;
                    }

                    Validate(array, bindingContext);

               
                    if (_isArray)
                    {
                        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, array, array.ToStringEx());
                        bindingContext.Result = ModelBindingResult.Success(array);
                    }
                    else
                    {
                        var result = Activator.CreateInstance(ModelType, array);
                        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, result, result.ToStringEx());
                        bindingContext.Result = ModelBindingResult.Success(result);
                    }

                    
                }
                else
                {
                    bindingContext.Result = ModelBindingResult.Failed();
                }
                
            }
            else
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }

            
        }
    }


    public static class JsonValueBindHelper
    {
        ///// <summary>
        ///// 启用action函数参数的json方式绑定,如果启用的话,在需要绑定参数值的action上加FromBodyJson特性
        ///// </summary>
        ///// <param name="opt"></param>
        //public static void EnableJsonValueModelBinder(this MvcOptions opt)
        //{
        //    opt.ModelBinderProviders.Insert(0, new JsonValueProviderFactory.JsonModelBinderProvider());
        //    opt.ValueProviderFactories.Insert(0, new JsonValueProviderFactory());
        //}

        public static IMvcBuilder EnableJsonValueModelBinder(this IMvcBuilder builder)
        {
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            //JsonConvert.DefaultSettings= (JsonConvert.DefaultSettings??(()=>new JsonSerializerSettings()));

            //JsonConvert.DefaultSettings().Converters.Add(new JObjectConverter());

            builder.AddMvcOptions(opt =>
            {
                opt.ModelBinderProviders.Insert(0,new JsonValueProviderFactory.JsonModelBinderProvider());
                opt.ValueProviderFactories.Insert(0, new JsonValueProviderFactory());

                //var jsonFormater = (JsonOutputFormatter)opt.OutputFormatters.FirstOrDefault(x => x is JsonOutputFormatter);

                //jsonFormater.PublicSerializerSettings.Converters.Add(new JObjectConverter());
                //jsonFormater..Converters.Add(new JObjectConverter());
                //opt.FormatterMappings.
                //opt.OutputFormatters.Add( new JsonOutputFormatter(null, null));
            });
            //    .AddJsonOptions((opts) =>
            //{
            //    opts.SerializerSettings.Converters.Add(new JObjectConverter());
            //});

            return builder;
        }

        public static IMvcCoreBuilder EnableJsonValueModelBinder(this IMvcCoreBuilder builder)
        {
            builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();

            //JsonConvert.DefaultSettings= (JsonConvert.DefaultSettings??(()=>new JsonSerializerSettings()));

            //JsonConvert.DefaultSettings().Converters.Add(new JObjectConverter());

            builder.AddMvcOptions(opt =>
            {
                opt.ModelBinderProviders.Insert(0, new JsonValueProviderFactory.JsonModelBinderProvider());
                opt.ValueProviderFactories.Insert(0, new JsonValueProviderFactory());

                //var jsonFormater = (JsonOutputFormatter)opt.OutputFormatters.FirstOrDefault(x => x is JsonOutputFormatter);

                //jsonFormater.PublicSerializerSettings.Converters.Add(new JObjectConverter());
                //jsonFormater..Converters.Add(new JObjectConverter());
                //opt.FormatterMappings.
                //opt.OutputFormatters.Add( new JsonOutputFormatter(null, null));
            });
            //    .AddJsonOptions((opts) =>
            //{
            //    opts.SerializerSettings.Converters.Add(new JObjectConverter());
            //});

            return builder;
        }
    }

    public class FromBodyJsonAttribute : Attribute
    {
        public FromBodyJsonAttribute(bool isCaseSensitive = true)
        {
            IsCaseSensitive = isCaseSensitive;
        }

        public bool IsCaseSensitive { get; }
    }
}
