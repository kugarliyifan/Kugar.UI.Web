using System;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.Web.ActionResult;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kugar.Core.Web.Filters
{
    /// <summary>
    /// 当出现校验错误时,自动返回json
    /// </summary>
    public class ModelValidationFilterAttribute:ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new ModelStateValidActionResult();
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
