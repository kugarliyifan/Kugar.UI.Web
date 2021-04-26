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
        Task<ResultReturn<string>> Login(Microsoft.AspNetCore.Http.HttpContext context, string userName, string password, bool isNeedEncoding = false);
    }

    public interface IUserPermissionFactoryService
    {
        Task<IReadOnlyList<string>> GetUserPermissions(Microsoft.AspNetCore.Http.HttpContext context, string userID);
    }
}
