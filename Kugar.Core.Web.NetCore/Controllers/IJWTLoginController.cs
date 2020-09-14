using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Authentications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


#if NETCOREAPP3_0 || NETCOREAPP3_1
namespace Kugar.Core.Web.Controllers
{


    /// <summary>
    /// 使用JWT作为身份验证的接口,用于快速生成Token或者写入cookie,请配合AddWebJWT扩展方法使用
    /// </summary>
    /// <typeparam name="TUserID"></typeparam>
    public interface IJWTLoginControlle<TUserID>
    {

        public async Task LoginJWTWithCookie(string userID, string password)
        {
            var optManager = (OptionsManager<WebJWTOption>)Request.HttpContext.RequestServices.GetService(typeof(OptionsManager<WebJWTOption>));

            var option = optManager.Get(CurrentSchemeName);

            var token = await BuildJWtToken(userID, password,option);

            var cookieName = string.IsNullOrEmpty(option.Cookie?.Name)
                ? $"jwt.{CurrentSchemeName}"
                : option.Cookie.Name;

            Response.Cookies.Append(cookieName, token, option.Cookie?.Build(Request.HttpContext));
        }


        public async Task LoginJWTWithCookie(string userID, string password,CookieBuilder cookieBuilder)
        {
            var token =await BuildJWtToken(userID, password);

            Response.Cookies.Append(cookieBuilder.Name,token,cookieBuilder?.Build(Request.HttpContext));
        }

        public Task<string> BuildJWtToken(string userID, string password)
        {
            var optManager = (OptionsManager<WebJWTOption>)Request.HttpContext.RequestServices.GetService(typeof(OptionsManager<WebJWTOption>));

            //var provider = (IAuthenticationSchemeProvider)Request.HttpContext.RequestServices.GetService(typeof(IAuthenticationSchemeProvider));

            //var scheme = provider.GetAllSchemesAsync().Result;

            var option = optManager.Get(CurrentSchemeName);

            return BuildJWtToken(userID, password, option);
        }

        public Task<string> BuildJWtToken(string userID, string password, WebJWTOption option)
        {
             var tokenHandler = new JwtSecurityTokenHandler();

            var authTime = DateTime.UtcNow;
            var expiresAt = authTime.Add(option.ExpireTimeSpan);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("aud", option.Audience),
                    new Claim("iss", option.Issuer),
                    new Claim("k",password.DesEncrypt(option.TokenEncKey.Left(8))),
                    new Claim(ClaimTypes.NameIdentifier,userID)
                }),
                Expires = expiresAt,
                SigningCredentials =
                    new SigningCredentials(new SymmetricSecurityKey(option.ActualEncKey), SecurityAlgorithms.HmacSha256Signature),
                EncryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(option.ActualEncKey),
                    JwtConstants.DirectKeyUseAlg, SecurityAlgorithms.Aes256CbcHmacSha512)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            return Task.FromResult(tokenString);
        }

        public ControllerContext ControllerContext { get; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        public TUserID CurrentUserID
        {
            get
            {
                if (Request.HttpContext.Items.TryGetValue("___userID",out var userID))
                {
                    return (TUserID) userID;
                }
                else
                {

                    var value= Request.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (string.IsNullOrEmpty(value))
                    {
                        return default(TUserID);
                    }
                    
                    var tmp= (TUserID)Convert.ChangeType(value, typeof(TUserID));

                    Request.HttpContext.Items.Add("___userID", tmp);

                    return tmp;
                }
            }
        }


        public string CurrentSchemeName
        {
            get
            {
                return (string)HttpContext.Current.Items.TryGetValue("SchemeName");
            }
        }

        /// <summary>
        /// Gets the <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> for the executing action.
        /// </summary>
        HttpRequest Request { get; }

        /// <summary>
        /// Gets the <see cref="T:Microsoft.AspNetCore.Http.HttpResponse" /> for the executing action.
        /// </summary>
        HttpResponse Response { get; }
    }


}
#endif