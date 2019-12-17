using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web.MVCApiExport
{
    public class MVCWebServiceController : Controller
    {
        [HttpGet]
        public MVCResultReturn<string[]> GetModule()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var moduleList = new List<Type>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();

                foreach (var type in types)
                {
                    if (type.Name.EndsWith("Controller", true, null) 
                        && type.IsSubclassOf(typeof(Controller))
                        && type.GetAttribute<MVCServiceAttribute>() != null 
                        && type.GetAttribute<NonActionAttribute>()!=null
                        && type.GetAttribute(typeof(ChildActionOnlyAttribute))!=null)
                    {
                        moduleList.Add(type);
                    }
                }
            }

            if (!moduleList.HasData())
            {
                return new MVCResultReturn<string[]>(new ResultReturn(false, message: "不存在可导出的列表"));
            }
            else
            {
                return
                    new MVCResultReturn<string[]>(new ResultReturn<string[]>(true,
                        moduleList.Select(x => x.FullName).ToArray()));
            }
        }


        [HttpGet]
        public MVCResultReturn<MVCServiceModuleInfo> GetModuleByName(string moduleName)
        {
            var type = Type.GetType(moduleName);

            if (type == null)
            {
                return new MVCResultReturn<MVCServiceModuleInfo>(new ResultReturn(false));
            }

            var moduleInfoLst = new List<MVCServiceModuleInfo>();

            var methods =
                type.GetMethods(BindingFlags.Public & BindingFlags.Instance)
                    .Where(x => x.GetAttribute<MVCServiceNoExportAttribute>() == null);

            if (!methods.HasData())
            {
                return new MVCResultReturn<MVCServiceModuleInfo>(new ResultReturn(false));
            }

            var temp = new MVCServiceModuleInfo();

            temp.Name = type.Name;
            temp.Summary = type.GetXmlDocumentation();

            var methodLst = new List<MVCServiceMethodInfo>();

            foreach (var method in methods)
            {
                var methodSummary = method.GetXmlDocumentation();

                var tempMethod = new MVCServiceMethodInfo();

                tempMethod.Name = method.Name;
                tempMethod.Summary = methodSummary;

                tempMethod.ReturnType = method.ReturnType.FullName;
                tempMethod.ReturnSummary = method.ReturnParameter.GetXmlDocumentation();

                methodLst.Add(tempMethod);

                var paramlist = new List<MVCServiceMethodParameterInfo>();

                foreach (var parameterInfo in method.GetParameters())
                {
                    var tempParamter = new MVCServiceMethodParameterInfo();

                    tempParamter.ParameterName = parameterInfo.Name;
                    tempParamter.Summary = parameterInfo.GetXmlDocumentation();
                    tempParamter.ValueType = parameterInfo.ParameterType.FullName;
                    tempParamter.DefaultValue = parameterInfo.DefaultValue.ToStringEx();
                    tempParamter.ParameterValueType = getServiceTypeEnumByType(parameterInfo.ParameterType);

                    paramlist.Add(tempParamter);
                }

                tempMethod.Parameters = paramlist.ToArray();
            }

            temp.Methods = methodLst.ToArray();
            moduleInfoLst.Add(temp);

            return new MVCResultReturn<MVCServiceModuleInfo>(temp);
        }

        public MVCResultReturn<MVCServiceClassProperty> GetTypeClassProperty(string moduleName)
        {
            var clsType = Type.GetType(moduleName);

            if (clsType==null)
            {
                return new MVCResultReturn<MVCServiceClassProperty>(new FailResultReturn("不存在指定的类型"));
            }

            var properties = clsType.GetProperties(BindingFlags.Public & BindingFlags.Instance);

            var lst = new List<MVCServiceMethodParameterInfo>();

            foreach (var property in properties)
            {
                var temp = new MVCServiceMethodParameterInfo();
                temp.DefaultValue = property.PropertyType.GetDefaultValue().ToString();
                temp.ParameterName = property.Name;
                temp.ValueType = property.PropertyType.FullName;
                temp.ParameterValueType = getServiceTypeEnumByType(property.PropertyType);
                temp.Summary = property.GetXmlDocumentation();

                lst.Add(temp);
            }

            var fields = clsType.GetFields(BindingFlags.Public & BindingFlags.Instance);

            foreach (var field in fields)
            {
                var temp = new MVCServiceMethodParameterInfo();
                temp.DefaultValue = field.FieldType.GetDefaultValue().ToString();
                temp.ParameterName = field.Name;
                temp.ValueType = field.FieldType.FullName;
                temp.ParameterValueType = getServiceTypeEnumByType(field.FieldType);
                temp.Summary = field.GetXmlDocumentation();

                lst.Add(temp);
            }

            var tempClass = new MVCServiceClassProperty();

            tempClass.Summary = clsType.GetXmlDocumentation();
            tempClass.Properties = lst.ToArray();
            tempClass.Name = clsType.Name;

            return new MVCResultReturn<MVCServiceClassProperty>(tempClass);
        }

        public MVCResultReturn<string[]> GetEnumValues(string enumName)
        {
            var type = Type.GetType(enumName);

            if (type==null)
            {
                return new MVCResultReturn<string[]>(new FailResultReturn("不存在指定的类型"));
            }

            var s = Enum.GetNames(type);

            return new MVCResultReturn<string[]>(s);
        }

        private MVCServiceTypeEnum getServiceTypeEnumByType(Type type)
        {
            MVCServiceTypeEnum e;

            if (type==typeof(int))
            {
                e= MVCServiceTypeEnum.Int;
            }
            else if (type==typeof(int?))
            {
                e=MVCServiceTypeEnum.IntNullable;
            }
            else if (type == typeof(long))
            {
                e = MVCServiceTypeEnum.Long;
            }
            else if (type == typeof(long?))
            {
                e = MVCServiceTypeEnum.LongNullable;
            }
            else if (type==typeof(decimal))
            {
                e=MVCServiceTypeEnum.Decimal;
            }
            else if (type==typeof(decimal?))
            {
                e= MVCServiceTypeEnum.DecimalNullable;
            }
            else if (type==typeof(float))
            {
                e=MVCServiceTypeEnum.Float;
            }
            else if (type==typeof(float?))
            {
                e=MVCServiceTypeEnum.FloatNullable;
            }
            else if (type==typeof(double))
            {
                e=MVCServiceTypeEnum.Double;
            }
            else if (type==typeof(double?))
            {
                e=MVCServiceTypeEnum.DoubleNullable;
            }
            else if (type==typeof(string))
            {
                e=MVCServiceTypeEnum.String;
            }
            else if (type==typeof(bool))
            {
                e=MVCServiceTypeEnum.Boolean;
            }
            else if (type.IsEnum)
            {
                e = MVCServiceTypeEnum.Enum;
            }
            else
            {
                e=MVCServiceTypeEnum.Class;
            }

            return e;
        }
    }

    public class MVCServiceModuleInfo
    {
        public string Name { set; get; }

        public string Summary { set; get; }

        public MVCServiceMethodInfo[] Methods { set; get; }
    }

    public class MVCServiceMethodInfo
    {
        public string Name { set; get; }

        public string Summary { set; get; }

        public string ReturnType { set; get; }

        public string ReturnSummary { set; get; }

        public MVCServiceMethodParameterInfo[] Parameters { set; get; }
    }

    public class MVCServiceMethodParameterInfo
    {
        public string ParameterName { set; get; }

        public string Summary { set; get; }

        public MVCServiceTypeEnum ParameterValueType { set; get; }

        public string DefaultValue { set; get; }


        public string ValueType { set; get; }
    }

    public class MVCServiceClassProperty
    {
        public string Name { set; get; }

        public string Summary { set; get; }

        public MVCServiceMethodParameterInfo[] Properties { set; get; }
    }

    //public class MVCServicePropertyInfo
    //{
    //    public string Name { set; get; }

    //    public string Summary { set; get; }

    //    public MVCServiceTypeEnum ParameterValueType { set; get; }

    //    public string ValueType { set; get; }
    //}


    public enum MVCServiceTypeEnum
    {
        Int,
        IntNullable,
        Long,
        LongNullable,
        Decimal,
        DecimalNullable,
        Double,
        DoubleNullable,
        Float,
        FloatNullable,
        String,
        Enum,
        Boolean,
        Class

    }
}
