using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Net;
using System.IO;

namespace UploadHelper
{
    public class HttpUploadHelper
    {
        private HttpUploadHelper()
        { }

        public static string Upload(string url, UploadFile[] files, NameValueCollection form)
        {
            var resp = Upload((HttpWebRequest)WebRequest.Create(url), files, form);

            using (var s = resp.GetResponseStream())
            using (var sr = new StreamReader(s))
            {
                return sr.ReadToEnd();
            }
        }

        public static HttpWebResponse Upload(HttpWebRequest req, UploadFile[] files, NameValueCollection form)
        {
            var mimeParts = new List<MimePart>();

            try
            {
                foreach (var key in form.AllKeys)
                {
                    var part = new StringMimePart();

                    part.Headers["Content-Disposition"] = "form-data; name=\"" + key + "\"";
                    part.StringData = form[key];

                    mimeParts.Add(part);
                }

                var nameIndex = 0;

                foreach (var file in files)
                {
                    var part = new StreamMimePart();

                    if (string.IsNullOrEmpty(file.FieldName))
                        file.FieldName = "file" + nameIndex++;

                    part.Headers["Content-Disposition"] = "form-data; name=\"" + file.FieldName + "\"; filename=\"" + file.FileName + "\"";
                    part.Headers["Content-Type"] = file.ContentType;

                    part.SetStream(file.Data);

                    mimeParts.Add(part);
                }

                var boundary = "----------" + DateTime.Now.Ticks.ToString("x");

                req.ContentType = "multipart/form-data; boundary=" + boundary;
                req.Method = "POST";

                long contentLength = 0;

                var footer = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");

                foreach (var part in mimeParts)
                {
                    contentLength += part.GenerateHeaderFooterData(boundary);
                }

                req.ContentLength = contentLength + footer.Length;

                var buffer = new byte[512*1024];
                var afterFile = Encoding.UTF8.GetBytes("\r\n");

            	using (var s = req.GetRequestStream())
                {
                    foreach (var part in mimeParts)
                    {
                        s.Write(part.Header, 0, part.Header.Length);

                    	int read;
                    	while ((read = part.Data.Read(buffer, 0, buffer.Length)) > 0)
                            s.Write(buffer, 0, read);

                        part.Data.Dispose();

                        s.Write(afterFile, 0, afterFile.Length);
                    }

                    s.Write(footer, 0, footer.Length);
                }

                return (HttpWebResponse)req.GetResponse();
            }
            catch
            {
                foreach (var part in mimeParts)
                    if (part.Data != null)
                        part.Data.Dispose();

                throw;
            }
        }
    }
}