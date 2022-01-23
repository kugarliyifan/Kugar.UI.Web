using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Kugar.Core.Web.ActionResult
{
    /// <summary>
    /// 用于格式化输出ModelState中的错误信息
    /// </summary>
    public class ModelStateValidActionResult : IActionResult
    {
        private int _returnCode;
        private ModelStateJsonConverter _converter = new ModelStateJsonConverter();
        private static JsonSerializerSettings _defaultJsonSerializerSettings = new JsonSerializerSettings();

        public ModelStateValidActionResult(int returnCode = 10001)
        {
            _returnCode = returnCode;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var stream = context.HttpContext.Response.Body;
            context.HttpContext.Response.ContentType = "application/json";

            var cancelToken = context.HttpContext.RequestAborted;

            using (var tw = new StreamWriter(stream, Encoding.UTF8))
            using (var writer = new JsonTextWriter(tw))
            {
                await writer.WriteStartObjectAsync(cancelToken); //start resultreturn

                    await writer.WritePropertyAsync("isSuccess", false);
                    await writer.WritePropertyAsync("returnCode", _returnCode);
                    await writer.WritePropertyAsync("message", "数据校验错误");
                    await writer.WritePropertyNameAsync("returnData",cancelToken);

                    await writer.WriteNullAsync(cancelToken);

                    await writer.WritePropertyNameAsync("error",cancelToken);  //start error

                    await writer.WriteStartObjectAsync(cancelToken);

                        await writer.WritePropertyAsync("isValid", context.ModelState.IsValid);

                        await writer.WritePropertyNameAsync("errors",cancelToken); //property errors

#if NETCOREAPP3_0 || NETCOREAPP3_1  || NET5_0 || NET6_0
                        var opt =
                            ((IOptions<MvcNewtonsoftJsonOptions>)context.HttpContext.RequestServices.GetService(
                                typeof(IOptions<MvcNewtonsoftJsonOptions>)))?.Value?.SerializerSettings?? JsonConvert.DefaultSettings?.Invoke() ?? _defaultJsonSerializerSettings;
#else
                var opt = JsonConvert.DefaultSettings?.Invoke() ?? _defaultJsonSerializerSettings;
#endif

                        _converter.WriteJson(writer, context.ModelState, JsonSerializer.Create(opt));

                    await writer.WriteEndObjectAsync(cancelToken);  //end error


                await writer.WriteEndObjectAsync(cancelToken); //end resultreturn

                await writer.FlushAsync(cancelToken);
            }
        }
    }
}
