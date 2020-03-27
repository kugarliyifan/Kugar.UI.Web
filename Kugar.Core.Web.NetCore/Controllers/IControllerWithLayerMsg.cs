using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kugar.Core.Web.Controllers
{


    public static class ControllerWithLayerMsgExt
    {

        public static void MsgBox(this Controller c, string message)
        {
            MsgBoxAndScript(c,msg: message);
        }

        /// <summary>
        /// 弹出信息,并且跳转到上一个页面,如果defaultUrl为空,则调用history.go(-1)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="defaultUrl"></param>
        public static void MsgAndGotoReferer(this Controller c, string message, string defaultUrl = "")
        {
            if (c.HttpContext.Request.Headers.TryGetValue("Referer", out var referer) && referer.HasData() && !string.IsNullOrWhiteSpace(referer.FirstOrDefault()))
            {
                MsgBoxAndGoto(c,message, referer.FirstOrDefault());
            }
            else if (!string.IsNullOrWhiteSpace(defaultUrl))
            {
                MsgBoxAndGoto(c,message, defaultUrl);
            }
            else
            {
                MsgBoxAndScript(c,message, "history.go(-1)");
            }
        }

        public static void MsgBoxAndRefresh(this Controller c, string msg)
        {
            MsgBoxAndGoto(c,msg, HttpContext.Current.Request.GetDisplayUrl());
        }

        /// <summary>
        /// 弹出提示框,点击确认后,跳转指定连接,如果当前页为弹出窗口,则当前窗口跳转到指定url
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="url"></param>
        public static void MsgBoxAndGoto(this Controller c, string msg, string url)
        {
            MsgBoxAndScript(c,msg, $"WebUIJS.GoTo('{url}',this);");
        }


        public static void MsgBoxAndScript(this Controller c, string msg, string script = "")
        {
            if (string.IsNullOrWhiteSpace(msg) && string.IsNullOrWhiteSpace(script))
            {
                return;
            }

            List<VM_MVCMsgBoxItem> msgList = (List<VM_MVCMsgBoxItem>)c.HttpContext.Items["MsgData_Temp"];

            if (msgList == null)
            {
                msgList=new List<VM_MVCMsgBoxItem>(2);
                c.HttpContext.Items["MsgData_Temp"] = msgList;
            }
            else
            {
                msgList = (List<VM_MVCMsgBoxItem>)c.HttpContext.Items["MsgData_Temp"];
            }

            msgList.Add(new VM_MVCMsgBoxItem(msg, script));
        }
    }

    /// <summary>
    /// 在view中,用于输出消息框的
    /// </summary>
    public class LayerServerMsg
    {
        private ViewContext _viewContext = null;

        public LayerServerMsg(ViewContext viewContext) 
        {
            _viewContext = viewContext;
        }

        public HtmlString RenderJS(bool renderJQCdn = false, bool renderLayerCdn = false)
        {
            var lst = (List<VM_MVCMsgBoxItem>)_viewContext.HttpContext.Items["MsgData_Temp"];

            if (lst.HasData())
            {
                var n1 = $"scriptServerMsg_{RandomEx.Next()}";

                if (renderJQCdn)
                {
                    _viewContext.Writer.WriteLine($"<script src=\"https://cdn.bootcss.com/jquery/2.2.4/jquery.min.js\" charset=\"utf-8\"></script>");
                }

                if (renderLayerCdn)
                {
                    _viewContext.Writer.WriteLine($"<script src=\"https://cdn.bootcss.com/layer/1.8.5/skin/layer.css\" charset=\"utf-8\"></script>");
                    _viewContext.Writer.WriteLine($"<script src=\"https://cdn.bootcss.com/layer/1.8.5/layer.min.js\" charset=\"utf-8\"></script>");
                }

                _viewContext.Writer.WriteLine($"<script id=\"{n1}\" type=\"text/javascript\">");

                _viewContext.Writer.WriteLine($@"var WebUIJS=WebUIJS??{{}};
                            WebUIJS.GoTo=function(url){{window.location.href=url;}};
                                ");

                _viewContext.Writer.WriteLine("$(document).ready(function(){");

                foreach (var item in lst)
                {
                    if (!string.IsNullOrWhiteSpace(item.Message))
                    {
                        _viewContext.Writer.WriteLine($"layer.alert('{item.Message}',function(index){{ layer.close(index); ");
                    }

                    if (!string.IsNullOrWhiteSpace(item.JavsScript))
                    {
                        var n = $"severmsg_{RandomEx.Next()}";
                        _viewContext.Writer.WriteLine($"function {n} (){{");
                        _viewContext.Writer.WriteLine(item.JavsScript);
                        _viewContext.Writer.WriteLine("}");
                        _viewContext.Writer.WriteLine($"{n}.call($('#{n1}'));");
                    }

                    //_viewContext.Writer.WriteLine("});");

                }
                _viewContext.Writer.WriteLine("}) });");


                _viewContext.Writer.WriteLine("</script>");

            }

            return HtmlString.Empty;
        }
    }

    public class WeUIServerMsg
    {
        private ViewContext _viewContext = null;

        public WeUIServerMsg(ViewContext viewContext)
        {
            _viewContext = viewContext;
        }

        public HtmlString RenderJS(bool renderWeUIJSCdn = false,bool renderWeUICssCdn=false)
        {
            var lst = (List<VM_MVCMsgBoxItem>)_viewContext.HttpContext.Items["MsgData_Temp"];

            if (lst.HasData())
            {
                var n1 = $"scriptServerMsg_{RandomEx.Next()}";

                if (renderWeUIJSCdn)
                {
                    _viewContext.Writer.WriteLine($"<script type=\"text/javascript\" src=\"https://res.wx.qq.com/open/libs/weuijs/1.2.1/weui.min.js\"></script>");
                }

                if (renderWeUICssCdn)
                {
                    _viewContext.Writer.WriteLine($"<link rel=\"stylesheet\" href=\"https://res.wx.qq.com/open/libs/weui/2.0.1/weui.min.css\">");
                }

                _viewContext.Writer.WriteLine($"<script id=\"{n1}\" type=\"text/javascript\">");

                //_viewContext.Writer.WriteLine($@"var WebUIJS=WebUIJS??{{}};
                //            WebUIJS.GoTo=function(url){{window.location.href=url;}};
                //                ");

                _viewContext.Writer.WriteLine("$(document).ready(function(){");

                foreach (var item in lst)
                {
                    if (!string.IsNullOrWhiteSpace(item.Message))
                    {
                        _viewContext.Writer.WriteLine($"var alertDom = weui.alert('{item.Message}',function(){{ alertDom.hide();\n ");
                    }

                    if (!string.IsNullOrWhiteSpace(item.JavsScript))
                    {
                        var n = $"severmsg_{RandomEx.Next()}";
                        _viewContext.Writer.WriteLine($"function {n} (){{");
                        _viewContext.Writer.WriteLine(item.JavsScript);
                        _viewContext.Writer.WriteLine("}");
                        _viewContext.Writer.WriteLine($"{n}.call($('#{n1}'));");
                    }

                    //_viewContext.Writer.WriteLine("});");

                }
                _viewContext.Writer.WriteLine("return false;}) });");


                _viewContext.Writer.WriteLine("</script>");

            }

            return HtmlString.Empty;
        }
    }
}
