using System.Collections.Generic;

namespace PSOTLP.Models
{
    public sealed class OTLPSpanEventPayload
    {
        public ulong TimeUnixNano { get; set; }
        public string Name { get; set; }
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public int DroppedAttributesCount { get; set; }
    }

    public sealed class OTLPSpanLinkPayload
    {
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string TraceState { get; set; }
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public int DroppedAttributesCount { get; set; }
        public uint Flags { get; set; }
    }

    public sealed class OTLPStatusPayload
    {
        public string Message { get; set; }
        public int Code { get; set; }
    }

    public sealed class OTLPSpanPayload
    {
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string TraceState { get; set; }
        public string ParentSpanId { get; set; }
        public uint Flags { get; set; }
        public string Name { get; set; }
        public int Kind { get; set; }
        public ulong StartTimeUnixNano { get; set; }
        public ulong EndTimeUnixNano { get; set; }
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public int DroppedAttributesCount { get; set; }
        public IList<OTLPSpanEventPayload> Events { get; set; } = new List<OTLPSpanEventPayload>();
        public int DroppedEventsCount { get; set; }
        public IList<OTLPSpanLinkPayload> Links { get; set; } = new List<OTLPSpanLinkPayload>();
        public int DroppedLinksCount { get; set; }
        public OTLPStatusPayload Status { get; set; }
    }

    public sealed class OTLPScopeSpans
    {
        public OTLPInstrumentationScope Scope { get; set; }
        public IList<OTLPSpanPayload> Spans { get; set; } = new List<OTLPSpanPayload>();
        public string SchemaUrl { get; set; }
    }

    public sealed class OTLPResourceSpans
    {
        public OTLPResource Resource { get; set; }
        public IList<OTLPScopeSpans> ScopeSpans { get; set; } = new List<OTLPScopeSpans>();
        public string SchemaUrl { get; set; }
    }

    public sealed class OTLPExportTraceServiceRequest
    {
        public IList<OTLPResourceSpans> ResourceSpans { get; set; } = new List<OTLPResourceSpans>();
    }
}
