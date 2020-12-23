using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Kugar.Core.Web.Authentications
{

    public class WebJWTOption
    {
        private static TimeSpan _defaultExpireTimeSpan=TimeSpan.FromDays(30);
        private TimeSpan _expireTimeSpan= _defaultExpireTimeSpan;
        private static readonly string _defaultToken= "0O9W6eOHVmooTnYT";
        private static readonly byte[] _defaultActualEncKey = Encoding.UTF8.GetBytes(_defaultToken.PadRight(128, '0'));

        static WebJWTOption()
        {

        }

        /// <summary>
        /// 构建cookie的配置
        /// </summary>
        public CookieBuilder Cookie { set; get; } = new CookieBuilder();

        /// <summary>
        /// 用于登录验证的服务接口
        /// </summary>
        public IWebJWTLoginService LoginService { get; set; }

        /// <summary>
        /// 授权名称
        /// </summary>
        public string AuthenticationScheme {internal set; get; } = "web";

        /// <summary>
        /// 过期时间,默认为30天
        /// </summary>
        public TimeSpan ExpireTimeSpan
        {
            set => _expireTimeSpan = value;
            get => _expireTimeSpan;
        }

        /// <summary>
        /// token校验成功后,触发该回调,如果回调中,需要登录失败,调用context的Fail函数,会触发OnChallenge回调
        /// </summary>
        public Func<Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext, Task> OnTokenValidated { set; get; }

        /// <summary>
        /// 登录失败时,触发该回调,如需要触发跳转,使用context.Response.Redirect,后使用context.HandleResponse()中止后续处理
        /// </summary>
        public Func<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerChallengeContext, Task> OnChallenge { set; get; }

        private string _tokenEncKey= _defaultToken;
        private byte[] _actualEncKey= _defaultActualEncKey;


        public string Issuer { set; get; } = "userlogin";

        public string Audience { set; get; } = "web";

        /// <summary>
        /// token加密的秘钥,默认为 0O9W6eOHVmooTnYT 的固定密码,请尽量修改该值
        /// </summary>
        public string TokenEncKey
        {
            set
            {
                _tokenEncKey = value;
                _actualEncKey = Encoding.UTF8.GetBytes(value.PadRight(128, '0'));
            }
            get => _tokenEncKey;
        }


        /// <summary>
        /// 登录地址,如需设置登陆跳转界面,这需要设置该跳转地址,如不设置,授权失败后,会返回401错误
        /// </summary>
        public string LoginUrl { set; get; }

        /// <summary>
        /// 退出登录地址
        /// </summary>
        public string LogoutUrl { set; get; }

        /// <summary>
        /// 实际使用的EncKey,不直接使用
        /// </summary>
        public byte[] ActualEncKey
        {
            get => _actualEncKey;
            private set => _actualEncKey = value;
        }
    }
}
