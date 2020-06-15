using System;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Kugar.Core.Web
{
    public class NewtonsoftJsonResult : IActionResult
    {
        private Encoding _encoding = null;
        private object _data = null;

        public NewtonsoftJsonResult(object data, Encoding encoding)
        {
            _data = data;
            _encoding = encoding;
        }
        

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var response = context.HttpContext.Response;
            response.Clear();

            // 设置 HTTP Header 的 ContentType
            response.ContentType = "application/json";
            
            if (_data != null)
            {
                await response.WriteAsync(JsonConvert.SerializeObject(_data), _encoding);
            }
            else
            {
                await response.WriteAsync("{}");
            }
        }
    }
}
