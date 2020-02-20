using System;
using System.Runtime.CompilerServices;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kugar.Core.Web.Converters
{
    public class ValueTupleConverter : JsonConverter
    {
        private string[] _tupleNames = null;
        private NamingStrategy _strategy = null;

        //也可以直接在这里传入特性
        public ValueTupleConverter(TupleElementNamesAttribute tupleNames, NamingStrategy strategy = null) 
        {
            _tupleNames = tupleNames.TransformNames.ToArrayEx();
            _strategy = strategy;
        }

        //这里在构造函数里把需要序列化的属性或函数返回类型的names传进来
        public ValueTupleConverter(string[] tupleNames, NamingStrategy strategy = null)  
        {
            _tupleNames = tupleNames;
            _strategy = strategy;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null && value is ITuple v)
            {
                writer.WriteStartObject();
                for (int i = 0; i < v.Length; i++)
                {
                    var pname = _tupleNames[i];

                    //根据规则,设置属性名
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
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //只需要实现序列化,,不需要反序列化,因为只管输出,所以,这个写不写无所谓
            throw new NotImplementedException();  
        }
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsValueTuple();
        }
    }
}