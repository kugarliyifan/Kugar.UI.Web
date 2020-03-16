using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using PropertyInfo = System.Reflection.PropertyInfo;
using ValueTuple = System.ValueTuple;

namespace Kugar.Core.Web.Core3.Demo.Controllers
{
    public class ApiTestController : ControllerBase
    {
        [FromBodyJson()]
        public IActionResult test1(List<(string productid,int qty)> details)
        {

            return Content("success");
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

        public T2 Prop2 { set; get; }

        public T1 Prop1 { set; get; }

        public (string sss2, string ppp) Prop3 { set; get; } = ("33333", "4444");
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