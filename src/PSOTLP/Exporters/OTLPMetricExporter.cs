using System;
using System.Collections.Generic;
using PSOTLP.Authentication;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Endpoints;
using PSOTLP.Errors;
using PSOTLP.Http;
using PSOTLP.Logging;
using PSOTLP.Models;
using PSOTLP.Resources;
using PSOTLP.Serialization;

namespace PSOTLP.Exporters
{
    /// <summary>
    /// Synchronous metric exporter. Mirrors <see cref="OTLPLogExporter"/>: builds a fresh request
    /// per call, materializes header secure strings only here, and clears the materialized header
    /// map before returning.
    /// </summary>
    public sealed class OTLPMetricExporter : IOTLPMetricExporter
    {
        public const string Component = "OTLPMetricExporter";

        private readonly IOTLPHttpClient _httpClient;
        private readonly IOTLPSerializer _serializer;
        private readonly IOTLPLogger _logger;

        public OTLPMetricExporter(IOTLPHttpClient httpClient, IOTLPSerializer serializer, IOTLPLogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public OTLPExportResult Export(OTLPConnection connection, IList<OTLPMetric> metrics)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }
            if (metrics == null || metrics.Count == 0)
            {
                return new OTLPExportResult { Success = true, RecordCount = 0, ExportedAtUtc = DateTimeOffset.UtcNow };
            }

            var definition = OTLPEndpointRegistry.Get(OTLPEndpointRegistry.ExportMetricsName);
            var uri = OTLPUriBuilder.Build(connection.EndpointUri, definition, connection.MetricsEndpointUri, connection.Encoding, connection.NoSignalPath);
            var payload = BuildPayload(connection, metrics);

            var body = _serializer.SerializeMetrics(payload);
            IDictionary<string, string> headers = null;
            try
            {
                headers = OTLPHeaderUtility.MaterializeForRequest(connection.Headers);
                var request = new OTLPHttpRequest
                {
                    OperationName = "ExportMetrics",
                    EndpointName = definition.Name,
                    SignalType = definition.SignalType,
                    Method = definition.Method,
                    Uri = uri,
                    Headers = headers,
                    Body = body,
                    ContentType = _serializer.ContentType,
                    Compression = connection.Compression,
                    TimeoutSeconds = connection.TimeoutSeconds,
                    BodyMayContainSensitiveData = true
                };

                if (_logger != null) { _logger.Info(Component, "Sending OTLP metric batch of " + metrics.Count + " records. Please Wait..."); }
                var response = _httpClient.Send(request);

                if (!response.IsSuccess)
                {
                    throw new OTLPHttpException("OTLP metric export failed with status " + response.StatusCode + ".")
                    {
                        StatusCode = response.StatusCode,
                        ReasonPhrase = response.ReasonPhrase
                    };
                }

                if (_logger != null) { _logger.Info(Component, "OTLP metric batch export was successful (" + metrics.Count + " records, status " + response.StatusCode + ")."); }
                return new OTLPExportResult
                {
                    SignalType = definition.SignalType,
                    EndpointUri = OTLPUriBuilder.Sanitize(uri),
                    StatusCode = response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    AttemptCount = 1,
                    RecordCount = metrics.Count,
                    Success = true,
                    ExportedAtUtc = DateTimeOffset.UtcNow
                };
            }
            finally
            {
                if (headers != null) { headers.Clear(); }
            }
        }

        private OTLPExportMetricsServiceRequest BuildPayload(OTLPConnection connection, IList<OTLPMetric> metrics)
        {
            var resource = OTLPResourceBuilder.Build(connection, null);
            var scope = OTLPResourceBuilder.BuildScope(connection);

            var scopeMetrics = new OTLPScopeMetrics { Scope = scope };
            foreach (var metric in metrics)
            {
                if (metric == null) { continue; }
                scopeMetrics.Metrics.Add(ToPayload(metric));
            }

            var resourceMetrics = new OTLPResourceMetrics { Resource = resource };
            resourceMetrics.ScopeMetrics.Add(scopeMetrics);
            var request = new OTLPExportMetricsServiceRequest();
            request.ResourceMetrics.Add(resourceMetrics);
            return request;
        }

        private static OTLPMetricPayload ToPayload(OTLPMetric metric)
        {
            var point = new OTLPNumberDataPoint
            {
                TimeUnixNano = OTLPTimeUtility.ToUnixNano(metric.TimestampUtc),
                StartTimeUnixNano = OTLPTimeUtility.ToUnixNano(metric.StartTimestampUtc),
                Attributes = OTLPAttributeConverter.ToKeyValueList(metric.Attributes)
            };
            if (metric.UseIntValue) { point.AsInt = metric.IntValue; }
            else { point.AsDouble = metric.DoubleValue; }

            var payload = new OTLPMetricPayload
            {
                Name = metric.Name,
                Description = metric.Description,
                Unit = metric.Unit,
                Type = metric.Type
            };
            if (metric.Type == OTLPMetricType.Sum)
            {
                payload.Sum = new OTLPSumPayload { AggregationTemporality = metric.Temporality, IsMonotonic = metric.IsMonotonic };
                payload.Sum.DataPoints.Add(point);
            }
            else
            {
                payload.Gauge = new OTLPGaugePayload();
                payload.Gauge.DataPoints.Add(point);
            }
            return payload;
        }
    }
}
