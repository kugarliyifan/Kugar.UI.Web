using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Kugar.Core.Web.Helpers
{
    public static class MVCBuilderHelper
    {
        /// <summary>
        /// 用于为mvc的数据验证增加中文错误提示
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcBuilder AddChineseDataAnnotation(this IMvcBuilder builder)
        {
            builder.AddDataAnnotationsLocalization(opt => {
                opt.DataAnnotationLocalizerProvider = (type, factory) =>
                {
                    var loc = factory.Create(typeof(DataAnnotationsResources))
#if NETCOREAPP3_1 || NETCOREAPP2_1 || NETCOREAPP3_0  || NET5_0 || NET6_0
                    .WithCulture(Thread.CurrentThread.CurrentUICulture)
#endif
                        ;
                    var s1 = loc["StringLengthAttribute_ValidationError"];
                    return loc;
                };
            });

            return builder;
        }

        /// <summary>
        /// 用于为mvc的数据验证增加中文错误提示
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMvcCoreBuilder AddChineseDataAnnotation(this IMvcCoreBuilder builder)
        {
            builder.AddDataAnnotationsLocalization(opt => {
                opt.DataAnnotationLocalizerProvider = (type, factory) =>
                {
                    var loc = factory.Create(typeof(DataAnnotationsResources))
#if NETCOREAPP3_1 || NETCOREAPP2_1 || NETCOREAPP3_0  || NET5_0 || NET6_0
                    .WithCulture(Thread.CurrentThread.CurrentUICulture)
#endif
                        ;
                    var s1 = loc["StringLengthAttribute_ValidationError"];
                    return loc;
                };
            });

            return builder;
        }

        /// <summary>
        /// 增加多语言绑定,默认语言为cultureInfos中的第一个语言
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="cultureInfos"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseRequestLocalization(this IApplicationBuilder builder,
            params CultureInfo[] cultureInfos)
        {

            if (!cultureInfos.HasData())
            {
                throw new ArgumentNullException(nameof(cultureInfos));
            }

            builder.UseRequestLocalization(new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new RequestCulture(cultureInfos.FirstOrDefault()),
                SupportedCultures = cultureInfos,
                SupportedUICultures = cultureInfos,
                FallBackToParentCultures = true,
                FallBackToParentUICultures = true,

            });

            return builder;
        }

        /// <summary>
        /// 允许多语言本地化,配合UseRequestLocalization使用
        /// </summary>
        /// <param name="services"></param>
        /// <param name="defaultCulture">默认语言</param>
        /// <param name="resourcePath">资源文件路径</param>
        /// <returns></returns>
        public static IServiceCollection AddLocalization(this IServiceCollection services,
            CultureInfo defaultCulture,string resourcePath="")
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(defaultCulture);
                options.SetDefaultCulture(defaultCulture.Name);
            });


            if (string.IsNullOrEmpty(resourcePath))
            {
                services.AddLocalization();
            }
            else
            {
                services.AddLocalization(opt =>
                {
                    opt.ResourcesPath = resourcePath;
                });
            }
            

            return services;
        }
    }
}
