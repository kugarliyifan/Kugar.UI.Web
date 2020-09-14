using System;
using System.Collections.Generic;
using System.Text;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Authentications;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Kugar.Core.Web.Helpers
{
    public static class ControllerHelper
    {
        //public static string JWTLogin(this ControllerBase controller, params (string key, string value)[] values)
        //{
        //    var authenticationType = controller.RouteData.Values.TryGetValue("authenticationType").ToStringEx();

        //    if (string.IsNullOrWhiteSpace(authenticationType))
        //    {
        //        authenticationType= controller.HttpContext.Items["SchemeName"].ToStringEx();
        //    }

        //    if (string.IsNullOrEmpty(authenticationType))
        //    {
        //        throw new ArgumentNullException("authenticationType为空");
        //    }

        //    var options =
        //        (OptionsManager<WebJWTOption>) controller.HttpContext.RequestServices.GetService(
        //            typeof(OptionsManager<WebJWTOption>));

        //    var option = options.Get(authenticationType);
        //}
    }
}
