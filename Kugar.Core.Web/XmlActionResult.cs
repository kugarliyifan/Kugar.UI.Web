using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;

namespace Kugar.Core.Web
{
    public class XmlActionResult:ActionResult
    {
        // 可被序列化的内容
        object Data { get; set; }

        // Data的类型
        Type DataType { get; set; }

        // 构造器
        public XmlActionResult(object data)
        {
            Data = data;
            DataType = data.GetType();
        }

        // 主要是重写这个方法
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpResponseBase response = context.HttpContext.Response;

            // 设置 HTTP Header 的 ContentType
            response.ContentType = "text/xml";

            if (Data != null)
            {
                // 序列化 Data 并写入 Response
                XmlSerializer serializer = new XmlSerializer(DataType);

                using (MemoryStream ms = new MemoryStream())
                {
                    serializer.Serialize(ms, Data);

                    var responseEncoding = context.HttpContext.Response.ContentEncoding;

                    if (responseEncoding==null)
                    {
                        responseEncoding = Encoding.UTF8;
                    }

                    response.Write(responseEncoding.GetString(ms.ToArray()));                    
                }
            }
        }
    }
}
