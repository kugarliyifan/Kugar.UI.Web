using Kugar.Core.ExtMethod;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kugar.Core.Web.Validators
{
    /// <summary>
    /// 检查字段值是否为手机号
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class MobileCheckAttribute : ValidationAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="required">是否必填,默认为false</param>
        /// <param name="errorMessage"></param>
        public MobileCheckAttribute(bool required = false, string errorMessage = "输入数据不是手机号") : base(errorMessage)
        {
            Required = required;
        }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required { set; get; }

        public override bool IsValid(object value)
        {
            if ((value == null || (string)value == ""))
            {
                return !Required;
            }
                

            if (value is string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return !Required;
                }

                return str.IsMatchPhoneNumber();
            }
            else
            {
                return !Required;
            }

            //return !(value is string str) ||  string.IsNullOrEmpty(str);

        }
    }
}
