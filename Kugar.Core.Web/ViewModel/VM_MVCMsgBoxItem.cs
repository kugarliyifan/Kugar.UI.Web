using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kugar.Core.Web.ViewModel
{
    public class VM_MVCMsgBoxItem
    {
        public VM_MVCMsgBoxItem(string msg, string js)
        {
            Message = msg;
            JavsScript = js;
        }

        public string Message { set; get; }

        public string JavsScript { set; get; }
    }
}
