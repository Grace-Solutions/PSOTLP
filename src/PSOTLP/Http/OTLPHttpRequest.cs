using System;
using System.Collections.Generic;
using PSOTLP.Common;

namespace PSOTLP.Http
{
    public sealed class OTLPHttpRequest
    {
        public string OperationName { get; set; }
        public string EndpointName { get; set; }
        public OTLPSignalType SignalType { get; set; }
        public string Method { get; set; }
        public Uri Uri { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public byte[] Body { get; set; }
        public string ContentType { get; set; }
        public OTLPCompression Compression { get; set; }
        public int TimeoutSeconds { get; set; }
        public bool BodyMayContainSensitiveData { get; set; }

        public void Clear()
        {
            Body = null;
            if (Headers != null) { Headers.Clear(); }
        }
    }
}
