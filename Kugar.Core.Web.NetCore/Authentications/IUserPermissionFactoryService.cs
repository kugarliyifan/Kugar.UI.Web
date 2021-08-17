using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kugar.Core.Web.Authentications
{
    public interface IUserPermissionFactoryService
    {
        Task<IReadOnlyList<string>> GetUserPermissions(Microsoft.AspNetCore.Http.HttpContext context, string userID);
    }
}