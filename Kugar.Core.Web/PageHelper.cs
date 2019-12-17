using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;

namespace Kugar.Core.Web
{
    public static class PageHelper
    {
        public static void MsgBox(this Page page, string msg,string gotoUrl=null)
        {
            const string containUrl = "alert('{0}');this.location.href='{1}'";
            const string noContainUrl = "alert('{0}');";

            page.Page.ClientScript.RegisterClientScriptBlock(page.GetType(), "s" + Guid.NewGuid().ToString(), string.Format(string.IsNullOrWhiteSpace(gotoUrl)?noContainUrl:containUrl, msg, gotoUrl), true);
        }
    }
}
