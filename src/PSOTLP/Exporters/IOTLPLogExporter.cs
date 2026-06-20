using System.Collections.Generic;
using PSOTLP.Connections;
using PSOTLP.Models;

namespace PSOTLP.Exporters
{
    public interface IOTLPLogExporter
    {
        OTLPExportResult Export(OTLPConnection connection, IList<OTLPLogRecord> records);
    }

    public interface IOTLPTraceExporter
    {
        OTLPExportResult Export(OTLPConnection connection, IList<OTLPSpan> spans);
    }

    public interface IOTLPMetricExporter
    {
        OTLPExportResult Export(OTLPConnection connection, IList<OTLPMetric> metrics);
    }
}
