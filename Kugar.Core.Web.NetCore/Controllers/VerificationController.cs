using System;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.BaseStruct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kugar.Core.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class VerificationController:ControllerBase
    {
        private static char[] _randomCoodes = "2346789ABCDEFGHJKLMNPRTUVWXYZ".ToCharArray();

        /// <summary>
        /// 获取一个校验码图片信息
        /// </summary>
        /// <param name="type">图片类型</param>
        /// <returns>返回图片数据流</returns>
        [Route("WebCore/Verification/VerificationCode/{type}")]
        [Route("WebCore/Verification/VerificationCode")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerificationCode(string type="")
        {
            var code = RandomEx.NextString(_randomCoodes,4);

            HttpContext.Session.SetString(string.IsNullOrEmpty(type)? "VerificationCode" :$"VerificationCode_{type}", code);

            var result = new ValidateCodeResult(code);

            return result;
        }
    }
}
