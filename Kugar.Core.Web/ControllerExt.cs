using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web
{
    public static class ControllerExt
    {
        public static ActionResult Xml(this Controller srcCtrl, XmlDocument xmlDocument)
        {
            HttpResponseBase response = srcCtrl.HttpContext.Response;

            // 设置 HTTP Header 的 ContentType
            response.ContentType = "text/xml";

            var responseEncoding = srcCtrl.HttpContext.Response.ContentEncoding;

            if (responseEncoding == null)
            {
                responseEncoding = Encoding.UTF8;
            }

            return new ContentResult() { Content = xmlDocument.SaveToString(), ContentEncoding = responseEncoding, ContentType = "text/xml" };
        }

        public static ActionResult Xml(this Controller srcCtrl, object data)
        {
            return new XmlActionResult(data);
        }

        public static ActionResult Image(this Controller srcCtrl, Image img, ImageFormat format = null)
        {
            return new ImageResult(img, img.RawFormat);
        }

        public static ActionResult Image(this Controller srcCtrl, string imagePath)
        {
            var img = System.Drawing.Image.FromFile(imagePath);

            return Image(srcCtrl, img, img.RawFormat);
        }

        public static ActionResult NewtonJson(this Controller srcCtrl, object data)
        {
            return new NewtonsoftJsonResult(data);
        }

        public static ActionResult ZipFile(this Controller srcCtrl, KeyValuePair<string, Stream>[] files, string defaultFileName = ""
            )
        {
            if (!files.HasData())
            {
                throw new ArgumentOutOfRangeException("zipFile");
            }

            if (defaultFileName == "")
            {
                defaultFileName = "压缩文件" + DateTime.Now.ToString("yyyyMMddHHmm");
            }

            return new ZipFileActionResult(defaultFileName, files);
        }

        public static ActionResult ZipFile(this Controller srcCtrl, KeyValuePair<string, byte[]>[] files, string defaultFileName = ""
            )
        {
            if (!files.HasData())
            {
                throw new ArgumentOutOfRangeException("zipFile");
            }

            if (defaultFileName == "")
            {
                defaultFileName = "压缩文件" + DateTime.Now.ToString("yyyyMMddHHmm");
            }

            return new ZipFileActionResult(defaultFileName, files);
        }
    }
}
