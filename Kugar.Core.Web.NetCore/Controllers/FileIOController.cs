using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;

#if NETCOREAPP3_1  || NET5_0 || NET6_0
using Kugar.Storage;
#endif

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

#if NETCOREAPP3_1 || NET5_0 || NET6_0

namespace Kugar.Core.Web
{
    /// <summary>
    /// 
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class FileIOController : ControllerBase
    {

        [Authorize("corefileio")]
        [Route("WebCore/FileIO/Upload/{type}")]
        [Route("WebCore/FileIO/Upload")]
        [HttpPost]
        public async Task<IActionResult> Upload(IEnumerable<IOptions<FileIOOption>> options, IStorage storage, string type = "")
        {
            if (options == null || !options.HasData())
            {
                throw new ArgumentNullException(nameof(options));
            }

            var option = options.FirstOrDefault(x => x.Value.Type == type);

            if (option == null)
            {
                throw new ArgumentNullException(nameof(options), "指定type不存在");
            }

            if (option.Value.Events?.OnRequestHandler != null)
            {
                var result = await option.Value.Events.OnRequestHandler.Invoke(HttpContext, type);

                if (result != null)
                {
                    return result;
                }
            }

            var file = Request.GetFile();
            var fileName = await (option.Value.Events?.OnGenerateFileName ?? onInternalGenerateFileName).Invoke(HttpContext, file, type);
            await using var fileStream = file.OpenReadStream();
            var ret = await (option.Value.Storage ?? storage).StorageFileAsync(fileName, fileStream);

            return new JsonResult(ret);


            //if (Option.TypeMappings.TryGetValue(type,out var path))
            //{
            //    var file = Request.GetFile();
            //    var fileName = Option.onGenerateFileName(file, type);

            //    if (string.IsNullOrWhiteSpace(fileName))
            //    {
            //        fileName = file.GetRandomName();
            //    }

            //    var newPath = Path.Combine(path, DateTime.Now.ToString("yyyyMMdd"), fileName);

            //    var result=await file.SaveAsExAsync(Path.Combine(Request.GetWebsitePath(), newPath.Trim('/')));

            //    if (result.IsSuccess)
            //    {
            //        if (Option.IncludeHost)
            //        {
            //            newPath =$"{(Request.IsHttps?"https":"http")}://{Request.Host.ToStringEx()}{newPath}";
            //        }

            //        return new JsonResult(new SuccessResultReturn(newPath));
            //    }
            //    else
            //    {
            //        return StatusCode(500, result.Message);
            //    }
            //}
            //else
            //{
            //    return NotFound("不存在指定类型的映射");
            //}
        }

        private Task<string> onInternalGenerateFileName(Microsoft.AspNetCore.Http.HttpContext contex, IFormFile file, string type)
        {
            return Task.FromResult($"/uploads/{type}/{DateTime.Now:yyyyMMdd}/{file.GetRandomName()}");
        }
    }

    public class FileIOOption : AuthenticationSchemeOptions
    {
        private string _issuerSigningKey;
        private string _tokenDecryptionKey;

        private SecurityKey _issuerSigning;
        private SecurityKey _tokenKey;


        public string Type { set; get; }

        /// <summary>
        /// JWT加密Key
        /// </summary>
        public string TokenDecryptionKey
        {
            set
            {
                _tokenDecryptionKey = value;
                _tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value.PadRight(128, '0')));
            }
            get => _tokenDecryptionKey;
        }

        public string IssuerSigningKey
        {
            set
            {
                _issuerSigningKey = value;
                _issuerSigning = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value.PadRight(128, '0')));
            }
            get => _issuerSigningKey;
        }

        internal SecurityKey ActualIssuerSigningKey => _issuerSigning;

        internal SecurityKey ActualTokenDecryptionKey => _tokenKey;

        public string Audience { set; get; } = "web";

        public string Issuer { set; get; } = "api";

        /// <summary>
        /// Checks that the options are valid for a specific scheme
        /// </summary>
        /// <param name="scheme">The scheme being validated.</param>
        public virtual void Validate(string scheme) => this.Validate();


        /// <summary>
        /// 用于处理文件实际存储的接口,顺序为 属性Storage中类->全局容器中的IStorage接口->存入本地/uploads/目录下,实际的存放路径,参考Events.OnGenerateFileName,如需修改,请赋值该属性
        /// </summary>
        public IStorage Storage { set; get; }

        public FileUploadEvents Events
        {
            get => (FileUploadEvents)base.Events;
            set => base.Events = (FileUploadEvents)value;
        }

    }
    

    public class FileIOBearerHandler : AuthenticationHandler<FileIOOption>
    {


        public FileIOBearerHandler(
          IEnumerable<IOptions<FileIOOption>> options,
          ILoggerFactory logger,
          UrlEncoder encoder,
          IDataProtectionProvider dataProtection,
          ISystemClock clock)
          : base(null, logger, encoder, clock)
        {
            var type = this.Context.GetRouteValue("type").ToStringEx();

            this.Options = options.FirstOrDefault(x => x.Value.Type == type)?.Value;
        }
        
        public FileIOOption Options {private set; get; }

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected FileUploadEvents Events
        {
            get => (FileUploadEvents)base.Events;
            set => base.Events = (FileUploadEvents)value;
        }

        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new FileUploadEvents());

        /// <summary>
        /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="T:Microsoft.IdentityModel.Tokens.TokenValidationParameters" /> set in the options.
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwtBearerHandler = this;
            string token = (string)null;
            object obj;
            AuthenticationFailedContext authenticationFailedContext;
            int num;
            try
            {
                MessageReceivedContext messageReceivedContext = new MessageReceivedContext(jwtBearerHandler.Context, jwtBearerHandler.Scheme, jwtBearerHandler.Options);
                await jwtBearerHandler.Events.OnMessageReceived(messageReceivedContext, Options.Type);
                if (messageReceivedContext.Result != null)
                    return messageReceivedContext.Result;
                token = messageReceivedContext.Token;

                if (string.IsNullOrEmpty(token))
                {
                    string header = (string)jwtBearerHandler.Request.Headers["Authorization"];
                    if (string.IsNullOrEmpty(header))
                        return AuthenticateResult.NoResult();
                    if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        token = header.Substring("Bearer ".Length).Trim();
                    if (string.IsNullOrEmpty(token))
                        return AuthenticateResult.NoResult();
                }

                var paramter = new TokenValidationParameters();
                paramter.TokenDecryptionKey = Options.ActualTokenDecryptionKey;
                paramter.ValidAudience = Options.Audience;
                paramter.ValidIssuer = Options.Issuer;
                paramter.ValidateIssuer = true;
                paramter.ValidateAudience = true;

                //TokenValidationParameters validationParameters1 = jwtBearerHandler.Options.TokenValidationParameters.Clone();
                //if (jwtBearerHandler._configuration != null)
                {
                    string[] strArray = new[]
                    {
                        Options.Issuer
                    };
                    //TokenValidationParameters validationParameters2 = validationParameters1;
                    //IEnumerable<string> validIssuers = validationParameters1.ValidIssuers;
                    //object obj1 = (validIssuers != null ? (object)validIssuers.Concat<string>((IEnumerable<string>)strArray) : (object)null) ?? (object)strArray;
                    paramter.ValidIssuers = strArray;
                    //TokenValidationParameters validationParameters3 = validationParameters1;
                    //IEnumerable<SecurityKey> issuerSigningKeys = new []{Options.InternalIssuerSigningKey};
                    //IEnumerable<SecurityKey> securityKeys = (issuerSigningKeys != null ? issuerSigningKeys.Concat<SecurityKey>((IEnumerable<SecurityKey>)jwtBearerHandler._configuration.SigningKeys) : (IEnumerable<SecurityKey>)null) ?? (IEnumerable<SecurityKey>)jwtBearerHandler._configuration.SigningKeys;
                    paramter.IssuerSigningKeys = new[] { Options.ActualIssuerSigningKey };
                }
                List<Exception> exceptionList = (List<Exception>)null;

                var securityTokenValidator = new JwtSecurityTokenHandler();

                //foreach (ISecurityTokenValidator securityTokenValidator in (IEnumerable<ISecurityTokenValidator>)jwtBearerHandler.Options.SecurityTokenValidators)
                //{
                if (securityTokenValidator.CanReadToken(token))
                {
                    SecurityToken validatedToken;
                    ClaimsPrincipal claimsPrincipal;
                    try
                    {
                        claimsPrincipal = securityTokenValidator.ValidateToken(token, paramter, out validatedToken);
                    }
                    catch (Exception ex)
                    {
                        return AuthenticateResult.Fail(ex);
                    }
                    //jwtBearerHandler.Logger.TokenValidationSucceeded();
                    TokenValidatedContext validatedContext = new TokenValidatedContext(jwtBearerHandler.Context, jwtBearerHandler.Scheme, jwtBearerHandler.Options);
                    validatedContext.Principal = claimsPrincipal;
                    validatedContext.SecurityToken = validatedToken;
                    //TokenValidatedContext tokenValidatedContext = validatedContext;
                    await jwtBearerHandler.Events.OnTokenValidated(validatedContext, Options.Type);


                    if (validatedContext.Result != null)
                        return validatedContext.Result;
                    //if (jwtBearerHandler.Options.SaveToken)
                    validatedContext.Properties.StoreTokens(new[]
                    {
                        new AuthenticationToken()
                        {
                            Name = "access_token",
                            Value = token
                        }
                    });
                    validatedContext.Success();
                    return validatedContext.Result;
                }
                else
                {
                    return AuthenticateResult.Fail("No SecurityTokenValidator available for token: " + token);
                }
                //}
                //if (exceptionList == null)
                //    return AuthenticateResult.Fail("No SecurityTokenValidator available for token: " + token);
                ////authenticationFailedContext = new AuthenticationFailedContext(jwtBearerHandler.Context, jwtBearerHandler.Scheme, jwtBearerHandler.Options)
                ////{
                ////    Exception = exceptionList.Count == 1 ? exceptionList[0] : (Exception)new AggregateException((IEnumerable<Exception>)exceptionList)
                ////};
                ////await jwtBearerHandler.Events.AuthenticationFailed(authenticationFailedContext);
                //return AuthenticateResult.Fail(new AuthenticationException());
            }
            catch (Exception ex)
            {
                obj = (object)ex;

                return AuthenticateResult.Fail(ex);
            }
            //if (num == 1)
            //{
            //    Exception ex = (Exception)obj;
            //    //wtBearerHandler.Logger.ErrorProcessingMessage(ex);
            //    authenticationFailedContext = new AuthenticationFailedContext(jwtBearerHandler.Context, jwtBearerHandler.Scheme, jwtBearerHandler.Options)
            //    {
            //        Exception = ex
            //    };
            //    await jwtBearerHandler.Events.AuthenticationFailed(authenticationFailedContext);
            //    if (authenticationFailedContext.Result != null)
            //        return authenticationFailedContext.Result;
            //    if (!(obj is Exception source))
            //        throw obj;
            //    ExceptionDispatchInfo.Capture(source).Throw();
            //    authenticationFailedContext = (AuthenticationFailedContext)null;
            //}
            //obj = (object)null;
            //token = (string)null;
            //AuthenticateResult authenticateResult;
            //return authenticateResult;
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var challenge = "Bearer";

            //var jwtBearerHandler = this;
            AuthenticateResult authenticateResult = await this.HandleAuthenticateOnceSafeAsync();
            var eventContext = new FileUploadChallengeContext(this.Context, this.Scheme, this.Options, properties)
            {
                AuthenticateFailure = authenticateResult?.Failure
            };
            if (eventContext.AuthenticateFailure != null)
            {
                eventContext.Error = "invalid_token";
                eventContext.ErrorDescription = CreateErrorDescription(eventContext.AuthenticateFailure);
            }
            await this.Events.OnChallenge(eventContext, Options.Type);
            if (eventContext.Handled)
                return;
            this.Response.StatusCode = 401;
            if (string.IsNullOrEmpty(eventContext.Error) && string.IsNullOrEmpty(eventContext.ErrorDescription) && string.IsNullOrEmpty(eventContext.ErrorUri))
            {
                this.Response.Headers.Append("WWW-Authenticate", challenge);
            }
            else
            {
                StringBuilder stringBuilder = new StringBuilder(challenge);
                if (challenge.IndexOf(" ", StringComparison.Ordinal) > 0)
                    stringBuilder.Append(',');
                if (!string.IsNullOrEmpty(eventContext.Error))
                {
                    stringBuilder.Append(" error=\"");
                    stringBuilder.Append(eventContext.Error);
                    stringBuilder.Append("\"");
                }
                if (!string.IsNullOrEmpty(eventContext.ErrorDescription))
                {
                    if (!string.IsNullOrEmpty(eventContext.Error))
                        stringBuilder.Append(",");
                    stringBuilder.Append(" error_description=\"");
                    stringBuilder.Append(eventContext.ErrorDescription);
                    stringBuilder.Append('"');
                }
                if (!string.IsNullOrEmpty(eventContext.ErrorUri))
                {
                    if (!string.IsNullOrEmpty(eventContext.Error) || !string.IsNullOrEmpty(eventContext.ErrorDescription))
                        stringBuilder.Append(",");
                    stringBuilder.Append(" error_uri=\"");
                    stringBuilder.Append(eventContext.ErrorUri);
                    stringBuilder.Append('"');
                }
                this.Response.Headers.Append("WWW-Authenticate", (StringValues)stringBuilder.ToString());
            }
        }

        private static string CreateErrorDescription(Exception authFailure)
        {
            IEnumerable<Exception> exceptions;
            if (authFailure is AggregateException aggregateException)
                exceptions = (IEnumerable<Exception>)aggregateException.InnerExceptions;
            else
                exceptions = (IEnumerable<Exception>)new Exception[1]
                {
          authFailure
                };
            List<string> stringList = new List<string>();
            foreach (Exception exception in exceptions)
            {
                switch (exception)
                {
                    case SecurityTokenInvalidAudienceException _:
                        stringList.Add("The audience is invalid");
                        continue;
                    case SecurityTokenInvalidIssuerException _:
                        stringList.Add("The issuer is invalid");
                        continue;
                    case SecurityTokenNoExpirationException _:
                        stringList.Add("The token has no expiration");
                        continue;
                    case SecurityTokenInvalidLifetimeException _:
                        stringList.Add("The token lifetime is invalid");
                        continue;
                    case SecurityTokenNotYetValidException _:
                        stringList.Add("The token is not valid yet");
                        continue;
                    case SecurityTokenExpiredException _:
                        stringList.Add("The token is expired");
                        continue;
                    case SecurityTokenSignatureKeyNotFoundException _:
                        stringList.Add("The signature key was not found");
                        continue;
                    case SecurityTokenInvalidSignatureException _:
                        stringList.Add("The signature is invalid");
                        continue;
                    default:
                        continue;
                }
            }
            return string.Join("; ", (IEnumerable<string>)stringList);
        }
    }

    public class TokenValidatedContext : ResultContext<FileIOOption>
    {
        public TokenValidatedContext(
            Microsoft.AspNetCore.Http.HttpContext context,
            AuthenticationScheme scheme,
            FileIOOption options)
            : base(context, scheme, options)
        {
        }

        public SecurityToken SecurityToken { get; set; }
    }
    
    public class MessageReceivedContext : ResultContext<FileIOOption>
    {
        public MessageReceivedContext(
            Microsoft.AspNetCore.Http.HttpContext context,
            AuthenticationScheme scheme,
            FileIOOption options)
            : base(context, scheme, options)
        {
        }

        /// <summary>
        /// Bearer Token. This will give the application an opportunity to retrieve a token from an alternative location.
        /// </summary>
        public string Token { get; set; }
    }

    public class FileUploadChallengeContext : PropertiesContext<FileIOOption>
    {
        public FileUploadChallengeContext(
            Microsoft.AspNetCore.Http.HttpContext context,
            AuthenticationScheme scheme,
            FileIOOption options,
            AuthenticationProperties properties)
            : base(context, scheme, options, properties)
        {
        }

        /// <summary>
        /// Any failures encountered during the authentication process.
        /// </summary>
        public Exception AuthenticateFailure { get; set; }

        /// <summary>
        /// Gets or sets the "error" value returned to the caller as part
        /// of the WWW-Authenticate header. This property may be null when
        /// <see cref="P:Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions.IncludeErrorDetails" /> is set to <c>false</c>.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the "error_description" value returned to the caller as part
        /// of the WWW-Authenticate header. This property may be null when
        /// <see cref="P:Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions.IncludeErrorDetails" /> is set to <c>false</c>.
        /// </summary>
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Gets or sets the "error_uri" value returned to the caller as part of the
        /// WWW-Authenticate header. This property is always null unless explicitly set.
        /// </summary>
        public string ErrorUri { get; set; }

        /// <summary>
        /// If true, will skip any default logic for this challenge.
        /// </summary>
        public bool Handled { get; private set; }

        /// <summary>Skips any default logic for this challenge.</summary>
        public void HandleResponse() => this.Handled = true;
    }

    public static class FileIOExt
    {
        public static AuthenticationBuilder AddFileUpload(this AuthenticationBuilder builder,
            string type,
            Action<FileIOOption> optionFactory
            )
        {
            
            //builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<FileIOOption>, JwtBearerPostConfigureOptions>());
            return builder.AddScheme<FileIOOption, FileIOBearerHandler>("corefileio", "corefileio", (opt) =>
            {
                opt.Type = type;
                optionFactory(opt);
            });

        }
    }


    public class FileUploadEvents
    {
        /// <summary>
        /// 用于根据Type获取不同的Token,默认使用Authorization或者{type}_Authorization,获取顺序:Headers->Cookie,如需自定义,使用该属性进行自定义
        /// </summary>
        public FileUploadMessageReceived OnMessageReceived { set; get; }

        /// <summary>
        /// 用于根据传入的type和context,返回
        /// </summary>
        public GenerateFileName OnGenerateFileName { set; get; }

        /// <summary>
        /// 用于当出现授权失败的时候自定义处理返回值,默认返回404
        /// </summary>
        public FileUploadChallenge OnChallenge { set; get; }

        /// <summary>
        /// 触发请求时,回掉该函数,如需特殊处理的,返回IActionResult,则直接返回处理结果忽略原有的存储步骤,如无需处理,则返回null,一般用于对请求进行额外校验
        /// </summary>
        public RequestHandler OnRequestHandler { set; get; }

        /// <summary>
        /// 当校验通过后,回调该函数,,如需验证用户权限,请在此处验证
        /// </summary>
        public TokenValidated OnTokenValidated { set; get; }
    }

    /// <summary>
    /// 用于获取
    /// </summary>
    /// <param name="context"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate Task<string> FileUploadMessageReceived(MessageReceivedContext context, string type);

    /// <summary>
    /// 用于返回基于基地址的路径及文件名,默认存在 基地址/{type}/{当前日期}/{随机文件名}.{扩展名}
    /// </summary>
    /// <param name="contex"></param>
    /// <param name="file"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate Task<string> GenerateFileName(Microsoft.AspNetCore.Http.HttpContext contex, IFormFile file, string type);

    public delegate Task<IActionResult> RequestHandler(Microsoft.AspNetCore.Http.HttpContext context, string type);

    /// <summary>
    /// 用于当出现授权失败的时候自定义处理返回值,默认返回404
    /// </summary>
    /// <param name="context"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate Task<IActionResult> FileUploadChallenge(FileUploadChallengeContext context, string type);

    /// <summary>
    /// 用于当出现授权失败的时候自定义处理返回值,默认返回404
    /// </summary>
    /// <param name="context"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate Task<IActionResult> TokenValidated(TokenValidatedContext context, string type);
}

#endif