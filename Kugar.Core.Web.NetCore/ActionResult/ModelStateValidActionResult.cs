using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Kugar.Core.Web.ActionResult
{
    public class ModelStateValidActionResult:IActionResult
    {
        private int _returnCode;

        public ModelStateValidActionResult(int returnCode = 10001)
        {
            _returnCode = returnCode;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var stream = context.HttpContext.Response.Body;
            context.HttpContext.Response.ContentType = "application/json";

            using (var tw=new StreamWriter(stream,Encoding.UTF8))
            using (var writer=new JsonTextWriter(tw))
            {
                await writer.WriteStartObjectAsync();

                    await writer.WriteProperty("isSuccess", false)
                        .WriteProperty("returnCode", _returnCode)
                        .WriteProperty("message","数据校验错误")
                        .WritePropertyNameAsync("returnData")
                        ;

                    writer.WriteNull();

                    writer.WritePropertyName("error");

                    writer.WriteStartObject();

                        writer.WriteProperty("isValid", context.ModelState.IsValid);

                        writer.WritePropertyName("errors");

                        var converter = new ModelStateJsonConverter();

                        converter.WriteJson(writer, context.ModelState, JsonSerializer.CreateDefault());

                    writer.WriteEndObject();

        

                await writer.WriteEndObjectAsync( );

                await writer.FlushAsync();
            }
        }
    }
}
