using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#if NETCOREAPP3_0
    

#endif

namespace Kugar.Core.Web
{
    /// <summary>
    /// 输出一个图片格式的ActionResult
    /// </summary>
    public class ImageResult : IActionResult
    {
        private byte[] _imageData = null;

        /// <summary>
        /// 直接输出Bitmap格式
        /// </summary>
        /// <param name="image">图片数据</param>
        /// <param name="format">输出的文件格式,默认为jpg</param>
        /// <param name="autoDispose">输出完成后,是否自动释放掉bitmap,默认为true</param>
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
        
        /// <summary>
        ///  图片数据
        /// </summary>
        public Image ImageData { set; get; }

        /// <summary>
        /// 输出的图片格式
        /// </summary>
        public ImageFormat Format { set; get; }

        /// <summary>
        /// 使用后自动释放图像对象
        /// </summary>
        public bool AutoDisposeImage { set; get; }
        

        
        // 主要需要重写的方法
        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var resp = context.HttpContext.Response;
            resp.Clear();
            // 设置 HTTP Header
            resp.ContentType = "image/" + ImageFormat.Jpeg;
            

            

            if (_imageData == null)
            {
                using (MemoryStream memoryStream = new MemoryStream(1024))
                {
                    ImageData.Save((Stream) memoryStream, Format);

                    await memoryStream.FlushAsync();

                    memoryStream.Position = 0;
                    
                    await resp.Body.WriteAsync(memoryStream.GetBuffer());
                    
                    resp.ContentLength = memoryStream.Length;
                }
                
                //var imgData = ImageData.SaveToBytes(ImageFormat.Jpeg);

                ////resp.ContentLength = imgData.Length;
                 
                ////ImageData.Save(resp.Body,ImageFormat.Jpeg);
                
                //ImageData.Save(resp.Body,Format);
                
                //await resp.Body.WriteAsync(imgData,0,imgData.Length);
                

                // 将图片数据写入Response
                //ImageData.Save(resp.Body, Format);

                if (AutoDisposeImage)
                {
                    ImageData.Dispose();
                }

                await resp.Body.FlushAsync();
            }
            else
            {
                resp.ContentLength = _imageData.Length;

                await resp.Body.WriteAsync(_imageData, 0, _imageData.Length);
            }
        }
    }
}
