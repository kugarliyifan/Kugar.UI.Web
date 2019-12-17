using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web
{
    /// <summary>
    /// A custom <see cref="IDocumentationProvider"/> that reads the API documentation from an XML documentation file.
    /// </summary>
    public class XmlDocumentationProvider : IDocumentationProvider
    {
        //private XPathNavigator _documentNavigator;
        //private const string TypeExpression = "/doc/members/member[@name='T:{0}']";
        //private const string MethodExpression = "/doc/members/member[@name='M:{0}']";
        //private const string PropertyExpression = "/doc/members/member[@name='P:{0}']";
        //private const string FieldExpression = "/doc/members/member[@name='F:{0}']";
        //private const string ParameterExpression = "param[@name='{0}']";

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDocumentationProvider"/> class.
        /// </summary>
        /// <param name="documentPath">The physical path to XML document.</param>
        public XmlDocumentationProvider()
        {
        }

        public string GetDocumentation(HttpControllerDescriptor controllerDescriptor)
        {
            return controllerDescriptor.ControllerType.GetXmlDocumentation();
        }

        public virtual string GetDocumentation(HttpActionDescriptor actionDescriptor)
        {
            ReflectedHttpActionDescriptor reflectedActionDescriptor = actionDescriptor as ReflectedHttpActionDescriptor;
            if (reflectedActionDescriptor != null)
            {
                return reflectedActionDescriptor.MethodInfo.GetXmlDocumentation();
            }
            else
            {
                return "";
            }
        }

        public virtual string GetDocumentation(HttpParameterDescriptor parameterDescriptor)
        {
            ReflectedHttpParameterDescriptor reflectedParameterDescriptor = parameterDescriptor as ReflectedHttpParameterDescriptor;
            if (reflectedParameterDescriptor != null)
            {
                return reflectedParameterDescriptor.ParameterInfo.GetXmlDocumentation();
            }

            return "";
        }

        public string GetResponseDocumentation(HttpActionDescriptor actionDescriptor)
        {
            ReflectedHttpActionDescriptor reflectedActionDescriptor = actionDescriptor as ReflectedHttpActionDescriptor;

            if (reflectedActionDescriptor!=null)
            {
                return reflectedActionDescriptor.MethodInfo.ReturnParameter.GetXmlDocumentation();
            }
            else
            {
                return "";
            }
        }

        public static void RegisterToConfig(HttpConfiguration config)
        {
            config.Services.Replace(typeof(IDocumentationProvider),new XmlDocumentationProvider());
        }

        //public string GetDocumentation(MemberInfo member)
        //{
        //    return member.GetXmlDocumentation();
        //}

        //public string GetDocumentation(Type type)
        //{
        //    return type.GetXmlDocumentation();
        //}

    }
}
