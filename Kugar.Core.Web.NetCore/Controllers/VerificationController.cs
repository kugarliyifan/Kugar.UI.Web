using System;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.BaseStruct;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kugar.Core.Web.Controllers
{
    public class VerificationController:ControllerBase
    {
        [Route("WebCore/Verification/VerificationCode/{type}")]
        [Route("WebCore/Verification/VerificationCode")]
        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerificationCode(string type="")
        {
            var code = RandomEx.NextString(4);

            HttpContext.Session.SetString(string.IsNullOrEmpty(type)? "VerificationCode" :$"VerificationCode_{type}", code);

            var result = new ValidateCodeResult(code);

            return result;
        }
    }
}
