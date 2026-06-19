using System;
using System.Collections.Generic;
using PSOTLP.Common;

namespace PSOTLP.Models
{
    public sealed class OTLPStatus
    {
        public OTLPStatusCode Code { get; set; } = OTLPStatusCode.Unset;
        public string Message { get; set; }
    }

    public sealed class OTLPSpanEvent
    {
        public string Name { get; set; }
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public IDictionary<string, object> Attributes { get; set; }
    }

    public sealed class OTLPSpan
    {
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string ParentSpanId { get; set; }
        public string Name { get; set; }
        public OTLPSpanKind Kind { get; set; } = OTLPSpanKind.Internal;
        public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndTimeUtc { get; set; }
        public IDictionary<string, object> Attributes { get; set; }
        public IList<OTLPSpanEvent> Events { get; set; } = new List<OTLPSpanEvent>();
        public OTLPStatus Status { get; set; } = new OTLPStatus();
    }
}
