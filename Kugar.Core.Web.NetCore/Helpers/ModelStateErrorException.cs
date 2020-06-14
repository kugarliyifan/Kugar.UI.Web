using System;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Kugar.Core.Web.Helpers
{
    /// <summary>
    /// 用于在webapi的时候,特性上加入数据校验之后,,可将数据校验后的结果当做exception返回给客户端,配合ResultReturn 使用
    /// </summary>
    [JsonConverter(typeof(ModelStateErrorExceptionJsonConverter))]
    public class ModelStateErrorException : Exception
    {
        public ModelStateErrorException(ModelStateDictionary modelState)
        {
            ModelState = modelState;
        }

        public ModelStateDictionary ModelState { get; }

    }

    public class ModelStateErrorExceptionJsonConverter : JsonConverter<ModelStateErrorException>
    {
        public override void WriteJson(JsonWriter writer, ModelStateErrorException value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WriteProperty("isValid", value.ModelState.IsValid);

            writer.WritePropertyName("errors");

            if (!value.ModelState.IsValid)
            {
                serializer.Converters.Add(new ModelStateJsonConverter());
                serializer.Serialize(writer,value.ModelState);
            }
            else
            {
                writer.WriteNull();
            }

            writer.WriteEndObject();

            
        }

        public override ModelStateErrorException ReadJson(JsonReader reader, Type objectType, ModelStateErrorException existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class ModelStateJsonConverter : JsonConverter<ModelStateDictionary>
    {
        public override void WriteJson(JsonWriter writer, ModelStateDictionary value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            //foreach (var item in value)
            //{
            //    if (string.IsNullOrWhiteSpace(item.Key))
            //    {
            //        continue;
            //    }

            //    writer.WritePropertyName(item.Key);

            //    writer.WriteStartArray();

            //    if (value.ErrorCount>0)
            //    {
            //        foreach (var item in error.Errors)
            //        {
            //            writer.WriteValue(item.ErrorMessage);
            //        }
            //    }

            //    writer.WriteEndArray();
            //}

            foreach (var modelStateKey in value.Keys)
            {
                if (string.IsNullOrWhiteSpace(modelStateKey))
                {
                    continue;
                }

                if (value.TryGetValue(modelStateKey, out var error))
                {
                    writer.WritePropertyName(modelStateKey);

                    writer.WriteStartArray();

                    if (error.Errors.HasData())
                    {
                        foreach (var item in error.Errors)
                        {
                            writer.WriteValue(string.IsNullOrWhiteSpace(item.ErrorMessage)?item.Exception.Message:item.ErrorMessage);
                        }
                    }

                    writer.WriteEndArray();
                }
            }

            writer.WriteEndObject();
        }

        public override ModelStateDictionary ReadJson(JsonReader reader, Type objectType, ModelStateDictionary existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
