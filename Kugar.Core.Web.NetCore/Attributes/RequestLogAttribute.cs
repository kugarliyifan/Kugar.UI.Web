using System;
using System.Collections.Generic;
using System.Text;

namespace Kugar.Core.Web.Attributes
{
    public class RequestLogAttribute:Attribute
    {
        public RequestLogAttribute(string tag="")
        {
            this.Tag = tag;
        }

        public string Tag { set; get; }
    }
}
