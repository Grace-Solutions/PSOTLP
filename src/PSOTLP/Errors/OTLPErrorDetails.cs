using System;
using PSOTLP.Common;

namespace PSOTLP.Errors
{
    public sealed class OTLPErrorDetails
    {
        public string Component { get; set; }
        public string Operation { get; set; }
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public string InnerExceptionMessage { get; set; }
        public int? HttpStatusCode { get; set; }
        public string HttpReasonPhrase { get; set; }
        public int? RetryAttempt { get; set; }
        public string EndpointName { get; set; }
        public Uri EndpointUri { get; set; }
        public OTLPSignalType? SignalType { get; set; }
        public OTLPEncoding? Encoding { get; set; }
        public OTLPCompression? Compression { get; set; }
        public int? SerializationErrorPosition { get; set; }
    }
}
