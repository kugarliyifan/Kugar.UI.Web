using System;
using System.ComponentModel.DataAnnotations;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web.Validators
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class MinValueAttribute : RangeAttribute
    {

        public MinValueAttribute(double minimum) : base(minimum, double.MaxValue)
        {
            
        }

        public MinValueAttribute(int minimum) : base(minimum, int.MaxValue)
        {
        }

        public MinValueAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum)
        {
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format($"{name}值不能小于{Minimum}");
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class MaxValueAttribute : RangeAttribute
    {
        public MaxValueAttribute(double maximum) : base(double.MinValue, maximum)
        {
        }

        public MaxValueAttribute(int maximum) : base(double.MinValue, maximum)
        {
        }

        public MaxValueAttribute(Type type, string minimum, string maximum) : base(type, minimum, maximum)
        {
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name}值不能大于{Maximum}";
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class ValueInAttribute : ValidationAttribute
    {
        private object[] _values = null;

        public ValueInAttribute(object[] inputValue)
        {
            _values = inputValue.ToArrayEx();
        }

        public override bool IsValid(object value)
        {
            var isSuccess = false;

            foreach (var v in _values)
            {
                if (v.SafeEquals(value))
                {
                    isSuccess = true;
                    return true;
                    break;
                }
            }

            if (!isSuccess)
            {
                return false;
                //return new ValidationResult("值不存在于备选值中");
            }

            return true;

            return base.IsValid(value);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name}值不在备选列表中";

            return base.FormatErrorMessage(name);
        }
    }
}
