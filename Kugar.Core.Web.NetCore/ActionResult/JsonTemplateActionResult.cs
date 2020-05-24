using System;
using System.Collections.Generic;
#if NETCOREAPP3_0 || NETCOREAPP3_1

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Fasterflect;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.Infrastructure;
using NSwag.Generation.AspNetCore;



namespace Kugar.Core.Web.ActionResult
{

    public interface IJsonTemplateActionResult : IActionResult
    {
        void GetNSwag(JsonSchemaGenerator generator, JsonSchemaResolver resolver, JsonObjectSchemeBuilder builder);

        void BuildJson(JsonTemplateBuilder writer);
    }

    public abstract class JsonTemplateActionResult<TModel> : IJsonTemplateActionResult
    {
        //private TModel _model = default;
        //private static bool _isInit = false;

        protected JsonTemplateActionResult()
        {
            // _model = model;
        }


        public TModel Model { set; get; }


        public abstract void BuildJson(JsonTemplateBuilder writer);


        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.ContentType = "application/json";

            using (var textWriter = new StreamWriter(context.HttpContext.Response.Body))
            using (var jsonWriter = new JsonTextWriter(textWriter))
            {
                BuildJson(new JsonTemplateBuilder(jsonWriter, context.HttpContext));

                await jsonWriter.FlushAsync();

                //await context.HttpContext.Response.Body.WriteAsync(memory.GetBuffer(), 0, (int)memory.Length);
            }

        }

        public virtual void GetNSwag(JsonSchemaGenerator generator, JsonSchemaResolver resolver, JsonObjectSchemeBuilder builder)
        {

        }
    }

    public class JsonTemplateBuilder
    {
        internal DefaultContractResolver _resolver = null;
        internal JsonSerializerSettings _defaultSettings = null;

        public JsonTemplateBuilder(JsonWriter writer, Microsoft.AspNetCore.Http.HttpContext context)
        {
            var opt = (IOptionsSnapshot<MvcNewtonsoftJsonOptions>)context.RequestServices.GetService(typeof(IOptions<MvcNewtonsoftJsonOptions>));

            _resolver = opt.Value.SerializerSettings.ContractResolver as DefaultContractResolver;

            _defaultSettings = opt.Value.SerializerSettings;

            Writer = writer;

            //_contractResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;

        }

        public JsonWriter Writer { get; }

        public JsonObjectBuilder StartObject()
        {
            return new JsonObjectBuilder(this);
        }

        public JsonArrayBuilder StartArray()
        {
            return new JsonArrayBuilder(this);
        }

    }


    public abstract class JsonBuilderBase : IDisposable
    {
        protected DefaultContractResolver _contractResolver = null;
        protected JsonSerializerSettings _jsonSettings = null;

        protected JsonBuilderBase(JsonTemplateBuilder builder)
        {
            Writer = builder.Writer;
            Builder = builder;
            _contractResolver = builder._resolver;
            _jsonSettings = builder._defaultSettings;
        }

        public JsonWriter Writer { get; }

        public JsonTemplateBuilder Builder { get; }

        protected string GetResolvedPropertyName(string propertyName)
        {
            return _contractResolver?.GetResolvedPropertyName(propertyName) ?? propertyName;
        }

        public abstract void Dispose();
    }

    public class JsonArrayBuilder : JsonBuilderBase
    {
        public JsonArrayBuilder(JsonTemplateBuilder builder) : base(builder)
        {
            Writer.WriteStartArray();
        }

        public JsonObjectBuilder StartObject()
        {
            return new JsonObjectBuilder(Builder);
        }

        public override void Dispose()
        {
            Writer.WriteEndArray();
        }
    }

    public class JsonObjectBuilder : JsonBuilderBase
    {
        private Lazy<JsonSerializer> _serializer = new Lazy<JsonSerializer>();

        public JsonObjectBuilder(JsonTemplateBuilder builder) : base(builder)
        {
            Writer.WriteStartObject();
        }

        public JsonObjectBuilder WriteProperty(string propertyName, string value)
        {
            Writer.WriteProperty(GetResolvedPropertyName(propertyName), value);

            return this;
        }

        public JsonObjectBuilder WriteProperty(string propertyName, int value)
        {
            Writer.WriteProperty(GetResolvedPropertyName(propertyName), value);

            return this;
        }

        public JsonObjectBuilder WriteProperty(string propertyName, decimal value)
        {
            Writer.WriteProperty(GetResolvedPropertyName(propertyName), value);

            return this;
        }

        public JsonObjectBuilder WriteProperty(string propertyName, byte value)
        {
            Writer.WriteProperty(GetResolvedPropertyName(propertyName), value);

            return this;
        }

        public JsonObjectBuilder WriteProperty(string propertyName, bool value)
        {
            Writer.WriteProperty(GetResolvedPropertyName(propertyName), value);

            return this;
        }

        public JsonObjectBuilder WriteProperty(string propertyName, object value, params string[] excludePropertyNames)
        {

            Writer.WritePropertyName(GetResolvedPropertyName(propertyName));

            if (excludePropertyNames.HasData())
            {
                var oldValue = _jsonSettings.ContractResolver;

                _jsonSettings.ContractResolver =
                    new CustomIgonePropertyResolverContact(_jsonSettings.ContractResolver, excludePropertyNames);

                JsonSerializer.Create(_jsonSettings).Serialize(Writer, value);

                _jsonSettings.ContractResolver = oldValue;
            }
            else
            {
                _serializer.Value.Serialize(Writer, value);
            }


            return this;
        }

        public JsonObjectBuilder WriteProperty(object value, params string[] excludePropertyNames)
        {
            if (excludePropertyNames.HasData())
            {
                if (_jsonSettings.ContractResolver == null)
                {
                    _jsonSettings.ContractResolver = new DefaultContractResolver();
                }

                var item = (JsonObjectContract)_jsonSettings.ContractResolver.ResolveContract(value.GetType());
                var serializer = JsonSerializer.Create(_jsonSettings);

                foreach (var prop in item.Properties)
                {
                    if (excludePropertyNames.HasData() && excludePropertyNames.Contains(prop.PropertyName))
                    {
                        continue;
                    }

                    WritePropertyName(prop.PropertyName);

                    serializer.Serialize(Writer, prop.ValueProvider.GetValue(value));
                }
            }
            else
            {
                _serializer.Value.Serialize(Writer, value);
            }


            return this;
        }

        public JsonObjectBuilder WriteProperty(string propertyName, IEnumerable<object> value)
        {
            Writer.WritePropertyName(GetResolvedPropertyName(propertyName));

            Writer.WriteStartArray();

            if (value != null)
            {
                foreach (var o in value)
                {
                    _serializer.Value.Serialize(Writer, o);
                }
            }

            Writer.WriteEndArray();

            return this;
        }

        public JsonObjectBuilder WritePropertyName(string propertyName)
        {
            Writer.WritePropertyName(GetResolvedPropertyName(propertyName));

            return this;
        }

        public JsonObjectBuilder StartObject(string propertyName)
        {
            WritePropertyName(propertyName);

            return new JsonObjectBuilder(Builder);
        }

        public JsonArrayBuilder StartArray(string propertyName)
        {
            WritePropertyName(propertyName);

            return new JsonArrayBuilder(Builder);
        }

        public override void Dispose()
        {
            Writer.WriteEndObject();
        }
    }


    public class CustomIgonePropertyResolverContact : DefaultContractResolver
    {
        private string[] _excludePropertyNames = null;
        private IContractResolver _resolver = null;

        public CustomIgonePropertyResolverContact(IContractResolver resolver, string[] excludePropertyNames)
        {
            if (resolver == null)
            {
                resolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver;
            }

            _resolver = resolver;
            _excludePropertyNames = excludePropertyNames;
        }

        public override JsonContract ResolveContract(Type type)
        {
            return _resolver.ResolveContract(type);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var p = base.CreateProperty(member, memberSerialization);

            p.ShouldSerialize = (o) => _excludePropertyNames.Contains(p.PropertyName);

            return p;
        }

    }

    public class ResultReturnBuilder : JsonObjectBuilder
    {
        public ResultReturnBuilder(JsonTemplateBuilder builder, ResultReturn result) : base(builder)
        {
            WriteProperty("Message", result.Message);
            WriteProperty("IsSuccess", result.IsSuccess);
            WriteProperty("ReturnCode", result.ReturnCode);
            WritePropertyName("ReturnData");

            //Writer.WriteStartObject();
        }

        public override void Dispose()
        {
            Writer.WriteEndObject();

            //base.Dispose();
        }
    }

    public class PagedListBuilder : JsonObjectBuilder
    {
        public PagedListBuilder(JsonTemplateBuilder builder, IPagedInfo result) : base(builder)
        {
            WriteProperty("PageCount", result.PageCount);
            WriteProperty("PageSize", result.PageSize);
            WriteProperty("TotalCount", result.TotalCount);
            WriteProperty("PageIndex", result.PageIndex);
            WritePropertyName("Data");

            Writer.WriteStartArray();
        }

        public override void Dispose()
        {
            Writer.WriteEndArray();

            Writer.WriteEndObject();
        }
    }

    public static class JsonBuilderExt
    {
        public static ResultReturnBuilder StartResultReturn(this JsonBuilderBase builder, ResultReturn result)
        {
            return new ResultReturnBuilder(builder.Builder, result);
        }

        public static ResultReturnBuilder StartResultReturn(this JsonTemplateBuilder builder, ResultReturn result)
        {
            return new ResultReturnBuilder(builder, result);
        }

        public static PagedListBuilder StartIPagedList(this JsonBuilderBase builder, IPagedInfo result)
        {
            return new PagedListBuilder(builder.Builder, result);
        }

        public static PagedListBuilder StartIPagedList(this JsonTemplateBuilder builder, IPagedInfo result)
        {
            return new PagedListBuilder(builder, result);
        }
    }

    public class JsonObjectSchemeBuilder : IDisposable
    {
        //private JsonSchema _jsonSchema = new JsonSchema();
        private IDictionary<string, JsonSchemaProperty> _properties = null;
        private Func<string, string> _getPropertyTitle = null;
        private JsonSchemaGenerator _generator = null;
        private JsonSchemaResolver _schemaResolver = null;


        public JsonObjectSchemeBuilder(JsonSchemaGenerator generator, JsonSchemaResolver resolver, IDictionary<string, JsonSchemaProperty> properties, Func<string, string> getPropertyTitle)
        {
            if (getPropertyTitle == null)
            {
                throw new ArgumentNullException("getPropertyTitle");
            }

            //_propertyName = propertyName;
            _properties = properties;

            _getPropertyTitle = getPropertyTitle;

            _generator = generator;
            _schemaResolver = resolver;
        }

        public JsonSchemaGenerator Generator => _generator;

        public JsonSchemaResolver Resolver => _schemaResolver;

        public JsonObjectSchemeBuilder AddProperty(string name, JsonObjectType type, string desciption, object example = null)
        {
            _properties.Add(_getPropertyTitle(name), new JsonSchemaProperty()
            {
                Type = type,
                Description = desciption,
                Example = example,
            });

            return this;
        }

        public JsonObjectSchemeBuilder AddObjectProperty(string name, string desciption)
        {
            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Object;
            p.Description = desciption;
            _properties.Add(_getPropertyTitle(name), p);

            return new JsonObjectSchemeBuilder(_generator, _schemaResolver, p.Properties, _getPropertyTitle);
        }

        public JsonObjectSchemeBuilder AddArrayProperty(string name, string desciption)
        {
            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Array;
            p.Description = desciption;
            p.Item = new JsonSchema();

            _properties.Add(_getPropertyTitle(name), p);


            return new JsonObjectSchemeBuilder(_generator, _schemaResolver, p.Item.Properties, _getPropertyTitle);
        }

        public JsonObjectSchemeBuilder ExcludeProperty(params string[] names)
        {
            foreach (var s in names)
            {
                _properties.Remove(s);
            }

            return this;
        }

        public JsonObjectSchemeBuilder AddResultReturnWithObject()
        {
            return AddProperty("isSuccess", JsonObjectType.Boolean, "是否成功", true)
                .AddProperty("returnCode", JsonObjectType.Integer, "返回码", 0)
                .AddProperty("message", JsonObjectType.String, "返回的消息内容呢", "")
                .AddObjectProperty("returnData", "返回的实际内容对象")
                ;
        }

        public JsonObjectSchemeBuilder AddResultReturnWithArray()
        {
            return AddProperty("isSuccess", JsonObjectType.Boolean, "是否成功", true)
                    .AddProperty("returnCode", JsonObjectType.Integer, "返回码", 0)
                    .AddProperty("message", JsonObjectType.String, "返回的消息内容呢", "")
                    .AddArrayProperty("returnData", "返回的实际内容数组")
                ;
        }

        public JsonObjectSchemeBuilder AddIPagedList()
        {
            return AddProperty("pageIndex", JsonObjectType.Integer, "页码", 1)
                .AddProperty("pageSize", JsonObjectType.Integer, "每页大小", 20)
                .AddProperty("pageCount", JsonObjectType.Integer, "分页总数", 20)
                .AddProperty("totalCount", JsonObjectType.Integer, "总记录数", 1000)
                .AddArrayProperty("data", "分页后的内容");
        }

        public JsonObjectSchemeBuilder AddType(Type target, params string[] excludePropertyNames)
        {
            var s = JsonSchema.FromType(target);

            foreach (var property in s.ActualProperties)
            {
                if (excludePropertyNames.HasData() && excludePropertyNames.Contains(property.Key))
                {
                    continue;
                }


                _properties.Add(_getPropertyTitle(property.Key), property.Value);
            }

            return this;
        }

        public JsonObjectSchemeBuilder AddProperty<T>(string propertyName)
        {
            _properties.Add(_getPropertyTitle(propertyName), Generator.Generate(typeof(T)).ActualProperties[propertyName]);

            return this;
        }

        public JsonObjectSchemeBuilder AddProperty<T>(Expression<Func<T>> property)
        {
            var propertyName = (property.Body as MemberExpression).Member.Name;

            _properties.Add(_getPropertyTitle(propertyName), Generator.Generate(typeof(T)).ActualProperties[propertyName]);

            return this;
        }

        public void Dispose()
        {
            //foreach (var p in _properties)
            //{
            //    _properties.Add(p.Key, p.Value);
            //}
        }
    }

    public static class JsonTemplateHelper
    {
        public static IServiceCollection EnableSyncIO(this IServiceCollection services)
        {
            // If using IIS:
            services.Configure<IISServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            return services;
        }

        public static void UseJsonTemplate(this AspNetCoreOpenApiDocumentGeneratorSettings opt)
        {
            var types =AppDomain.CurrentDomain.GetAssemblies().SelectMany(x=>x.GetTypes())
                .Where(x => x.IsImplementlInterface(typeof(IJsonTemplateActionResult)) && !x.IsAbstract &&
                            x.IsPublic && x.GetMethod("GetNSwag")?.IsInstance() == true)
                .ToArrayEx();


            foreach (var t in types)
            {
                opt.TypeMappers.Add(new ObjectTypeMapper(t, (gen, resolver) =>
                {
                    var options =
                        (IOptions<MvcNewtonsoftJsonOptions>)Kugar.Core.Web.HttpContext.Current.RequestServices.GetService(
                            typeof(IOptions<MvcNewtonsoftJsonOptions>));

                    Func<string, string> getName = (propertyName) =>
                    {
                        return (options.Value.SerializerSettings.ContractResolver as DefaultContractResolver)
                            ?.NamingStrategy?.GetPropertyName(propertyName, false) ?? propertyName;
                    };

                    var scheme = new JsonSchema();

                    var builder = new JsonObjectSchemeBuilder(gen, resolver, scheme.Properties, getName);

                    var tmp = Activator.CreateInstance(t);

                    Fasterflect.MethodExtensions.CallMethod(tmp, "GetNSwag", gen,
                        resolver, builder);

                    return scheme;

                }));
            }
        }
    }
}

#endif
