using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Exporters;
using PSOTLP.Http;
using PSOTLP.Models;
using PSOTLP.Redaction;
using PSOTLP.Serialization;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommunications.Write, "OTLPLog", DefaultParameterSetName = BodyParameterSet)]
    [OutputType(typeof(OTLPLogRecord))]
    public sealed class WriteOTLPLogCommand : OTLPCmdletBase
    {
        private const string BodyParameterSet = "Body";
        private const string InputObjectParameterSet = "InputObject";

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = BodyParameterSet, ValueFromPipeline = true)]
        public string Body { get; set; }

        [Parameter(ParameterSetName = BodyParameterSet)]
        public OTLPSeverity Severity { get; set; } = OTLPSeverity.Information;

        [Parameter(ParameterSetName = BodyParameterSet)]
        [Alias("Attribute")]
        public IDictionary Attributes { get; set; }

        [Parameter(ParameterSetName = BodyParameterSet)]
        [Alias("ResourceAttribute")]
        public IDictionary ResourceAttributes { get; set; }

        [Parameter(ParameterSetName = BodyParameterSet)]
        [Alias("LogAttribute")]
        public IDictionary LogAttributes { get; set; }

        [Parameter(ParameterSetName = BodyParameterSet)]
        public OTLPAttributeMergeMode AttributeMergeMode { get; set; } = OTLPAttributeMergeMode.Merge;

        [Parameter(ParameterSetName = BodyParameterSet)]
        public string EventName { get; set; }

        [Parameter(ParameterSetName = BodyParameterSet)]
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        [Parameter(ParameterSetName = BodyParameterSet)]
        public string TraceId { get; set; }

        [Parameter(ParameterSetName = BodyParameterSet)]
        public string SpanId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = InputObjectParameterSet, ValueFromPipeline = true)]
        public OTLPLogRecord InputObject { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var connection = OTLPSessionManager.RequireCurrentConnection();
                var record = ParameterSetName == InputObjectParameterSet ? InputObject : BuildRecord();
                EnrichFromActiveSpan(record);

                WriteVerboseLine("Attempting to queue OTLP log record. Please Wait...");
                var exporter = BuildExporter(connection);
                exporter.Export(connection, new List<OTLPLogRecord> { record });
                WriteVerboseLine("OTLP log record queue operation was successful.");

                if (PassThru.IsPresent) { WriteObject(record); }
            }
            catch (Exception ex)
            {
                HandleException("Write", ex);
            }
        }

        private static void EnrichFromActiveSpan(OTLPLogRecord record)
        {
            if (record == null) { return; }
            var active = OTLPSpanContextStack.Peek();
            if (active == null) { return; }
            if (string.IsNullOrEmpty(record.TraceId)) { record.TraceId = active.TraceId; }
            if (string.IsNullOrEmpty(record.SpanId)) { record.SpanId = active.SpanId; }
        }

        private OTLPLogRecord BuildRecord()
        {
            return new OTLPLogRecord
            {
                Body = Body,
                Severity = Severity,
                EventName = EventName,
                TimestampUtc = TimestampUtc,
                ObservedTimestampUtc = DateTimeOffset.UtcNow,
                TraceId = TraceId,
                SpanId = SpanId,
                Attributes = HashtableToDictionary(Attributes),
                ResourceAttributes = HashtableToDictionary(ResourceAttributes),
                LogAttributes = HashtableToDictionary(LogAttributes),
                AttributeMergeMode = MyInvocation.BoundParameters.ContainsKey("AttributeMergeMode") ? AttributeMergeMode : (OTLPAttributeMergeMode?)null
            };
        }

        private IOTLPLogExporter BuildExporter(OTLPConnection connection)
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = OTLPSerializerFactory.Create(connection != null ? connection.Encoding : OTLPEncoding.Json);
            var redaction = new OTLPRedactionEngine(connection != null ? connection.RedactPatterns : null);
            return new OTLPLogExporter(http, serializer, redaction, Logger);
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
