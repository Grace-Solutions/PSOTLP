using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Exporters;
using PSOTLP.Http;
using PSOTLP.Models;
using PSOTLP.Serialization;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Stop, "OTLPSpan")]
    [OutputType(typeof(OTLPSpan))]
    public sealed class StopOTLPSpanCommand : OTLPCmdletBase
    {
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true)]
        public string SpanId { get; set; }

        [Parameter] public OTLPStatusCode StatusCode { get; set; } = OTLPStatusCode.Unset;
        [Parameter] public string StatusMessage { get; set; }
        [Parameter] public DateTimeOffset EndTimeUtc { get; set; } = DateTimeOffset.UtcNow;

        [Parameter] public SwitchParameter NoExport { get; set; }
        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var connection = OTLPSessionManager.RequireCurrentConnection();
                OTLPSpan span;
                if (!string.IsNullOrEmpty(SpanId))
                {
                    span = OTLPSpanContextStack.FindById(SpanId);
                    if (span == null) { throw new InvalidOperationException("No active OTLP span found with SpanId '" + SpanId + "'."); }
                    OTLPSpanContextStack.RemoveById(SpanId);
                }
                else
                {
                    span = OTLPSpanContextStack.Pop();
                    if (span == null) { throw new InvalidOperationException("No active OTLP span on the context stack."); }
                }

                span.EndTimeUtc = EndTimeUtc;
                if (span.Status == null) { span.Status = new OTLPStatus(); }
                span.Status.Code = StatusCode;
                if (!string.IsNullOrEmpty(StatusMessage)) { span.Status.Message = StatusMessage; }

                if (!NoExport.IsPresent)
                {
                    WriteVerboseLine("Sending OTLP span (name=" + span.Name + ", spanId=" + span.SpanId + "). Please Wait...");
                    var exporter = BuildExporter(connection);
                    exporter.Export(connection, new List<OTLPSpan> { span });
                    WriteVerboseLine("OTLP span export was successful.");
                }

                if (PassThru.IsPresent) { WriteObject(span); }
            }
            catch (Exception ex)
            {
                HandleException("StopSpan", ex);
            }
        }

        private IOTLPTraceExporter BuildExporter(OTLPConnection connection)
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = OTLPSerializerFactory.Create(connection != null ? connection.Encoding : OTLPEncoding.Json);
            return new OTLPTraceExporter(http, serializer, Logger);
        }
    }
}
