using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Kugar.Core.Web.Convention
{
    public class AuthorizeControllerConvention : IControllerModelConvention
    {
        private HashSet<Type> _controllers = null;
        private AuthorizeFilter[] _authorizeFilters = Array.Empty<AuthorizeFilter>();

        public AuthorizeControllerConvention(Type[] controllers,string[] authorizeScheme)
        {
            _controllers = new HashSet<Type>(controllers);
             
            var policy = new AuthorizationPolicyBuilder();

            policy.AddAuthenticationSchemes(authorizeScheme);

            _authorizeFilters = new AuthorizeFilter[]
            {
                new AuthorizeFilter(policy.Build())
            };
        }

        public AuthorizeControllerConvention(Type[] controllers,params AuthorizeFilter[] filters)
        {
            _controllers = new HashSet<Type>(controllers);
            _authorizeFilters = filters;
        }

        public void Apply(ControllerModel controller)
        {
            if (_controllers.Contains(controller.ControllerType.DeclaringType))
            {
                return;
            } 

            foreach (var filter in _authorizeFilters)
            {
                controller.Filters.Add(filter);
            }
        }
    }
}