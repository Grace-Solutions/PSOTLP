using PSOTLP.Common;
using PSOTLP.Models;
using PbMetrics = OpenTelemetry.Proto.Metrics.V1;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// Metric-side partial of <see cref="OTLPProtobufConverter"/>. Maps the public metric model to
    /// the generated OpenTelemetry.Proto.Metrics.V1 types. Only Gauge and Sum are supported;
    /// Histogram, Summary, and ExponentialHistogram remain unmapped pending demand.
    /// </summary>
    internal static partial class OTLPProtobufConverter
    {
        private static PbMetrics.ResourceMetrics ToProto(OTLPResourceMetrics resourceMetrics)
        {
            var proto = new PbMetrics.ResourceMetrics { Resource = ToProto(resourceMetrics.Resource) };
            if (!string.IsNullOrEmpty(resourceMetrics.SchemaUrl)) { proto.SchemaUrl = resourceMetrics.SchemaUrl; }
            foreach (var scope in resourceMetrics.ScopeMetrics)
            {
                if (scope == null) { continue; }
                proto.ScopeMetrics.Add(ToProto(scope));
            }
            return proto;
        }

        private static PbMetrics.ScopeMetrics ToProto(OTLPScopeMetrics scopeMetrics)
        {
            var proto = new PbMetrics.ScopeMetrics { Scope = ToProto(scopeMetrics.Scope) };
            if (!string.IsNullOrEmpty(scopeMetrics.SchemaUrl)) { proto.SchemaUrl = scopeMetrics.SchemaUrl; }
            foreach (var metric in scopeMetrics.Metrics)
            {
                if (metric == null) { continue; }
                proto.Metrics.Add(ToProto(metric));
            }
            return proto;
        }

        private static PbMetrics.Metric ToProto(OTLPMetricPayload metric)
        {
            var proto = new PbMetrics.Metric
            {
                Name = metric.Name ?? string.Empty,
                Description = metric.Description ?? string.Empty,
                Unit = metric.Unit ?? string.Empty
            };
            if (metric.Type == OTLPMetricType.Gauge && metric.Gauge != null)
            {
                proto.Gauge = ToProto(metric.Gauge);
            }
            else if (metric.Type == OTLPMetricType.Sum && metric.Sum != null)
            {
                proto.Sum = ToProto(metric.Sum);
            }
            return proto;
        }

        private static PbMetrics.Gauge ToProto(OTLPGaugePayload gauge)
        {
            var proto = new PbMetrics.Gauge();
            foreach (var point in gauge.DataPoints)
            {
                if (point == null) { continue; }
                proto.DataPoints.Add(ToProto(point));
            }
            return proto;
        }

        private static PbMetrics.Sum ToProto(OTLPSumPayload sum)
        {
            var proto = new PbMetrics.Sum
            {
                AggregationTemporality = (PbMetrics.AggregationTemporality)sum.AggregationTemporality,
                IsMonotonic = sum.IsMonotonic
            };
            foreach (var point in sum.DataPoints)
            {
                if (point == null) { continue; }
                proto.DataPoints.Add(ToProto(point));
            }
            return proto;
        }

        private static PbMetrics.NumberDataPoint ToProto(OTLPNumberDataPoint point)
        {
            var proto = new PbMetrics.NumberDataPoint
            {
                StartTimeUnixNano = point.StartTimeUnixNano,
                TimeUnixNano = point.TimeUnixNano,
                Flags = point.Flags
            };
            if (point.AsInt.HasValue) { proto.AsInt = point.AsInt.Value; }
            else if (point.AsDouble.HasValue) { proto.AsDouble = point.AsDouble.Value; }
            AddAttributes(proto.Attributes, point.Attributes);
            return proto;
        }
    }
}
