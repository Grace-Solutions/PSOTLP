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
    [Cmdlet(VerbsCommunications.Send, "OTLPMetricBatch")]
    [OutputType(typeof(OTLPExportResult))]
    public sealed class SendOTLPMetricBatchCommand : OTLPCmdletBase
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNull]
        public OTLPMetric[] InputObject { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }

        private readonly List<OTLPMetric> _buffer = new List<OTLPMetric>();

        protected override void ProcessRecord()
        {
            if (InputObject == null) { return; }
            foreach (var metric in InputObject) { if (metric != null) { _buffer.Add(metric); } }
        }

        protected override void EndProcessing()
        {
            try
            {
                if (_buffer.Count == 0) { return; }
                var connection = OTLPSessionManager.RequireCurrentConnection();

                WriteVerboseLine("Sending OTLP metric batch of " + _buffer.Count + " records. Please Wait...");
                var exporter = BuildExporter(connection);
                var result = exporter.Export(connection, _buffer);
                WriteVerboseLine("OTLP metric batch send was successful (status " + result.StatusCode + ").");

                if (PassThru.IsPresent) { WriteObject(result); }
            }
            catch (Exception ex)
            {
                HandleException("SendMetricBatch", ex);
            }
        }

        private IOTLPMetricExporter BuildExporter(OTLPConnection connection)
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = OTLPSerializerFactory.Create(connection != null ? connection.Encoding : Common.OTLPEncoding.Json);
            return new OTLPMetricExporter(http, serializer, Logger);
        }
    }
}
