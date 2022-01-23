using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flee.PublicTypes;
using Kugar.Core.ExtMethod;
using Kugar.Core.Log;
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
    public class PermissionCheckAttribute:Attribute, IAsyncActionFilter
    {
        private ExpressionContext _ruleContext = null;
        private VariableCollection _ruleVariables = null;
        private Lazy<IDynamicExpression> _express = null;
        private AsyncLocal<HashSet<string>> _currentActionContext = new AsyncLocal<HashSet<string>>();
        private string _codeOrExp = "";

        private PermissionCheckAttribute()
        {

            _express = new Lazy<IDynamicExpression>(lazyInitRule);
            //_ruleContext = new ExpressionContext();
            //_ruleContext.Options.CaseSensitive = false;
            ////_ruleContext.Options.

            //_ruleVariables = _ruleContext.Variables;

            //_ruleVariables.ResolveVariableType += resolveVariableType;

            //_ruleVariables.ResolveVariableValue += resolveVariableValue;

        }



        public PermissionCheckAttribute(params string[] codeOrExp):this()
        {
            _codeOrExp= codeOrExp.Select(x => x.Replace('.', '_')).JoinToString(" And ", "(", ")");
        }

        public string[] Codes { get; }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            //Debugger.Break();
            
            _currentActionContext.Value = context.HttpContext.GetPermissions();

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
            
            if (!checkPermission(context.HttpContext))
            {
                context.Result = build401Result(context);
                return;
            }
            
            await next();
        }

        //public void OnActionExecuting(ActionExecutingContext context)
        //{
        //    var service = (IUserPermissionFactoryService)context.HttpContext.RequestServices.GetService(typeof(IUserPermissionFactoryService));

        //    if (service==null)
        //    {
        //        context.Result = new ContentResult()
        //        {
        //            Content = "接口IUserPermissionFactoryService不存在",
        //            StatusCode = 500
        //        };

        //        return;
        //    }
            
        //    if (!checkPermission(context.HttpContext))
        //    {
        //        context.Result = build401Result(context);
        //        return;
        //    }
        //}

        //public void OnActionExecuted(ActionExecutedContext context)
        //{
        //    context.e
        //}

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

        private bool checkPermission(Microsoft.AspNetCore.Http.HttpContext context)
        {
            

            if (_codeOrExp=="")
            {
                _codeOrExp = "AllPermissionOK123465";
            }

            try
            {
                return (bool)_express.Value.Evaluate();
            }
            catch (Exception e)
            {
                Debugger.Break();
                return false;
            }
            

            var permissions = context.GetPermissions();

            if (Codes.HasData())
            {
                foreach (var code in Codes)
                {
                    if (!permissions.Contains(code))
                    {
                        return false;    
                    }
                    
                }
            }

            return true;
        }

        private void resolveVariableType(object sender, ResolveVariableTypeEventArgs e)
        {
            e.VariableType = typeof(bool);
        }

        private void resolveVariableValue(object sender, ResolveVariableValueEventArgs e)
        {
            //Debugger.Break();

            if (e.VariableName== "AllPermissionOK123465")
            {
                e.VariableValue=true;
            }

            if (_currentActionContext.Value.Contains(e.VariableName))
            {
                e.VariableValue = true;
            }
            else
            {
                e.VariableValue = false;
            }
            
        }

        private IDynamicExpression lazyInitRule()
        {
            //var exp = _codeOrExp.Select(x => x.Replace('.', '_')).JoinToString(" And ", "(", ")");

            _ruleContext = new ExpressionContext();
            //_ruleContext.Options.CaseSensitive = false;
            //_ruleContext.Options.

            _ruleVariables = _ruleContext.Variables;

            _ruleVariables.ResolveVariableType += resolveVariableType;

            _ruleVariables.ResolveVariableValue += resolveVariableValue;

            try
            {
                //_express = _ruleContext.CompileDynamic(_codeOrExp);

                return _ruleContext.CompileDynamic(_codeOrExp);

            }
            catch (Exception e)
            {
                Debugger.Break();
                Debugger.Log(0, "表达式解析失败", _codeOrExp);
                LoggerManager.Default.Error($"权限表达式解析失败:{_codeOrExp}", e);
                throw;
            }
        }
    }
    
}
