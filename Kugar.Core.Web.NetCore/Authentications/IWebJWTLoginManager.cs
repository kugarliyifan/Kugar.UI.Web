using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;

namespace Kugar.Core.Web.Authentications
{
    public interface IWebJWTLoginService
    {
        Task<ResultReturn<string>> Login(string userName, string password, bool isNeedEncoding = false);
    }
}
