using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Kugar.Core.Web
{
    public class MvcHtmlString:HtmlString
    {
        private static readonly MvcHtmlString _default = new MvcHtmlString("");

        public MvcHtmlString(string value) : base(value)
        {
        }

        public static MvcHtmlString Create(string value)
        {
            return new MvcHtmlString(value);
        }

        public static MvcHtmlString Empty => _default;
    }

    public static class HelperResultExtMethod
    {
        public static async Task<string> ToHtmlStringAsync(this HelperResult helper)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                await helper.WriteAction(writer);

                return writer.ToString();
            }
        }

        public static string ToHtmlString(this HelperResult helper)
        {
            return helper.ToHtmlStringAsync().Result;
        }
    }
}
