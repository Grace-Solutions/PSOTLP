using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Models;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "OTLPSpan")]
    [OutputType(typeof(OTLPSpan))]
    public sealed class StartOTLPSpanCommand : OTLPCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter] public OTLPSpanKind Kind { get; set; } = OTLPSpanKind.Internal;

        [Parameter] public string TraceId { get; set; }
        [Parameter] public string SpanId { get; set; }
        [Parameter] public string ParentSpanId { get; set; }

        [Parameter] public IDictionary Attribute { get; set; }
        [Parameter] public DateTimeOffset StartTimeUtc { get; set; } = DateTimeOffset.UtcNow;

        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                OTLPSessionManager.RequireCurrentConnection();

                var parent = OTLPSpanContextStack.Peek();
                var resolvedTraceId = !string.IsNullOrEmpty(TraceId)
                    ? TraceId
                    : (parent != null ? parent.TraceId : OTLPIdGenerator.NewTraceId());
                var resolvedParent = !string.IsNullOrEmpty(ParentSpanId)
                    ? ParentSpanId
                    : (parent != null ? parent.SpanId : null);

                var span = new OTLPSpan
                {
                    TraceId = resolvedTraceId,
                    SpanId = !string.IsNullOrEmpty(SpanId) ? SpanId : OTLPIdGenerator.NewSpanId(),
                    ParentSpanId = resolvedParent,
                    Name = Name,
                    Kind = Kind,
                    StartTimeUtc = StartTimeUtc,
                    Attributes = HashtableToDictionary(Attribute)
                };

                OTLPSpanContextStack.Push(span);
                WriteVerboseLine("OTLP span started (name=" + Name + ", spanId=" + span.SpanId + ").");

                if (PassThru.IsPresent) { WriteObject(span); }
            }
            catch (Exception ex)
            {
                HandleException("StartSpan", ex);
            }
        }

        private static IDictionary<string, object> HashtableToDictionary(IDictionary source)
        {
            if (source == null) { return null; }
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in source) { if (entry.Key != null) { result[entry.Key.ToString()] = entry.Value; } }
            return result;
        }
    }
}
