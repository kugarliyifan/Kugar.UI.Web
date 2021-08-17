using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Authentications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Kugar.Core.Web.Filters
{
    /// <summary>
    /// 检查是否包含指定的权限项,必须在start.cs里注入IUserPermissionFactoryService接口以及使用AddWebJWT之类的授权
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,AllowMultiple = true)]
    public class PermissionCheckAttribute:Attribute, IAsyncActionFilter,IActionFilter
    {
        public PermissionCheckAttribute(params string[] codes)
        {
            Codes = codes;
        }

        public string[] Codes { get; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var service = (IUserPermissionFactoryService)context.HttpContext.RequestServices.GetService(typeof(IUserPermissionFactoryService));

            if (service==null)
            {
                context.Result = new ContentResult()
                {
                    Content = "接口IUserPermissionFactoryService不存在",
                    StatusCode = 500
                };

                return;
            }

            var permissions = context.HttpContext.GetPermissions();

            if (Codes.HasData())
            {
                foreach (var code in Codes)
                {
                    if (!permissions.Contains(code))
                    {
                        context.Result = build401Result(context);
                        return;
                    }
                }
            }
            

            await next();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var service = (IUserPermissionFactoryService)context.HttpContext.RequestServices.GetService(typeof(IUserPermissionFactoryService));

            if (service==null)
            {
                context.Result = new ContentResult()
                {
                    Content = "接口IUserPermissionFactoryService不存在",
                    StatusCode = 500
                };

                return;
            }

            var permissions = context.HttpContext.GetPermissions();

            if (Codes.HasData())
            {
                foreach (var code in Codes)
                {
                    if (!permissions.Contains(code))
                    {
                        context.Result = build401Result(context);
                        return;
                    }
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {

        }

        private IActionResult build401Result(ActionExecutingContext context)
        {
            var t = (OptionsManager<WebJWTOption>)context.HttpContext.RequestServices.GetService(
                typeof(OptionsManager<WebJWTOption>));

            if (context.HttpContext.Items.TryGetValue("SchemeName", out var authName))
            {
                var tmpOpt = t.Get(authName.ToStringEx());

                if (!string.IsNullOrWhiteSpace(tmpOpt.LoginUrl))
                {
                    return new RedirectResult(tmpOpt.LoginUrl);
                }
                else
                {
                    return new UnauthorizedResult();
                }
            }
            else
            {
                return new ContentResult()
                {
                    Content = "不存在授权",
                    StatusCode = 500
                };
            }
        }
    }
}
