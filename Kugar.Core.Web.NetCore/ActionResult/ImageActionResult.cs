using System;
using System.Drawing;
using System.Drawing.Imaging;

using System.Threading.Tasks;
using System.Web;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#if NETCOREAPP3_0
    

#endif

namespace Kugar.Core.Web
{
    public class ImageResult : IActionResult
    {
        private byte[] _imageData = null;

        // 图片
        public Image ImageData { set; get; }

        public ImageFormat Format { set; get; }

        /// <summary>
        /// 使用后自动释放图像对象
        /// </summary>
        public bool AutoDisposeImage { set; get; }

        // 构造器
        public ImageResult(Image image, ImageFormat format = null, bool autoDispose = true)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image", "输入图像不能为空");
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
        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var resp = context.HttpContext.Response;

            // 设置 HTTP Header
            resp.ContentType = "image/jpeg";
            resp.Clear();

            

            if (_imageData == null)
            {
                var imgData = ImageData.SaveToBytes(ImageFormat.Jpeg);

                resp.ContentLength = imgData.Length;

                await resp.Body.WriteAsync(imgData,0,imgData.Length);
                

                // 将图片数据写入Response
                //ImageData.Save(resp.Body, Format);

                if (AutoDisposeImage)
                {
                    ImageData.Dispose();
                }
            }
            else
            {
                resp.ContentLength = _imageData.Length;

                await resp.Body.WriteAsync(_imageData, 0, _imageData.Length);
            }
        }
    }
}
