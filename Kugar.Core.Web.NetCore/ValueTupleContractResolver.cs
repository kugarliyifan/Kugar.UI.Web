using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Converters;
using Kugar.Core.Web.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kugar.Core.Web
{
    public class ValueTupleContractResolver : DefaultContractResolver
    {
        private MethodInfo _methodInfo = null;
        private IContractResolver _parentResolver = null;

        public ValueTupleContractResolver(MethodInfo methodInfo, IContractResolver parentContractResolver = null)
        {
            _methodInfo = methodInfo;
            _parentResolver = parentContractResolver;
        }

        public override JsonContract ResolveContract(Type type)
        {
            if (!type.GetProperties()
                .Where(x => x.CanRead && ValueTupleHelper.IsValueTuple(x.PropertyType))
                .Any())  //如果Type类中不包含可读的ValueTuple类型的属性,则调用预定义的Resolver处理,当前Resolver只处理包含ValueTuple的类
            {
                return _parentResolver?.ResolveContract(type);
            }

            var rc = base.ResolveContract(type);

            return rc;
        }

        public MethodInfo Method => _methodInfo;

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            //CreateProperty函数的结果,不需要额外加缓存,因为每个Method的返回Type,只会调用一次
            JsonProperty property = base.CreateProperty(member, memberSerialization);  //先调用默认的CreateProperty函数,创建出默认JsonProperty

            var pi = member as PropertyInfo;

            if (property.PropertyType.IsValueTuple())
            {
                var attr = pi.GetCustomAttribute<TupleElementNamesAttribute>();  //获取定义在属性上的特性

                if (attr != null)  
                {
                    //如果该属性是已经编译时有添加了TupleElementNamesAttribute特性的,,则不需要从method获取
                    //这里主要是为了处理 (string str1,int int2) Prop3 这种情况
                    property.Converter = new ValueTupleConverter(attr, this.NamingStrategy);
                }
                else 
                {
                    //从输入的method获取,并且需要计算当前属性所属的泛型是在第几个,然后计算出在TupleElementNamesAttribute.Names中的偏移
                    //这个主要是处理比如T2 Prop2 T2=ValueTuple的这种情况
                    var mAttr = (TupleElementNamesAttribute)_methodInfo.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(TupleElementNamesAttribute), true).FirstOrDefault(); //用来获取valueTuple的各个字段名称
                    var basePropertyClass = pi.DeclaringType.GetGenericTypeDefinition(); //属性定义的泛型基类 如 A<T1,T2>
                    var basePropertyType = basePropertyClass.GetProperty(pi.Name).PropertyType; //获取基类属性的返回类型 就是T1 ,比如获取在A<(string str1,string str2),(string str3,string str4)> 中 Prop1 返回的类型是对应基类中的T1还是T2
                    var index = basePropertyType.GenericParameterPosition;//获取属性所在的序号,用于计算 mAttr.Names中的偏移量
                    var skipNamesCount = (pi.DeclaringType as TypeInfo).GenericTypeArguments
                        .Take(index)
                        .Sum(x => x.IsValueTuple() ? x.GenericTypeArguments.Length : 0); ;  //计算TupleElementNamesAttribute.TransformNames中当前类的偏移量
                    var names = mAttr.TransformNames
                        .Skip(skipNamesCount)
                        .Take(pi.PropertyType.GenericTypeArguments.Length)
                        .ToArrayEx(); //获取当前类的所有name
                    property.Converter = new ValueTupleConverter(names, this.NamingStrategy);  //传入converter
                }

                property.GetIsSpecified = x => true;
                property.ItemConverter = property.Converter;  //传入converter
                property.ShouldSerialize = x => true;
                property.HasMemberAttribute = false;
            }
            return property;
        }
        protected override JsonConverter ResolveContractConverter(Type objectType) //该函数可用于返回特定类型类型的JsonConverter
        {
            var type = base.ResolveContractConverter(objectType);

            //这里主要是为了忽略一些在class上定义了JsonConverter的情况,因为有些比如 A<T1,T2> 在序列化的时候,并无法知道ValueTuple定义的属性名,这里添加忽略是为了跳过已定义过的JsonConverter
            //如有需要,可在这里多添加几个
            if (type is ResultReturnConverter)
            {
                return null;
            }
            else
            {
                return type;
            }
        }
    }
}