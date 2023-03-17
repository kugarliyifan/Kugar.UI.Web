using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kugar.Core.Web.Validators
{
    /// <summary>
    /// 判断Guid字段值不能为空,且不能全0
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class GuidNotEmptyAttribute : ValidationAttribute
    {
        public GuidNotEmptyAttribute(string errorMessage= "不能为空") : base(errorMessage) { }

        public override bool IsValid(object value)
        {
            if (value == null)
                return false;

            if (value is string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return false;
                }

                if (Guid.TryParseExact(str, "D", out var g))
                {
                    return g!=Guid.Empty;
                }
                else if (Guid.TryParseExact(str, "N", out var g1))
                {
                    return g1 != Guid.Empty;
                }
                else
                {
                    if (Guid.TryParse(str, out var g2))
                    {
                        return g2 != Guid.Empty;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            else if (value is Guid g)
            {
                return g != Guid.Empty;
            }
            else
            {
                return false;
            }

        }
    }

}
