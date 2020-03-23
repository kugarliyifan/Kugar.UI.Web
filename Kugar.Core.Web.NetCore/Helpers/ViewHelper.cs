using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kugar.Core.Web.Helpers
{
    public static class ViewHelper
    {
        /// <summary>
        /// 获取ModelState中,指定field的错误提示字符串,如果未包含错误,返回false
        /// </summary>
        /// <param name="modelState"></param>
        /// <param name="key">字段名</param>
        /// <param name="errorMsg"></param>
        /// <returns>如果未包含错误,返回false</returns>
        public static bool TryGetModelStateErrorString(this ModelStateDictionary modelState, string key,out string[] errorMsg)
        {
            if (modelState.IsValid && modelState.TryGetValue(key, out var error))
            {
                if (error.Errors.Count>0)
                {
                    errorMsg = error.Errors.Where(x => !string.IsNullOrEmpty(x.ErrorMessage)).Select(x => x.ErrorMessage)
                        .ToArrayEx();
                    return true;
                }
                
            }
       
            errorMsg = null;
            return false;
            
        }

        /// <summary>
        /// 获取ModelState中,指定field的错误提示字符串,如果未包含错误,返回false
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key">字段名</param>
        /// <param name="errorMsg"></param>
        /// <returns>如果未包含错误,返回false</returns>
        public static bool TryGetModelStateErrorString(this ViewContext context, string key,
            out string[] errorMsg)
        {
            return TryGetModelStateErrorString(context.ModelState,key,out errorMsg);
        }
    }
}
