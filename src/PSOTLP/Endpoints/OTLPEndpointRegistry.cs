using System;
using System.Collections.Generic;
using PSOTLP.Common;

namespace PSOTLP.Endpoints
{
    /// <summary>
    /// Centralized registry of OTLP signal endpoint definitions. No cmdlet may hard-code signal
    /// paths; all callers consult this registry through <see cref="Get"/>.
    /// </summary>
    public static class OTLPEndpointRegistry
    {
        public const string ExportLogsName = "ExportLogs";
        public const string ExportTracesName = "ExportTraces";
        public const string ExportMetricsName = "ExportMetrics";

        private static readonly Dictionary<string, OTLPEndpointDefinition> Definitions =
            new Dictionary<string, OTLPEndpointDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    ExportLogsName,
                    new OTLPEndpointDefinition
                    {
                        Name = ExportLogsName,
                        SignalType = OTLPSignalType.Logs,
                        Method = "POST",
                        DefaultPath = "/v1/logs",
                        DefaultContentType = "application/json",
                        SupportsCompression = true,
                        RequiresAuthorization = false
                    }
                },
                {
                    ExportTracesName,
                    new OTLPEndpointDefinition
                    {
                        Name = ExportTracesName,
                        SignalType = OTLPSignalType.Traces,
                        Method = "POST",
                        DefaultPath = "/v1/traces",
                        DefaultContentType = "application/json",
                        SupportsCompression = true,
                        RequiresAuthorization = false
                    }
                },
                {
                    ExportMetricsName,
                    new OTLPEndpointDefinition
                    {
                        Name = ExportMetricsName,
                        SignalType = OTLPSignalType.Metrics,
                        Method = "POST",
                        DefaultPath = "/v1/metrics",
                        DefaultContentType = "application/json",
                        SupportsCompression = true,
                        RequiresAuthorization = false
                    }
                }
            };

        public static OTLPEndpointDefinition Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) { throw new ArgumentException("Endpoint name is required.", nameof(name)); }
            OTLPEndpointDefinition definition;
            if (!Definitions.TryGetValue(name, out definition))
            {
                throw new KeyNotFoundException("The OTLP endpoint definition '" + name + "' is not registered.");
            }
            return definition;
        }

        public static OTLPEndpointDefinition GetForSignal(OTLPSignalType signalType)
        {
            switch (signalType)
            {
                case OTLPSignalType.Logs: return Get(ExportLogsName);
                case OTLPSignalType.Traces: return Get(ExportTracesName);
                case OTLPSignalType.Metrics: return Get(ExportMetricsName);
                default: throw new ArgumentOutOfRangeException(nameof(signalType));
            }
        }

        public static IEnumerable<OTLPEndpointDefinition> All() { return Definitions.Values; }
    }
}
