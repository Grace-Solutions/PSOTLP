using System;
using System.Collections.Generic;
using PSOTLP.Common;

namespace PSOTLP.Models
{
    /// <summary>
    /// Public metric record model exposed to PowerShell. The exporter is responsible for converting
    /// this model into <see cref="OTLPMetricPayload"/> via the centralized attribute merger.
    /// Only scalar Gauge and Sum types are supported in the public surface; Histogram, Summary, and
    /// ExponentialHistogram remain available in the underlying protobuf schema for future use.
    /// </summary>
    public sealed class OTLPMetric
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Unit { get; set; }
        public OTLPMetricType Type { get; set; } = OTLPMetricType.Gauge;
        public OTLPAggregationTemporality Temporality { get; set; } = OTLPAggregationTemporality.Cumulative;
        public bool IsMonotonic { get; set; }
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset StartTimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public double DoubleValue { get; set; }
        public long IntValue { get; set; }
        public bool UseIntValue { get; set; }
        public IDictionary<string, object> Attributes { get; set; }
        public IDictionary<string, object> ResourceAttributes { get; set; }
        public OTLPAttributeMergeMode? AttributeMergeMode { get; set; }
    }
}
