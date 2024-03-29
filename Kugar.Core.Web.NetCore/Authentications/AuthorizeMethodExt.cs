﻿using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;

namespace Kugar.Core.Web.Authentications
{
    public static class AuthorizeMethodExt
    {
        /// <summary>
        /// 添加一个jwt方式的授权验证,请配合IJWTLoginControlle接口,方便使用,将使用options.cookie.name或者headers中的Authorization 作为token的获取来源<br/>
        /// 因此,可以通用于webapi和web页面进行授权验证
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="authenticationScheme">授权名称</param>
        /// <param name="displayName">授权名称</param>
        /// <param name="options">配置项</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddWebJWT(this AuthenticationBuilder builder,
            string authenticationScheme,
            string displayName,
            WebJWTOption options)
        {
            if (options.LoginService == null)
            {
                throw new ArgumentNullException("Options.LoginService不能为空");
            }

            options.AuthenticationScheme = authenticationScheme;

            builder.Services.AddSingleton(typeof(OptionsManager<>));

            builder.Services.AddOptions().Configure<WebJWTOption>(authenticationScheme, opt =>
             {

                 //opt.Cookie = options.Cookie;
                 //opt.AuthenticationScheme = authenticationScheme;
                 //opt.OnTokenValidated = options.OnTokenValidated;
                 //opt.Issuer = options.Issuer;
                 //opt.Audience = options.Audience;
                 //opt.TokenEncKey = options.TokenEncKey;
                 //opt.LoginService = options.LoginService;
                 //opt.ExpireTimeSpan = options.ExpireTimeSpan;
                 //opt.OnChallenge = options.OnChallenge;
                 //opt.LoginUrl = options.LoginUrl;

                 options.CopyValue(opt);

                 //options.CopyValue(opt);
             });

            builder.AddJwtBearer(authenticationScheme, (opt) =>
            {
                opt.Events = opt.Events ?? new JwtBearerEvents();

                opt.Events.OnMessageReceived = async (context) =>
                {
                    var authName = context.Scheme.Name;

                    var tmp = (OptionsManager<WebJWTOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<WebJWTOption>));

                    context.HttpContext.Items.Remove("SchemeName");
                    context.HttpContext.Items.Add("SchemeName", authName);//.TryGetValue("SchemeName", "")

                    var option = tmp.Get(authName);

                    if (option.TokenFactory!=null)
                    {
                        context.Token = option.TokenFactory(context);
                    }
                    else
                    {
                        if (context.HttpContext.Request.Cookies.TryGetValue(string.IsNullOrEmpty(option.Cookie?.Name) ? $"jwt.{authName}" : option.Cookie?.Name,
                            out var v))
                        {
                            context.Token = v;
                        }

                        if (string.IsNullOrEmpty(context.Token) && context.Request.Headers.ContainsKey("Authorization"))
                        {
                            context.Token = context.Request.Headers.TryGetValue("Authorization").FirstOrDefault();
                        }

                        if (string.IsNullOrEmpty(context.Token) && context.Request.Query.ContainsKey("access_token"))
                        {
                            context.Token = context.Request.Query["access_token"];
                        }
                    }

                    
                    if (string.IsNullOrWhiteSpace(context.Token))
                    {
                        context.Fail("未包含token");
                    }
                    //兼容Bearer开头
                    if (context.Token!=null && context.Token.StartsWith("Bearer ",StringComparison.CurrentCultureIgnoreCase))
                    {
                        context.Token = context.Token.Substring(7);
                    }

                };


                opt.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer ?? options.AuthenticationScheme,
                    ValidateAudience = true,
                    ValidAudience = options.Audience ?? options.AuthenticationScheme,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(options.ActualEncKey),
                    TokenDecryptionKey = new SymmetricSecurityKey(options.ActualEncKey),
                };

                opt.Events.OnTokenValidated = async (context) =>
                {

                    var authName = context.Scheme.Name;

                    var t = (OptionsManager<WebJWTOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<WebJWTOption>));

                    var tmpOpt = t.Get(authName);


                    if (tmpOpt.LoginService != null)
                    {
#if NETCOREAPP2_1
                        var userName = context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.ToStringEx();
                        var pw = (context.Principal.FindFirst("k")?.Value).ToStringEx().DesDecrypt(tmpOpt.TokenEncKey.Left(8));
#endif
#if NETCOREAPP3_0_OR_GREATER
                        var userName = context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                        var pw = context.Principal.FindFirstValue("k").DesDecrypt(tmpOpt.TokenEncKey.Left(8));
#endif
                        userName = userName.Trim();
                        pw = pw.Trim();

                        var ret = await tmpOpt.LoginService.Login(context.HttpContext, userName, pw);
                         
                        if (!ret.IsSuccess)
                        {
                            context.Fail("身份校验错误");

                            return;
                        }
                        else
                        {
                            var permissionFactoryService =
                                (IUserPermissionFactoryService)context.HttpContext.RequestServices.GetService(typeof(IUserPermissionFactoryService));

                            if (permissionFactoryService!=null)
                            {
                                var permissions =
                                    await permissionFactoryService.GetUserPermissions(context.HttpContext,
                                        ret.ReturnData);

                                context.HttpContext.Items["___CurrentUserPermisions"] = new HashSet<string>(permissions);
                            }

                            context.Principal.AddClaim("userID", ret.ReturnData);
                        }
                    }

                    context.HttpContext.Items.Remove("SchemeName");
                    context.HttpContext.Items.Add("SchemeName", authName);//.TryGetValue("SchemeName", "")


                    //HttpContext.Current.Items.Add("SchemeName", authenticationScheme);//.TryGetValue("SchemeName", "")

                    if (tmpOpt.OnTokenValidated != null)
                    {
                        await tmpOpt.OnTokenValidated(context);
                    }
                };

                opt.Events.OnChallenge = async (context) =>
                {
                    var authName = context.Scheme.Name;

                    var t = (OptionsManager<WebJWTOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<WebJWTOption>));

                    var tmpOpt = t.Get(authName);

                    context.HttpContext.Items.Remove("SchemeName");
                    context.HttpContext.Items.Add("SchemeName", authName);//.TryGetValue("SchemeName", "")


                    if (tmpOpt.OnChallenge != null)
                    {
                        context.Response.StatusCode = 200;
                        await tmpOpt.OnChallenge(context);
                    }
                    
                    if(!context.Handled)
                    {
                        if (string.IsNullOrWhiteSpace(tmpOpt.LoginUrl))
                        {
                            context.Response.StatusCode = 401;

                            //context.Response.Redirect($"/AdminCore/Logout/{authenticationScheme}?backurl=" + context.Request.GetDisplayUrl());
                        }
                        else
                        {
                            //context.Response.StatusCode=302;
                            context.Response.Redirect($"{tmpOpt.LoginUrl}?backurl={context.Request.GetDisplayUrl()}");
                        }


                        context.HandleResponse();
                    }


                };

                opt.Events.OnAuthenticationFailed = async (context) =>
                {
                    var authName = context.Scheme.Name;

                    var t = (OptionsManager<WebJWTOption>)context.HttpContext.RequestServices.GetService(
                        typeof(OptionsManager<WebJWTOption>));

                    var tmpOpt = t.Get(authName);

                    if (string.IsNullOrWhiteSpace(tmpOpt.LoginUrl))
                    {
                        //if (context.)
                        //{
                            
                        //}
                        //context.Response.StatusCode = 401;
                        //context.Response.Redirect($"/AdminCore/Logout/{authenticationScheme}?backurl=" + context.Request.GetDisplayUrl());
                    }
                    else
                    {
                        context.Response.Redirect($"{tmpOpt.LoginUrl}?backurl={context.Request.GetDisplayUrl()}");
                    }

                };
            });

            return builder;
        }

        /// <summary>
        /// 添加一个jwt方式的授权验证,请配合IJWTLoginControlle接口,方便使用,将使用options.cookie.name或者headers中的Authorization 作为token的获取来源<br/>
        /// 因此,可以通用于webapi和web页面进行授权验证
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="authenticationScheme">授权名称</param>
        /// <param name="options">配置项</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddWebJWT(this AuthenticationBuilder builder,
            string authenticationScheme,
            WebJWTOption options)
        {
            return AddWebJWT(builder, authenticationScheme, authenticationScheme, options);
        }
    }
}
