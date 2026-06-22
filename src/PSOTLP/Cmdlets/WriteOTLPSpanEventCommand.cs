using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSOTLP.Common;
using PSOTLP.Models;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommunications.Write, "OTLPSpanEvent")]
    [OutputType(typeof(OTLPSpanEvent))]
    public sealed class WriteOTLPSpanEventCommand : OTLPCmdletBase
    {
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        [Alias("Attribute")]
        public IDictionary Attributes { get; set; }

        [Parameter] public OTLPAttributeMergeMode AttributeMergeMode { get; set; } = OTLPAttributeMergeMode.Merge;

        [Parameter] public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        [Parameter] public string SpanId { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var span = !string.IsNullOrEmpty(SpanId)
                    ? OTLPSpanContextStack.FindById(SpanId)
                    : OTLPSpanContextStack.Peek();
                if (span == null) { throw new InvalidOperationException("No active OTLP span is available to attach this event to."); }

                var ev = new OTLPSpanEvent
                {
                    Name = Name,
                    TimestampUtc = TimestampUtc,
                    Attributes = HashtableToDictionary(Attributes),
                    AttributeMergeMode = MyInvocation.BoundParameters.ContainsKey("AttributeMergeMode") ? AttributeMergeMode : (OTLPAttributeMergeMode?)null
                };
                if (span.Events == null) { span.Events = new List<OTLPSpanEvent>(); }
                span.Events.Add(ev);

                WriteVerboseLine("OTLP span event recorded (name=" + Name + ", spanId=" + span.SpanId + ").");
                if (PassThru.IsPresent) { WriteObject(ev); }
            }
            catch (Exception ex)
            {
                HandleException("WriteSpanEvent", ex);
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
