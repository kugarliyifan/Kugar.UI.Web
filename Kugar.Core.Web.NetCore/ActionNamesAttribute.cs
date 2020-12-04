﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Kugar.Core.ExtMethod;

#if NETCOREAPP3_0
   
#endif

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;

namespace Kugar.Core.Web
{
    /// <summary>
    /// 允许一个Action有多个ActionName
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ActionNamesAttribute : ActionMethodSelectorAttribute
    {
        private HashSet<string> _names=new HashSet<string>(StringComparer.CurrentCultureIgnoreCase); 

        public ActionNamesAttribute(params string[] names)
        {
            if (!names.HasData())
            {
                throw new ArgumentOutOfRangeException(nameof(names));
                
            } 

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentNullException(nameof(names));
                    
                }

                _names.Add(name);
            }
        }

        //private HashSet<string> Names { get; }


        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            var actionName = routeContext.RouteData.DataTokens.TryGetValue("action", "");
            
            return _names.Contains(actionName);
        }
    }
}
