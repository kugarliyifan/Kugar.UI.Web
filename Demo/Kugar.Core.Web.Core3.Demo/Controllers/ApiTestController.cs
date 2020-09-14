using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.ActionResult;
using Kugar.Core.Web.Controllers;
using Kugar.Core.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;
using PropertyInfo = System.Reflection.PropertyInfo;
using ValueTuple = System.ValueTuple;

namespace Kugar.Core.Web.Core3.Demo.Controllers
{
    [Route("apitest/[action]")]
    public class ApiTestController : ControllerBase//, IJWTLoginControlle<int>
    {
        /// <summary>
        /// fsdfsfs
        /// </summary>
        /// <param name="str1">sssss</param>
        /// <param name="tupleTest">ddddddddddd</param>
        /// <returns></returns>
        [FromBodyJson,HttpPost]
        public async Task<IActionResult> TestValid(
             [Display(Name = "ssssss"),StringLength(100,MinimumLength = 6),Required] string str1,
             [Range(20,50), Required] int int3,
             (string key1,string key2) tupleTest
        )
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new FailResultReturn("")
                {
                    Error = new ModelStateErrorException(ModelState),
                    ReturnCode = 10001
                });
            }

            return null;
        }

        [FromBodyJson()]
        public IActionResult test1(/*List<(string productid,int qty)> details*/)
        {
            ModelState.AddModelError("sss","sdfsdfsdf");

            return new JsonResult(new FailResultReturn("")
            {
                Error = new ModelStateErrorException(ModelState),
                ReturnCode = 10001
            });

            return new ModelStateValidActionResult();

            return Content("success");
        }

        /// <summary>
        /// ppppp
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(TestJsonTemplate1),200)]
        public async Task<IActionResult> test2()
        {
            var view=new TestJsonTemplate1()
            {
                Model = new Test<string, int>("ddddd",2)
            };

            return view;
        }

        /// <summary>
        /// oooo
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(TestJsonTemplate2),200)]
        public async Task<IActionResult> Test3()
        {
            var v=new TestJsonTemplate2()
            {
                Model = new Test<string, string>("iiii","oooo")
            };

            return v;
        }

        [ProducesResponseType(typeof(TestJsonTemplate3),200)]
        public async Task<IActionResult> Test4()
        {
            var v = new TestJsonTemplate3()
            {
                Model = new Test<string, string>("iiii", "oooo")
            };

            return v;
        }

        [ProducesResponseType(typeof(TestCustomJsonTemplate4), 200)]
        public async Task<IActionResult> Test5()
        {
            var v=new TestCustomJsonTemplate4()
            {
                Model = new Test<string, string>("iiii", "oooo")
            };

            return v;
        }

        public ResultReturn<(string str1, int int3)> Test()
        {
            return new SuccessResultReturn<(string str1, int int3)>(("2222",222));
        }

        public object Test2()
        {
            var s=  new Test< (string Y1, string Y2),(string str1, string t2)>(("111","22222"),("3333","44444") );

            JsonConvert.SerializeObject(s);

            return null;
        }
    }

    public class Test<T1, T2>
    {
        public Test(T1 p1,T2 p2)
        {
            Prop1 = p1;
            Prop2 = p2;
        }

        /// <summary>
        /// prop2原备注
        /// </summary>
        public T2 Prop2 { set; get; }

        /// <summary>
        /// prop1原备注
        /// </summary>
        public T1 Prop1 { set; get; }

        /// <summary>
        /// prop3备注
        /// </summary>
        public (string sss2, string ppp) Prop3 { set; get; } = ("33333", "4444");

        /// <summary>
        /// 数组测试备注
        /// </summary>
        public T2[] ArrayTest => Enumerable.Repeat(Prop2, 20).ToArrayEx();

        /// <summary>
        /// 数组2测试备注
        /// </summary>
        public AP[] ArrayTest2 { get; }=new AP[]
        {
            new AP(){str2 = "11",str3 = "222",int2 = 10},
            new AP(){str2 = "12",str3 = "223",int2 = 11},
            new AP(){str2 = "13",str3 = "224",int2 = 12},
            new AP(){str2 = "14",str3 = "225",int2 = 13},

        };
    }

    public class AP
    {
        /// <summary>
        /// str2原备注
        /// </summary>
        public string str2 { set; get; }

        /// <summary>
        /// str3原备注
        /// </summary>
        public string str3
        {
            set;
            get;
        }

        /// <summary>
        /// int2原备注
        /// </summary>
        public int int2 { set; get; }
}

    public class TestJsonTemplate1 : StaticJsonTemplateActionResult<Test<string,int>>
    {
        protected override void BuildSchema()
        {
            this.AddProperty(x => x.Prop1,"属性1")
                .AddProperty(x=>x.Prop2)
                ;

            this.AddObject("prop3", "sdfsfdsf")
                .AddPropertyFrom(x => x.Prop3, x => x.ppp, x => x.sss2)
                .AddProperty(x=>x.Prop2)
                .End();

        }
    }

    public class TestJsonTemplate2 : StaticJsonTemplateActionResult<Test<string, string>>
    {
        protected override void BuildSchema()
        {
            this.AddProperty("prop2",x=>x.Prop2,"sdfsfsf");

            this.AddProperty("prop3", x => x.Prop1, "测试属性2");
        }
    }

    public class TestJsonTemplate3: StaticJsonTemplateActionResult<Test<string, string>>
    {
        protected override void BuildSchema()
        {
            this.AddProperty("prop2", x => x.Prop2, "prop2测试")
                .AddProperty(x => x.Prop1);

            this.AddObject("prop3","测试子object")
                .AddPropertyFrom(x=>x.Prop3,x=>x.ppp,x=>x.sss2)
                .End();

            this.AddArrayObject("arraytest",x=>x.ArrayTest2,"")
                .AddProperty(x=>x.str3,"str3备注")
                .AddProperty(x=>x.str2)
                .AddProperty(x=>x.int2)
                .End();
                
        }
    }

    public class TestCustomJsonTemplate4 : JsonTemplateActionResult<Test<string, string>>
    {
        public override void GetNSwag(JsonSchemaGenerator generator, JsonSchemaResolver resolver, JsonObjectSchemeBuilder builder)
        {
            builder.AddProperty("prop2", JsonObjectType.String, "prop2原备注")
                .With<Test<string, string>>()
                .Property(x=>x.Prop1)
                .End()
                ;

            using (var a1= builder.AddObjectProperty("prop3", "prop3原备注"))
            {
                a1.AddProperty("prop3", JsonObjectType.String, "")
                    .AddProperty("sss2", JsonObjectType.String, "");
            }

            using (var a2=builder.AddArrayProperty("arraytest", "arraytest"))
            {
                a2.With<AP>()
                    .Property(x => x.str3)
                    .Property(x => x.str2)
                    .Property(x => x.int2)
                    .End();
            }

        }

        public override void BuildJson(JsonTemplateBuilder writer)
        {
            using (var o=writer.StartObject())
            {
                o.WriteProperty("prop2", Model.Prop2)
                    .With(Model)
                    .WriteProperty(x => x.Prop1)
                    .End();

                using (o.StartObject("prop3"))
                {
                    o.WriteProperty("prop3", Model.Prop3.ppp)
                        .WriteProperty("sss2", Model.Prop3.sss2);
                }

                using (var ar=o.StartArray("arraytest"))
                {
                    foreach (var item in Model.ArrayTest2)
                    {
                        using (var o2=ar.StartObject())
                        {
                            o2.With(item)
                                .WriteProperty(x => x.str3)
                                .WriteProperty(x => x.str2)
                                .WriteProperty(x => x.int2)
                                .End();
                        }
                    }
                }
                    
            }
        }


    }

    //public class A : DefaultContractResolver
    //{
    //    private MethodInfo _methodInfo = null;

    //    public A(MethodInfo methodInfo)
    //    {
    //        //_isCamel = JsonConvert.DefaultSettings?.Invoke()?.ContractResolver is CamelCasePropertyNamesContractResolver;
    //        _methodInfo = methodInfo;

    //    }

    //    public override JsonContract ResolveContract(Type type)
    //    {
    //        if(!type.GetProperties()
    //            .Where(x=>x.CanWrite && x.PropertyType.IsValueTuple())
    //            .Any())
    //        {
    //            return null;
    //        }

    //        var rc= base.ResolveContract(type);

    //        return rc;
    //    }

    //    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    //    {
    //        JsonProperty property =base.CreateProperty(member, memberSerialization);

    //        var pi = member as PropertyInfo;

    //        if ( property.PropertyType.IsValueTuple())
    //        {
    //            var dtype = member.DeclaringType.GetGenericTypeDefinition();
    //            var p = dtype.GetGenericTypeDefinition().GetProperty(member.Name);
    //            var t= ((System.Reflection.TypeInfo)dtype).GenericTypeParameters.Where(x => x == p.PropertyType).Select(x=>x).FirstOrDefault();

    //            var attr = member.DeclaringType.GetCustomAttribute<TupleElementNamesAttribute>();

    //            if (attr!=null)
    //            {
    //                property.Converter = new ValueTupleConverter(attr,this.NamingStrategy);
    //            }
    //        }

    //        return property;
    //    }
    //}


    /// <summary>
    /// 用于在泛型类的参数中,带有ValueTuple的情况下使用
    /// </summary>
    internal class GenericTypeInnerValueTupleJsonConverter : JsonConverter
    {
        private PropertyInfo _propertyInfo = null;
        private int _genericParameterIndex = -1;
        private NamingStrategy _strategy = null;
        private int _startIndex = 0;
        private int _endIndex = 0;
        private string[] _names = null;
        private static ConcurrentDictionary<MemberInfo,string[]> _namesCaches=new ConcurrentDictionary<MemberInfo, string[]>();
        private MethodInfo _method = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="genericParamterIndex">在泛型类的定义中所在的序号</param>
        /// <param name="strategy"></param>
        public GenericTypeInnerValueTupleJsonConverter(MethodInfo method, NamingStrategy strategy = null)
        {
            _method = method;
            _strategy = strategy;

            
            var sourceArgs = _propertyInfo.DeclaringType.GetGenericArguments();

            for (int i = 0; i < _genericParameterIndex; i++)
            {
                _startIndex += sourceArgs[i].GenericTypeArguments.Length;
            }

            _startIndex += 1;

            _endIndex += sourceArgs[_genericParameterIndex].GenericTypeArguments.Length;


        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var names = getNames(_method);

            if (value != null && value is ITuple v)
            {
                writer.WriteStartObject();

                for (int i = 0; i < v.Length; i++)
                {
                    var pname = names[i].IfEmptyOrWhileSpace($"Item{i+1}");

                    writer.WritePropertyName(_strategy?.GetPropertyName(pname, true) ?? pname);
                    
                    if (v[i] == null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        serializer.Serialize(writer, v[i]);
                    }
                }

                writer.WriteEndObject();
            }
            else
            {
                writer.WriteNull();
            }
        }

        private string[] getNames(MethodInfo method)
        {
            return _namesCaches.GetOrAdd(method, m =>
            {
                var attr = method.ReturnTypeCustomAttributes
                    .GetCustomAttributes(typeof(TupleElementNamesAttribute), true)
                    .Select(x => (TupleElementNamesAttribute) x)
                    .FirstOrDefault();

                if (attr==null)
                {
                    return Array.Empty<string>();
                }
                return attr.TransformNames.Skip(_startIndex).Take(_endIndex - _startIndex).ToArrayEx();
            });
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }
    }
}