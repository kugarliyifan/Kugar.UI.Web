using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using System.Web.Http.Routing;
using System.Web.Routing;

namespace Kugar.Core.Web.WebApi
{
    /// <summary>
    /// 用于在WebApi的路由中,加入namespace参数筛选: 如defaults:{@namespace = "YL.Labi.WebApi.Areas.BabyApi.Controllers"}
    /// </summary>
    public class NamespaceHttpControllerSelector : IHttpControllerSelector
    {
        private const string NamespaceKey = "namespace";
        private const string ControllerKey = "controller";

        private readonly HttpConfiguration _configuration;
        private readonly Lazy<Dictionary<string, HttpControllerDescriptor>> _controllers;
        private readonly HashSet<string> _duplicates;

        public NamespaceHttpControllerSelector(HttpConfiguration config)
        {
            _configuration = config;
            _duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _controllers = new Lazy<Dictionary<string, HttpControllerDescriptor>>(InitializeControllerDictionary);
        }

        private Dictionary<string, HttpControllerDescriptor> InitializeControllerDictionary()
        {
            var dictionary = new Dictionary<string, HttpControllerDescriptor>(StringComparer.OrdinalIgnoreCase);

            // Create a lookup table where key is "namespace.controller". The value of "namespace" is the last
            // segment of the full namespace. For example:
            // MyApplication.Controllers.V1.ProductsController => "V1.Products"
            IAssembliesResolver assembliesResolver = _configuration.Services.GetAssembliesResolver();
            IHttpControllerTypeResolver controllersResolver = _configuration.Services.GetHttpControllerTypeResolver();

            ICollection<Type> controllerTypes = controllersResolver.GetControllerTypes(assembliesResolver);

            foreach (Type t in controllerTypes)
            {
                if (t.IsAbstract)
                {
                    continue;
                }

                //var segments = t.Namespace.Split(Type.Delimiter);

                // For the dictionary key, strip "Controller" from the end of the type name.
                // This matches the behavior of DefaultHttpControllerSelector.
                var controllerName = t.Name.Remove(t.Name.Length - DefaultHttpControllerSelector.ControllerSuffix.Length);

                var key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", t.Namespace/*segments[segments.Length - 1]*/, controllerName);

                // Check for duplicate keys.
                if (dictionary.Keys.Contains(key))
                {
                    
                    _duplicates.Add(key);
                }
                else
                {
                    try
                    {
                        dictionary.Add(key, new HttpControllerDescriptor(_configuration, t.Name, t));
                    }
                    catch (Exception e)
                    {
                        
                    }
                    
                }
            }

            // Remove any duplicates from the dictionary, because these create ambiguous matches. 
            // For example, "Foo.V1.ProductsController" and "Bar.V1.ProductsController" both map to "v1.products".
            foreach (string s in _duplicates)
            {
                dictionary.Remove(s);
            }
            return dictionary;
        }

        // Get a value from the route data, if present.
        private static T GetRouteVariable<T>(IHttpRouteData routeData, string name)
        {
            object result = null;
            if (routeData.Values.TryGetValue(name, out result))
            {
                return (T)result;
            }
            return default(T);
        }

        public HttpControllerDescriptor SelectController(HttpRequestMessage request)
        {
            IHttpRouteData routeData = request.GetRouteData();
            if (routeData == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            // Get the namespace and controller variables from the route data.
            string namespaceName = GetRouteVariable<string>(routeData, NamespaceKey);
            if (namespaceName == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            string controllerName = GetRouteVariable<string>(routeData, ControllerKey);
            if (controllerName == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            // Find a matching controller.
            string key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", namespaceName, controllerName);

            HttpControllerDescriptor controllerDescriptor;
            if (_controllers.Value.TryGetValue(key, out controllerDescriptor))
            {
                return controllerDescriptor;
            }
            else if (_duplicates.Contains(key))
            {
                throw new HttpResponseException(
                    request.CreateErrorResponse(HttpStatusCode.InternalServerError,
                    "Multiple controllers were found that match this request."));
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
        }

        public IDictionary<string, HttpControllerDescriptor> GetControllerMapping()
        {
            return _controllers.Value;
        }

        public static void Register(HttpConfiguration config)
        {
            config.Services.Replace(typeof(IHttpControllerSelector), new NamespaceHttpControllerSelector(config));

        }
    }

    public static class NamespaceHttpControllerExt
    {
        //
        // 摘要:
        //     映射指定的路由模板。
        //
        // 参数:
        //   routes:
        //     应用程序的路由的集合。
        //
        //   name:
        //     要映射的路由的名称。
        //
        //   routeTemplate:
        //     路由的路由模板。
        //
        // 返回结果:
        //     对映射路由的引用。
        public static RouteBase MapHttpRouteWithNamespance(this RouteCollection routes, string name, string routeTemplate, string namespaces)
        {
            return MapHttpRouteWithNamespance(routes, name, routeTemplate,null, namespaces, null,null);
        }
        //
        // 摘要:
        //     映射指定的路由模板并设置默认路由。
        //
        // 参数:
        //   routes:
        //     应用程序的路由的集合。
        //
        //   name:
        //     要映射的路由的名称。
        //
        //   routeTemplate:
        //     路由的路由模板。
        //
        //   defaults:
        //     一个包含默认路由值的对象。
        //
        // 返回结果:
        //     对映射路由的引用。
        public static RouteBase MapHttpRouteWithNamespance(this RouteCollection routes, string name, string routeTemplate,
            object defaults, string namespaces)
        {
            return MapHttpRouteWithNamespance(routes, name, routeTemplate, defaults, namespaces, null, null);
        }
        //
        // 摘要:
        //     映射指定的路由模板并设置默认路由值和约束。
        //
        // 参数:
        //   routes:
        //     应用程序的路由的集合。
        //
        //   name:
        //     要映射的路由的名称。
        //
        //   routeTemplate:
        //     路由的路由模板。
        //
        //   defaults:
        //     一个包含默认路由值的对象。
        //
        //   constraints:
        //     一组表达式，用于指定 routeTemplate 的值。
        //
        // 返回结果:
        //     对映射路由的引用。
        public static RouteBase MapHttpRouteWithNamespance(this RouteCollection routes, string name, string routeTemplate,
            object defaults, string namespaces, object constraints)
        {
            return MapHttpRouteWithNamespance(routes, name, routeTemplate, defaults, namespaces, constraints, null);
        }

        //
        // 摘要:
        //     映射指定的路由模板并设置默认的路由值、约束和终结点消息处理程序。
        //
        // 参数:
        //   routes:
        //     应用程序的路由的集合。
        //
        //   name:
        //     要映射的路由的名称。
        //
        //   routeTemplate:
        //     路由的路由模板。
        //
        //   defaults:
        //     一个包含默认路由值的对象。
        //
        //   constraints:
        //     一组表达式，用于指定 routeTemplate 的值。
        //
        //   handler:
        //     请求将被调度到的处理程序。
        //
        // 返回结果:
        //     对映射路由的引用。
        public static RouteBase MapHttpRouteWithNamespance(this RouteCollection routes, string name, string routeTemplate,
            object defaults,string namespaces, object constraints, HttpMessageHandler handler)
        {
            if (routes == null)
            {
                throw new ArgumentNullException("routes");
            }

            HttpRouteValueDictionary defaultsDictionary = new HttpRouteValueDictionary(defaults);
            HttpRouteValueDictionary constraintsDictionary = new HttpRouteValueDictionary(constraints);

            defaultsDictionary.Add("namespace", namespaces);

            var httpRoute =(RouteBase)GlobalConfiguration.Configuration.Routes.CreateRoute(routeTemplate, defaultsDictionary, constraintsDictionary, dataTokens: null, handler: handler);
            //Route route = httpRoute.OriginalRoute;
            routes.Add(name, httpRoute);

            return httpRoute;
        }
    }
}
