using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using HttpTransfomerService.Log;
using log4net;

namespace HttpTransfomerService.Fliter
{
    public class RequestHandler : IHttpModule
    {
        private static ILog _log = Logger.Log;
        private static HttpWebRequest hRequest;

        public void Dispose()
        {

        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += BeginRequest;

        }

        public HttpRequest Request { get; set; }
        private readonly string defaultUrl = "http://116.62.232.164:9898/";

        /// <summary>
        /// 要将Http请求转发 到 的 目标Url
        /// </summary>
        public Uri ToUrl
        {
            get
            {
                //从配置中读取
                string toUrl = Request.Headers["TransToURL"] ?? defaultUrl;
                //判断Url是否/结尾
                if (!toUrl.EndsWith("/"))
                {
                    toUrl = toUrl + "/";
                }
                Uri uri = new Uri(toUrl);
                return uri;
            }
        }

        /// <summary>
        /// 目标UrlHost
        /// </summary>
        public string ToUrlHost
        {
            get { return ToUrl.Host; }
        }

        /// <summary>
        /// 目标Url 的端口
        /// </summary>
        public string ToPort
        {
            get
            {
                var result = Regex.Match(ToUrl.ToString(), @"^http://.+:(\d+)", RegexOptions.IgnoreCase);
                if (result.Groups.Count > 1)
                {
                    return result.Groups[1].Value;
                }
                else
                {
                    return "";
                }
            }
        }

        /// <summary>
        /// 客户端直接请求的Url ，也就是 本 应用程序的 Url ，所有对该Url的请求都会被转发到 目标Url
        /// </summary>
        public Uri FromUrl { get; set; }

        /// <summary>
        /// 本应用程序Url Host
        /// </summary>
        public string FromUrlHost
        {
            get { return FromUrl.Host; }
        }

        /// <summary>
        /// 本应用程序Url 端口
        /// </summary>
        public string FromPort
        {
            get
            {
                var result = Regex.Match(FromUrl.ToString(), @"^http[s]{0,1}://.+:(\d+)", RegexOptions.IgnoreCase);
                if (result.Groups.Count > 1)
                {
                    return result.Groups[1].Value;
                }
                else
                {
                    return "";
                }
            }
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            try
            {
                System.Net.ServicePointManager.DefaultConnectionLimit = 50;
                HttpApplication app = sender as HttpApplication;
                this.Request = app.Request;
                var header = Request.Headers["TransToURL"];
                if (header == null || header == "")
                {
                    _log.Info("no redict");
                    return;
                }
                var respone = app.Response;
                var request = Request;
                string toUrl = this.ToUrl.ToString();
                _log.Info(toUrl);
                //初始化 本应用程序 Url
                FromUrl = new Uri(request.Url.ToString());
                _log.Info("FromUrl:" + FromUrl);

                //获取转换目标后的Url
                //将请求报文中的 Url 替换为 目标 Url
                string tempUrl = this.ReplaceHostAndPort(FromUrl.ToString(), TransType.TransTo);
                _log.Info("tempUrl:" + tempUrl);
                //创建 Http 请求 用于将 替换后 请求报文 发往 目标 Url
                hRequest = HttpWebRequest.CreateHttp(tempUrl);
                _log.Info(hRequest);
                //设置请求头
                this.SetRequestHead(hRequest, request);
                _log.Info("设置请求头");

                #region 设置特殊请求头

                if (!string.IsNullOrEmpty(request.Headers["Accept"]))
                {
                    hRequest.Accept = request.Headers["Accept"];
                }
                if (!string.IsNullOrEmpty(request.Headers["Connection"]))
                {
                    string connection = request.Headers["Connection"];
                    hRequest.KeepAlive =
                        string.Compare(connection, "keep-alive", StringComparison.CurrentCultureIgnoreCase) == 0;

                }
                if (!string.IsNullOrEmpty(request.Headers["Content-Type"]))
                {
                    hRequest.ContentType = request.Headers["Content-Type"];
                }
                //if (!string.IsNullOrEmpty(request.Headers["Expect"]))
                //{
                //    hRequest.Expect = request.Headers["Expect"];
                //}
                if (!string.IsNullOrEmpty(request.Headers["Date"]))
                {
                    hRequest.Date = Convert.ToDateTime(request.Headers["Date"]);
                }
                if (!string.IsNullOrEmpty(request.Headers["Host"]))
                {
                    hRequest.Host = this.ToUrlHost;
                }
                if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
                {
                    hRequest.IfModifiedSince = Convert.ToDateTime(request.Headers["If-Modified-Since"]);
                }
                if (!string.IsNullOrEmpty(request.Headers["Referer"]))
                {
                    hRequest.Referer = this.ReplaceHostAndPort(request.Headers["Referer"], TransType.TransTo);
                }
                if (!string.IsNullOrEmpty(request.Headers["User-Agent"]))
                {
                    hRequest.UserAgent = request.Headers["User-Agent"];
                }
                if (!string.IsNullOrEmpty(request.Headers["Content-Length"]))
                {
                    hRequest.ContentLength = Convert.ToInt32(request.Headers["Content-Length"]);
                }

                #endregion

                //判断是否是Get请求,如果不是Get就写入请求报文体
                if (String.Compare(request.HttpMethod, "get", StringComparison.CurrentCultureIgnoreCase) != 0)
                {
                    _log.Info("设置请求体");
                    //设置请求体
                    this.SetRequestBody(hRequest, request);
                    _log.Info("判断是否是Get请求,如果不是Get就写入请求报文体");
                }


                //获取响应报文
                WebResponse hRespone = null;
                try
                {
                    _log.Info("========================");
                    _log.Info(hRequest);
                    hRespone = hRequest.GetResponse();
                    _log.Info("========================");
                }
                catch (Exception exp)
                {

                    respone.Write(exp.Message);
                    respone.End();
                }


                //设置响应头
                this.SetResponeHead(hRespone, respone);

                #region 设置特殊响应头

                if (!string.IsNullOrEmpty(hRespone.Headers["Content-Type"]))
                {
                    respone.ContentType = hRespone.Headers["Content-Type"];
                }
                if (!string.IsNullOrEmpty(hRespone.Headers["Host"]))
                {
                    respone.AddHeader("Host", FromUrlHost);
                }
                if (!string.IsNullOrEmpty(hRespone.Headers["Referer"]))
                {
                    respone.AddHeader("Referer",
                        this.ReplaceHostAndPort(hRespone.Headers["Referer"], TransType.TransBack));
                }

                #endregion

                //写入响应内容
                this.SetResponeBody(hRespone, respone);

                respone.End();
            }

            catch (Exception exception)
            {
                _log.Error(exception);
            }
            finally
            {
                hRequest.Abort();
                hRequest = null;
            }
        }

        void BeginRequest(object sender, EventArgs e)
        {

            using (HttpApplication app = sender as HttpApplication)
            {
                if (app.Request.Headers["TransToUrl"] == null || app.Request.Headers["TransToUrl"] == "")
                    return;
                using (var client = new HttpClient())
                {
                    this.Request = app.Request;
                    FromUrl = new Uri(Request.Url.ToString());
                    SetRequestHead(client, Request);
                    SetRequestSpecialHead(client, Request);
                    HttpContent hc = new StreamContent(app.Request.InputStream);
                    SetRequestSpecialHead(hc, Request);
                    var response = client.PostAsync(app.Request.Headers["TransToUrl"], hc).Result;
                    var respone = app.Response;
                    SetResponsHeader(response, respone);
                    SetResponeBody(response, respone);
                    respone.End();
                }
            }
        }

        /// <summary>
        /// 设置请求头
        /// </summary>
        /// <param name="nrq"></param>
        /// <param name="orq"></param>
        private void SetRequestHead(HttpClient nrq, HttpRequest orq)
        {
            foreach (var key in orq.Headers.AllKeys)
            {
                try
                {
                    nrq.DefaultRequestHeaders.Add(key, orq.Headers[key]);
                }
                catch (Exception e)
                {
                    //_log.Error("SetRequestHead ERROR!msg: " +e);
                    continue;
                }

            }
        }
        /// <summary>
        /// 设置特殊请求头
        /// </summary>
        /// <param name="nrq"></param>
        /// <param name="orq"></param>
        private void SetRequestSpecialHead(HttpClient nrq, HttpRequest request)
        {
            #region 设置特殊请求头
            //if (!string.IsNullOrEmpty(request.Headers["Accept"]))
            //{
            //    var accept = new MediaTypeWithQualityHeaderValue(request.Headers["Accept"]);
            //    nrq.DefaultRequestHeaders.Accept.Add(accept);
            //}
            if (!string.IsNullOrEmpty(request.Headers["Connection"]))
            {
                nrq.DefaultRequestHeaders.Connection.Add(request.Headers["Connection"]);

            }

            //if (!string.IsNullOrEmpty(request.Headers["Expect"]))
            //{
            //    hRequest.Expect = request.Headers["Expect"];
            //}
            if (!string.IsNullOrEmpty(request.Headers["Date"]))
            {
                nrq.DefaultRequestHeaders.Date = Convert.ToDateTime(request.Headers["Date"]);
            }
            if (!string.IsNullOrEmpty(request.Headers["Host"]))
            {
                nrq.DefaultRequestHeaders.Host = this.ToUrlHost;
            }
            if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"]))
            {
                nrq.DefaultRequestHeaders.IfModifiedSince = Convert.ToDateTime(request.Headers["If-Modified-Since"]);
            }
            if (!string.IsNullOrEmpty(request.Headers["Referer"]))
            {
                nrq.DefaultRequestHeaders.Referrer = new Uri(this.ReplaceHostAndPort(request.Headers["Referer"], TransType.TransTo));
            }
            //if (!string.IsNullOrEmpty(request.Headers["User-Agent"]))
            //{
            //    var agent =new ProductInfoHeaderValue(request.Headers["User-Agent"]);
            //    nrq.DefaultRequestHeaders.UserAgent.Add(agent);
            //}
            //if (!string.IsNullOrEmpty(request.Headers["Content-Length"]))
            //{
            //    var length = new MediaTypeWithQualityHeaderValue(request.Headers["Content-Length"]);
            //    nrq.DefaultRequestHeaders.Accept.Add(length);
            //}

            #endregion
        }

        private void SetRequestSpecialHead(HttpContent httpContent, HttpRequest request)
        {
            if (!string.IsNullOrEmpty(request.Headers["Content-Type"]))
            {
                var contentType = new MediaTypeWithQualityHeaderValue(request.Headers["Content-Type"]);
                httpContent.Headers.ContentType = contentType;
            }
            if (!string.IsNullOrEmpty(request.Headers["Content-Length"]))
            {
                httpContent.Headers.ContentLength= long.Parse(request.Headers["Content-Length"]);
            }
        }
        /// <summary>
        /// 设置请求头
        /// </summary>
        /// <param name="nrq"></param>
        /// <param name="orq"></param>
        private void SetRequestHead(WebRequest nrq, HttpRequest orq)
        {
            foreach (var key in orq.Headers.AllKeys)
            {
                try
                {
                    nrq.Headers.Add(key, orq.Headers[key]);
                }
                catch (Exception)
                {
                    _log.Error("SetRequestHead ERROR!");
                    continue;
                }

            }
        }


        /// <summary>
        /// 设置请求 报文体
        /// </summary>
        /// <param name="nrq"></param>
        /// <param name="orq"></param>
        private void SetRequestBody(HttpWebRequest nrq, HttpRequest orq)
        {
            try
            {
                _log.Info("设置POST");
                nrq.Method = "POST";
                var nStream = nrq.GetRequestStream();
                _log.Info("获取Body流");
                byte[] buffer = new byte[1024 * 2];
                int rLength = 0;
                _log.Info("开始循环");
                do
                {
                    _log.Info("rLength: " + rLength);
                    rLength = orq.InputStream.Read(buffer, 0, buffer.Length);
                    nStream.Write(buffer, 0, rLength);
                    _log.Info("nStream:" + nStream.Length);
                } while (rLength > 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        /// <summary>
        /// 设置响应头
        /// </summary>
        /// <param name="nrp"></param>
        /// <param name="orp"></param>
        private void SetResponeHead(WebResponse nrp, HttpResponse orp)
        {
            foreach (var key in nrp.Headers.AllKeys)
            {
                try
                {
                    orp.Headers.Add(key, nrp.Headers[key]);
                }
                catch (Exception)
                {

                    continue;
                }

            }
        }

        /// <summary>
        /// 设置响应报文体
        /// </summary>
        /// <param name="nrp"></param>
        /// <param name="respone"></param>
        private void SetResponeBody(WebResponse nrp, HttpResponse respone)
        {
            var nStream = nrp.GetResponseStream();
            byte[] buffer = new byte[1024 * 2];
            int rLength = 0;
            do
            {
                rLength = nStream.Read(buffer, 0, buffer.Length);
                respone.OutputStream.Write(buffer, 0, rLength);
            } while (rLength > 0);
        }

        private void SetResponsHeader(HttpResponseMessage hRespone, HttpResponse respone)
        {
            foreach (var header in hRespone.Headers)
            {
                try
                {
                    if (header.Value.FirstOrDefault() != null)
                        respone.Headers.Add(header.Key, header.Value.FirstOrDefault());
                }
                catch (Exception)
                {

                    continue;
                }

            }
        }

        /// <summary>
        /// 设置响应报文体
        /// </summary>
        /// <param name="hRespone"></param>
        /// <param name="respone"></param>
        private void SetResponeBody(HttpResponseMessage hRespone, HttpResponse respone)
        {

            var nStream = hRespone.Content.ReadAsStreamAsync().Result;
            byte[] buffer = new byte[1024 * 2];
            int rLength = 0;
            do
            {
                rLength = nStream.Read(buffer, 0, buffer.Length);
                respone.OutputStream.Write(buffer, 0, rLength);
            } while (rLength > 0);
        }

        /// <summary>
        /// 替换 Host和Port
        /// </summary>
        /// <param name="url"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private string ReplaceHostAndPort(string url, TransType type)
        {
            string tempToPortStr = string.IsNullOrEmpty(ToPort) ? "" : ":" + ToPort;
            string tempFromPortStr = string.IsNullOrEmpty(FromPort) ? "" : ":" + FromPort;
            if (type == TransType.TransBack)
            {
                return url.Replace(ToUrlHost + tempToPortStr, FromUrlHost + tempFromPortStr);
            }
            else
            {
                return url.Replace(FromUrlHost + tempFromPortStr, ToUrlHost + tempToPortStr);
            }
        }
    }

    public enum TransType
    {
        TransTo,
        TransBack
    }
}