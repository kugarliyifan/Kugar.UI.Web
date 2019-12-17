using System;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace Kugar.Core.Web
{
    public class NewtonsoftJsonResult:ActionResult
    {
        public NewtonsoftJsonResult(object data)
        {
            Data = data;
        }

        // 可被序列化的内容
        object Data { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpResponseBase response = context.HttpContext.Response;

            // 设置 HTTP Header 的 ContentType
            response.ContentType = "application/json";

            response.Clear();

            if (Data != null)
            {
                var responseEncoding = context.HttpContext.Response.ContentEncoding;

                if (responseEncoding == null)
                {
                    responseEncoding = Encoding.UTF8;
                }

                response.Write(JsonConvert.SerializeObject(Data));
            }
            else
            {
                response.Write("{}");
            }
        }
    }
}
