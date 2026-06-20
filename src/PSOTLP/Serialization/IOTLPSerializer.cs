using PSOTLP.Models;

namespace PSOTLP.Serialization
{
    public interface IOTLPSerializer
    {
        string ContentType { get; }
        byte[] SerializeLogs(OTLPExportLogsServiceRequest request);
        byte[] SerializeTraces(OTLPExportTraceServiceRequest request);
        byte[] SerializeMetrics(OTLPExportMetricsServiceRequest request);
    }
}
