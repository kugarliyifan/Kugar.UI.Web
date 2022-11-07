using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Microsoft.AspNetCore.Http;

namespace Kugar.Core.Web.Authentications
{
    public interface IWebJWTLoginService
    {
        Task<ResultReturn<string>> Login(Microsoft.AspNetCore.Http.HttpContext context, string userName, string password,IEnumerable<KeyValuePair<string,string>> values=null, bool isNeedEncoding = false);
    }
}
