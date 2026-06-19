using System;
using System.IO;
using System.Net;
using System.Text;
using PSOTLP.Common;
using PSOTLP.Errors;
using PSOTLP.Logging;

namespace PSOTLP.Http
{
    /// <summary>
    /// Synchronous OTLP HTTP client built on HttpWebRequest. Centralizes header application,
    /// gzip request bodies, response body materialization, and retry handling. No cmdlet may
    /// construct HttpWebRequest or HttpClient directly.
    /// </summary>
    public sealed class OTLPHttpClient : IOTLPHttpClient
    {
        public const string Component = "OTLPHttpClient";

        private readonly IOTLPLogger _logger;
        private readonly OTLPRetryPolicy _retryPolicy;

        public OTLPHttpClient(IOTLPLogger logger, OTLPRetryPolicy retryPolicy)
        {
            _logger = logger;
            _retryPolicy = retryPolicy ?? new OTLPRetryPolicy();
        }

        public OTLPHttpResponse Send(OTLPHttpRequest request)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }
            if (request.Uri == null) { throw new OTLPHttpException("The OTLP HTTP request cannot be sent because its target URI is null."); }

            var attempt = 0;
            OTLPHttpResponse response = null;
            while (true)
            {
                attempt++;
                try
                {
                    response = SendOnce(request);
                }
                catch (Exception ex)
                {
                    if (_retryPolicy.ShouldRetry(0, attempt))
                    {
                        if (_logger != null) { _logger.Info(Component, "OTLP HTTP request attempt " + attempt + " failed with a transport exception. Retrying. Please Wait..."); }
                        _retryPolicy.Sleep(_retryPolicy.GetDelay(attempt, null));
                        continue;
                    }
                    throw new OTLPHttpException("The OTLP HTTP request failed: " + ex.Message, ex);
                }

                if (response.IsSuccess) { return response; }
                if (!_retryPolicy.ShouldRetry(response.StatusCode, attempt)) { return response; }

                if (_logger != null) { _logger.Info(Component, "OTLP HTTP attempt " + attempt + " returned status " + response.StatusCode + ". Retrying. Please Wait..."); }
                var delay = _retryPolicy.GetDelay(attempt, response);
                response.Clear();
                _retryPolicy.Sleep(delay);
            }
        }

        private OTLPHttpResponse SendOnce(OTLPHttpRequest request)
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create(request.Uri);
            httpRequest.Method = string.IsNullOrWhiteSpace(request.Method) ? "POST" : request.Method.ToUpperInvariant();
            httpRequest.ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/json" : request.ContentType;
            httpRequest.KeepAlive = true;
            httpRequest.Timeout = request.TimeoutSeconds > 0 ? request.TimeoutSeconds * 1000 : 30000;
            httpRequest.ReadWriteTimeout = httpRequest.Timeout;
            httpRequest.Accept = httpRequest.ContentType;
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip;

            if (request.Headers != null)
            {
                foreach (var pair in request.Headers) { ApplyHeader(httpRequest, pair.Key, pair.Value); }
            }

            byte[] body = request.Body ?? new byte[0];
            if (request.Compression == OTLPCompression.Gzip && body.Length > 0)
            {
                body = OTLPCompressionUtility.Compress(body, OTLPCompression.Gzip);
                httpRequest.Headers[OTLPCompressionUtility.ContentEncodingHeader] = OTLPCompressionUtility.GzipEncodingValue;
            }

            httpRequest.ContentLength = body.Length;
            if (body.Length > 0)
            {
                using (var stream = httpRequest.GetRequestStream()) { stream.Write(body, 0, body.Length); }
            }

            try
            {
                using (var webResponse = (HttpWebResponse)httpRequest.GetResponse()) { return BuildResponse(webResponse); }
            }
            catch (WebException webEx) when (webEx.Response is HttpWebResponse errorResponse)
            {
                using (errorResponse) { return BuildResponse(errorResponse); }
            }
        }

        private static void ApplyHeader(HttpWebRequest request, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name)) { return; }
            if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase)) { request.ContentType = value; return; }
            if (string.Equals(name, "Accept", StringComparison.OrdinalIgnoreCase)) { request.Accept = value; return; }
            if (string.Equals(name, "User-Agent", StringComparison.OrdinalIgnoreCase)) { request.UserAgent = value; return; }
            if (string.Equals(name, "Host", StringComparison.OrdinalIgnoreCase)) { request.Host = value; return; }
            request.Headers[name] = value;
        }

        private static OTLPHttpResponse BuildResponse(HttpWebResponse webResponse)
        {
            var result = new OTLPHttpResponse
            {
                StatusCode = (int)webResponse.StatusCode,
                ReasonPhrase = webResponse.StatusDescription,
                Headers = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };

            foreach (string headerName in webResponse.Headers)
            {
                if (string.IsNullOrEmpty(headerName)) { continue; }
                result.Headers[headerName] = webResponse.Headers[headerName];
            }

            using (var responseStream = webResponse.GetResponseStream())
            {
                if (responseStream == null) { result.Body = new byte[0]; return result; }
                using (var memory = new MemoryStream())
                {
                    var buffer = new byte[8192];
                    int read;
                    while ((read = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        memory.Write(buffer, 0, read);
                    }
                    result.Body = memory.ToArray();
                }
            }

            if (result.Headers != null
                && result.Headers.TryGetValue(OTLPCompressionUtility.ContentEncodingHeader, out var encoding)
                && string.Equals(encoding, OTLPCompressionUtility.GzipEncodingValue, StringComparison.OrdinalIgnoreCase)
                && result.Body != null && result.Body.Length > 0)
            {
                try { result.Body = OTLPCompressionUtility.Decompress(result.Body); } catch { /* leave body */ }
            }
            return result;
        }
    }
}
