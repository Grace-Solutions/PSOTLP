using System;
using Google.Protobuf;
using PSOTLP.Errors;
using PSOTLP.Models;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// OTLP/HTTP protobuf serializer. Maps internal payload models to the generated
    /// OpenTelemetry.Proto types and emits canonical protobuf bytes. Required by backends
    /// (e.g. Rootprint) that reject JSON at /v1/logs with HTTP 415.
    /// </summary>
    public sealed class OTLPProtobufSerializer : IOTLPSerializer
    {
        public string ContentType { get { return "application/x-protobuf"; } }

        public byte[] SerializeLogs(OTLPExportLogsServiceRequest request)
        {
            if (request == null) { return new byte[0]; }
            try
            {
                var proto = OTLPProtobufConverter.ToProto(request);
                return proto.ToByteArray();
            }
            catch (Exception ex)
            {
                throw new OTLPSerializationException("OTLP protobuf log serialization failed: " + ex.Message, ex);
            }
        }

        public byte[] SerializeTraces(OTLPExportTraceServiceRequest request)
        {
            if (request == null) { return new byte[0]; }
            try
            {
                var proto = OTLPProtobufConverter.ToProto(request);
                return proto.ToByteArray();
            }
            catch (Exception ex)
            {
                throw new OTLPSerializationException("OTLP protobuf trace serialization failed: " + ex.Message, ex);
            }
        }

        public byte[] SerializeMetrics(OTLPExportMetricsServiceRequest request)
        {
            if (request == null) { return new byte[0]; }
            try
            {
                var proto = OTLPProtobufConverter.ToProto(request);
                return proto.ToByteArray();
            }
            catch (Exception ex)
            {
                throw new OTLPSerializationException("OTLP protobuf metric serialization failed: " + ex.Message, ex);
            }
        }
    }
}
