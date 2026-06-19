using System;
using PSOTLP.Common;

namespace PSOTLP.Exporters
{
    public sealed class OTLPExportResult
    {
        public OTLPSignalType SignalType { get; set; }
        public Uri EndpointUri { get; set; }
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public int AttemptCount { get; set; }
        public int RecordCount { get; set; }
        public bool Success { get; set; }
        public DateTimeOffset ExportedAtUtc { get; set; }
    }
}
