using System.Collections.Generic;

namespace PSOTLP.Http
{
    public sealed class OTLPHttpResponse
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public byte[] Body { get; set; }
        public IDictionary<string, string> Headers { get; set; }

        public bool IsSuccess
        {
            get { return StatusCode >= 200 && StatusCode < 300; }
        }

        public void Clear()
        {
            Body = null;
            if (Headers != null) { Headers.Clear(); }
        }
    }
}
