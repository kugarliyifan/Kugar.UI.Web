using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.ActionResult;
using Kugar.Core.Web.Core3.Demo.Controllers;
using Kugar.Core.Web.Formatters;
using Kugar.Core.Web.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Security;
using DataAnnotationsResources = Kugar.Core.Web.Resources.DataAnnotationsResources;

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

            services/*.EnableSyncIO()*/.AddSwaggerDocument(opt =>
            {
                //opt.DocumentName = "api";
               // opt.ApiGroupNames = new[] { "wxapi" };
                opt.DocumentName = "wxapi";
                opt.Title = "微信小程序接口";

                AppDomain.CurrentDomain.GetAssemblies();

                opt.UseJsonTemplate(typeof(Startup).Assembly);
                opt.PostProcess = (doc) =>
                {
                    doc.Consumes = new string[] { "application/json" };
                    doc.Produces = new string[] { "application/json" };
                };

                opt.DocumentProcessors.Add(new SecurityDefinitionAppender("Authorization", new OpenApiSecurityScheme()
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Description = "授权token",

                }));

                //opt.UseJsonTemplate(typeof(Startup).Assembly);

                //opt.OperationProcessors.Add(new OperationProcessor((context) =>
                //{
                //    foreach (var parameter in context.Parameters)
                //    {
                //        if (parameter.Key.ParameterType.ToStringEx().Contains("ValueTuple"))
                //        {
                            
                //            context.OperationDescription.Operation.Parameters.RemoveAt((int)parameter.Value.Position);
                            
                //            parameter.Value.Type = JsonObjectType.Object;

                //            var attr = (TupleElementNamesAttribute)parameter.Key.GetCustomAttributes(typeof(TupleElementNamesAttribute), true).FirstOrDefault();

                //            var t = parameter.Key.ParameterType.GetGenericArguments();
                            
                //            for (int i = 0; i < attr.TransformNames.Count; i++)
                //            {
                //                parameter.Value.Properties.Add( attr.TransformNames[i],new JsonSchemaProperty()
                //                {
                //                    Type = netTypeToJsonObjectType(t[i]),
                                    
                //                    IsNullableRaw = !t[i].IsValueType,
                //                });
                //            }

                //            parameter.Value.Name = parameter.Key.Name;

                            
                //        }
                //    }

                //    return true;
                //}));
            });

            var t = new ResourceManager(typeof(DataAnnotationsResources));

            //var t1=t.GetString("ArgumentIsNullOrWhitespace");

            //var field=AppDomain.CurrentDomain.GetAssemblies().Where(x => x.GetName().Name == "System.ComponentModel.Annotations")
            //    .First()
            //    .GetType("SR")
            //    .GetField("s_resourceManager");
                
            

            services.AddControllersWithViews(opt =>
                {
                    opt.ModelBindingMessageProvider.SetValueIsInvalidAccessor(s =>
                    {
                        return s;
                    });
                opt.OutputFormatters.Insert(0,new ValueTupleOutputFormatter(x =>
                {
                    x.NamingStrategy= new CamelCaseNamingStrategy(true,true);
                }));
            }).AddNewtonsoftJson().EnableJsonValueModelBinder()
                .AddDataAnnotationsLocalization(opt => {
                    opt.DataAnnotationLocalizerProvider = (type, factory) =>
                    {
                        var loc = factory.Create(typeof(DataAnnotationsResources)).WithCulture(Thread.CurrentThread.CurrentUICulture);
                        var s1 = loc["StringLengthAttribute_ValidationError"];
                        return loc;
                    };
                });
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

        private JsonObjectType netTypeToJsonObjectType(Type type)
        {
            return JsonObjectType.String;
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
                Thread.CurrentThread.CurrentUICulture=new CultureInfo("en");;

                await next();
            });

            app.UseRouting();

            app.UseSession();

            app.UseAuthorization();

            app.UseStaticHttpContext();

            var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("zh-cn") };
            app.UseRequestLocalization(new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new RequestCulture(new CultureInfo("zh-cn")),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseOpenApi();       // serve OpenAPI/Swagger documents

            app.UseSwaggerUi3();    // serve Swagger UI

            app.UseSwaggerUi3(config =>  // serve ReDoc UI
            {
                // 這裡的 Path 用來設定 ReDoc UI 的路由 (網址路徑) (一定要以 / 斜線開頭)
                config.Path = "/swager";
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
