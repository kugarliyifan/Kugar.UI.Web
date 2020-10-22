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
        Task<ResultReturn<string>> Login(Microsoft.AspNetCore.Http.HttpContext request, string userName, string password, bool isNeedEncoding = false);
    }
}
