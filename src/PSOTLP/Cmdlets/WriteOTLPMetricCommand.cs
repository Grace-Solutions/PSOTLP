using System;
using System.Collections;
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
    [Cmdlet(VerbsCommunications.Write, "OTLPMetric", DefaultParameterSetName = ValueParameterSet)]
    [OutputType(typeof(OTLPMetric))]
    public sealed class WriteOTLPMetricCommand : OTLPCmdletBase
    {
        private const string ValueParameterSet = "Value";
        private const string InputObjectParameterSet = "InputObject";

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = ValueParameterSet)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public string Description { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public string Unit { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public OTLPMetricType Type { get; set; } = OTLPMetricType.Gauge;

        [Parameter(ParameterSetName = ValueParameterSet)]
        public OTLPAggregationTemporality Temporality { get; set; } = OTLPAggregationTemporality.Cumulative;

        [Parameter(ParameterSetName = ValueParameterSet)]
        public SwitchParameter IsMonotonic { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public double Value { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public long IntValue { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public SwitchParameter AsInt { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        [Alias("Attribute")]
        public IDictionary Attributes { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        [Alias("ResourceAttribute")]
        public IDictionary ResourceAttributes { get; set; }

        [Parameter(ParameterSetName = ValueParameterSet)]
        public OTLPAttributeMergeMode AttributeMergeMode { get; set; } = OTLPAttributeMergeMode.Merge;

        [Parameter(ParameterSetName = ValueParameterSet)]
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        [Parameter(ParameterSetName = ValueParameterSet)]
        public DateTimeOffset StartTimestampUtc { get; set; } = DateTimeOffset.UtcNow;

        [Parameter(Mandatory = true, ParameterSetName = InputObjectParameterSet, ValueFromPipeline = true)]
        public OTLPMetric InputObject { get; set; }

        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var connection = OTLPSessionManager.RequireCurrentConnection();
                var metric = ParameterSetName == InputObjectParameterSet ? InputObject : BuildMetric();

                WriteVerboseLine("Attempting to send OTLP metric. Please Wait...");
                var exporter = BuildExporter(connection);
                exporter.Export(connection, new List<OTLPMetric> { metric });
                WriteVerboseLine("OTLP metric send operation was successful.");

                if (PassThru.IsPresent) { WriteObject(metric); }
            }
            catch (Exception ex)
            {
                HandleException("WriteMetric", ex);
            }
        }

        private OTLPMetric BuildMetric()
        {
            return new OTLPMetric
            {
                Name = Name,
                Description = Description,
                Unit = Unit,
                Type = Type,
                Temporality = Temporality,
                IsMonotonic = IsMonotonic.IsPresent,
                DoubleValue = Value,
                IntValue = IntValue,
                UseIntValue = AsInt.IsPresent,
                TimestampUtc = TimestampUtc,
                StartTimestampUtc = StartTimestampUtc,
                Attributes = HashtableToDictionary(Attributes),
                ResourceAttributes = HashtableToDictionary(ResourceAttributes),
                AttributeMergeMode = MyInvocation.BoundParameters.ContainsKey("AttributeMergeMode") ? AttributeMergeMode : (OTLPAttributeMergeMode?)null
            };
        }

        private IOTLPMetricExporter BuildExporter(OTLPConnection connection)
        {
            var http = new OTLPHttpClient(Logger, new OTLPRetryPolicy());
            var serializer = OTLPSerializerFactory.Create(connection != null ? connection.Encoding : OTLPEncoding.Json);
            return new OTLPMetricExporter(http, serializer, Logger);
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
