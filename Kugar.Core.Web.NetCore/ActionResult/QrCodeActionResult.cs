using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Kugar.Core.Web
{
    /// <summary>
    /// 输出二维码图片
    /// </summary>
    public class QrCodeActionResult: IActionResult
    {
        private string _qrCodeData = "";
        private ECCLevel _eccLevel = ECCLevel.M;
        private Bitmap _icon = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="qrCodeData">二维码数据内容</param>
        /// <param name="eccLevel"></param>
        /// <param name="icon">二维码中间的logo</param>
        public QrCodeActionResult(string qrCodeData, ECCLevel eccLevel = ECCLevel.M,Bitmap icon=null)
        {
            _qrCodeData = qrCodeData;
            _eccLevel = eccLevel;
            _icon = icon;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(_qrCodeData, (QRCodeGenerator.ECCLevel)(int)_eccLevel))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrCodeImage = qrCode.GetGraphic(20,Color.Black,Color.White,_icon))
            {
                var resp = context.HttpContext.Response;

                // 设置 HTTP Header
                resp.ContentType = "image/jpeg";
                resp.Clear();

                //var imgData = qrCodeImage.SaveToBytes(ImageFormat.Jpeg);

                using (MemoryStream memoryStream = new MemoryStream(1024))
                {
                    qrCodeImage.Save(memoryStream,ImageFormat.Jpeg);
                    
                    //ImageData.Save((Stream) memoryStream, Format);

                    await memoryStream.FlushAsync();

                    memoryStream.Position = 0;
                    
                    await resp.Body.WriteAsync(memoryStream.GetBuffer());
                    
                    resp.ContentLength = memoryStream.Length;
                }
                
                

                await resp.Body.FlushAsync();

                //resp.ContentLength = imgData.Length;

                //await resp.BodyWriter.WriteAsync()

                //await resp.Body.WriteAsync(imgData, 0, imgData.Length);
            }
        }

        public string QrCodeData => _qrCodeData;

        public ECCLevel ECCLevel => _eccLevel;
    }
}
