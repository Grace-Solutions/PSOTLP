using System.Collections.Generic;
using Newtonsoft.Json;
using PSOTLP.Common;

namespace PSOTLP.Models
{
    public sealed class OTLPNumberDataPoint
    {
        public ulong StartTimeUnixNano { get; set; }
        public ulong TimeUnixNano { get; set; }
        public double? AsDouble { get; set; }
        public long? AsInt { get; set; }
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public uint Flags { get; set; }
    }

    public sealed class OTLPGaugePayload
    {
        public IList<OTLPNumberDataPoint> DataPoints { get; set; } = new List<OTLPNumberDataPoint>();
    }

    public sealed class OTLPSumPayload
    {
        public IList<OTLPNumberDataPoint> DataPoints { get; set; } = new List<OTLPNumberDataPoint>();
        public OTLPAggregationTemporality AggregationTemporality { get; set; } = OTLPAggregationTemporality.Cumulative;
        public bool IsMonotonic { get; set; }
    }

    public sealed class OTLPMetricPayload
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        [JsonIgnore] public OTLPMetricType Type { get; set; }
        public OTLPGaugePayload Gauge { get; set; }
        public OTLPSumPayload Sum { get; set; }
    }

    public sealed class OTLPScopeMetrics
    {
        public OTLPInstrumentationScope Scope { get; set; }
        public IList<OTLPMetricPayload> Metrics { get; set; } = new List<OTLPMetricPayload>();
        public string SchemaUrl { get; set; }
    }

    public sealed class OTLPResourceMetrics
    {
        public OTLPResource Resource { get; set; }
        public IList<OTLPScopeMetrics> ScopeMetrics { get; set; } = new List<OTLPScopeMetrics>();
        public string SchemaUrl { get; set; }
    }

    public sealed class OTLPExportMetricsServiceRequest
    {
        public IList<OTLPResourceMetrics> ResourceMetrics { get; set; } = new List<OTLPResourceMetrics>();
    }
}
