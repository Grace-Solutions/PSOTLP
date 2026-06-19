using System.Collections.Generic;

namespace PSOTLP.Models
{
    public sealed class OTLPLogRecordPayload
    {
        public ulong TimeUnixNano { get; set; }
        public ulong ObservedTimeUnixNano { get; set; }
        public int SeverityNumber { get; set; }
        public string SeverityText { get; set; }
        public OTLPAnyValue Body { get; set; }
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public int DroppedAttributesCount { get; set; }
        public uint Flags { get; set; }
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string EventName { get; set; }
    }

    public sealed class OTLPScopeLogs
    {
        public OTLPInstrumentationScope Scope { get; set; }
        public IList<OTLPLogRecordPayload> LogRecords { get; set; } = new List<OTLPLogRecordPayload>();
        public string SchemaUrl { get; set; }
    }

    public sealed class OTLPResourceLogs
    {
        public OTLPResource Resource { get; set; }
        public IList<OTLPScopeLogs> ScopeLogs { get; set; } = new List<OTLPScopeLogs>();
        public string SchemaUrl { get; set; }
    }

    public sealed class OTLPExportLogsServiceRequest
    {
        public IList<OTLPResourceLogs> ResourceLogs { get; set; } = new List<OTLPResourceLogs>();
    }

    public sealed class OTLPExportLogsServiceResponse
    {
        public OTLPPartialSuccess PartialSuccess { get; set; }
    }

    public sealed class OTLPPartialSuccess
    {
        public long RejectedLogRecords { get; set; }
        public long RejectedSpans { get; set; }
        public string ErrorMessage { get; set; }
    }
}
