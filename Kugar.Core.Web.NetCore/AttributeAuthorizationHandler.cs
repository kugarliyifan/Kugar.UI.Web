using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;

#if NETCOREAPP2_1
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;    
#endif



namespace Kugar.Core.Web
{
#if NETCOREAPP2_1
    public abstract class AttributeAuthorizationHandler<TRequirement, TAttribute> : AuthorizationHandler<TRequirement> where TRequirement : IAuthorizationRequirement where TAttribute : Attribute
    {
        private static ConcurrentDictionary<MemberInfo, IEnumerable<Attribute>> _attributes = new ConcurrentDictionary<MemberInfo, IEnumerable<Attribute>>();

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement)
        {
            IEnumerable<Attribute> attributes = null;

            var action = (context.Resource as AuthorizationFilterContext)?.ActionDescriptor as ControllerActionDescriptor;

            if (action != null)
            {
                attributes = GetAttributes(action.MethodInfo);
            }

            return HandleRequirementAsync(context,context.Resource as AuthorizationFilterContext, requirement, attributes?.Select(x=>(TAttribute)x));
        }

        /// <summary>
        /// 用于进行权限判断
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filterContext"></param>
        /// <param name="requirement"></param>
        /// <param name="attributes">当前action的特性,从action开始往上一层一层枚举所有指定类型的特性</param>
        /// <returns></returns>
        protected abstract Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthorizationFilterContext filterContext, TRequirement requirement, IEnumerable<TAttribute> attributes);

        private IEnumerable<Attribute> GetAttributes(MemberInfo memberInfo)
        {
            return _attributes.GetOrAdd(memberInfo, getActionAttribute);
        }

        private IEnumerable<Attribute> getActionAttribute(MemberInfo member)
        {
            //var attr = typeof(TAttribute).GetCustomAttributes<AttributeUsageAttribute>(true).FirstOrDefault();
            var attributes = new List<Attribute>();
            
            attributes.AddRange(member.GetCustomAttributes<TAttribute>(true));  //优先读取函数的attribute
            attributes.AddRange(member.DeclaringType.GetCustomAttributes<TAttribute>(true));

            var type = member.DeclaringType.BaseType;

            while (type != typeof(object))
            {
                var tmp = type.GetCustomAttributes<TAttribute>(true);

                if (tmp.HasData())
                {
                    attributes.AddRange(tmp);
                }

                type = type.BaseType;
            }

            return attributes.Where(x => x != null).ToArrayEx();
        }
    }    
#endif

}
