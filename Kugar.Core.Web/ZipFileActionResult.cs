using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;

namespace Kugar.Core.Web
{
    /// <summary>
    /// 返回Zip压缩文件
    /// </summary>
    public class ZipFileActionResult:ActionResult
    {
        private KeyValuePair<string, Stream>[] _zipFile = null;
        private string _fileName = "";

        public ZipFileActionResult(string fileName, KeyValuePair<string, Stream>[] zipFile)
        {
            _zipFile = zipFile;
            _fileName = fileName;
        }

        public ZipFileActionResult(string fileName, KeyValuePair<string, byte[]>[] zipFile)
        {
            _zipFile = zipFile.Select(x=>new KeyValuePair<string,Stream>(key:x.Key,value:new ByteStream(x.Value))).ToArray();
            _fileName = fileName;
        }


        public override void ExecuteResult(ControllerContext context)
        {

            using (var stream = new MemoryStream())
            {
                using (ZipFile zip = ZipFile.Create(stream))
                {
                    zip.BeginUpdate();

                    foreach (var file in _zipFile)
                    {
                        SteamDataSource s1 = new SteamDataSource(file.Value);

                        zip.Add(s1,file.Key);
                    
                    }

                    zip.CommitUpdate();
                }

                var response = context.HttpContext.Response;

                if (!string.IsNullOrWhiteSpace(_fileName))
                {
                    response.AddHeader("Content-Disposition",
                        "attachment;filename=" +
                        HttpUtility.UrlEncode(_fileName, System.Text.Encoding.UTF8).Replace("+", "%20"));
                }
                
                response.ContentType = "application/zip";

                stream.Seek(0L, SeekOrigin.Begin);

                var data = stream.ReadAllBytes();

                response.AddHeader("Content-Length ", data.Length.ToString());
                
                response.BinaryWrite(data);                
            }


            
        }

        private class SteamDataSource : IStaticDataSource
        {
            public Stream Stream { get; set; }

            public SteamDataSource(Stream s)
            {
                this.Stream = s;
            }

            public Stream GetSource()
            {
                return Stream;
            }
        }
    }
}
