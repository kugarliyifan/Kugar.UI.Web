using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Kugar.Core.Web
{
    public class QrCodeActionResult: IActionResult
    {
        private string _qrCodeData = "";
        private ECCLevel _eccLevel = ECCLevel.M;

        public QrCodeActionResult(string qrCodeData, ECCLevel eccLevel = ECCLevel.M)
        {
            _qrCodeData = qrCodeData;
            _eccLevel = eccLevel;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(_qrCodeData, (QRCodeGenerator.ECCLevel)(int)_eccLevel))
            using (QRCode qrCode = new QRCode(qrCodeData))
            using (Bitmap qrCodeImage = qrCode.GetGraphic(20))
            {
                var resp = context.HttpContext.Response;

                // 设置 HTTP Header
                resp.ContentType = "image/jpeg";
                resp.Clear();

                var imgData = qrCodeImage.SaveToBytes(ImageFormat.Jpeg);

                resp.ContentLength = imgData.Length;

                await resp.Body.WriteAsync(imgData, 0, imgData.Length);
            }
        }

        public string QrCodeData => _qrCodeData;

        public ECCLevel ECCLevel => _eccLevel;
    }
}
