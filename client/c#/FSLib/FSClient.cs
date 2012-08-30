using System.Globalization;

namespace FSLib
{
    #region Using

    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Net.Mime;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web;

    using UploadHelper;

    #endregion

    public class FSClient
    {
        enum Action
        {
            del,
            exists,
            size,
            get,
            put,
            md5
        }

        #region Fields

        private readonly long _chunkSize;
        
        private readonly string _storageUrl;

        private readonly int _requestTimeOut;

        #endregion

        #region Constructors

        /// <summary>
        /// Конструктор с настройками библиотеки
        /// </summary>
        /// <param name="chunkSizeByte">Размер порции пересылаемых данных, байт</param>
        /// <param name="requestTimeOutSec">Таймаут запроса, сек</param>
        /// <param name="storageUrl">Адрес системы хранения</param>
        /// <param name="concurrentConnectionLimit">Количество одновременных потоков работы с системой хранения</param>
        public FSClient(int chunkSizeByte, int requestTimeOutSec, string storageUrl, int concurrentConnectionLimit)
        {
            _chunkSize = chunkSizeByte;
            _requestTimeOut = (int)TimeSpan.FromSeconds(requestTimeOutSec).TotalMilliseconds;
            _storageUrl = new Uri(storageUrl).ToString();
            ServicePointManager.DefaultConnectionLimit = concurrentConnectionLimit;
        }

        #endregion

        #region Public Methods

        public SimpleActionResult DelFile(string fileName, string id, string scope)
        {
            return ProcessSimpleReuest(Action.del, fileName, id, scope);
        }

        public SimpleActionResult FileExists(string fileName, string id, string scope)
        {
            return ProcessSimpleReuest(Action.exists, fileName, id, scope);
        }

        public LongActionResult FileLength(string fileName, string id, string scope)
        {
            var url = GetUrl(
                Action.size,
                fileName,
                id,
                scope);
            using (var response = SendHttpRequest(url))
            {

                var result = new LongActionResult(CheckResponseStatusCode(response));
                if (result.Status == ActionStatus.Error)
                    return result;

                var responseData = ReadTextResponse(response);
                if (!long.TryParse(responseData, out result.Value))
                {
                    result.Status = ActionStatus.Error;
                    result.ErrorDescription = "Can't parse response";
                }
                return result;
            }
        }

        public StreamActionResult GetFile(string fileName, string id, string scope)
        {
            var url = GetUrl(
                Action.get,
                fileName,
                id,
                scope);
            var response = SendHttpRequest(url);

            var result = new StreamActionResult(CheckResponseStatusCode(response));
            if (result.Status == ActionStatus.Error)
                return result;

            result.Size = response.ContentLength;
            result.Value = response.GetResponseStream();
            return result;
        }

        public SimpleActionResult SendFile(Stream fileStream, string fileName, string id, string scope, bool shouldCloseStream = true)
        {
            var url = GetUrl(
                Action.put,
                fileName,
                id,
                scope);

            var br = new BinaryReader(fileStream);
            while (true)
            {
                var total = br.BaseStream.Length;
                var start = br.BaseStream.Position;
                var end = total - start > _chunkSize ? start + _chunkSize - 1 : total - 1;

                var form = new NameValueCollection { { "start", start.ToString() }, { "end", end.ToString() }, { "total", total.ToString() } };

                using (var chunk = new MemoryStream(br.ReadBytes((int)(end - start + 1))))
                {
                    var files = new[] {new UploadFile(chunk, "data", fileName, "application/octet-stream")};
                    using (var response = HttpUploadHelper.Upload((HttpWebRequest)WebRequest.Create(url), files, form))
                    {
                        var result = CheckResponseStatusCode(response);
                        if (result.Status == ActionStatus.Error)
                            return result;
                    }
                }

                if (end == total - 1)
                {
                    break;
                }
            }

            if (shouldCloseStream)
                br.Close();

            return new SimpleActionResult() {Status = ActionStatus.Ok};
        }

        public StringActionResult FileMD5(string fileName, string id, string scope)
        {
            var url = GetUrl(
                Action.md5,
                fileName,
                id,
                scope);

            using(var response = SendHttpRequest(url))
            {

                var result = new StringActionResult(CheckResponseStatusCode(response));
                if (result.Status == ActionStatus.Error)
                    return result;

                result.Value = ReadTextResponse(response);
                return result;
            }
        }

        public static string FileMD5Local(Stream file)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            var retVal = md5.ComputeHash(file);
            file.Close();

            var sb = new StringBuilder();
            for (var i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString().ToLower();
        }

        #endregion

        #region Private Methods

        private string ReadTextResponse(HttpWebResponse response)
        {
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd().Trim();
            }
        }

        private HttpWebResponse SendHttpRequest(string requesUrl)
        {
            var request = (HttpWebRequest)WebRequest.Create(requesUrl);
            request.Timeout = this._requestTimeOut;
            try
            {
                return (HttpWebResponse)request.GetResponse();
            }
            catch (WebException we)
            {
                if (we.Response != null)
                {
                    return (HttpWebResponse)we.Response;
                }
                throw;
            }
        }
        
        private string GetUrl(Action action, string fileName, string id, string scope)
        {
            return string.Format(
                "{0}api.php?action={1}&fname={2}&id={3}&scope={4}",
                _storageUrl,
                action,
                fileName,
                id,
                scope);
        }

        private SimpleActionResult CheckResponseStatusCode(HttpWebResponse response)
        {
            var result = new SimpleActionResult() { Status = ActionStatus.Ok };
            if (response.StatusCode != HttpStatusCode.OK)
            {
                result.Status = ActionStatus.Error;
                result.ErrorDescription = ReadTextResponse(response);
            }
            return result;
        }

        private SimpleActionResult ProcessSimpleReuest(Action action, string fileName, string id, string scope)
        {
            var url = GetUrl(
                action,
                fileName,
                id,
                scope);
            using (var response = SendHttpRequest(url))
            {
                return CheckResponseStatusCode(response);
            }
        }

        #endregion
    }
}