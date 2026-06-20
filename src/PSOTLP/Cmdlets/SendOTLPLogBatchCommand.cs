using System;
using System.Collections.Generic;
using System.Management.Automation;
using PSOTLP.Connections;
using PSOTLP.Exporters;
using PSOTLP.Http;
using PSOTLP.Models;
using PSOTLP.Redaction;
using PSOTLP.Serialization;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommunications.Send, "OTLPLogBatch")]
    [OutputType(typeof(OTLPExportResult))]
    public sealed class SendOTLPLogBatchCommand : OTLPCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = false)]
        [ValidateNotNull]
        public OTLPLogRecord[] InputObject { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }

        private readonly List<OTLPLogRecord> _buffer = new List<OTLPLogRecord>();

        protected override void ProcessRecord()
        {
            if (InputObject == null) { return; }
            foreach (var record in InputObject)
            {
                if (record != null) { _buffer.Add(record); }
            }
        }

        protected override void EndProcessing()
        {
            try
            {
                if (_buffer.Count == 0) { return; }
                var connection = OTLPSessionManager.RequireCurrentConnection();

                WriteVerboseLine("Sending OTLP log batch of " + _buffer.Count + " records. Please Wait...");
                var exporter = BuildExporter(connection);
                var result = exporter.Export(connection, _buffer);
                WriteVerboseLine("OTLP log batch send was successful (status " + result.StatusCode + ").");

                if (PassThru.IsPresent) { WriteObject(result); }
            }
            catch (Exception ex)
            {
                HandleException("Send", ex);
            }
        }

        private IOTLPLogExporter BuildExporter(OTLPConnection connection)
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = OTLPSerializerFactory.Create(connection != null ? connection.Encoding : Common.OTLPEncoding.Json);
            var redaction = new OTLPRedactionEngine(connection != null ? connection.RedactPatterns : null);
            return new OTLPLogExporter(http, serializer, redaction, Logger);
        }
    }
}
