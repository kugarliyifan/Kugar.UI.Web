using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web;
using Kugar.UI.MVC2;
using Webdiyer.WebControls.Mvc;
using ImageResult = Kugar.UI.MVC.ImageResult;
using XmlActionResult = Kugar.UI.MVC.XmlActionResult;

namespace Kugar.Web.MVC
{
    public static class RouteDataHelper
    {
        public static int GetInt(this RouteData route, string key, int defaultValue = 0)
        {
            if (route == null)
            {
                return defaultValue;
            }

            if (route.Values.ContainsKey(key))
            {
                return route.Values[key].ToInt(defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }

        public static T GetIntEnum<T>(this RouteData route, string key, T defaultValue)
        {
            var v = GetInt(route, key, (int)Convert.ToInt32(defaultValue));

            if (Enum.IsDefined(typeof(T), v))
            {
                return (T)Enum.ToObject(typeof(T), v);
            }
            else
            {
                return defaultValue;
            }
        }

        public static string GetString(this RouteData route, string key, string defaultValue = "")
        {
            if (route == null)
            {
                return defaultValue;
            }

            if (route.Values.ContainsKey(key))
            {
                return string.IsNullOrEmpty(key) ? defaultValue : route.Values[key].ToStringEx().Trim();
            }
            else
            {
                return defaultValue;
            }
        }

        public static long GetLong(this RouteData route, string key, long defaultValue = 0)
        {
            if (route == null)
            {
                return defaultValue;
            }

            if (route.Values.ContainsKey(key))
            {
                return string.IsNullOrEmpty(key) ? defaultValue : route.Values[key].ToStringEx().ToLong(defaultValue);
            }
            else
            {
                return defaultValue;
            }
        }

        public static string GetRouteName(this Route route)
        {
            if (route == null)
            {
                return null;
            }
            return route.DataTokens.GetRouteName();
        }

        public static string GetRouteName(this RouteData routeData)
        {
            if (routeData == null)
            {
                return null;
            }
            return routeData.DataTokens.GetRouteName();
        }

        public static string GetRouteName(this RouteValueDictionary routeValues)
        {
            if (routeValues == null)
            {
                return null;
            }
            object routeName = null;
            routeValues.TryGetValue("RouteName", out routeName);
            return routeName as string;
        }

        public static Route SetRouteName(this Route route, string routeName)
        {
            if (route == null)
            {
                throw new ArgumentNullException("route");
            }
            if (route.DataTokens == null)
            {
                route.DataTokens = new RouteValueDictionary();
            }
            route.DataTokens["__RouteName"] = routeName;
            return route;
        }
    }

    public static class HtmlHelperExt
    {
        public static object GetRouteValue(this HtmlHelper helper, string name)
        {
            return helper.RouteCollection.GetRouteData(helper.ViewContext.HttpContext).Values[name].ToStringEx();
        }

        public static T GetRouteValue<T>(this HtmlHelper helper, string name)
        {
            var value = GetRouteValue(helper, name);

            return (T)value;
        }

        public static RouteValueDictionary GetRouteValues(this HtmlHelper helper)
        {
            return helper.ViewContext.RouteData.Values;

            //return helper.RouteCollection.GetRouteData(helper.ViewContext.HttpContext).Values;
        }


    }

    public static class ControllerExt
    {
        public static ActionResult Xml(this Controller srcCtrl, XmlDocument xmlDocument)
        {
            HttpResponseBase response = srcCtrl.HttpContext.Response;

            // 设置 HTTP Header 的 ContentType
            response.ContentType = "text/xml";

            var responseEncoding = srcCtrl.HttpContext.Response.ContentEncoding;

            if (responseEncoding == null)
            {
                responseEncoding = Encoding.UTF8;
            }

            return new ContentResult() { Content = xmlDocument.SaveToString(), ContentEncoding = responseEncoding, ContentType = "text/xml" };
        }

        public static ActionResult Xml(this Controller srcCtrl, object data)
        {
            return new XmlActionResult(data);
        }

        public static ActionResult Image(this Controller srcCtrl, Image img,ImageFormat format= null)
        {
            return new ImageResult(img,img.RawFormat);
        }

        public static ActionResult Image(this Controller srcCtrl, string imagePath)
        {
            var img = System.Drawing.Image.FromFile(imagePath);

            return Image(srcCtrl, img, img.RawFormat);
        }

        public static ActionResult NewtonJson(this Controller srcCtrl, object data)
        {
            return new NewtonsoftJsonResult(data);
        }

        public static ActionResult ZipFile(this Controller srcCtrl,KeyValuePair<string, Stream>[] files, string defaultFileName=""
            )
        {
            if (!files.HasData())
            {
                throw new ArgumentOutOfRangeException("zipFile");
            }

            if (defaultFileName=="")
            {
                defaultFileName = "压缩文件" + DateTime.Now.ToString("yyyyMMddHHmm");
            }

            return new ZipFileActionResult(defaultFileName, files);
        }

        public static ActionResult ZipFile(this Controller srcCtrl, KeyValuePair<string, byte[]>[] files, string defaultFileName = ""
            )
        {
            if (!files.HasData())
            {
                throw new ArgumentOutOfRangeException("zipFile");
            }

            if (defaultFileName == "")
            {
                defaultFileName = "压缩文件" + DateTime.Now.ToString("yyyyMMddHHmm");
            }

            return new ZipFileActionResult(defaultFileName, files);
        }
    }


    public static class RouteValueDictionaryExt
    {
        public static RouteValueDictionary Copy(this RouteValueDictionary routeValues)
        {
            var temp = new RouteValueDictionary();

            foreach (var item in routeValues)
            {
                temp.Add(item.Key, item.Value);
            }

            return temp;
        }

        public static RouteValueDictionary AddOrUpdate(this RouteValueDictionary routeValues, string key, object value)
        {
            if (routeValues.Keys.Contains(key))
            {
                routeValues[key] = value;
            }
            else
            {
                routeValues.Add(key, value);
            }

            return routeValues;
        }
    }

    public static class RazorExt
    {
        public static MvcHtmlString ToMvcHtmlString(this string str)
        {
            return new MvcHtmlString(str);
        }
    }

    //public static class PageHelper
    //{

    //    public static MvcHtmlString Pager(this HtmlHelper helper, string routeName, int pageIndex, int pageSize,
    //                                      int totalCount, PagerOptions pagerOptions,
    //                                      string pageKey = "Page")
    //    {
    //        var actionName = helper.GetRouteValue<string>("action");
    //        var controlerName = helper.GetRouteValue<string>("controller");

    //        var pageCount = 0;

    //        if (totalCount % pageSize > 0)
    //        {
    //            pageCount = totalCount / pageSize + 1;
    //        }
    //        else
    //        {
    //            pageCount = totalCount / pageSize;
    //        }

    //        var s = helper.RouteCollection.GetRouteData(helper.ViewContext.HttpContext).Values;

    //        foreach (var s1 in helper.ViewContext.RouteData.Values)
    //        {
    //            if (!s.ContainsKey(s1.Key))
    //            {
    //                s.Add(s1.Key, s1.Value);
    //            }
    //            else
    //            {
    //                s[s1.Key] = s1.Value;
    //            }
    //        }


    //        var t = new MvcHtmlString(Pager(helper, pageCount, pageIndex, actionName, controlerName, pagerOptions,
    //                                  routeName,
    //                                  s,//helper.RouteCollection.GetRouteData(helper.ViewContext.HttpContext).Values,
    //                                  null));//new { @class = "icon2 pagenum" });

    //        return t;
    //    }



    //    public static MvcHtmlString Pager(this HtmlHelper helper, int pageIndex, int pageSize, int totalCount, PagerOptions pagerOptions,
    //                                      string pageKey = "Page")
    //    {
    //        var routeName = helper.RouteCollection.GetRouteData(helper.ViewContext.HttpContext).GetRouteName();





    //        return Pager(helper, routeName, pageIndex, pageSize, totalCount, pagerOptions, pageKey);

    //        //return Pager(helper, totalCount, pageSize, pageIndex, actionName, controlerName, pageOper, routeName, helper.RouteCollection.GetRouteData(helper.ViewContext.HttpContext).Values, new { @class = "icon2 pagenum" });
    //    }

    //    public static MvcHtmlString Pager(this HtmlHelper helper, int totalItemCount, int pageSize, int pageIndex, string actionName, string controllerName,
    //        PagerOptions pagerOptions, string routeName, object routeValues = null, object htmlAttributes = null)
    //    {
    //        if (pagerOptions == null)
    //        {
    //            pagerOptions = new PagerOptions() { PageIndexParameterName = "page" };
    //        }

    //        var pageCount = 0;

    //        if (totalItemCount % pageSize > 0)
    //        {
    //            pageCount = totalItemCount / pageSize + 1;
    //        }
    //        else
    //        {
    //            pageCount = totalItemCount / pageSize;
    //        }

    //        var t = PagerHelper.Pager(helper, pageCount, pageIndex, actionName, controllerName, pagerOptions, routeName, routeValues, htmlAttributes);
    //        return new MvcHtmlString(t);
    //    }
    //}

    public static class RouteCollectionExtensions
    {
        public static Route MapRouteWithName(
            this RouteCollection routes, string name, string url, object defaults = null, object constrain = null)
        {
            Route route = routes.MapRoute(name, url, defaults, constrain);
            route.DataTokens = new RouteValueDictionary();
            route.DataTokens.Add("RouteName", name);
            return route;
        }
    }


}
