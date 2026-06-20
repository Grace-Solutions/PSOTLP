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
    /// Synchronous trace exporter. Mirrors <see cref="OTLPLogExporter"/>: builds a fresh request
    /// per call, materializes header secure strings only here, and clears the materialized header
    /// map before returning.
    /// </summary>
    public sealed class OTLPTraceExporter : IOTLPTraceExporter
    {
        public const string Component = "OTLPTraceExporter";

        private readonly IOTLPHttpClient _httpClient;
        private readonly IOTLPSerializer _serializer;
        private readonly IOTLPLogger _logger;

        public OTLPTraceExporter(IOTLPHttpClient httpClient, IOTLPSerializer serializer, IOTLPLogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger;
        }

        public OTLPExportResult Export(OTLPConnection connection, IList<OTLPSpan> spans)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }
            if (spans == null || spans.Count == 0)
            {
                return new OTLPExportResult { Success = true, RecordCount = 0, ExportedAtUtc = DateTimeOffset.UtcNow };
            }

            var definition = OTLPEndpointRegistry.Get(OTLPEndpointRegistry.ExportTracesName);
            var uri = OTLPUriBuilder.Build(connection.EndpointUri, definition, connection.TracesEndpointUri, connection.Encoding);
            var payload = BuildPayload(connection, spans);

            var body = _serializer.SerializeTraces(payload);
            IDictionary<string, string> headers = null;
            try
            {
                headers = OTLPHeaderUtility.MaterializeForRequest(connection.Headers);
                var request = new OTLPHttpRequest
                {
                    OperationName = "ExportTraces",
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

                if (_logger != null) { _logger.Info(Component, "Sending OTLP trace batch of " + spans.Count + " spans. Please Wait..."); }
                var response = _httpClient.Send(request);

                if (!response.IsSuccess)
                {
                    throw new OTLPHttpException("OTLP trace export failed with status " + response.StatusCode + ".")
                    {
                        StatusCode = response.StatusCode,
                        ReasonPhrase = response.ReasonPhrase
                    };
                }

                if (_logger != null) { _logger.Info(Component, "OTLP trace batch export was successful (" + spans.Count + " spans, status " + response.StatusCode + ")."); }
                return new OTLPExportResult
                {
                    SignalType = definition.SignalType,
                    EndpointUri = OTLPUriBuilder.Sanitize(uri),
                    StatusCode = response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    AttemptCount = 1,
                    RecordCount = spans.Count,
                    Success = true,
                    ExportedAtUtc = DateTimeOffset.UtcNow
                };
            }
            finally
            {
                if (headers != null) { headers.Clear(); }
            }
        }

        private OTLPExportTraceServiceRequest BuildPayload(OTLPConnection connection, IList<OTLPSpan> spans)
        {
            var resource = OTLPResourceBuilder.Build(connection, null);
            var scope = OTLPResourceBuilder.BuildScope(connection);

            var scopeSpans = new OTLPScopeSpans { Scope = scope };
            foreach (var span in spans)
            {
                if (span == null) { continue; }
                scopeSpans.Spans.Add(ToPayload(span));
            }

            var resourceSpans = new OTLPResourceSpans { Resource = resource };
            resourceSpans.ScopeSpans.Add(scopeSpans);
            var request = new OTLPExportTraceServiceRequest();
            request.ResourceSpans.Add(resourceSpans);
            return request;
        }

        private OTLPSpanPayload ToPayload(OTLPSpan span)
        {
            var payload = new OTLPSpanPayload
            {
                TraceId = span.TraceId,
                SpanId = span.SpanId,
                ParentSpanId = span.ParentSpanId,
                Name = span.Name,
                Kind = (int)span.Kind + 1,
                StartTimeUnixNano = OTLPTimeUtility.ToUnixNano(span.StartTimeUtc),
                EndTimeUnixNano = span.EndTimeUtc.HasValue ? OTLPTimeUtility.ToUnixNano(span.EndTimeUtc.Value) : 0UL,
                Attributes = OTLPAttributeConverter.ToKeyValueList(span.Attributes)
            };

            if (span.Status != null)
            {
                payload.Status = new OTLPStatusPayload { Code = (int)span.Status.Code, Message = span.Status.Message };
            }

            if (span.Events != null)
            {
                foreach (var ev in span.Events)
                {
                    if (ev == null) { continue; }
                    payload.Events.Add(new OTLPSpanEventPayload
                    {
                        Name = ev.Name,
                        TimeUnixNano = OTLPTimeUtility.ToUnixNano(ev.TimestampUtc),
                        Attributes = OTLPAttributeConverter.ToKeyValueList(ev.Attributes)
                    });
                }
            }

            return payload;
        }
    }
}
