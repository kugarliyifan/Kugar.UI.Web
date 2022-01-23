using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fasterflect;
using Kugar.Core.ExtMethod;
using Kugar.Core.Web.Resources;
using Kugar.Core.Web.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Kugar.Core.Web.Filters
{
    public class ValidateRuleAttribute : ActionFilterAttribute, IActionHttpMethodProvider
    {
        private static string[] _httpMethod = new[] { "RULE" };
        private static ResourceManagerStringLocalizerFactory _defaultLocalizerFactory = null;


        static ValidateRuleAttribute()
        {
            _defaultLocalizerFactory = new ResourceManagerStringLocalizerFactory(new OptionsWrapper<LocalizationOptions>(new LocalizationOptions()), new NullLoggerFactory());
        }

        public ValidateRuleAttribute()
        {
            this.Order = 0;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext.Request.Method.CompareTo("RULE", true))
            {
                var rules = new JObject();
                var formData = new JObject();

                foreach (ControllerParameterDescriptor parameter in context.ActionDescriptor.Parameters)
                {
                    var attrs = parameter.ParameterInfo.GetCustomAttributes(typeof(ValidationAttribute), true).ToArrayEx();

                    var array = new JArray();
                    var displayName = ((DisplayNameAttribute)parameter.ParameterInfo.GetCustomAttributes(typeof(DisplayAttribute), true).FirstOrDefault())?.DisplayName;

                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = ((DescriptionAttribute)parameter.ParameterInfo.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault())?.Description;
                    }

                    if (string.IsNullOrEmpty(displayName))
                    {
                        displayName = parameter.Name;
                    }

                    var pt = parameter.ParameterType;

                    if (IsNullableType(pt))
                    {
                        pt = pt.GetGenericArguments()[0];
                    }

                    //if (pt == typeof(DateTime) || pt == typeof(DateTime?))
                    //{
                    //    array.Add(new JObject()
                    //    {
                    //        //["type"] = "date",
                    //        ["pattern"]=@"/^[1-9]\d{3}-(0[1-9]|1[0-2])-(0[1-9]|[1-2][0-9]|3[0-1])\s+(20|21|22|23|[0-1]\d):[0-5]\d:[0-5]\d$/",
                    //        ["message"] ="请输入正确的日期格式"
                    //    });

                    //}
                    if (pt==typeof(int) || pt==typeof(short) || pt==typeof(long))
                    {
                        array.Add(new JObject()
                        {
                            ["type"]="integer",
                            ["message"] ="请输入正确的数字格式"
                        });
                    }
                    else if (pt == typeof(float) || pt == typeof(short) || pt == typeof(decimal))
                    {
                        array.Add(new JObject()
                        {
                            ["type"]="number",
                            ["message"]="请输入正确的数字格式"
                        });
                    }
                    else if (pt == typeof(string))
                    {
                        array.Add(new JObject()
                        {
                            ["type"] = "string",
                            ["message"] = "请输入正确的字符串格式"
                        });
                    }
                    else if (pt == typeof(bool))
                    {
                        array.Add(new JObject()
                        {
                            ["type"]="bool",
                            ["message"] = "请输入选择正确的格式"
                        });
                    }

                    if (attrs.HasData())
                    {
                        foreach (ValidationAttribute attr in attrs)
                        {
                            var errorMessage = attr.ErrorMessage ?? getValidateAttrErrorMessage(context, attr);
                            JObject json = null;
                            if (attr is RequiredAttribute required)
                            {
                                json = new JObject()
                                {
                                    ["required"] = true,
                                    ["message"] = string.Format(errorMessage, displayName),
                                    ["trigger"] =  new JArray("change","blur")
                                };
                            }
                            
                            else if (attr is EmailAddressAttribute email)
                            {
                                json = new JObject()
                                {
                                    ["email"] = true,
                                    ["message"] = string.Format(errorMessage, displayName),
                                    ["trigger"] = new JArray("change","blur")
                                };
                            }
                            else if (attr is MinValueAttribute minv)
                            {
                                json = new JObject()
                                {
                                    ["min"] = JToken.FromObject(minv.Minimum),
                                    ["message"] = attr.ErrorMessage ?? attr.FormatErrorMessage(displayName),
                                    ["trigger"] =  new JArray("change","blur")
                                };

                                if (pt.IsNumericType())
                                {
                                    json.Add("type","number");
                                }
                            }
                            else if (attr is MaxValueAttribute maxv)
                            {
                                json = new JObject()
                                {
                                    ["max"] = JToken.FromObject(maxv.Maximum),
                                    ["message"] = attr.ErrorMessage ?? attr.FormatErrorMessage(displayName),
                                    ["trigger"] =  new JArray("change","blur")
                                };
                                if (pt.IsNumericType())
                                {
                                    json.Add("type","number");
                                }
                            }
                            else if (attr is MinLengthAttribute minlen)
                            {
                                json = new JObject()
                                {
                                    ["min"] = minlen.Length,
                                    ["message"] = string.Format(errorMessage, displayName, minlen.Length),
                                    ["trigger"] =  new JArray("change","blur")
                                };

                                if (pt==typeof(string))
                                {
                                    json.Add("type","string");
                                }

                            }
                            else if (attr is MaxLengthAttribute maxlen)
                            {
                                json = new JObject()
                                {
                                    ["max"] = maxlen.Length,
                                    ["message"] = string.Format(errorMessage, displayName, maxlen.Length),
                                    ["trigger"] =  new JArray("change","blur")
                                };

                                if (pt==typeof(string))
                                {
                                    json.Add("type","string");
                                }
                            }
                            else if (attr is RangeAttribute range)
                            {
                                json = new JObject()
                                {
                                    ["min"] = JToken.FromObject(range.Minimum),
                                    ["max"] = JToken.FromObject(range.Maximum),
                                    ["message"] = string.Format(errorMessage, displayName, range.Minimum, range.Maximum),
                                    ["trigger"] = new JArray("change","blur")
                                };
                                if (pt.IsNumericType())
                                {
                                    json.Add("type","number");
                                }
                            }
                            else if (attr is StringLengthAttribute len)
                            {
                                json = new JObject()
                                {
                                    ["min"] = len.MinimumLength,
                                    ["max"] = len.MaximumLength,
                                    ["message"] = string.Format(errorMessage, displayName, len.MaximumLength),
                                    ["trigger"] =  new JArray("change","blur")
                                };
                                if (pt==typeof(string))
                                {
                                    json.Add("type","string");
                                }
                            }
                            array.Add(json);
                        }
                    }

                    

                    if (array.HasData())
                    {
                        rules.Add(parameter.Name, array);
                    }
                    

                    var defaultValue = parameter.ParameterInfo.DefaultValue;

                    if (defaultValue == null || defaultValue == DBNull.Value)
                    {
                        if (IsNullableType(parameter.ParameterType))
                        {
                            //var type = parameter.ParameterType.GetGenericArguments()[0];
                            defaultValue = null;
                        }
                        else
                        {
                            defaultValue = parameter.ParameterType.GetDefaultValue();
                        }
                    }

                    formData.Add(parameter.Name, defaultValue == null ? null : JToken.FromObject(defaultValue));
                }

                context.Result = new JsonResult(new JObject()
                {
                    ["rules"] = rules,
                    ["formData"] = formData
                });

                return;
            }

            await base.OnActionExecutionAsync(context, next);
        }

        private string getValidateAttrErrorMessage(ActionExecutingContext context, ValidationAttribute validatorAttr)
        {
            var f = (IStringLocalizerFactory)context.HttpContext.RequestServices.GetService(typeof(IStringLocalizerFactory));

            IStringLocalizer loc = null;


            var propertyKey = (string)validatorAttr.GetPropertyValue("ErrorMessageString");

            if (propertyKey != null)
            {
                if (loc == null)
                {
                    if (f != null)
                    {
                        loc = f.Create(typeof(DataAnnotationsResources))
#if NETCOREAPP3_1 || NETCOREAPP2_1 || NETCOREAPP3_0  
                                
                    .WithCulture(Thread.CurrentThread.CurrentUICulture)
#endif
                            ;
                    }
                    else
                    {
                        loc = _defaultLocalizerFactory.Create(typeof(DataAnnotationsResources));

                    }
                }

                if (loc != null)
                {
                    var v = loc[propertyKey];

                    if (!string.IsNullOrEmpty(v))
                    {
                        return v;
                    }
                }
            }

            return validatorAttr.ErrorMessage;

        }

        public IEnumerable<string> HttpMethods => _httpMethod;

        private static bool IsNullableType( Type that) => !that.IsArray && (object) that != null && that.FullName.StartsWith("System.Nullable`1[");
    }

}
