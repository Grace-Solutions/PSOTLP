using System;
using System.Collections.Generic;
using PSOTLP.Common;

namespace PSOTLP.Models
{
    /// <summary>
    /// Public log record model exposed to PowerShell. The exporter is responsible for converting
    /// this model into <see cref="OTLPLogRecordPayload"/> via the centralized attribute merger.
    /// </summary>
    public sealed class OTLPLogRecord
    {
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ObservedTimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public OTLPSeverity Severity { get; set; } = OTLPSeverity.Information;
        public string SeverityText { get; set; }
        public int SeverityNumber { get; set; }
        public string Body { get; set; }
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string EventName { get; set; }
        public IDictionary<string, object> Attributes { get; set; }
        public IDictionary<string, object> ResourceAttributes { get; set; }
        public IDictionary<string, object> LogAttributes { get; set; }
        public OTLPAttributeMergeMode? AttributeMergeMode { get; set; }
    }
}
