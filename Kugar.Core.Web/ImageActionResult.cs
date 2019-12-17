using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web;
using System.Web.Mvc;

namespace Kugar.Core.Web
{
    public class ImageResult : ActionResult
    {
        private byte[] _imageData = null;

        // 图片
        public Image ImageData{ set; get; }

        public ImageFormat Format { set; get; }

        /// <summary>
        /// 使用后自动释放图像对象
        /// </summary>
        public bool AutoDisposeImage { set; get; }

        // 构造器
        public ImageResult(Image image,ImageFormat format=null,bool autoDispose=true)
        {
            if (image==null)
            {
                throw new ArgumentNullException("image","输入图像不能为空");
            }

            AutoDisposeImage = autoDispose;

            ImageData = image;

            if (format == null)
            {
                Format = ImageFormat.Jpeg;
            }
            else
            {
                Format = format;
            }

            
        }

        /// <summary>
        /// 直接输出直接格式的数据,不经过转码
        /// </summary>
        /// <param name="data"></param>
        public ImageResult(byte[] data)
        {
            _imageData = data;
        }

        // 主要需要重写的方法
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            HttpResponseBase response = context.HttpContext.Response;

            // 设置 HTTP Header
            response.ContentType = "image/jpeg";

            if (_imageData==null)
            {
                // 将图片数据写入Response
                ImageData.Save(context.HttpContext.Response.OutputStream, Format);

                if (AutoDisposeImage)
                {
                    ImageData.Dispose();
                }
                
            }
            else
            {
                
            }
            
        }
    }
}
