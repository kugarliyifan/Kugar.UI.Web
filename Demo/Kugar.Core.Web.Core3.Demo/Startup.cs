using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.ActionResult;
using Kugar.Core.Web.Core3.Demo.Controllers;
using Kugar.Core.Web.Formatters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kugar.Core.Web.Core3.Demo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("zn-CN");
                options.SetDefaultCulture("zn-CN");
            });

            services.AddLocalization();

            services.AddHttpContextAccessor();

            services.AddSession();

            services.EnableSyncIO();


            services.AddControllersWithViews(opt =>
            {
                opt.OutputFormatters.Insert(0,new ValueTupleOutputFormatter(x =>
                {
                    x.NamingStrategy= new CamelCaseNamingStrategy(true,true);
                }));
            }).AddNewtonsoftJson().EnableJsonValueModelBinder()
                .AddDataAnnotationsLocalization();
            ;


            services.Configure<FileIOOption>(opt =>
            {
                opt.TypeMappings=new Dictionary<string, string>()
                {
                    ["1"]="/uploads/adv"
                };
                opt.IncludeHost = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();


            app.Use(async (context, next) =>
            {
                Thread.CurrentThread.CurrentUICulture=new CultureInfo("zh-CN");;

                await next();
            });

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.UseStaticHttpContext();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
