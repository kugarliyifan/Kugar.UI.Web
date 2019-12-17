using System;
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

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ActionNamesAttribute : ActionMethodSelectorAttribute
    {
        public ActionNamesAttribute(params string[] names)
        {
            if (!names.HasData())
            {
                throw new ArgumentException("ActionNames cannot be empty or null", "names");
            }
            this.Names = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("ActionNames cannot be empty or null", "names");
                }

                this.Names.Add(name);
            }
        }

        private HashSet<string> Names { get; }


        public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        {
            var actionName = routeContext.RouteData.DataTokens.TryGetValue("action", "");
            
            return this.Names.Contains(actionName);
        }
    }
}
