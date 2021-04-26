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

        public override string Message => "数据校验错误,请查看errors属性";

        public override string ToString()
        {
            return "数据校验错误,请查看error.errors属性";
        }
    }

    public class ModelStateErrorExceptionJsonConverter : JsonConverter<ModelStateErrorException>
    {
        public override void WriteJson(JsonWriter writer, ModelStateErrorException value, JsonSerializer serializer)
        {
            writer.WriteStartObjectAsync();

            writer.WritePropertyAsync("isValid", value.ModelState.IsValid);

            writer.WritePropertyNameAsync("errors");

            if (!value.ModelState.IsValid)
            {
                serializer.Converters.Add(new ModelStateJsonConverter());
                serializer.Serialize(writer,value.ModelState);
            }
            else
            {
                writer.WriteNullAsync();
            }

            writer.WriteEndObjectAsync();

            
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
            writer.WriteStartObjectAsync();
            
            foreach (var modelStateKey in value.Keys)
            {
                if (string.IsNullOrWhiteSpace(modelStateKey))
                {
                    continue;
                }

                if (value.TryGetValue(modelStateKey, out var error))
                {
                    writer.WritePropertyNameAsync(modelStateKey);

                    writer.WriteStartArrayAsync();

                    if (error.Errors.HasData())
                    {
                        foreach (var item in error.Errors)
                        {
                            writer.WriteValueAsync(string.IsNullOrWhiteSpace(item.ErrorMessage)?item.Exception.Message:item.ErrorMessage);
                        }
                    }

                    writer.WriteEndArrayAsync();
                }
            }

            writer.WriteEndObjectAsync();
        }


        public override ModelStateDictionary ReadJson(JsonReader reader, Type objectType, ModelStateDictionary existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
