using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Kugar.Core.Web.Controllers
{
#if NETCOREAPP3_0 || NETCOREAPP3_1
    public interface IControllerWithLayerMsg
    {

        protected void MsgBox(string message)
        {
            MsgBoxAndScript(msg: message);
        }

        /// <summary>
        /// 弹出信息,并且跳转到上一个页面,如果defaultUrl为空,则调用history.go(-1)
        /// </summary>
        /// <param name="message"></param>
        /// <param name="defaultUrl"></param>
        protected void MsgAndGotoReferer(string message, string defaultUrl = "")
        {
            if (HttpContext.Current.Request.Headers.TryGetValue("Referer", out var referer) && referer.HasData() && !string.IsNullOrWhiteSpace(referer.FirstOrDefault()))
            {
                MsgBoxAndGoto(message, referer.FirstOrDefault());
            }
            else if (!string.IsNullOrWhiteSpace(defaultUrl))
            {
                MsgBoxAndGoto(message, defaultUrl);
            }
            else
            {
                MsgBoxAndScript(message, "history.go(-1)");
            }
        }

        protected void MsgBoxAndRefresh(string msg)
        {
            MsgBoxAndGoto(msg, HttpContext.Current.Request.GetDisplayUrl());
        }

        /// <summary>
        /// 弹出提示框,点击确认后,跳转指定连接,如果当前页为弹出窗口,则当前窗口跳转到指定url
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="url"></param>
        protected void MsgBoxAndGoto(string msg, string url)
        {
            MsgBoxAndScript(msg, $"WebUIJS.GoTo('{url}',this);");
        }


        protected void MsgBoxAndScript(string msg, string script = "")
        {
            if (string.IsNullOrWhiteSpace(msg) && string.IsNullOrWhiteSpace(script))
            {
                return;
            }

            List<VM_MVCMsgBoxItem> msgList = (List<VM_MVCMsgBoxItem>)HttpContext.Current.Items["MsgData_Temp"];

            if (msgList == null)
            {
                msgList=new List<VM_MVCMsgBoxItem>(2);
                HttpContext.Current.Items["MsgData_Temp"] = msgList;
            }
            else
            {
                msgList = (List<VM_MVCMsgBoxItem>) HttpContext.Current.Items["MsgData_Temp"];
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

        public HtmlString RenderNoWebUIJS(bool forceRenderJQ = false, bool renderLayerCdn = false)
        {
            var lst = (List<VM_MVCMsgBoxItem>)_viewContext.HttpContext.Items["MsgData_Temp"];

            

            if (lst.HasData())
            {
                var n1 = $"scriptServerMsg_{RandomEx.Next()}";

                if (forceRenderJQ)
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
#endif


}
