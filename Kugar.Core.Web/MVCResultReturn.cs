using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Mvc;
using Kugar.Core.BaseStruct;

namespace Kugar.Core.Web
{
    public class MVCResultReturn<T> : ActionResult where T:class 
    {
        private ResultReturn _baseReturn = null; 

        public MVCResultReturn(ResultReturn<T> baseReturn)
        {
            _baseReturn = baseReturn;
        }

        public MVCResultReturn(ResultReturn baseReturn)
        {
            _baseReturn = baseReturn;
        }

        public MVCResultReturn(T baseData)
        {
            _baseReturn=new ResultReturn(baseData==null,baseData);
        }

        public MVCResultReturn(object baseData)
        {
            _baseReturn = new ResultReturn(baseData == null, baseData);
        }

        public ResultReturn Data { get { return _baseReturn; } }

        public override void ExecuteResult(ControllerContext context)
        {
            var result = new NewtonsoftJsonResult(_baseReturn);

            result.ExecuteResult(context);
        }

    }

    


}
