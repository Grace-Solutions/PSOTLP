using System;
using PSOTLP.Models;
using PbTrace = OpenTelemetry.Proto.Trace.V1;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// Trace-side partial of <see cref="OTLPProtobufConverter"/>. Shares the resource, scope,
    /// attribute and hex helpers defined in the log-side partial.
    /// </summary>
    internal static partial class OTLPProtobufConverter
    {
        private static PbTrace.ResourceSpans ToProto(OTLPResourceSpans resourceSpans)
        {
            var proto = new PbTrace.ResourceSpans { Resource = ToProto(resourceSpans.Resource) };
            if (!string.IsNullOrEmpty(resourceSpans.SchemaUrl)) { proto.SchemaUrl = resourceSpans.SchemaUrl; }
            foreach (var scope in resourceSpans.ScopeSpans)
            {
                if (scope == null) { continue; }
                proto.ScopeSpans.Add(ToProto(scope));
            }
            return proto;
        }

        private static PbTrace.ScopeSpans ToProto(OTLPScopeSpans scopeSpans)
        {
            var proto = new PbTrace.ScopeSpans { Scope = ToProto(scopeSpans.Scope) };
            if (!string.IsNullOrEmpty(scopeSpans.SchemaUrl)) { proto.SchemaUrl = scopeSpans.SchemaUrl; }
            foreach (var span in scopeSpans.Spans)
            {
                if (span == null) { continue; }
                proto.Spans.Add(ToProto(span));
            }
            return proto;
        }

        private static PbTrace.Span ToProto(OTLPSpanPayload span)
        {
            var proto = new PbTrace.Span
            {
                TraceId = HexToByteString(span.TraceId, 16),
                SpanId = HexToByteString(span.SpanId, 8),
                TraceState = span.TraceState ?? string.Empty,
                ParentSpanId = HexToByteString(span.ParentSpanId, 8),
                Flags = span.Flags,
                Name = span.Name ?? string.Empty,
                Kind = (PbTrace.Span.Types.SpanKind)span.Kind,
                StartTimeUnixNano = span.StartTimeUnixNano,
                EndTimeUnixNano = span.EndTimeUnixNano,
                DroppedAttributesCount = (uint)Math.Max(0, span.DroppedAttributesCount),
                DroppedEventsCount = (uint)Math.Max(0, span.DroppedEventsCount),
                DroppedLinksCount = (uint)Math.Max(0, span.DroppedLinksCount)
            };

            AddAttributes(proto.Attributes, span.Attributes);

            if (span.Events != null)
            {
                foreach (var ev in span.Events)
                {
                    if (ev == null) { continue; }
                    var pbEvent = new PbTrace.Span.Types.Event
                    {
                        Name = ev.Name ?? string.Empty,
                        TimeUnixNano = ev.TimeUnixNano,
                        DroppedAttributesCount = (uint)Math.Max(0, ev.DroppedAttributesCount)
                    };
                    AddAttributes(pbEvent.Attributes, ev.Attributes);
                    proto.Events.Add(pbEvent);
                }
            }

            if (span.Links != null)
            {
                foreach (var link in span.Links)
                {
                    if (link == null) { continue; }
                    var pbLink = new PbTrace.Span.Types.Link
                    {
                        TraceId = HexToByteString(link.TraceId, 16),
                        SpanId = HexToByteString(link.SpanId, 8),
                        TraceState = link.TraceState ?? string.Empty,
                        Flags = link.Flags,
                        DroppedAttributesCount = (uint)Math.Max(0, link.DroppedAttributesCount)
                    };
                    AddAttributes(pbLink.Attributes, link.Attributes);
                    proto.Links.Add(pbLink);
                }
            }

            if (span.Status != null)
            {
                proto.Status = new PbTrace.Status
                {
                    Message = span.Status.Message ?? string.Empty,
                    Code = (PbTrace.Status.Types.StatusCode)span.Status.Code
                };
            }

            return proto;
        }
    }
}
