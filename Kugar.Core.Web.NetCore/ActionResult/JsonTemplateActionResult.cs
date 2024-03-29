﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Fasterflect;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

#if NETCOREAPP3_1 || NETCOREAPP3_0
using Microsoft.AspNetCore.Server.Kestrel.Core;
#endif


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


        public virtual void Build()
        {

        }
    }

    //public class StaticJsonTemplateActionResult<TModel> : IJsonTemplateActionResult
    //{
    //    public Task ExecuteResultAsync(ActionContext context)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public void GetNSwag(JsonSchemaGenerator generator, JsonSchemaResolver resolver, JsonObjectSchemeBuilder builder)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class StaticJsonBuilder<TModel>
    //{


    //    public void Build()
    //    {

    //    }

    //    public JsonSchemaGenerator SchemaGenerator { set; get; }

    //    public JsonSchemaResolver SchemaResolver { set; get; }

    //    public StaticJsonBuilder<TModel> AddProperty<TValue>(string propertyName, JsonObjectType type, string desciption, JsonValueFactory<TModel,TValue> valueFactory, object example = null,
    //        bool nullable = false)
    //    {
    //        return this;
    //    }

    //    public StaticJsonBuilder<TModel> AddProperty<TInput,TValue>(TInput model, Expression<Func<TInput, object>> propertyExpr,
    //        JsonValueFactory<TModel, TValue> valueFactory=null, object example = null,
    //        bool nullable = false)
    //    {
    //        Expression<JsonValueFactory<TModel, TValue>> bind = Expression.Lambda<>((x) => valueFactory(x));

    //        Expression expr = Expression.Invoke(bind, Expression.Constant(5));

    //        var s = new DynamicMethod("ss", typeof(void), new[] {typeof(JsonWriter), typeof(TModel)});

    //        s.

    //        return this;
    //    }

    //    public StaticJsonBuilder<TModel> AddObject(string propertyName)
    //    {
    //        return this;
    //    }

    //    public StaticJsonBuilder<TModel> AddArray(string propertyName)
    //    {
    //        return this;
    //    }
    //}

    //public delegate TValue JsonValueFactory<TModel, TValue>(TModel Model);

    public class JsonTemplateBuilder
    {
        internal DefaultContractResolver _resolver = null;
        internal JsonSerializerSettings _defaultSettings = null;

        public JsonTemplateBuilder(JsonWriter writer,Microsoft.AspNetCore.Http. HttpContext context)
        {
 
#if NETCOREAPP2_1
            _resolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
            _defaultSettings = JsonConvert.DefaultSettings?.Invoke();
#endif

#if NETCOREAPP3_0_OR_GREATER
            Debugger.Break();
            var opt = (IOptions<MvcNewtonsoftJsonOptions>)context.RequestServices.GetService(typeof(IOptions<MvcNewtonsoftJsonOptions>));

            _resolver = opt.Value.SerializerSettings.ContractResolver as DefaultContractResolver;

            _defaultSettings = opt.Value.SerializerSettings;
#endif



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
        private bool _autoBeginObject = true;

        public JsonObjectBuilder(JsonTemplateBuilder builder,bool autoBeginObject=true) : base(builder)
        {
            _autoBeginObject = autoBeginObject;

            if(_autoBeginObject)
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
                if (_jsonSettings.ContractResolver==null)
                {
                    _jsonSettings.ContractResolver=new DefaultContractResolver();
                }

                var item = (JsonObjectContract)_jsonSettings.ContractResolver.ResolveContract(value.GetType());
                var serializer = JsonSerializer.Create(_jsonSettings);

                foreach (var prop in item.Properties)
                {
                    if (excludePropertyNames.HasData() && excludePropertyNames.Contains(prop.PropertyName,StringComparer.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    WritePropertyName(prop.PropertyName);

                    serializer.Serialize(Writer,prop.ValueProvider.GetValue(value));
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

        public JsonObjectBuilder StartObject()
        {
            return new JsonObjectBuilder(Builder);
        }

        public JsonArrayBuilder StartArray(string propertyName)
        {
            WritePropertyName(propertyName);

            return new JsonArrayBuilder(Builder);
        }

        public JsonArrayBuilder StartArray()
        {
            return new JsonArrayBuilder(Builder);
        }

        public JsonObjectBuilderWithValue<T> With<T>(T value)
        {
            return new JsonObjectBuilderWithValue<T>(value, this);
        }

        public override void Dispose()
        {
            if (_autoBeginObject)
            {
                Writer.WriteEndObject();
            }
            
        }

        public class JsonObjectBuilderWithValue<T> : JsonObjectBuilder
        {
            private JsonObjectBuilder _parent = null;
            private T _value = default;

            public JsonObjectBuilderWithValue(T value, JsonObjectBuilder parent) : base(parent.Builder,false)
            {
                _parent = parent;
                _value = value;
            }

            /// <summary>
            /// 获取属性名,并且获取该属性成员的值
            /// </summary>
            /// <typeparam name="TValue"></typeparam>
            /// <param name="property">类型对应的属性</param>
            /// <returns></returns>
            public JsonObjectBuilderWithValue<T> WriteProperty<TValue>(Expression<Func<T, TValue>> property)
            {
                var body = property.Body as MemberExpression;

                if (body == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(property), "property表达式返回的必须是property或者field");
                }

                var value = property.Compile()(_value);

                WritePropertyName(body.Member.Name);
                Writer.WriteValue(value);

                return this;
            }

            /// <summary>
            /// 获取属性名,并使用newValue赋值
            /// </summary>
            /// <typeparam name="TValue"></typeparam>
            /// <param name="property">获取属性的注释,名称,以及类型</param>
            /// <param name="newValue">属性值</param>
            /// <returns></returns>
            public JsonObjectBuilderWithValue<T> WriteProperty<TValue, TNewValue>(Expression<Func<T, TValue>> property,TNewValue newValue)
            {
                var body = property.Body as MemberExpression;

                if (body == null)
                {
                    throw new ArgumentOutOfRangeException(nameof(property), "property表达式返回的必须是property或者field");
                }

                WritePropertyName(body.Member.Name);
                Writer.WriteValue(newValue);

                return this;
            }


            /// <summary>
            /// 添加多个属性值
            /// </summary>
            /// <typeparam name="TValue"></typeparam>
            /// <param name="properties"></param>
            /// <returns></returns>
            public JsonObjectBuilderWithValue<T> WriteProperty(params Expression<Func<T, object>>[] properties)
            {
                if (properties.HasData())
                {
                    foreach (var property in properties)
                    {
                        if (property.Body.NodeType == ExpressionType.Convert)
                        {
                            var p = property.Body as UnaryExpression;

                            if (p.Operand is MemberExpression m1)
                            {
                                WritePropertyName(m1.Member.Name);

                                var value = property.Compile()(_value);
                                Writer.WriteValue(value);
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException(nameof(properties), $"表达式{property.Body.ToString()}无效");
                            }

                        }
                        else if (property.Body is MemberExpression m)
                        {
                            WritePropertyName(m.Member.Name);

                            var value = property.Compile()(_value);
                            Writer.WriteValue(value);
       
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException(nameof(properties), $"表达式{property.Body.ToString()}无效");
                        }

                    }
                }

                return this;
            }

            public JsonObjectBuilder End()
            {
                return _parent;
            }

        }
    }
    

    public class CustomIgonePropertyResolverContact: DefaultContractResolver
    {
        private string[] _excludePropertyNames = null;
        private IContractResolver _resolver = null;

        public CustomIgonePropertyResolverContact(IContractResolver resolver, string[] excludePropertyNames)
        {
            if (resolver==null)
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
            var p= base.CreateProperty(member, memberSerialization);

            p.ShouldSerialize = (o) => _excludePropertyNames.Contains(p.PropertyName);

            return p;
        }

    }

    public class ResultReturnBuilder : JsonObjectBuilder
    {
        private ResultReturnDataType _type = ResultReturnDataType.Auto;

        public ResultReturnBuilder(JsonTemplateBuilder builder, ResultReturn result,ResultReturnDataType type= ResultReturnDataType.Value) : base(builder)
        {
            WriteProperty("Message", result.Message);
            WriteProperty("IsSuccess", result.IsSuccess);
            WriteProperty("ReturnCode", result.ReturnCode);
            WritePropertyName("ReturnData");

            _type = type;

            if (type== ResultReturnDataType.Auto)
            {
                var resultType=result.ReturnData.GetType();

                if (resultType.IsPrimitive || resultType==typeof(string))
                {
                    _type = ResultReturnDataType.Value;
                }
                else if (result.ReturnData is IEnumerable)
                {
                    _type = ResultReturnDataType.Array;
                }
                else
                {
                    _type = ResultReturnDataType.Object;
                }
            }

            switch (_type)
            {
                case ResultReturnDataType.Object:
                {
                    Writer.WriteStartObject();
                    break;
                }
                case ResultReturnDataType.Array:
                {
                    Writer.WriteStartArray();
                    break;
                }
            }

            //Writer.WriteStartObject();
        }

        public override void Dispose()
        {
            switch (_type)
            {
                case ResultReturnDataType.Object:
                {
                    Writer.WriteEndObject();
                    break;
                }
                case ResultReturnDataType.Array:
                {
                    Writer.WriteEndArray();
                    break;
                }
            }

            Writer.WriteEndObject();

            //base.Dispose();
        }
    }

    public enum ResultReturnDataType
    {
        Object=0,

        Array=1,

        Value=2,

        Auto=99

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
        public static ResultReturnBuilder StartResultReturn(this JsonBuilderBase builder, ResultReturn result, ResultReturnDataType returnDataType = ResultReturnDataType.Value)
        {
            return new ResultReturnBuilder(builder.Builder, result, returnDataType);
        }

        public static ResultReturnBuilder StartResultReturn(this JsonTemplateBuilder builder, ResultReturn result,ResultReturnDataType returnDataType= ResultReturnDataType.Value)
        {
            return new ResultReturnBuilder(builder, result, returnDataType);
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


        public JsonObjectSchemeBuilder(IDictionary<string, JsonSchemaProperty> properties, Func<string, string> getPropertyTitle)
        {
            if (getPropertyTitle == null)
            {
                throw new ArgumentNullException(nameof(getPropertyTitle));
            }

            //_propertyName = propertyName;
            _properties = properties;

             _getPropertyTitle = getPropertyTitle;


        }

        public JsonObjectSchemeBuilder AddProperty(string name, JsonObjectType type, string desciption, object example = null,bool nullable=false)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name),$"参数名:{realName}重复");
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

        public JsonObjectSchemeBuilder AddObjectProperty(string name, string desciption)
        {
            var realName = _getPropertyTitle(name);

            if (_properties.ContainsKey(realName))
            {
                throw new ArgumentOutOfRangeException(nameof(name), $"参数名:{realName}重复");
            }

            var p = new JsonSchemaProperty();
            p.Type = JsonObjectType.Object;
            p.Description = desciption;
            _properties.Add(realName, p);

            return new JsonObjectSchemeBuilder(p.Properties, _getPropertyTitle);
        }

        public JsonObjectSchemeBuilder AddArrayProperty(string name, string desciption)
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

            _properties.Add(realName, p);


            return new JsonObjectSchemeBuilder(p.Item.Properties, _getPropertyTitle);
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

        public JsonObjectSchemeBuilder AddType(Type target,params string[] excludePropertyNames)
        {
            var s = JsonSchema.FromType(target);

            foreach (var property in s.ActualProperties)
            {
                if (excludePropertyNames.HasData() && excludePropertyNames.Contains(property.Key,StringComparer.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                
                _properties.Add(_getPropertyTitle(property.Key), property.Value);
            }

            return this;
        }

        public JsonObjectSchemeBuilderWithType<T> With<T>()  
        {
            return new JsonObjectSchemeBuilderWithType<T>(this,_properties,_getPropertyTitle);
        }

        public void Dispose()
        {
            //foreach (var p in _properties)
            //{
            //    _properties.Add(p.Key, p.Value);
            //}
        }

        public IDictionary<string, JsonSchemaProperty> Properties
        {
            get => _properties;
            set
            {
                _properties = value;
            }
        }

        public JsonObjectType _typeToJsonObjectType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition()== typeof(Nullable<>))
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
            else if (type==typeof(double) || type==typeof(float) || type==typeof(decimal))
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


    public class JsonObjectSchemeBuilderWithType<T> : JsonObjectSchemeBuilder
    {
        private JsonObjectSchemeBuilder _parent = null;
        private Dictionary<string,string> _typeXmlDesc=new Dictionary<string, string>();

        public JsonObjectSchemeBuilderWithType(JsonObjectSchemeBuilder parent, IDictionary<string, JsonSchemaProperty> properties, Func<string, string> getPropertyTitle) : base(properties, getPropertyTitle)
        {
            _parent = parent;
            readXmlFile(typeof(T), _typeXmlDesc);
        }

        /// <summary>
        /// 获取属性名,并且使用该属性名对应的类型以及注释
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="property">获取属性的注释,名称,以及类型</param>
        /// <param name="newProperty">覆盖原属性名称</param>
        /// <param name="type">覆盖原属性数据类型</param>
        /// <returns></returns>
        public JsonObjectSchemeBuilderWithType<T> Property<TValue>(Expression<Func<T, TValue>> property, string newPropertyName = null, JsonObjectType? type = null,object example=null,bool nullable = false,string desciption = "")
        {
            var body = getMemberExpr(property);

            if (body==null)
            {
                throw new ArgumentOutOfRangeException(nameof(property),"property表达式返回的必须是property或者field");
            }

            return Property(body, newPropertyName, type, example, nullable, desciption);
        }

        /// <summary>
        /// 添加多个属性
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="properties"></param>
        /// <returns></returns>
        public JsonObjectSchemeBuilderWithType<T> Property(params Expression<Func<T, object>>[] properties)
        {
            if (properties.HasData())
            {
                foreach (var property in properties)
                {
                    if (property.Body.NodeType== ExpressionType.Convert)
                    {
                        var p = property.Body as UnaryExpression;

                        if (p.Operand is MemberExpression m1)
                        {
                            this.Property(m1);
                        }
                        else
                        {
                            throw new ArgumentOutOfRangeException(nameof(properties), $"表达式{property.Body.ToString()}无效");
                        }
                        
                    }
                    else if(property.Body is MemberExpression m)
                    {
                        this.Property(m);
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(properties),$"表达式{property.Body.ToString()}无效");
                    }

                    //var expr=property.NodeType== ExpressionType.Convert ?property as UnaryExpression
                    
                }
            }

            return this;
        }

        protected JsonObjectSchemeBuilderWithType<T> Property(MemberExpression property,
            string newProperty = null, JsonObjectType? type = null, object example = null, bool nullable = false, string desciption = "")
        {
            var name = string.IsNullOrWhiteSpace(newProperty) ? property.Member.Name : newProperty;

            JsonObjectType realtype;

            if (string.IsNullOrWhiteSpace(desciption))
            {
                desciption = _typeXmlDesc.TryGetValue( property.Member.Name, "");

                if (string.IsNullOrWhiteSpace(desciption))
                {
                    desciption = property.Member.GetCustomAttribute<DescriptionAttribute>()?.Description;
                }

                if (string.IsNullOrWhiteSpace(desciption))
                {
                    desciption = property.Member.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
                }
            }

            if (type == null)
            {
                if (property.Member is PropertyInfo p)
                {
                    realtype = _typeToJsonObjectType(p?.PropertyType);
                    nullable = p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                }
                else if (property.Member is FieldInfo f)
                {
                    realtype = _typeToJsonObjectType(f?.FieldType);
                    nullable = f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(property), "传入的必须是属性或者字段");
                }
            }
            else
            {
                realtype = type.Value;
            }

            _parent.AddProperty(name, realtype, desciption, example: example, nullable: nullable);

            return this;
        }

        public JsonObjectSchemeBuilder End()
        {
            return _parent;
        }

        private void readXmlFile(Type type, Dictionary<string,string> dic)
        {
            //var type = typeof(T);

            var xmlFilePath = Path.Join( Path.GetDirectoryName(type.Assembly.Location),Path.GetFileNameWithoutExtension(type.Assembly.Location)+".xml");

            if (!File.Exists(xmlFilePath))
            {
                return;
            }


            var xml=new XmlDocument();

            try
            {
                xml.Load(xmlFilePath);
            }
            catch (Exception e)
            {
                return;
            }

            var l1 = xml.GetElementsByTagName("member").AsEnumerable<XmlElement>();

            var lst= l1
                .Where(x => x.GetAttribute("name").StartsWith($"P:{type.FullName}"))
                .Select(x =>
                    new KeyValuePair<string, string>(
                        x.GetAttribute("name").Substring($"P:{type.FullName}".Length + 1).ToStringEx(),
                        x.GetElementsByTagName("summary").AsEnumerable<XmlElement>().FirstOrDefault()?.InnerText.ToStringEx()))
                .ToArrayEx();

            if (!lst.HasData())
            {
                return;
            }

            foreach (var item in lst)
            {
                if (dic.ContainsKey(item.Key))
                {
                    continue;
                }
                dic.Add(item.Key,item.Value.ToStringEx().Trim());
            }

            if (!type.IsInterface && type.BaseType!=null)
            {
                if (type.BaseType != null && type.BaseType != typeof(object))
                {
                    readXmlFile(type.BaseType, dic);
                }
            }

            if (type!=null)
            {
                foreach (var face in type.GetInterfaces())
                {
                    readXmlFile(face, dic);
                }
            }
            
        }

        protected MemberExpression getMemberExpr<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var p = expression.Body as UnaryExpression;

                if (p.Operand is MemberExpression m1)
                {
                    return m1;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
                }

            }
            else if (expression.Body is MemberExpression m)
            {
                return m;
            }

            throw new ArgumentException();
        }
    }

    public static class JsonTemplateHelper
    {
#if NETCOREAPP3_1 || NETCOREAPP3_0
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
       
#endif

        public static void UseJsonTemplate(this AspNetCoreOpenApiDocumentGeneratorSettings opt,params Assembly[] typeAssemblies)
        {
            var types = typeAssemblies.SelectMany(x=>x.GetTypes())
                .Where(x => x.IsImplementlInterface(typeof(IJsonTemplateActionResult)) && !x.IsAbstract &&
                            x.IsPublic)
                .ToArrayEx();

            
            foreach (var t in types)
            {
                opt.TypeMappers.Add(new ObjectTypeMapper(t, (gen, resolver) =>
                {
                    DefaultContractResolver contactResolver = null;

#if NETCOREAPP3_0_OR_GREATER
                    var options =
                        ((IOptions<MvcNewtonsoftJsonOptions>)HttpContext.Current.RequestServices.GetService(
                            typeof(IOptions<MvcNewtonsoftJsonOptions>)))?.Value?.SerializerSettings ?? JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

                    contactResolver = options?.ContractResolver as DefaultContractResolver;
                    
#endif
#if NETCOREAPP2_1
                    contactResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
                    
#endif

                    

                    Func<string, string> getName = (propertyName) =>
                    {
                        return contactResolver
                            ?.NamingStrategy?.GetPropertyName(propertyName, false) ?? propertyName;
                    };

                    var scheme = new JsonSchema();

                    var builder = new JsonObjectSchemeBuilder(scheme.Properties, getName);

                    var tmp = Activator.CreateInstance(t) as IJsonTemplateActionResult;

                    tmp.GetNSwag(gen,resolver,builder);

                    //Fasterflect.MethodExtensions.CallMethod(tmp, "GetNSwag", gen,
                    //    resolver, builder);

                    return scheme;

                }));
            }
        }
    }
}