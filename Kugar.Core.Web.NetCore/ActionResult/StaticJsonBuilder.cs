using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Annotations;
using NJsonSchema.Generation;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.AspNetCore;

namespace Kugar.Core.Web.ActionResult
{
    public abstract class StaticJsonBuilderBase
    {
        protected string[] getPropNames<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            var newExp = expression.Body as NewExpression;
            if (newExp == null)
            {
                throw new ArgumentException();
            }

            var props = new List<string>(newExp.Arguments.Count);
            foreach (var argExp in newExp.Arguments)
            {
                var memberExp = argExp as MemberExpression;
                if (memberExp == null)
                {
                    throw new ArgumentException();
                }
                props.Add(memberExp.Member.Name);
            }
            return props.ToArray();
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

        protected string getPropName<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var p = expression.Body as UnaryExpression;

                if (p.Operand is MemberExpression m1)
                {
                    return m1.Member.Name;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
                }

            }
            else if (expression.Body is MemberExpression m)
            {
                return m.Member.Name;
            }

            throw new ArgumentException();
        }

        protected Type getExprReturnType<T1, T2>(Expression<Func<T1, T2>> expression)
        {
            if (expression.Body.NodeType == ExpressionType.Convert)
            {
                var p = expression.Body as UnaryExpression;

                if (p.Operand is MemberExpression m1)
                {
                    if (m1.Member is PropertyInfo p1)
                    {
                        return p1.PropertyType;
                    }
                    else if (m1.Member is FieldInfo f1)
                    {
                        return f1.FieldType;
                    }
                    else if (m1.Member is MethodInfo m2)
                    {
                        return m2.ReturnType;
                    }
                }
                else if (p.Operand.NodeType == ExpressionType.Invoke)
                {
                    return p.Operand.Type;
                }

                throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
            }
            else if (expression.Body is MemberExpression m)
            {
                if (m.Member is PropertyInfo p1)
                {
                    return p1.PropertyType;
                }
                else if (m.Member is FieldInfo f1)
                {
                    return f1.FieldType;
                }
                else if (m.Member is MethodInfo m2)
                {
                    return m2.ReturnType;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(expression), $"表达式{expression.Body.ToString()}无效");
        }

        protected bool isNullable(Type type)
        {
            return false;
        }

    }

    public delegate Task PipeAction<in TModel>(JsonWriter writer, TModel model);

    public class JsonSchemaObjectBuilder<TModel> : StaticJsonBuilderBase, IDisposable
    {
        private static ConcurrentDictionary<Type, List<PipeAction<TModel>>> _cacheActionList = new ConcurrentDictionary<Type, List<PipeAction<TModel>>>();
        private JsonObjectSchemeBuilder _parentSchemaBulder = null;
        private List<PipeAction<TModel>> _currentList = null;
        private bool _hasStart = false;

        public JsonSchemaObjectBuilder(List<PipeAction<TModel>> lst, JsonObjectSchemeBuilder schemaBuilder)
        {
            _currentList = lst;
            _parentSchemaBulder = schemaBuilder;
        }


        //public JsonSchemaObjectBuilder()
        //{
        //    _lst = getJsonActionsList(this.GetType());
        //}

        internal JsonObjectSchemeBuilder SchemaBuilder
        {
            set
            {
                _parentSchemaBulder = value;
            }
            get
            {
                return _parentSchemaBulder;
            }
        }

        internal List<PipeAction<TModel>> ActionList
        {
            get
            {
                return _currentList ?? _cacheActionList.GetOrAdd(this.GetType(), x =>
                  {
                      return new List<PipeAction<TModel>>();
                  });
            }
            //set
            //{
            //    _lst = value;
            //}
        }

        protected JsonSchemaGenerator Generator { set; get; }

        protected JsonSchemaResolver Resolver { set; get; }


        public virtual JsonSchemaObjectBuilder<TModel> Start()
        {
            ActionList.Add(async (writer, model) =>
            {
                await writer.WriteStartObjectAsync();
                
                //if (model!=null)
                //{
                //    await writer.WriteStartObjectAsync();
                //}
                //else
                //{
                //    await writer.WriteNullAsync();
                //    await writer.WriteEndObjectAsync();
                //}
                
            });

            _hasStart = true;

            return this;
        }

        /// <summary>
        /// 添加单个属性
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="valueFactory"></param>
        /// <param name="desciption"></param>
        /// <param name="example"></param>
        /// <param name="nullable"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddProperty<TValue>(string propertyName, Func<TModel, TValue> valueFactory, string desciption = "",/* JsonObjectType type,*/  object example = null,
            bool? nullable = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory != null);

            //var valueFunc = valueFactory.Compile();

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            PipeAction<TModel> s = async (writer, model) =>
             {
                 await writer.WritePropertyNameAsync(propertyName);

                 var value = valueFactory(model);

                 if (value != null)
                 {
                     await writer.WriteValueAsync(value);
                 }
                 else
                 {
                     await writer.WriteNullAsync();
                 }
             };


            _parentSchemaBulder.AddProperty(propertyName, _parentSchemaBulder._typeToJsonObjectType(typeof(TValue)), desciption,
                example: example, nullable ?? isNullable(typeof(TValue)));

            ActionList.Add(s);

            return this;
        }

        /// <summary>
        /// 从inputValueFactory 获取一个对象,然后从这个对象中,添加多个字段
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="inputValueFactory"></param>
        /// <param name="propertyExpr"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddPropertyFrom<TInput>(Expression<Func<TModel, TInput>> inputValueFactory,
           params Expression<Func<TInput, object>>[] propertyExpr)
        {
            if (!propertyExpr.HasData())
            {
                return this;
            }

            Debug.Assert(inputValueFactory != null);


            var funcList = new List<(string properyName, Func<TInput, object> valueCaller)>();

            var typeBuilder = _parentSchemaBulder.With<TInput>();

            foreach (var item in propertyExpr)
            {
                var caller = item;

                var propertyName = SchemaBuilder.GetFormatPropertyName(getPropName(caller));

                var callerReturnType = getExprReturnType(item);

                var t = item.Compile();

                typeBuilder.Property(item, type: _parentSchemaBulder._typeToJsonObjectType(callerReturnType), nullable: isNullable(callerReturnType));

                funcList.Add((propertyName, t));
            }

            typeBuilder.End();

            var inputValueFunc = inputValueFactory.Compile();

            PipeAction<TModel> s = async (writer, model) =>
             {
                 var inputValue = inputValueFunc(model);

                 if (inputValue == null)
                 {
                     foreach (var item in funcList)
                     {
                         await writer.WritePropertyNameAsync(SchemaBuilder.GetFormatPropertyName(item.properyName));

                         var v = item.valueCaller(inputValue);

                         await writer.WriteValueAsync(v);
                     }
                 }
                 else
                 {
                     await writer.WriteNullAsync();
                 }


             };

            ActionList.Add(s);

            return this;
        }

        /// <summary>
        /// 添加单个字段
        /// </summary>
        /// <param name="propertyExpr"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddProperty(params Expression<Func<TModel, object>>[] propertyExpr)
        {
            if (!propertyExpr.HasData())
            {
                return this;
            }

            var funcList = new List<(string propertyName, Func<TModel, object> valueCaller)>();


            var typeBuilder = _parentSchemaBulder.With<TModel>();

            foreach (var item in propertyExpr)
            {
                var caller = item;

                var propertyName = SchemaBuilder.GetFormatPropertyName(getPropName(caller));

                //var memberExpr = getMemberExpr(caller);

                var callerReturnType = getExprReturnType(caller);

                var t = caller.Compile();

                typeBuilder.Property(caller, type: _parentSchemaBulder._typeToJsonObjectType(callerReturnType), nullable: isNullable(callerReturnType));

                funcList.Add((propertyName, t));
            }

            typeBuilder.End();

            PipeAction<TModel> s = async (writer, model) =>
            {
                if (model != null)
                {
                    foreach (var item in funcList)
                    {
                        await writer.WritePropertyNameAsync(SchemaBuilder.GetFormatPropertyName(item.propertyName));

                        var v = item.valueCaller(model);


                        await writer.WriteValueAsync(v);
                    }
                }
                else
                {
                    await writer.WriteNullAsync();
                }


            };

            ActionList.Add(s);

            return this;
        }

        /// <summary>
        /// 添加单个字段
        /// </summary>
        /// <param name="propertyExpr"></param>
        /// <param name="desciption"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddProperty(Expression<Func<TModel, object>> propertyExpr, string desciption)
        {

            //var funcList = new List<(string propertyName, Func<TModel, object> valueCaller)>();


            var typeBuilder = _parentSchemaBulder.With<TModel>();


            var caller = propertyExpr;

            var propertyName = SchemaBuilder.GetFormatPropertyName(getPropName(caller));

            //var memberExpr = getMemberExpr(caller);

            var callerReturnType = getExprReturnType(caller);

            var valueFunc = caller.Compile();

            typeBuilder.Property(caller, type: _parentSchemaBulder._typeToJsonObjectType(callerReturnType), desciption: desciption, nullable: isNullable(callerReturnType));


            typeBuilder.End();

            PipeAction<TModel> s = async (writer, model) =>
            {
                //foreach (var item in funcList)
                //{
                await writer.WritePropertyNameAsync(SchemaBuilder.GetFormatPropertyName(propertyName));

                var v = valueFunc(model);

                if (v != null)
                {
                    await writer.WriteValueAsync(v);
                }
                else
                {
                    await writer.WriteNullAsync();
                }


                //}
            };

            ActionList.Add(s);

            return this;
        }


        /// <summary>
        /// 添加一个值数组,,用于不需要单独处理每个类中字段信息的情况
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="inputValueFactory"></param>
        /// <param name="desciption"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddArrayValue<TElement>(string propertyName,
            Expression<Func<TModel, Task<IEnumerable<TElement>>>> inputValueFactory, string desciption = "")
        {
            var inputValueFunc = inputValueFactory.Compile();

            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            _parentSchemaBulder.AddArrayProperty(propertyName, desciption);

            PipeAction<TModel> s = async (writer, model) =>
            {
                var inputValue = await inputValueFunc(model);

                await writer.WritePropertyNameAsync(propertyName);

                await writer.WriteStartArrayAsync();

                if (inputValue!=null && inputValue.HasData())
                {
                    foreach (var item in inputValue)
                    {
                        await writer.WriteValueAsync(item);
                    }
                }
                

                await writer.WriteEndArrayAsync();
            };

            ActionList.Add(s);

            return this;
        }

        ///// <summary>
        ///// 添加一个对象属性,,可用using结尾,或End()结尾
        ///// </summary>
        ///// <param name="propertyName"></param>
        ///// <param name="desciption"></param>
        ///// <returns></returns>
        //public JsonSchemaObjectBuilder<TValue> AddObjectFrom<TValue>(string propertyName, Expression<Func<TModel, TValue>> valueFactory, string desciption = "")
        //{
        //    propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

        //    if (!string.IsNullOrWhiteSpace(propertyName))
        //    {
        //        ActionList.Add(async (writer, _) =>
        //        {
        //            await writer.WritePropertyNameAsync(propertyName);
        //        });
        //    }
        //    else
        //    {
        //        throw new ArgumentNullException(nameof(propertyName));
        //    }

        //    var c = _parentSchemaBulder.AddObjectProperty(propertyName, desciption);

        //    var actionList = new List<PipeAction<TValue>>();

        //    var s = new JsonSchemaObjectBuilder<TValue>(actionList,c);

        //    s.Start();

        //    //s.OnEndCallback+= bulder
        //    //{

        //    //}

        //    return s;
        //}

        /// <summary>
        /// 添加一个对象属性,,可用using结尾,或End()结尾
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="desciption"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddObject(string propertyName, string desciption = "")
        {
            SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                ActionList.Add(async (writer, _) =>
                {
                    await writer.WritePropertyNameAsync(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var c = _parentSchemaBulder.AddObjectProperty(propertyName, desciption);

            var s = new JsonSchemaObjectBuilder<TModel>(ActionList, c);

            s.Start();

            return s;
        }

        /// <summary>
        /// 添加一个对象属性,,可用using结尾,或End()结尾
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="desciption"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TNewModel> AddObject<TNewModel>(string propertyName, Func<TModel, TNewModel> valueFactory = null, string desciption = "")
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                ActionList.Add(async (writer, _) =>
                {
                    await writer.WritePropertyNameAsync(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var c = _parentSchemaBulder.AddObjectProperty(propertyName, desciption);

            var s = new JsonSchemaChildObjectBuilder<TModel, TNewModel>(ActionList, valueFactory, new List<PipeAction<TNewModel>>(), c);

            s.Start();

            return s;
        }

        /// <summary>
        /// 将指定valueFactory生成的数据添加到当前对象,与AddObject不同的是,本函数直接添加在当前对象中,而非添加一个{  key:{}} 的子属性对象,与AddPropertyFrom类似,但是本函数ValueFactory只执行一次
        /// </summary>
        /// <typeparam name="TNewModel"></typeparam>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TNewModel> FromObject<TNewModel>(Func<TModel, TNewModel> valueFactory)
        {
            var s = new JsonSchemaChildObjectBuilder<TModel, TNewModel>(ActionList, valueFactory, new List<PipeAction<TNewModel>>(), this.SchemaBuilder,false);

            return s;
        }

        //protected JsonSchemaObjectBuilder<TElement> AddObject<TElement>(string propertyName,JsonValueFactory<TModel, TElement> valueFactory)
        //{
        //    if (!string.IsNullOrWhiteSpace(propertyName))
        //    {
        //        _lst.Add((writer, _) =>
        //        {
        //            writer.WritePropertyName(propertyName);
        //        });
        //    }
        //    else
        //    {
        //        throw new ArgumentNullException(nameof(propertyName));
        //    }

        //    var s = new JsonSchemaObjectBuilder<TElement>(_lst);

        //    s.Start();

        //    return s;
        //}

        /// <summary>
        /// 添加一个数组对象,并允许自定义数组对象字段
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="propertyName">属性名</param>
        /// <param name="loopValueFactory">获取数据列表的函数</param>
        /// <param name="desciption">备注</param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TElement> AddArrayObject<TElement>(string propertyName, Func<TModel, IEnumerable<TElement>> loopValueFactory, string desciption = "")
        {
            propertyName = SchemaBuilder.GetFormatPropertyName(propertyName);

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                ActionList.Add(async (writer, _) =>
                {
                    await writer.WritePropertyNameAsync(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = _parentSchemaBulder.AddArrayProperty(propertyName, desciption: desciption);

            var s = new JsonSchemaArrayBuilder<TModel, TElement>(loopValueFactory, ActionList, s1);

            var obj = s.Start();

            obj.Start();

            return obj;
        }

        /// <summary>
        /// 用于添加自定义的action操作,可用于扩展
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddCustomAction(PipeAction<TModel> action)
        {
            ActionList.Add(action);

            return this;
        }

        ///// <summary>
        ///// 添加对象数组
        ///// </summary>
        ///// <typeparam name="TElement"></typeparam>
        ///// <param name="loopValueFactory"></param>
        ///// <returns></returns>
        //protected JsonSchemaArrayBuilder<TModel, TElement> AddArrayObject<TElement>(Expression<Func<TModel, IEnumerable<TElement>>> loopValueFactory)
        //{
        //    //var lst = getList();

        //    var s = new JsonSchemaArrayBuilder<TModel, TElement>(loopValueFactory, _lst);

        //    s.Start();

        //    return s;
        //}


        public virtual void End()
        {
            if (_hasStart)
            {
                ActionList.Add(async (writer, model) =>
                {
                    if (model!=null)
                    {
                        await writer.WriteEndObjectAsync();    
                    }
                    

                    //await writer.FlushAsync();
                });

                _parentSchemaBulder.Dispose();

                OnEndCallback?.Invoke(this);
            }


        }

        public virtual void Dispose()
        {
            this.End();
        }

        public event Action<JsonSchemaObjectBuilder<TModel>> OnEndCallback;

        //protected List<PipeAction<TModel>> getJsonActionsList(Type type)
        //{
        //    if (_cache.TryGetValue(type, out var lst))
        //    {
        //        return lst;
        //    }
        //    else
        //    {
        //        lst = new List<PipeAction<TModel>>();

        //        _cache.Add(this.GetType(), lst);

        //        return lst;
        //    }

        //}

    }

    public class JsonSchemaChildObjectBuilder<TModel, TChildModel> : JsonSchemaObjectBuilder<TChildModel>
    {
        private List<PipeAction<TModel>> _parentPipe = null;
        private Func<TModel, TChildModel> _valueFactory = null;
        private bool _isNewObj = true;

        public JsonSchemaChildObjectBuilder(List<PipeAction<TModel>> parentPipe, Func<TModel, TChildModel> valueFactory, List<PipeAction<TChildModel>> lst, JsonObjectSchemeBuilder schemaBuilder,bool isNewObj=true) : base(lst, schemaBuilder)
        {
            _parentPipe = parentPipe;
            _valueFactory = valueFactory;
            _isNewObj = isNewObj;
        }

        public override JsonSchemaObjectBuilder<TChildModel> Start()
        {
            if (_isNewObj)
            {
                return base.Start();    
            }

            return this;
        }

        public override void End()
        {
            //var loopValueFactory = _valueFactory.Compile();

            _parentPipe.Add(async (writer, model) =>
            {
                //await writer.WriteStartObjectAsync();

                var childValue = _valueFactory(model);

                if (childValue!=null)
                {
                    try
                    {
                        foreach (var action in ActionList)
                        {
                            await action(writer, childValue);
                        }
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e);
                        throw;
                    }
                }
                else
                {
                    await writer.WriteNullAsync();
                }

                //此处不要调用WriteEndObjectAsync,,因为在输出的object的end中,已经调用了
                //await writer.WriteEndObjectAsync();


                //await writer.WriteEndObjectAsync();
            });

            base.End();
        }

        public override void Dispose()
        {
            this.End();
        }
    }

    public class JsonSchemaArrayBuilder<TModel, TElement> : IDisposable
    {
        private List<PipeAction<TModel>> _lst = null;

        private List<PipeAction<TElement>> _loopObject = new List<PipeAction<TElement>>();
        private Func<TModel, IEnumerable<TElement>> _listValueFactory = null;
        private JsonObjectSchemeBuilder _schemaBuilder = null;

        public JsonSchemaArrayBuilder(Func<TModel, IEnumerable<TElement>> listValueFactory, List<PipeAction<TModel>> lst, JsonObjectSchemeBuilder schemaBuilder)
        {
            _lst = lst;
            _listValueFactory = listValueFactory;
            _schemaBuilder = schemaBuilder;
        }

        /// <summary>
        /// 由框架调用,请不要调用该函数
        /// </summary>
        /// <returns></returns>
        [Browsable(false)]
        public JsonSchemaObjectBuilder<TElement> Start()
        {
            var builder = new JsonSchemaObjectBuilder<TElement>(_loopObject, _schemaBuilder);

            builder.OnEndCallback += onChildBuildEnd;

            return builder;
        }

        private void onChildBuildEnd(JsonSchemaObjectBuilder<TElement> obj)
        {
            obj.OnEndCallback -= onChildBuildEnd;
            End();
        }

        public void End()
        {
            //var loopValueFactory = _listValueFactory.Compile();

            _lst.Add(async (writer, model) =>
            {
                await writer.WriteStartArrayAsync();

                var array = _listValueFactory(model);

                foreach (var element in array)
                {
                    //await writer.WriteStartObjectAsync();

                    try
                    {
                        foreach (var action in _loopObject)
                        {
                            await action(writer, element);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    //此处不要调用WriteEndObjectAsync,,因为在输出的object的end中,已经调用了
                    //await writer.WriteEndObjectAsync();
                }

                await writer.WriteEndArrayAsync();
            });

        }

        public void Dispose()
        {
            End();
        }
    }



    public delegate Task<TValue> JsonValueFactory<in TModel, TValue>(TModel model);

    /// <summary>
    /// 静态预编译的JsonTemplateActionResult,以提升输出性能
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public abstract class StaticJsonTemplateActionResult<TModel> : JsonSchemaObjectBuilder<TModel>, IJsonTemplateActionResult
    {
        //private readonly List<PipeAction<TModel>> _actionList=null;
        //private static Dictionary<Type, JsonObjectSchemeBuilder> _cacheSchema = new Dictionary<Type, JsonObjectSchemeBuilder>();
        private static Dictionary<Type, JsonObjectSchemeBuilder> _cacheSchemaBuilder = new Dictionary<Type, JsonObjectSchemeBuilder>();

        protected StaticJsonTemplateActionResult() : base(null, null)
        {
            //Build();
        }

        private List<PipeAction<TModel>> Build()
        {
            //var lst = getJsonActionsList(this.GetType());

            if (ActionList.Count > 0)
            {

                return ActionList;
            }
            else
            {
                if (Generator == null)
                {
                    var opt =
                        (IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>)HttpContext.Current.RequestServices.GetService(
                            typeof(IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>));

                    var g = (JsonSchemaGenerator)HttpContext.Current.RequestServices.GetService(typeof(JsonSchemaGenerator));

                    //var register = (OpenApiDocumentRegistration)HttpContext.Current.RequestServices.GetService(typeof(OpenApiDocumentRegistration));

                    //var opt1 = HttpContext.Current.Features.Get<IOptions<AspNetCoreOpenApiDocumentGeneratorSettings>>();

                    var document = new OpenApiDocument();
                    //var settings = new AspNetCoreOpenApiDocumentGeneratorSettings();
                    var schemaResolver = new OpenApiSchemaResolver(document, opt.Value);
                    var generator = g ?? new JsonSchemaGenerator(opt.Value);

                    Resolver = schemaResolver;
                    Generator = generator;

                    DefaultContractResolver jsonResolver = null;

                    var scheme = new JsonSchema();

#if NETCOREAPP3_0 || NETCOREAPP3_1
                    var jsonOpt = (IOptionsSnapshot<MvcNewtonsoftJsonOptions>)HttpContext.Current.RequestServices.GetService(typeof(IOptions<MvcNewtonsoftJsonOptions>));

                    jsonResolver = jsonOpt.Value.SerializerSettings.ContractResolver as DefaultContractResolver;

                    //var _defaultSettings = opt.Value.SerializerSettings;
#endif
#if NETCOREAPP2_1
                    jsonResolver = JsonConvert.DefaultSettings?.Invoke().ContractResolver as DefaultContractResolver;
                    //var _defaultSettings = JsonConvert.DefaultSettings?.Invoke();
#endif

                    var builder = new JsonObjectSchemeBuilder(scheme.Properties, s => jsonResolver.GetResolvedPropertyName(s));

                    SchemaBuilder = builder;
                }
                //ActionList.Clear();

                this.Start();

                BuildSchema();

                this.End();

                _cacheSchemaBuilder.Add(this.GetType(), SchemaBuilder);

                return this.ActionList;
            }
        }

        protected abstract void BuildSchema();

        //void  IActionResult.Execute(TModel model)
        //{
        //    var lst = Build();

        //    var data = "";

        //    using (var stream = new MemoryStream())
        //    using (var textWriter = new StreamWriter(stream))
        //    using (var writer = new JsonTextWriter(textWriter))
        //    {
        //        //writer.WriteStartObject();

        //        foreach (var action in lst)
        //        {
        //            action(writer, model);
        //        }

        //        //writer.WriteEndObject();

        //        writer.Flush();

        //        stream.Position = 0;

        //        //data = Encoding.UTF8.GetString(stream.ToArray());
        //    }
        //}

        public TModel Model { set; get; }

        private static JsonSerializerSettings _defaultJsonSerializerSettings = new JsonSerializerSettings();

        async Task IActionResult.ExecuteResultAsync(ActionContext context)
        {
            var lst = Build();

            var data = "";

            context.HttpContext.Response.ContentType = "application/json";

            //using (var stream = new MemoryStream())


#if NETCOREAPP3_0 || NETCOREAPP3_1
            var opt =
                ((IOptions<MvcNewtonsoftJsonOptions>)context.HttpContext.RequestServices.GetService(
                    typeof(IOptions<MvcNewtonsoftJsonOptions>)))?.Value?.SerializerSettings?? JsonConvert.DefaultSettings?.Invoke() ?? _defaultJsonSerializerSettings;
#else
            var opt = JsonConvert.DefaultSettings?.Invoke() ?? _defaultJsonSerializerSettings;
#endif


            using (var textWriter = new StreamWriter(/*stream*/context.HttpContext.Response.Body, Encoding.UTF8))
            using (var writer = new JsonTextWriter(textWriter))
            {
                //writer.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                //writer.WriteStartObject();


                writer.Formatting = opt.Formatting;
                writer.DateFormatString = opt.DateFormatString;
                writer.DateFormatHandling = opt.DateFormatHandling;
                writer.Culture = opt.Culture;


                foreach (var action in lst)
                {
                    await action(writer, Model);
                }

                //writer.WriteEndObject();

                await writer.FlushAsync();

                //stream.Position = 0;

                //data = Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public void GetNSwag(JsonSchemaGenerator generator, JsonSchemaResolver resolver, JsonObjectSchemeBuilder builder)
        {
            //由于框架初始化的时候,就调用该函数了,所以,,build在这里就直接初始化完成了


            if (_cacheSchemaBuilder.TryGetValue(this.GetType(), out var tmp))
            {
                foreach (var property in tmp.Properties)
                {
                    builder.Properties.Add(property);
                }
            }
            else
            {
                this.SchemaBuilder = builder;
                this.Generator = generator;
                this.Resolver = resolver;

                Build();
            }


        }
    }

    public static class StaticJsonBuilderExt
    {
        /// <summary>
        /// 用于需要在在传入model的时候未包含ResultReturn结构,但在返回的时候,需要一个公用的ResultReturn的外框结构的情况下使用
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="resultFactory">可在函数中根据Model中的内容判断是否成功等操作</param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TModel> AddReturnResult<TModel>(this JsonSchemaObjectBuilder<TModel> builder, Func<TModel, (bool isSuccess, int returnCode, string message)> resultFactory)
        {
            builder.FromObject(resultFactory)
                .AddProperty("IsSuccess", x => x.isSuccess, "操作是否成功")
                .AddProperty("Message", x => x.message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.returnCode, "操作结果代码")
                .AddProperty("Error", x => x.isSuccess ? x.message : "", "错误信息")
                .End();

            return builder.AddObject<TModel>("ReturnData", x => x);
        }

        /// <summary>
        /// 用于需要在在传入model的时候未包含ResultReturn结构,但在返回的时候,需要一个公用的ResultReturn的外框结构的情况下使用
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="builder"></param>
        /// <param name="resultFactory">可在函数中根据Model中的内容判断是否成功等操作</param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TModel> AddReturnResult<TModel>(this JsonSchemaObjectBuilder<TModel> builder, Func<TModel, (bool isSuccess, string message)> resultFactory)
        {
            builder.FromObject(resultFactory)
                .AddProperty("IsSuccess", x => x.isSuccess, "操作是否成功")
                .AddProperty("Message", x => x.message, "返回的提示信息")
                .AddProperty("ReturnCode", x => 0, "操作结果代码")
                .AddProperty("Error", x => x.isSuccess ? x.message : "", "错误信息").End();

            return builder.AddObject<TModel>("ReturnData", x => x);
        }

        /// <summary>
        /// 用于需要在在传入model的时候未包含ResultReturn结构,但在返回的时候,需要一个公用的ResultReturn的外框结构的情况下使用
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <param name="resultFactory">可在函数中根据Model中的内容判断是否成功等操作</param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddReturnResultArray<TElement>(this JsonSchemaObjectBuilder<IEnumerable<TElement>> builder, Func<IEnumerable<TElement>, (bool isSuccess, int returnCode, string message)> resultFactory)

        {
            builder.FromObject(resultFactory)
                .AddProperty("IsSuccess", x => x.isSuccess, "操作是否成功")
                .AddProperty("Message", x => x.message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.returnCode, "操作结果代码")
                .AddProperty("Error", x => x.isSuccess ? x.message : "", "错误信息").End();

            return builder.AddArrayObject<TElement>("ReturnData", x => x);
        }

        /// <summary>
        /// 用于需要在在传入model的时候未包含ResultReturn结构,但在返回的时候,需要一个公用的ResultReturn的外框结构的情况下使用
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <param name="resultFactory">可在函数中根据Model中的内容判断是否成功等操作</param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddReturnResultArray<TElement>(this JsonSchemaObjectBuilder<IEnumerable<TElement>> builder, Func<IEnumerable<TElement>, (bool isSuccess, string message)> resultFactory)
        {
            builder.FromObject(resultFactory)
                .AddProperty("IsSuccess", x => x.isSuccess, "操作是否成功")
                .AddProperty("Message", x => x.message, "返回的提示信息")
                .AddProperty("ReturnCode", x => 0, "操作结果代码")
                .AddProperty("Error", x => x.isSuccess ? x.message : "", "错误信息")
                .End();

            return builder.AddArrayObject<TElement>("ReturnData", x => x);
        }

                /// <summary>
        /// 用于需要在在传入model的时候未包含ResultReturn结构,但在返回的时候,需要一个公用的ResultReturn的外框结构的情况下使用
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <param name="resultFactory">可在函数中根据Model中的内容判断是否成功等操作</param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddReturnResultArray<TElement>(this JsonSchemaObjectBuilder<IReadOnlyList<TElement>> builder, Func<IEnumerable<TElement>, (bool isSuccess, int returnCode, string message)> resultFactory)
        {
            builder.FromObject(resultFactory)
                .AddProperty("IsSuccess", x => x.isSuccess, "操作是否成功")
                .AddProperty("Message", x => x.message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.returnCode, "操作结果代码")
                .AddProperty("Error", x => x.isSuccess ? x.message : "", "错误信息").End();

            return builder.AddArrayObject<TElement>("ReturnData", x => x);
        }

        /// <summary>
        /// 用于需要在在传入model的时候未包含ResultReturn结构,但在返回的时候,需要一个公用的ResultReturn的外框结构的情况下使用
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <param name="resultFactory">可在函数中根据Model中的内容判断是否成功等操作</param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddReturnResultArray<TElement>(this JsonSchemaObjectBuilder<IReadOnlyList<TElement>> builder, Func<IEnumerable<TElement>, (bool isSuccess, string message)> resultFactory)
        {
            builder.FromObject(resultFactory)
                .AddProperty("IsSuccess", x => x.isSuccess, "操作是否成功")
                .AddProperty("Message", x => x.message, "返回的提示信息")
                .AddProperty("ReturnCode", x => 0, "操作结果代码")
                .AddProperty("Error", x => x.isSuccess ? x.message : "", "错误信息")
                .End();

            return builder.AddArrayObject<TElement>("ReturnData", x => x);
        }

        

        /// <summary>
        /// 添加当前对象的关于ReturnResult的通用属性,并返回returnData对象的构建器
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TValue> AddReturnResult<TValue>(this JsonSchemaObjectBuilder<ResultReturn<TValue>> builder)
        {
            builder.AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                .AddProperty("Message", x => x.Message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                .AddProperty("Error", x => x.Error?.Message, "错误信息").End();

            return builder.AddObject<TValue>("ReturnData", x => x.GetResultData());
        }

        /// <summary>
        /// 添加当前对象的关于ReturnResult的通用属性,并通过returnDataConverter参数,重新转换returnData中的类型,并返回returnData对象的构建器
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="builder"></param>
        /// <param name="returnDataConverter"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TValue> AddReturnResult<TValue>(this JsonSchemaObjectBuilder<ResultReturn> builder, Func<object, TValue> returnDataConverter)
        {
            builder.AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                .AddProperty("Message", x => x.Message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                .AddProperty("Error", x => x.Error?.Message, "错误信息").End();

            return builder.AddObject<TValue>("ReturnData", x => returnDataConverter(x.ReturnData));
        }

        /// <summary>
        /// 从valueFactory中创建一个ResultReturn对象,并返回returnData参数对象的构建器
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TReturnData"></typeparam>
        /// <param name="builder"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TReturnData> AddReturnResult<TModel, TReturnData>(this JsonSchemaObjectBuilder<TModel> builder, Func<TModel, ResultReturn> valueFactory)
        {
            builder.FromObject(valueFactory)
                .AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                .AddProperty("Message", x => x.Message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                .AddProperty("Error", x => x.IsSuccess ? x.Message : "", "错误信息")
                .End();
            
            return builder.AddObject("ReturnData", x => (TReturnData)valueFactory(x).ReturnData);
            
            using (var b = builder.FromObject(valueFactory))
            {
                b.AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                    .AddProperty("Message", x => x.Message, "返回的提示信息")
                    .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                    .AddProperty("Error", x => x.Error?.Message, "错误信息").End();

                return b.AddObject("ReturnData", x => (TReturnData)x.ReturnData);
            }

            //builder.AddProperty("IsSuccess", x => result.IsSuccess, "操作是否成功")
            //    .AddProperty("Message", x => result.Message, "返回的提示信息")
            //    .AddProperty("ReturnCode", x => result.ReturnCode, "操作结果代码")
            //    .AddProperty("Error", x => result.Error?.Message, "错误信息");

            //return builder.AddObject("ReturnData", x => (TReturnData)result.ReturnData);
        }

        /// <summary>
        /// 从valueFactory中创建一个ResultReturn对象,并返回returnData参数对象的构建器
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TReturnData"></typeparam>
        /// <param name="builder"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TReturnData> AddReturnResult<TModel, TReturnData>(this JsonSchemaObjectBuilder<TModel> builder, Func<TModel, ResultReturn<TReturnData>> valueFactory)
        {
            Debug.Assert(valueFactory != null);
            
            builder.FromObject(valueFactory).AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                .AddProperty("Message", x => x.Message, "返回的提示信息")
                .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                .AddProperty("Error", x => x.Error?.Message, "错误信息").End();
            
            return builder.AddObject("ReturnData", x => valueFactory(x).GetResultData());
        }

        /// <summary>
        /// 添加一个ReturnData返回类型为数组的ReturnResult对象,并且该对象可以从valueFactory中获取到
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddReturnResultArray<TElement>(
            this JsonSchemaObjectBuilder<ResultReturn<IEnumerable<TElement>>> builder)
        {
            using (var b=builder.FromObject(x => x))
            {
                return b.AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                    .AddProperty("Message", x => x.Message, "返回的提示信息")
                    .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                    .AddProperty("Error", x => x.Error?.Message, "错误信息")
                    .AddArrayObject("ReturnData", x => x.GetResultData())
                    ;
            }
            
        

            //builder.AddProperty("IsSuccess", x => result.IsSuccess,"操作是否成功")
            //    .AddProperty("Message", x => result.Message,"返回的提示信息")
            //    .AddProperty("ReturnCode", x => result.ReturnCode,"操作结果代码")
            //    .AddProperty("Error", x => result.Error?.Message,"错误信息");

            //return builder.AddArrayObject<TElement>("ReturnData", x => result.GetResultData());
        }

        /// <summary>
        /// 添加一个ReturnData返回类型为数组的ReturnResult对象,并且该对象可以从valueFactory中获取到
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <param name="valueFactory"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddReturnResultArray<TModel, TElement>(
            this JsonSchemaObjectBuilder<TModel> builder,
            Func<TModel, IResultReturn<IEnumerable<TElement>>> valueFactory) where TModel : IResultReturn<IEnumerable<TElement>>
        {

            using (var b = builder.FromObject(valueFactory))
            {
                return b.AddProperty("IsSuccess", x => x.IsSuccess, "操作是否成功")
                    .AddProperty("Message", x => x.Message, "返回的提示信息")
                    .AddProperty("ReturnCode", x => x.ReturnCode, "操作结果代码")
                    .AddProperty("Error", x => x.Error?.Message, "错误信息")
                    .AddArrayObject("ReturnData", x => x.GetResultData());
            }

            
        

            //builder.AddProperty("IsSuccess", x => result.IsSuccess,"操作是否成功")
            //    .AddProperty("Message", x => result.Message,"返回的提示信息")
            //    .AddProperty("ReturnCode", x => result.ReturnCode,"操作结果代码")
            //    .AddProperty("Error", x => result.Error?.Message,"错误信息");

            //return builder.AddArrayObject<TElement>("ReturnData", x => result.GetResultData());
        }

        /// <summary>
        /// 自动添加当前对象中关于IPagedList的属性,并返回IPagedList.Data的数组构建对象
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddPagedList<TElement>(this JsonSchemaObjectBuilder<IPagedList<TElement>> builder)
        {
            using (var b = builder.FromObject(x => x))
            {
                return b.AddProperty("PageCount", x => x.PageCount, "总页数")
                    .AddProperty("PageSize", x => x.PageSize, "分页大小")
                    .AddProperty("PageIndex", x => x.PageIndex, "页码")
                    .AddProperty("TotalCount", x => x.TotalCount, "总记录数")
                    .AddArrayObject("Data", x => x.GetData(), "数据内容");

            }

            //return bulder.AddProperty("PageCount", x => lst.PageCount,"总页数")
            //    .AddProperty("PageSize", x => lst.PageSize,"分页大小")
            //    .AddProperty("PageIndex", x => lst.PageIndex,"页码")
            //    .AddProperty("TotalCount", x => lst.TotalCount,"总记录数")
            //    .AddArrayObject("Data", x => lst.GetData(),"数据内容");
        }

        /// <summary>
        /// 自动添加当前对象中关于IPagedList的属性,并返回IPagedList.Data的数组构建对象
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static JsonSchemaObjectBuilder<TElement> AddPagedList<TElement>(this JsonSchemaObjectBuilder<VM_PagedList<TElement>> builder)
        {
            using (var b = builder.FromObject(x => x))
            {
                return b.AddProperty("PageCount", x => x.PageCount, "总页数")
                    .AddProperty("PageSize", x => x.PageSize, "分页大小")
                    .AddProperty("PageIndex", x => x.PageIndex, "页码")
                    .AddProperty("TotalCount", x => x.TotalCount, "总记录数")
                    .AddArrayObject("Data", x => x.GetData(), "数据内容");

            }

            //return bulder.AddProperty("PageCount", x => lst.PageCount,"总页数")
            //    .AddProperty("PageSize", x => lst.PageSize,"分页大小")
            //    .AddProperty("PageIndex", x => lst.PageIndex,"页码")
            //    .AddProperty("TotalCount", x => lst.TotalCount,"总记录数")
            //    .AddArrayObject("Data", x => lst.GetData(),"数据内容");
        }

        public static JsonSchemaObjectBuilder<TElement> AddPagedList<TModel, TElement>(this JsonSchemaObjectBuilder<TModel> bulder,
           [NotNull] Func<TModel, IPagedList<TElement>> valueFactory)
        {
            return bulder.AddProperty("PageCount", x => valueFactory(x).PageCount, "总页数")
                .AddProperty("PageSize", x => valueFactory(x).PageSize, "分页大小")
                .AddProperty("PageIndex", x => valueFactory(x).PageIndex, "页码")
                .AddProperty("TotalCount", x => valueFactory(x).TotalCount, "总记录数")
                .AddArrayObject("Data", x => valueFactory(x).GetData(), "数据内容");
        }


        //public static JsonSchemaObjectBuilder<TResult> AddResultReturn<TModel,TResult>(this JsonSchemaObjectBuilder<TModel> builder,
        //    Expression<Func<TModel, IResultReturn>> valueFactory) where TResult:ResultReturn
        //{

        //}

        //public static JsonSchemaObjectBuilder<object> UseResultReturn<TModel>(this JsonSchemaObjectBuilder<TModel> builder,
        //    Expression<Func<TModel, IResultReturn>> valueFactory) where TModel:ResultReturn
        //{
        //    return builder.AddProperty(x => x.IsSuccess, x => x.Message, x => x.ReturnCode, x => x.Error)
        //        .AddObject("returnData", "返回数据")
        //        .Start();


        //}
    }
}
