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
using PSOTLP.Redaction;
using PSOTLP.Resources;
using PSOTLP.Serialization;

namespace PSOTLP.Exporters
{
    /// <summary>
    /// Synchronous log exporter. Builds a fresh OTLPHttpRequest per call, materializes header
    /// secure strings only here, and clears the materialized header map before returning.
    /// </summary>
    public sealed class OTLPLogExporter : IOTLPLogExporter
    {
        public const string Component = "OTLPLogExporter";

        private readonly IOTLPHttpClient _httpClient;
        private readonly IOTLPSerializer _serializer;
        private readonly OTLPRedactionEngine _redaction;
        private readonly IOTLPLogger _logger;

        public OTLPLogExporter(IOTLPHttpClient httpClient, IOTLPSerializer serializer, OTLPRedactionEngine redaction, IOTLPLogger logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _redaction = redaction;
            _logger = logger;
        }

        public OTLPExportResult Export(OTLPConnection connection, IList<OTLPLogRecord> records)
        {
            if (connection == null) { throw new ArgumentNullException(nameof(connection)); }
            if (records == null || records.Count == 0)
            {
                return new OTLPExportResult { Success = true, RecordCount = 0, ExportedAtUtc = DateTimeOffset.UtcNow };
            }

            var definition = OTLPEndpointRegistry.Get(OTLPEndpointRegistry.ExportLogsName);
            var uri = OTLPUriBuilder.Build(connection.EndpointUri, definition, connection.LogsEndpointUri, connection.Encoding, connection.NoSignalPath);
            var payload = BuildPayload(connection, records);

            var body = _serializer.SerializeLogs(payload);
            IDictionary<string, string> headers = null;
            try
            {
                headers = OTLPHeaderUtility.MaterializeForRequest(connection.Headers);
                var request = new OTLPHttpRequest
                {
                    OperationName = "ExportLogs",
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

                if (_logger != null) { _logger.Info(Component, "Sending OTLP log batch of " + records.Count + " records. Please Wait..."); }
                var response = _httpClient.Send(request);

                if (!response.IsSuccess)
                {
                    throw new OTLPHttpException("OTLP log export failed with status " + response.StatusCode + ".")
                    {
                        StatusCode = response.StatusCode,
                        ReasonPhrase = response.ReasonPhrase
                    };
                }

                if (_logger != null) { _logger.Info(Component, "OTLP log batch export was successful (" + records.Count + " records, status " + response.StatusCode + ")."); }
                return new OTLPExportResult
                {
                    SignalType = definition.SignalType,
                    EndpointUri = OTLPUriBuilder.Sanitize(uri),
                    StatusCode = response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    AttemptCount = 1,
                    RecordCount = records.Count,
                    Success = true,
                    ExportedAtUtc = DateTimeOffset.UtcNow
                };
            }
            finally
            {
                if (headers != null) { headers.Clear(); }
            }
        }

        private OTLPExportLogsServiceRequest BuildPayload(OTLPConnection connection, IList<OTLPLogRecord> records)
        {
            var resource = OTLPResourceBuilder.Build(connection, null);
            var scope = OTLPResourceBuilder.BuildScope(connection);

            var scopeLogs = new OTLPScopeLogs { Scope = scope };
            foreach (var record in records)
            {
                if (record == null) { continue; }
                scopeLogs.LogRecords.Add(ToPayload(connection, record));
            }

            var resourceLogs = new OTLPResourceLogs { Resource = resource };
            resourceLogs.ScopeLogs.Add(scopeLogs);
            var request = new OTLPExportLogsServiceRequest();
            request.ResourceLogs.Add(resourceLogs);
            return request;
        }

        private OTLPLogRecordPayload ToPayload(OTLPConnection connection, OTLPLogRecord record)
        {
            var severityNumber = record.SeverityNumber > 0 ? record.SeverityNumber : OTLPSeverityMapper.ToNumber(record.Severity);
            var severityText = !string.IsNullOrWhiteSpace(record.SeverityText) ? record.SeverityText : OTLPSeverityMapper.ToText(record.Severity);
            var bodyText = _redaction != null ? _redaction.Redact(record.Body) : record.Body;

            var mode = OTLPResourceBuilder.ResolveMode(connection, record.AttributeMergeMode);
            var overrides = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (record.LogAttributes != null) { foreach (var pair in record.LogAttributes) { overrides[pair.Key] = pair.Value; } }
            if (record.Attributes != null) { foreach (var pair in record.Attributes) { overrides[pair.Key] = pair.Value; } }
            var baseline = connection != null ? connection.LogAttributes : null;
            var attributes = OTLPAttributeMerger.Apply(baseline, overrides, mode);

            return new OTLPLogRecordPayload
            {
                TimeUnixNano = OTLPTimeUtility.ToUnixNano(record.TimestampUtc),
                ObservedTimeUnixNano = OTLPTimeUtility.ToUnixNano(record.ObservedTimestampUtc),
                SeverityNumber = severityNumber,
                SeverityText = severityText,
                Body = OTLPAttributeConverter.ToAnyValue(bodyText),
                Attributes = OTLPAttributeConverter.ToKeyValueList(attributes),
                TraceId = record.TraceId,
                SpanId = record.SpanId,
                EventName = record.EventName
            };
        }
    }
}
