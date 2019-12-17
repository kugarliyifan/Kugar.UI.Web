using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Kugar.Core.Web
{
    public static class ControllerExt
    {
        /// <summary>
        /// 输出返回一个图像的结果到客户端
        /// </summary>
        /// <param name="srcCtrl"></param>
        /// <param name="img"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static IActionResult Image(this Controller srcCtrl, Image img, ImageFormat format = null)
        {
            return new ImageResult(img, img.RawFormat);
        }

        /// <summary>
        /// 输出返回一个图像的结果到客户端
        /// </summary>
        /// <param name="srcCtrl"></param>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static IActionResult Image(this Controller srcCtrl, string imagePath)
        {
            var img = System.Drawing.Image.FromFile(imagePath);

            return Image(srcCtrl, img, img.RawFormat);
        }

        /// <summary>
        /// 调用newtonsoft.json输出json格式的数据
        /// </summary>
        /// <param name="srcCtrl"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IActionResult NewtonJson(this Controller srcCtrl, object data,Encoding encoding=null)
        {
            if (encoding==null)
            {
                encoding=Encoding.UTF8;
            }

            return new NewtonsoftJsonResult(data, encoding);
        }
    }

    public static class ClaimsPrincipalExtMethod
    {
        public static ClaimsPrincipal AddClaim(this ClaimsPrincipal principal, params Claim[] item)
        {
            var indent= principal.Identities.FirstOrDefault(x => x is CompositionIdentitie);

            if (indent==null)
            {
                indent=new CompositionIdentitie();

                principal.AddIdentity(indent);
            }

            foreach (var claim in item)
            {
                indent.AddClaim(claim);
            }

            return principal;
        }

        public static ClaimsPrincipal AddClaim(this ClaimsPrincipal principal, string type, string value,string valueType="")
        {
            var indent = principal.Identities.FirstOrDefault(x => x is CompositionIdentitie);

            if (indent == null)
            {
                indent = new CompositionIdentitie();

                principal.AddIdentity(indent);
            }

            indent.AddClaim(new Claim(type,value, valueType));

            return principal;
        }

        public class CompositionIdentitie: ClaimsIdentity
        {
            public override string Name => "CompositionIdentitie";
        }
    }
}
