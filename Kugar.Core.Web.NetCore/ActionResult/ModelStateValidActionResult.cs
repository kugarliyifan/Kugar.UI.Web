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
    /// <summary>
    /// 用于格式化输出ModelState中的错误信息
    /// </summary>
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

                    await writer.WriteNullAsync();

                    await writer.WritePropertyNameAsync("error");

                    await writer.WriteStartObjectAsync();

                        await writer.WritePropertyNameAsync("isValid", context.ModelState.IsValid);

                        await writer.WritePropertyNameAsync("errors");

                        var converter = new ModelStateJsonConverter();

                        converter.WriteJson(writer, context.ModelState, JsonSerializer.CreateDefault());

                    await writer.WriteEndObjectAsync();


                await writer.WriteEndObjectAsync( );

                await writer.FlushAsync();
            }
        }
    }
}
