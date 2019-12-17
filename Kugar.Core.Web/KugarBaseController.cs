using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Kugar.Core.Configuration;
using Kugar.Core.Web.ViewModel;

namespace Kugar.Core.Web
{
    /// <summary>
    /// 已过期,请使用WebUIController
    /// </summary>
    [Obsolete]
    public abstract class KugarBaseController:Controller
    {
        private Lazy<List<VM_MVCMsgBoxItem>> _msgList = new Lazy<List<VM_MVCMsgBoxItem>>(() => { return new List<VM_MVCMsgBoxItem>(3); });

        private static string _mainUrl { set; get; }

        protected KugarBaseController()
        {
            _mainUrl = CustomConfigManager.Default.AppSettings.GetValueByName<string>("MainUrl","");
        }

        public string MainUrl
        {
            get { return _mainUrl; }
        }

        public void MsgBox(string text,string script)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            List<VM_MVCMsgBoxItem> msgList = null;

            if (!_msgList.IsValueCreated)
            {
                TempData["MsgData_Temp"] = _msgList.Value;
            }

            msgList = _msgList.Value;

            msgList.Add(new VM_MVCMsgBoxItem(text, script));

        }


        protected void SetSeo(string title, string metaDesc = "", string keyword = "")
        {
            ViewBag.Title = title;
            ViewBag.MetaDesc = metaDesc;
            ViewBag.Keyword = keyword;
        }
    }
}
