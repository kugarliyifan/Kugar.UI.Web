using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.Generation;

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
        private List<PipeAction<TModel>> _lst = null;
        private JsonObjectSchemeBuilder _parentSchemaBulder = null;

        public JsonSchemaObjectBuilder(List<PipeAction<TModel>> lst, JsonObjectSchemeBuilder schemaBuilder)
        {
            _lst = lst;
            _parentSchemaBulder = schemaBuilder;
        }

        //public JsonSchemaObjectBuilder()
        //{
        //    _lst = getJsonActionsList(this.GetType());
        //}

        internal JsonObjectSchemeBuilder SchemaBulder
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
            get => _lst;
            set
            {
                _lst = value;
            }
        }

        protected JsonSchemaGenerator Generator { set; get; }

        protected JsonSchemaResolver Resolver { set; get; }


        public JsonSchemaObjectBuilder<TModel> Start()
        {
            _lst.Add(async (writer, model) =>
            {
                await writer.WriteStartObjectAsync();
            });

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
        public JsonSchemaObjectBuilder<TModel> AddProperty<TValue>(string propertyName, JsonValueFactory<TModel, TValue> valueFactory, string desciption="",/* JsonObjectType type,*/  object example = null,
            bool? nullable = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(propertyName));
            Debug.Assert(valueFactory!=null);

            PipeAction<TModel> s =async (writer, model) =>
            {
                await writer.WritePropertyNameAsync(propertyName);

                var value =await valueFactory(model);

                await writer.WriteValueAsync(value);
            };

            _parentSchemaBulder.AddProperty(propertyName, _parentSchemaBulder._typeToJsonObjectType(typeof(TValue)), desciption,
                example: example,nullable ?? isNullable(typeof(TValue)));

            _lst.Add(s);

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

            Debug.Assert(inputValueFactory!=null);
            

            var funcList = new List<(string properyName, Func<TInput, object> valueCaller)>();

            var typeBuilder = _parentSchemaBulder.With<TInput>();

            foreach (var item in propertyExpr)
            {
                var caller = item;

                var properyName = getPropName(caller);

                var callerReturnType = getExprReturnType(item);

                var t = item.Compile();

                typeBuilder.Property(item, type: _parentSchemaBulder._typeToJsonObjectType(callerReturnType),nullable:isNullable(callerReturnType));

                funcList.Add((properyName, t));
            }

            typeBuilder.End();

            var inputValueFunc = inputValueFactory.Compile();

            PipeAction<TModel> s =async (writer, model) =>
            {
                var inputValue = inputValueFunc(model);

                foreach (var item in funcList)
                {
                    await writer.WritePropertyNameAsync(item.properyName);

                    var v = item.valueCaller(inputValue);

                    await writer.WriteValueAsync(v);
                }
            };

            _lst.Add(s);

            return this;
        }

        /// <summary>
        /// 添加单个字段
        /// </summary>
        /// <param name="propertyExpr"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddProperty(Expression<Func<TModel, object>> propertyExpr)
        {
            //if (!propertyExpr.HasData())
            //{
            //    return this;
            //}

            var funcList = new List<(string properyName, Func<TModel, object> valueCaller)>();

            var typeBuilder = _parentSchemaBulder.With<TModel>();

            //foreach (var item in propertyExpr)
            //{
                var caller = propertyExpr;

                var properyName = getPropName(caller);

                var callerReturnType = getExprReturnType(propertyExpr);

                var t = propertyExpr.Compile();

                typeBuilder.Property(propertyExpr, type: _parentSchemaBulder._typeToJsonObjectType(callerReturnType), nullable: isNullable(callerReturnType));

                funcList.Add((properyName, t));
            //}

            typeBuilder.End();

            PipeAction<TModel> s = async (writer, model) =>
            {
                foreach (var item in funcList)
                {
                    await writer.WritePropertyNameAsync(item.properyName);

                    var v = item.valueCaller(model);

                    await writer.WriteValueAsync(v);
                }
            };

            _lst.Add(s);

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
        public JsonSchemaObjectBuilder<TModel> AddArrayValue<TElement >(string propertyName,
            Expression<Func<TModel, Task<IEnumerable<TElement>>>> inputValueFactory, string desciption = "")
        {
            var inputValueFunc = inputValueFactory.Compile();

            _parentSchemaBulder.AddArrayProperty(propertyName, desciption);

            PipeAction<TModel> s = async (writer, model) =>
            {
                var inputValue =await inputValueFunc(model);

                await writer.WritePropertyNameAsync(propertyName);

                await writer.WriteStartArrayAsync();

                foreach (var item in inputValue)
                {
                    await writer.WriteValueAsync(item);
                }

                await writer.WriteEndArrayAsync();
            };

            _lst.Add(s);

            return this;
        }

        /// <summary>
        /// 添加一个对象属性,,可用using结尾,或End()结尾
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="desciption"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddObject(string propertyName,string desciption="")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                _lst.Add(async (writer, _) =>
                {
                    await writer.WritePropertyNameAsync(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var c=_parentSchemaBulder.AddObjectProperty(propertyName, desciption);

            var s = new JsonSchemaObjectBuilder<TModel>(_lst, c);

            s.Start();

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
        public JsonSchemaArrayBuilder<TModel, TElement> AddArrayObject<TElement>(string propertyName, Expression<Func<TModel, Task<IEnumerable<TElement>>>> loopValueFactory, string desciption="")
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                _lst.Add(async (writer, _) =>
                {
                    await writer.WritePropertyNameAsync(propertyName);
                });
            }
            else
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            var s1 = _parentSchemaBulder.AddArrayProperty(propertyName, desciption: desciption);

            var s = new JsonSchemaArrayBuilder<TModel, TElement>(loopValueFactory, _lst,s1);

            return s;
        }

        /// <summary>
        /// 用于添加自定义的action操作,可用于扩展
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public JsonSchemaObjectBuilder<TModel> AddCustomAction(PipeAction<TModel> action)
        {
            _lst.Add(action);

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


        public void End()
        {
            _lst.Add(async (writer, model) =>
            {
                await writer.WriteEndObjectAsync();
            });

            _parentSchemaBulder.Dispose();
        }

        public void Dispose()
        {
            End();
        }

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

    public class JsonSchemaArrayBuilder<TModel, TElement> : IDisposable
    {
        private List<PipeAction<TModel>> _lst = null;

        private List<PipeAction<TElement>> _loopObject = new List<PipeAction<TElement>>();
        private Expression<Func<TModel, Task<IEnumerable<TElement>>>> _listValueFactory = null;
        private JsonObjectSchemeBuilder _schemaBuilder = null;

        public JsonSchemaArrayBuilder(Expression<Func<TModel, Task<IEnumerable<TElement>>>> listValueFactory, List<PipeAction<TModel>> lst, JsonObjectSchemeBuilder schemaBuilder)
        {
            _lst = lst;
            _listValueFactory = listValueFactory;
            _schemaBuilder = schemaBuilder;
        }

        public JsonSchemaObjectBuilder<TElement> Start()
        {
            return new JsonSchemaObjectBuilder<TElement>(_loopObject, _schemaBuilder);
        }

        public void End()
        {
            var loopValueFactory = _listValueFactory.Compile();

            _lst.Add(async (writer, model) =>
            {
                await writer.WriteStartArrayAsync();

                var array =await loopValueFactory(model);

                foreach (var element in array)
                {
                    await writer.WriteStartObjectAsync();

                    foreach (var action in _loopObject)
                    {
                        await action(writer, element);
                    }

                    await writer.WriteEndObjectAsync();
                }

                await writer.WriteEndArrayAsync();
            });

        }

        public void Dispose()
        {
            End();
        }
    }



    public delegate Task<TValue> JsonValueFactory<in TModel, TValue>(TModel Model);

    public abstract class StaticJsonBuilder<TModel> : JsonSchemaObjectBuilder<TModel>, IJsonTemplateActionResult
    {
        //private readonly List<PipeAction<TModel>> _actionList=null;
        //private static Dictionary<Type, JsonObjectSchemeBuilder> _cacheSchema = new Dictionary<Type, JsonObjectSchemeBuilder>();

        protected StaticJsonBuilder() : base(new List<PipeAction<TModel>>(), null)
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
                this.Start();

                BuildSchema();

                this.End();

                return this.ActionList;
            }
        }

        protected abstract void BuildSchema();

        public void Execute(TModel model)
        {
            var lst = Build();

            var data = "";

            using var stream = new MemoryStream();
            using var textWriter = new StreamWriter(stream);
            using (var writer = new JsonTextWriter(textWriter))
            {
                //writer.WriteStartObject();

                foreach (var action in lst)
                {
                    action(writer, model);
                }

                //writer.WriteEndObject();

                writer.Flush();

                stream.Position = 0;

                data = Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public TModel Model { set; get; }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var lst = Build();

            var data = "";

            context.HttpContext.Response.ContentType = "application/json";

            using (var textWriter = new StreamWriter(context.HttpContext.Response.Body,Encoding.UTF8))
            using (var writer = new JsonTextWriter(textWriter))
            {
                //writer.WriteStartObject();

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
            this.SchemaBulder = builder;
            this.Generator = generator;
            this.Resolver = resolver;

            Build();
        }
    }
}
