using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Kugar.Core.ExtMethod;
using Newtonsoft.Json;

namespace Kugar.Core.Web.WebApi
{
    public class NewtonJsonTextFormatter : MediaTypeFormatter
    {
        public NewtonJsonTextFormatter()
        {
            SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));
            SupportedEncodings.Clear();
            SupportedEncodings.Add(new UTF8Encoding(false, true));
        }

        public override bool CanReadType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return true;
        }

        public override bool CanWriteType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            return true;
        }

        public override Task<object> ReadFromStreamAsync(Type type, Stream readStream, HttpContent content,
            IFormatterLogger formatterLogger)
        {
            return Task<object>.Factory.StartNew(() =>
            {
                var str = Encoding.UTF8.GetString(readStream.ReadAllBytes());

                //if (type == typeof(ObjectId))
                //{
                //    return str.ToObjectId();
                //}
                //else if (type == typeof(ObjectId?))
                //{
                //    return str.ToObjectId();
                //}

                return JsonConvert.DeserializeObject(str, type,JsonConvert.DefaultSettings());
            });
        }


        public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content,
            TransportContext transportContext)
        {
            return Task.Factory.StartNew(() =>
            {
                byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

                try
                {
                    writeStream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception)
                {
                    
                    throw;
                }
                
            });
        }

        public static void Register(HttpConfiguration config)
        {
            config.Formatters.Insert(0, new NewtonJsonTextFormatter());
        }
    }
}
