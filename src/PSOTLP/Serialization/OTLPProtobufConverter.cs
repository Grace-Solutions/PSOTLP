using System;
using System.Collections.Generic;
using Google.Protobuf;
using PSOTLP.Models;
using PbCommon = OpenTelemetry.Proto.Common.V1;
using PbResource = OpenTelemetry.Proto.Resource.V1;
using PbLogs = OpenTelemetry.Proto.Logs.V1;
using PbTrace = OpenTelemetry.Proto.Trace.V1;
using PbLogsService = OpenTelemetry.Proto.Collector.Logs.V1;
using PbTraceService = OpenTelemetry.Proto.Collector.Trace.V1;
using PbMetricsService = OpenTelemetry.Proto.Collector.Metrics.V1;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// Maps PSOTLP internal payload models to the generated OpenTelemetry.Proto types.
    /// Hex string trace/span ids are decoded into raw bytes per the OTLP spec.
    /// </summary>
    internal static partial class OTLPProtobufConverter
    {
        public static PbLogsService.ExportLogsServiceRequest ToProto(OTLPExportLogsServiceRequest request)
        {
            var proto = new PbLogsService.ExportLogsServiceRequest();
            if (request == null) { return proto; }
            foreach (var resourceLogs in request.ResourceLogs)
            {
                if (resourceLogs == null) { continue; }
                proto.ResourceLogs.Add(ToProto(resourceLogs));
            }
            return proto;
        }

        public static PbTraceService.ExportTraceServiceRequest ToProto(OTLPExportTraceServiceRequest request)
        {
            var proto = new PbTraceService.ExportTraceServiceRequest();
            if (request == null) { return proto; }
            foreach (var resourceSpans in request.ResourceSpans)
            {
                if (resourceSpans == null) { continue; }
                proto.ResourceSpans.Add(ToProto(resourceSpans));
            }
            return proto;
        }

        public static PbMetricsService.ExportMetricsServiceRequest ToProto(OTLPExportMetricsServiceRequest request)
        {
            var proto = new PbMetricsService.ExportMetricsServiceRequest();
            if (request == null) { return proto; }
            foreach (var resourceMetrics in request.ResourceMetrics)
            {
                if (resourceMetrics == null) { continue; }
                proto.ResourceMetrics.Add(ToProto(resourceMetrics));
            }
            return proto;
        }

        private static PbLogs.ResourceLogs ToProto(OTLPResourceLogs resourceLogs)
        {
            var proto = new PbLogs.ResourceLogs { Resource = ToProto(resourceLogs.Resource) };
            if (!string.IsNullOrEmpty(resourceLogs.SchemaUrl)) { proto.SchemaUrl = resourceLogs.SchemaUrl; }
            foreach (var scope in resourceLogs.ScopeLogs)
            {
                if (scope == null) { continue; }
                proto.ScopeLogs.Add(ToProto(scope));
            }
            return proto;
        }

        private static PbLogs.ScopeLogs ToProto(OTLPScopeLogs scopeLogs)
        {
            var proto = new PbLogs.ScopeLogs { Scope = ToProto(scopeLogs.Scope) };
            if (!string.IsNullOrEmpty(scopeLogs.SchemaUrl)) { proto.SchemaUrl = scopeLogs.SchemaUrl; }
            foreach (var record in scopeLogs.LogRecords)
            {
                if (record == null) { continue; }
                proto.LogRecords.Add(ToProto(record));
            }
            return proto;
        }

        private static PbLogs.LogRecord ToProto(OTLPLogRecordPayload record)
        {
            var proto = new PbLogs.LogRecord
            {
                TimeUnixNano = record.TimeUnixNano,
                ObservedTimeUnixNano = record.ObservedTimeUnixNano,
                SeverityNumber = (PbLogs.SeverityNumber)record.SeverityNumber,
                SeverityText = record.SeverityText ?? string.Empty,
                Body = ToProto(record.Body),
                Flags = record.Flags,
                TraceId = HexToByteString(record.TraceId, 16),
                SpanId = HexToByteString(record.SpanId, 8),
                EventName = record.EventName ?? string.Empty,
                DroppedAttributesCount = (uint)Math.Max(0, record.DroppedAttributesCount)
            };
            AddAttributes(proto.Attributes, record.Attributes);
            return proto;
        }

        private static PbResource.Resource ToProto(OTLPResource resource)
        {
            var proto = new PbResource.Resource();
            if (resource == null) { return proto; }
            proto.DroppedAttributesCount = (uint)Math.Max(0, resource.DroppedAttributesCount);
            AddAttributes(proto.Attributes, resource.Attributes);
            return proto;
        }

        private static PbCommon.InstrumentationScope ToProto(OTLPInstrumentationScope scope)
        {
            var proto = new PbCommon.InstrumentationScope();
            if (scope == null) { return proto; }
            proto.Name = scope.Name ?? string.Empty;
            proto.Version = scope.Version ?? string.Empty;
            proto.DroppedAttributesCount = (uint)Math.Max(0, scope.DroppedAttributesCount);
            AddAttributes(proto.Attributes, scope.Attributes);
            return proto;
        }

        private static PbCommon.AnyValue ToProto(OTLPAnyValue value)
        {
            var proto = new PbCommon.AnyValue();
            if (value == null) { return proto; }
            if (value.StringValue != null) { proto.StringValue = value.StringValue; return proto; }
            if (value.BoolValue.HasValue) { proto.BoolValue = value.BoolValue.Value; return proto; }
            if (value.IntValue.HasValue) { proto.IntValue = value.IntValue.Value; return proto; }
            if (value.DoubleValue.HasValue) { proto.DoubleValue = value.DoubleValue.Value; return proto; }
            if (value.BytesValue != null) { proto.BytesValue = ByteString.CopyFrom(value.BytesValue); return proto; }
            if (value.ArrayValue != null)
            {
                var arr = new PbCommon.ArrayValue();
                foreach (var item in value.ArrayValue) { arr.Values.Add(ToProto(item)); }
                proto.ArrayValue = arr;
                return proto;
            }
            if (value.KvlistValue != null)
            {
                var kv = new PbCommon.KeyValueList();
                foreach (var pair in value.KvlistValue)
                {
                    if (pair == null || string.IsNullOrEmpty(pair.Key)) { continue; }
                    kv.Values.Add(new PbCommon.KeyValue { Key = pair.Key, Value = ToProto(pair.Value) });
                }
                proto.KvlistValue = kv;
                return proto;
            }
            return proto;
        }

        private static void AddAttributes(Google.Protobuf.Collections.RepeatedField<PbCommon.KeyValue> target, IList<OTLPKeyValue> source)
        {
            if (source == null) { return; }
            foreach (var pair in source)
            {
                if (pair == null || string.IsNullOrEmpty(pair.Key)) { continue; }
                target.Add(new PbCommon.KeyValue { Key = pair.Key, Value = ToProto(pair.Value) });
            }
        }

        private static ByteString HexToByteString(string hex, int expectedBytes)
        {
            if (string.IsNullOrEmpty(hex)) { return ByteString.Empty; }
            if ((hex.Length & 1) != 0) { return ByteString.Empty; }
            int byteLen = hex.Length / 2;
            if (expectedBytes > 0 && byteLen != expectedBytes) { return ByteString.Empty; }
            var buffer = new byte[byteLen];
            for (int i = 0; i < byteLen; i++)
            {
                int hi = FromHex(hex[i * 2]);
                int lo = FromHex(hex[(i * 2) + 1]);
                if (hi < 0 || lo < 0) { return ByteString.Empty; }
                buffer[i] = (byte)((hi << 4) | lo);
            }
            return ByteString.CopyFrom(buffer);
        }

        private static int FromHex(char c)
        {
            if (c >= '0' && c <= '9') { return c - '0'; }
            if (c >= 'a' && c <= 'f') { return 10 + (c - 'a'); }
            if (c >= 'A' && c <= 'F') { return 10 + (c - 'A'); }
            return -1;
        }
    }
}
