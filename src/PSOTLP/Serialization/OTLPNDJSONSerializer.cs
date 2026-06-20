using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using PSOTLP.Errors;
using PSOTLP.Models;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// NDJSON serializer for backends that consume one flat snake_case log document per line
    /// (e.g. Rootprint's /api/ingest/ndjson). Each log record is flattened with its resource
    /// and scope attributes materialized into the same document and terminated by a single LF.
    /// Trace export is intentionally unsupported; the NDJSON contract is logs-only.
    /// </summary>
    public sealed class OTLPNDJSONSerializer : IOTLPSerializer
    {
        public string ContentType { get { return "application/x-ndjson"; } }

        private readonly JsonSerializerSettings _settings;

        public OTLPNDJSONSerializer()
        {
            _settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.None
            };
        }

        public byte[] SerializeLogs(OTLPExportLogsServiceRequest request)
        {
            if (request == null) { return new byte[0]; }
            try
            {
                using (var buffer = new MemoryStream())
                using (var writer = new StreamWriter(buffer, new UTF8Encoding(false)))
                {
                    foreach (var resourceLogs in request.ResourceLogs)
                    {
                        if (resourceLogs == null) { continue; }
                        var resourceAttributes = ToFlatDictionary(resourceLogs.Resource != null ? resourceLogs.Resource.Attributes : null);
                        var resourceDropped = resourceLogs.Resource != null ? resourceLogs.Resource.DroppedAttributesCount : 0;
                        foreach (var scopeLogs in resourceLogs.ScopeLogs)
                        {
                            if (scopeLogs == null) { continue; }
                            var scopeName = scopeLogs.Scope != null ? scopeLogs.Scope.Name : null;
                            var scopeVersion = scopeLogs.Scope != null ? scopeLogs.Scope.Version : null;
                            var scopeAttributes = ToFlatDictionary(scopeLogs.Scope != null ? scopeLogs.Scope.Attributes : null);
                            var scopeDropped = scopeLogs.Scope != null ? scopeLogs.Scope.DroppedAttributesCount : 0;
                            foreach (var record in scopeLogs.LogRecords)
                            {
                                if (record == null) { continue; }
                                var document = BuildDocument(record, resourceAttributes, resourceDropped, scopeName, scopeVersion, scopeAttributes, scopeDropped);
                                writer.Write(JsonConvert.SerializeObject(document, _settings));
                                writer.Write('\n');
                            }
                        }
                    }
                    writer.Flush();
                    return buffer.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new OTLPSerializationException("OTLP NDJSON serialization failed: " + ex.Message, ex);
            }
        }

        public byte[] SerializeTraces(OTLPExportTraceServiceRequest request)
        {
            throw new OTLPSerializationException("NDJSON encoding does not support trace export. Use JSON or Protobuf for traces.");
        }

        public byte[] SerializeMetrics(OTLPExportMetricsServiceRequest request)
        {
            throw new OTLPSerializationException("NDJSON encoding does not support metric export. Use JSON or Protobuf for metrics.");
        }

        private static IDictionary<string, object> BuildDocument(OTLPLogRecordPayload record, IDictionary<string, object> resourceAttributes, int resourceDropped, string scopeName, string scopeVersion, IDictionary<string, object> scopeAttributes, int scopeDropped)
        {
            string serviceName = null;
            if (resourceAttributes != null && resourceAttributes.TryGetValue("service.name", out var svc) && svc != null)
            {
                serviceName = svc.ToString();
            }

            var document = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["timestamp_nanos"] = (long)record.TimeUnixNano,
                ["observed_timestamp_nanos"] = (long)record.ObservedTimeUnixNano,
                ["severity_text"] = record.SeverityText,
                ["severity_number"] = record.SeverityNumber,
                ["body"] = ToBody(record.Body),
                ["service_name"] = serviceName,
                ["attributes"] = ToFlatDictionary(record.Attributes),
                ["resource_attributes"] = resourceAttributes,
                ["trace_id"] = record.TraceId,
                ["span_id"] = record.SpanId,
                ["trace_flags"] = record.Flags,
                ["scope_name"] = scopeName,
                ["scope_version"] = scopeVersion,
                ["scope_attributes"] = scopeAttributes,
                ["dropped_attributes_count"] = record.DroppedAttributesCount,
                ["resource_dropped_attributes_count"] = resourceDropped,
                ["scope_dropped_attributes_count"] = scopeDropped
            };
            return document;
        }

        private static object ToBody(OTLPAnyValue body)
        {
            if (body == null) { return new Dictionary<string, object>(StringComparer.Ordinal) { ["message"] = string.Empty }; }
            if (body.KvlistValue != null) { return ToFlatDictionary(body.KvlistValue); }
            var text = AnyValueToScalar(body);
            return new Dictionary<string, object>(StringComparer.Ordinal) { ["message"] = text == null ? string.Empty : text.ToString() };
        }

        private static IDictionary<string, object> ToFlatDictionary(IList<OTLPKeyValue> attributes)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            if (attributes == null) { return result; }
            foreach (var pair in attributes)
            {
                if (pair == null || string.IsNullOrWhiteSpace(pair.Key)) { continue; }
                result[pair.Key] = AnyValueToScalar(pair.Value);
            }
            return result;
        }

        private static object AnyValueToScalar(OTLPAnyValue value)
        {
            if (value == null) { return null; }
            if (value.StringValue != null) { return value.StringValue; }
            if (value.BoolValue.HasValue) { return value.BoolValue.Value; }
            if (value.IntValue.HasValue) { return value.IntValue.Value; }
            if (value.DoubleValue.HasValue) { return value.DoubleValue.Value; }
            if (value.BytesValue != null) { return Convert.ToBase64String(value.BytesValue); }
            if (value.KvlistValue != null) { return ToFlatDictionary(value.KvlistValue); }
            if (value.ArrayValue != null)
            {
                var list = new List<object>(value.ArrayValue.Count);
                foreach (var item in value.ArrayValue) { list.Add(AnyValueToScalar(item)); }
                return list;
            }
            return null;
        }
    }
}
