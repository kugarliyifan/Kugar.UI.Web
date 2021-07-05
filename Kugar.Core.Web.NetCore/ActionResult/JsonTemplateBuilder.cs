using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fasterflect;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.ActionResult;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag;
using NSwag.Generation.AspNetCore;

namespace Kugar.Core.Web
{
    public interface IObjectBuilder<TModel>:IDisposable
    {
        IObjectBuilder<TModel> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
        );

        IObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifNullRender = null
        );

        IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null
        )  ;

        IObjectBuilder<TArrayElement> AddArrayValue<TArrayElement,TArray>(string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TArray> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TArray>, bool> ifNullRender = null
        ) where TArray:IEnumerable<TArrayElement>;

        IObjectBuilder<TModel> Start();

        IObjectBuilder<TModel> End();

        IList<PipeActionBuilder<TModel>> Pipe { get; }
    }

    public interface IArrayBuilder<TElement>:IDisposable
    {
        IArrayBuilder<TElement> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TElement>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Func<IJsonTemplateBuilderContext<TElement>, bool> ifCheckExp = null
        );

        IArrayBuilder<TElement> End();
    }
    
    internal class JsonTemplateObjectBuilder<TModel>:IObjectBuilder<TModel>
    {
        private List<PipeActionBuilder<TModel>> _pipe = new List<PipeActionBuilder<TModel>>();
 
        public JsonTemplateObjectBuilder(
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver
        )
        {
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }

        /// <summary>
        /// 添加单个属性
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="propertyName">属性名</param>
        /// <param name="valueFactory">获取值的函数</param>
        /// <param name="description">属性备注</param>
        /// <param name="isNull">是否允许为空</param>
        /// <param name="example">示例</param>
        /// <param name="ifCheckExp">运行时,检查是否要输出该属性</param>
        /// <returns></returns>
        public IObjectBuilder<TModel> AddProperty<TValue>(
            string propertyName,
            Func<IJsonTemplateBuilderContext<TModel>, TValue> valueFactory,
            string description = "",
            bool isNull = false,
            object example = null,
            Func<IJsonTemplateBuilderContext<TModel>, bool> ifCheckExp = null
            )
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName=SchemaBuilder.GetFormatPropertyName(propertyName);

            _pipe.Add(async (writer, context) =>
            {
                if (!(ifCheckExp?.Invoke(context) ?? true))
                {
                    return;
                }

                await writer.WritePropertyNameAsync(propertyName,context.CancellationToken);

                var value = valueFactory(context);

                if (value != null)
                {
                    await writer.WriteValueAsync(value,context.CancellationToken);
                }
                else
                {
                    await writer.WriteNullAsync(context.CancellationToken);
                }

            });

            SchemaBuilder.AddSingleProperty(propertyName, SchemaBuilder.NetTypeToJsonObjectType(typeof(TValue)),
                description, example, isNull);

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TChildModel"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="isNull"></param>
        /// <param name="description"></param>
        /// <param name="ifNullRender">如果值为null,是否继续调用输出,为true时,继续调用各种参数回调,,为false时,直接输出null</param>
        /// <returns></returns>
        public IObjectBuilder<TChildModel> AddObject<TChildModel>(string propertyName ,
            Func<IJsonTemplateBuilderContext<TModel>, TChildModel> valueFactory,
            bool isNull = false,
            string description = "",
            //Func<IJsonTemplateBuilderContext<TChildModel>, bool> ifCheckExp = null,
            Func<IJsonTemplateBuilderContext<TChildModel>,bool> ifNullRender=null
            )
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName=SchemaBuilder.GetFormatPropertyName(propertyName);

            //SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            _pipe.Add(async (writer, context) =>
            {
                await writer.WritePropertyNameAsync(propertyName, context.CancellationToken);
            });

            var childSchemeBuilder = SchemaBuilder.AddObjectProperty(propertyName, description, isNull);

            return new ChildJsonTemplateObjectBuilder<TModel,TChildModel>(this,
                valueFactory,
                childSchemeBuilder,
                Generator,
                Resolver,
                ifNullRender).Start();
        }

        public IArrayBuilder<TArrayElement> AddArrayObject<TArrayElement>(
            string propertyName, 
            Func<IJsonTemplateBuilderContext<TModel>, IEnumerable<TArrayElement>> valueFactory, 
            bool isNull = false,
            string description = "", 
            Func<IJsonTemplateBuilderContext<IEnumerable<TArrayElement>>, bool> ifNullRender = null)
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                _pipe.Add(async (writer, model) =>
                {
                    await writer.WritePropertyNameAsync(propertyName,model.CancellationToken);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = SchemaBuilder.AddObjectArrayProperty(propertyName,desciption:description,nullable:isNull);

            var s = new ArrayObjectTemplateObjectBuilder<TModel, TArrayElement>(this,valueFactory,s1,Generator,Resolver);
             
            return s;
        }

        public IObjectBuilder<TArrayElement> AddArrayValue<TArrayElement, TArray>(string propertyName, Func<IJsonTemplateBuilderContext<TModel>, TArray> valueFactory, bool isNull = false,
            string description = "", Func<IJsonTemplateBuilderContext<TArray>, bool> ifNullRender = null) where TArray : IEnumerable<TArrayElement>
        {
            throw new NotImplementedException();
        }

        public virtual IObjectBuilder<TModel> Start()
        {
            _pipe.Add(async (writer, context) =>
            {
                await writer.WriteStartObjectAsync(context.CancellationToken);
            });

            return this;
        }

        public virtual IObjectBuilder<TModel> End()
        {
            _pipe.Add(async (writer, context) =>
            {
                await writer.WriteEndObjectAsync(context.CancellationToken);
            });

            return this;
        }

        public virtual IList<PipeActionBuilder<TModel>> Pipe => _pipe;
        

        protected internal NSwagSchemeBuilder SchemaBuilder {  get; set; }

        protected JsonSchemaGenerator Generator { get; set; }

        protected JsonSchemaResolver Resolver {  get; set; }

        public void Dispose()
        {
            End();
        }
    }

    internal class ChildJsonTemplateObjectBuilder<TParentModel, TModel> : JsonTemplateObjectBuilder<TModel>
    {
        private JsonTemplateObjectBuilder<TParentModel> _parent;
        private Func<IJsonTemplateBuilderContext<TParentModel>, TModel> _childObjFactory;
        private Func<IJsonTemplateBuilderContext<TModel>, bool> _ifNullRender = null;


        public ChildJsonTemplateObjectBuilder(JsonTemplateObjectBuilder<TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TParentModel>,TModel> childObjFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver,
            Func<IJsonTemplateBuilderContext<TModel>,bool> ifNullRender=null
            )
            :base(schemeBuilder, generator, resolver)
        {
            _parent = parent;
            _childObjFactory = childObjFactory;
            _ifNullRender = ifNullRender;

            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }

        public override IObjectBuilder<TModel> End()
        {
            base.End();

            _parent.Pipe.Add(async (writer, context) =>
            {
                var value = _childObjFactory(context);

                var newContext = new JsonTemplateBuilderContext<TModel>(context.HttpContext, value);

                if (value == null)
                {
                    if (_ifNullRender(newContext))
                    {
                        foreach (var builder in Pipe)
                        {
                            builder.Invoke(writer, newContext);
                        }
                    }
                    else
                    {
                        await writer.WriteNullAsync(context.CancellationToken);
                    }
                }
                else
                {
                    foreach (var builder in Pipe)
                    {
                        builder.Invoke(writer, newContext);
                    }
                }
            });

            return this;
        }
    }

    internal class ArrayObjectTemplateObjectBuilder<TParentModel, TElementModel>:IArrayBuilder<TElementModel>
    {
        private List<PipeActionBuilder<TElementModel>> _pipe = new List<PipeActionBuilder<TElementModel>>();
        private Func<IJsonTemplateBuilderContext<TParentModel>, IEnumerable<TElementModel>> _arrayValueFactory = null;
        private JsonTemplateObjectBuilder<TParentModel> _parent = null;

        public ArrayObjectTemplateObjectBuilder(
            JsonTemplateObjectBuilder<TParentModel> parent,
            Func<IJsonTemplateBuilderContext<TParentModel>,IEnumerable<TElementModel>> arrayValueFactory,
            NSwagSchemeBuilder schemeBuilder,
            JsonSchemaGenerator generator,
            JsonSchemaResolver resolver
        )
        {
            _arrayValueFactory = arrayValueFactory;
            _parent = parent;
            this.SchemaBuilder = schemeBuilder;
            this.Generator = generator;
            this.Resolver = resolver;
        }

        public void Dispose()
        {
            this.End();
        }

        public IArrayBuilder<TElementModel> AddProperty<TValue>(string propertyName, 
            Func<IJsonTemplateBuilderContext<TElementModel>, TValue> valueFactory, 
            string description = "", 
            bool isNull = false,
            object example = null, 
            Func<IJsonTemplateBuilderContext<TElementModel>, bool> ifCheckExp = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            propertyName=SchemaBuilder.GetFormatPropertyName(propertyName);

            _pipe.Add(async (writer, context) =>
            {
                if (!(ifCheckExp?.Invoke(context) ?? true))
                {
                    return;
                }

                await writer.WritePropertyNameAsync(propertyName,context.CancellationToken);

                var value = valueFactory(context);

                if (value != null)
                {
                    await writer.WriteValueAsync(value,context.CancellationToken);
                }
                else
                {
                    await writer.WriteNullAsync(context.CancellationToken);
                }

            });

            SchemaBuilder.AddSingleProperty(propertyName, SchemaBuilder.NetTypeToJsonObjectType(typeof(TValue)),
                description, example, isNull);

            return this;
        }
        
        public IArrayBuilder<TElementModel> End()
        {
            _parent.Pipe.Add(async (writer, context) =>
            {
                await writer.WriteStartArrayAsync(context.CancellationToken);

                var array = _arrayValueFactory(new JsonTemplateBuilderContext<TParentModel>(context.HttpContext,context.Model));

                foreach (var element in array)
                {
                    await writer.WriteStartObjectAsync(context.CancellationToken);

                    var newContext = new JsonTemplateBuilderContext<TElementModel>(context.HttpContext, element);

                    foreach (var func in _pipe)
                    {
                        await func(writer, newContext);
                    }

                    await writer.WriteEndObjectAsync(context.CancellationToken);
                }

                await writer.WriteEndArrayAsync(context.CancellationToken);
            });

            return this;
        }

        public IList<PipeActionBuilder<TElementModel>> Pipe => _pipe;

        protected internal NSwagSchemeBuilder SchemaBuilder { get; set; }

        protected JsonSchemaGenerator Generator { get; set; }

        protected JsonSchemaResolver Resolver { get; set; }
    }

    public interface IJsonTemplateObject 
    { 
    }

    public interface IJsonTemplateActionResult : IActionResult
    {
        public object Model { set; get; }
    }

    public abstract class JsonTemplateObjectBase<TModel> :IJsonTemplateObject
    {
        public abstract void BuildScheme(IObjectBuilder<TModel> builder);
                
    }


    internal static class GlobalJsonTemplateCache
    {
        private static ConcurrentDictionary<string, object> _cache = new();
        public static IServiceProvider Provider { set; get; }



        public static IObjectBuilder<TModel> GetTemplate<TModel>(Type builderType)
            //where TBuilder : JsonTemplateObjectBase<TModel>, new()
        {
            var objectBuilder=(IObjectBuilder<TModel>)_cache.GetOrAdd($"{builderType.FullName}-{typeof(TModel).FullName}", (type) =>
            {
                return (IObjectBuilder<TModel>) typeof(GlobalJsonTemplateCache)
                    .GetMethod("Build", BindingFlags.Static & BindingFlags.Public)
                    .MakeGenericMethod(builderType, typeof(TModel))
                    .Invoke(null,null);
                // return Build<TBuilder, TModel>();
            });

            return objectBuilder;
        }

        public static IObjectBuilder<TModel> Build<TBuilder,TModel>(Type builderType,Type modelType)  where TBuilder : JsonTemplateObjectBase<TModel>, new()
        {
            var opt =
                (IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>)Provider.GetService(
                    typeof(IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>));

            var g = (JsonSchemaGenerator)Provider.GetService(typeof(JsonSchemaGenerator));

            //var register = (OpenApiDocumentRegistration)HttpContext.Current.RequestServices.GetService(typeof(OpenApiDocumentRegistration));

            //var opt1 = HttpContext.Current.Features.Get<IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>>();

            var document = new OpenApiDocument();
            //var settings = new AspNetCoreOpenApiDocumentGeneratorSettings();
            var schemaResolver = new OpenApiSchemaResolver(document, opt.Value);
            var generator = g ?? new JsonSchemaGenerator(opt.Value);
            
            DefaultContractResolver jsonResolver = null;

            var scheme = new JsonSchema();

#if NETCOREAPP3_0 || NETCOREAPP3_1
            var jsonOpt = (IOptionsSnapshot<MvcNewtonsoftJsonOptions>)Provider.GetService(typeof(IOptions<MvcNewtonsoftJsonOptions>));

            if (jsonOpt!=null &&jsonOpt.Value!=null)
            {
                jsonResolver = jsonOpt.Value.SerializerSettings.ContractResolver as DefaultContractResolver;
            }
            else
            {
                jsonResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
            }
#endif
#if NETCOREAPP2_1
            jsonResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
            //var _defaultSettings = JsonConvert.DefaultSettings?.Invoke();
#endif

            using var builder = new JsonTemplateObjectBuilder<TModel>(
                new NSwagSchemeBuilder(scheme.Properties, s =>jsonResolver?.NamingStrategy?.GetPropertyName(s, false)??s),
                generator,
                schemaResolver);

            var b = new TBuilder();

            builder.Start();
            b.BuildScheme(builder);
            builder.End();
            
            return builder;
        }
    }

    public static class JsonTemplateObjectExt
    {
        public static IActionResult JsonTemplate<TBuilder>(this ControllerBase controller,
            object value) where TBuilder : IJsonTemplateObject, new()
        {
            var actionResultType = typeof(JsonTemplateActionResult<>).MakeGenericType(value.GetType());

            var o =  (IJsonTemplateActionResult)actionResultType.CreateInstance(Flags.Public, new Type[] {typeof(Type)},typeof(TBuilder));

            //o.FastSetValue("Model",value);

            //actionResultType.Constructor(BindingFlags.Public, new Type[]{typeof(Type)})
            //    .Invoke()

            //var tmp= (IJsonTemplateActionResult) actionResultType.GetConstructor(new Type[]{typeof(Type)})
            //    .Invoke(new object[] {typeof(TBuilder)});

            o.Model = value;

            return o;
        }
        
    }

    public class NSwagSchemeBuilder
    {
        private IDictionary<string, JsonSchemaProperty> _properties = null;
        private Func<string, string> _getPropertyTitle = null;


        public NSwagSchemeBuilder(IDictionary<string, JsonSchemaProperty> properties,
            Func<string, string> getPropertyTitle)
        {
            //_propertyName = propertyName;
            _properties = properties;

            _getPropertyTitle = getPropertyTitle ?? throw new ArgumentNullException(nameof(getPropertyTitle));

        }

        public NSwagSchemeBuilder AddSingleProperty(string name, JsonObjectType type, string desciption,
            object example = null, bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            _properties.Add(realName, new JsonSchemaProperty()
            {
                Type = type,
                Description = desciption,
                Example = example,
                IsNullableRaw = nullable
            });

            return this;
        }

        public NSwagSchemeBuilder AddObjectProperty(string name, string desciption, bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Object;
            p.Description = desciption;
            p.IsNullableRaw = nullable;
            _properties.Add(realName, p);

            return new NSwagSchemeBuilder(p.Properties, _getPropertyTitle);
        }

        public NSwagSchemeBuilder AddObjectArrayProperty(string name, string desciption, bool nullable = false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Array;
            p.Description = desciption;
            p.Item = new JsonSchema();
            p.IsNullableRaw = nullable;
            _properties.Add(realName, p);


            return new NSwagSchemeBuilder(p.Item.Properties, _getPropertyTitle);
        }

        public JsonObjectType NetTypeToJsonObjectType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            if (type == typeof(int) ||
                type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(long) ||
                type == typeof(uint) ||
                type == typeof(ushort) ||
                type == typeof(ulong) ||
                type.IsEnum
            )
            {
                return JsonObjectType.Integer;
            }
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return JsonObjectType.Number;
            }
            else if (type == typeof(string))
            {
                return JsonObjectType.String;
            }
            else if (type == typeof(bool))
            {
                return JsonObjectType.Boolean;
            }
            else if (type.IsIEnumerable())
            {
                return JsonObjectType.Array;
            }
            else if (type == typeof(DateTime))
            {
                return JsonObjectType.String;
            }
            else
            {
                return JsonObjectType.Object;
            }
        }

        public string GetFormatPropertyName(string name)
        {
            return _getPropertyTitle?.Invoke(name) ?? name;
        }
    }

    internal class JsonTemplateActionResult<TModel> : IJsonTemplateActionResult //where TBuilder:JsonTemplateObjectBase<TModel>,new()
    {
        private Type _builderType = null;

        public JsonTemplateActionResult(Type builderType)
        {
            _builderType = builderType;
        }
        
        public object Model { get; set; }
        
        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "application/json";

            using (var textWriter = new StreamWriter(context.HttpContext.Response.Body))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                var objectBuilder=(IObjectBuilder<TModel>)GlobalJsonTemplateCache.GetTemplate<TModel>(_builderType);

                var model = (TModel) Model;

                var modelContext = new JsonTemplateBuilderContext<TModel>(context.HttpContext, model);

                foreach (var pipe in objectBuilder.Pipe)
                {
                    await pipe(jsonWriter,modelContext );
                }

                await jsonWriter.FlushAsync(context.HttpContext.RequestAborted);
            }

        }
    }

    public interface IJsonTemplateBuilderContext<out TModel>
    {
        /// <summary>
        /// 当前请求的HttpContext
        /// </summary>
        Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        /// <summary>
        /// 本次输出的临时数据
        /// </summary>
        Dictionary<string, object> TemporaryData { get; }

        /// <summary>
        /// 传入的Model数据
        /// </summary>
        TModel Model { get; }

        /// <summary>
        /// Request上的RequestAborted通知
        /// </summary>
        CancellationToken CancellationToken { get; }
    }

    internal class JsonTemplateBuilderContext<TModel> : IJsonTemplateBuilderContext<TModel>
    {
        private Lazy<Dictionary<string, object>> _temporaryData = new Lazy<Dictionary<string, object>>();

        public JsonTemplateBuilderContext(Microsoft.AspNetCore.Http.HttpContext context, TModel model)
        {
            HttpContext = context;
            Model = model;
            //CancellationToken = context.RequestAborted;
        }

        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get; }

        public Dictionary<string, object> TemporaryData => _temporaryData.Value;

        public TModel Model { get; }

        public CancellationToken CancellationToken => HttpContext.RequestAborted;
    }

    public delegate Task PipeActionBuilder<in TModel>(JsonWriter writer, IJsonTemplateBuilderContext<TModel> context);
}
