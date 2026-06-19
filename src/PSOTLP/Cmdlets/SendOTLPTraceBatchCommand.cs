using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSOTLP.Connections;
using PSOTLP.Exporters;
using PSOTLP.Http;
using PSOTLP.Models;
using PSOTLP.Serialization;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommunications.Send, "OTLPTraceBatch")]
    [OutputType(typeof(OTLPExportResult))]
    public sealed class SendOTLPTraceBatchCommand : OTLPCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public OTLPSpan[] InputObject { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }

        private readonly List<OTLPSpan> _buffer = new List<OTLPSpan>();

        protected override void ProcessRecord()
        {
            if (InputObject == null) { return; }
            foreach (var span in InputObject) { if (span != null) { _buffer.Add(span); } }
        }

        protected override void EndProcessing()
        {
            try
            {
                if (_buffer.Count == 0) { return; }
                var connection = OTLPSessionManager.RequireCurrentConnection();

                WriteVerboseLine("Sending OTLP trace batch of " + _buffer.Count + " spans. Please Wait...");
                var exporter = BuildExporter();
                var result = exporter.Export(connection, _buffer);
                WriteVerboseLine("OTLP trace batch send was successful (status " + result.StatusCode + ").");

                if (PassThru.IsPresent) { WriteObject(result); }
            }
            catch (Exception ex)
            {
                HandleException("SendTraceBatch", ex);
            }
        }

        private IOTLPTraceExporter BuildExporter()
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = new OTLPJsonSerializer();
            return new OTLPTraceExporter(http, serializer, Logger);
        }
    }
}
