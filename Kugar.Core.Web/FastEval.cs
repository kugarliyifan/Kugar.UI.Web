using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web
{
    public static class FastEvalExtensions
    {
        public static object FastEval(this Control control, object o, string propertyName)
        {
            return o.FastGetValue(propertyName);

            //return s_cache.GetAccessor(o.GetType(), propertyName).GetValue(o);
        }
        public static object FastEval(this TemplateControl control, string propertyName)
        {
            return control.FastEval(control.Page.GetDataItem(), propertyName);
        }
    }
}
