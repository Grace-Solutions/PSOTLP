using System;
using System.Collections.Generic;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Models;

namespace PSOTLP.Resources
{
    /// <summary>
    /// Centralized resource attribute merger. Resolves the final OTLPResource value used by the
    /// log and trace exporters by combining (in precedence order):
    ///   1. Built-in system inventory attributes (host/os/process).
    ///   2. Module identity attributes (telemetry.sdk.*, service.* defaults).
    ///   3. Connection-level resource attributes (Connect-OTLP -ResourceAttribute).
    ///   4. Per-record resource attributes supplied by the cmdlet caller.
    /// </summary>
    public static class OTLPResourceBuilder
    {
        private static readonly OTLPSystemInformationProvider SystemInfo = new OTLPSystemInformationProvider();

        public static OTLPResource Build(OTLPConnection connection, IDictionary<string, object> recordOverrides)
        {
            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            MergeInto(merged, SystemInfo.GetHostAttributes());
            MergeInto(merged, SystemInfo.GetHardwareAttributes());
            MergeInto(merged, GetModuleAttributes(connection));

            if (connection != null && connection.ResourceAttributes != null)
            {
                MergeInto(merged, connection.ResourceAttributes);
            }

            if (recordOverrides != null) { MergeInto(merged, recordOverrides); }

            return new OTLPResource { Attributes = OTLPAttributeConverter.ToKeyValueList(merged) };
        }

        public static OTLPInstrumentationScope BuildScope(OTLPConnection connection)
        {
            return new OTLPInstrumentationScope
            {
                Name = OTLPAttributeConverter.NormalizeMissing(
                    connection != null && !string.IsNullOrWhiteSpace(connection.ScopeName)
                        ? connection.ScopeName
                        : OTLPModuleInfo.TelemetrySdkName),
                Version = OTLPAttributeConverter.NormalizeMissing(
                    connection != null && !string.IsNullOrWhiteSpace(connection.ScopeVersion)
                        ? connection.ScopeVersion
                        : OTLPModuleInfo.Version)
            };
        }

        private static IDictionary<string, object> GetModuleAttributes(OTLPConnection connection)
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["service.name"] = OTLPAttributeConverter.NormalizeMissing(connection != null ? connection.ServiceName : null),
                ["service.namespace"] = OTLPAttributeConverter.NormalizeMissing(connection != null ? connection.ServiceNamespace : null),
                ["service.instance.id"] = OTLPAttributeConverter.NormalizeMissing(connection != null ? connection.ServiceInstanceId : null),
                ["deployment.environment"] = OTLPAttributeConverter.NormalizeMissing(connection != null ? connection.EnvironmentName : null),
                ["telemetry.sdk.name"] = OTLPModuleInfo.TelemetrySdkName,
                ["telemetry.sdk.language"] = OTLPModuleInfo.TelemetrySdkLanguage,
                ["telemetry.sdk.version"] = OTLPAttributeConverter.NormalizeMissing(OTLPModuleInfo.Version),
                ["telemetry.distro.name"] = OTLPModuleInfo.DistroName,
                ["telemetry.distro.version"] = OTLPAttributeConverter.NormalizeMissing(OTLPModuleInfo.Version)
            };

            var commit = OTLPModuleInfo.CommitHash;
            if (!string.IsNullOrEmpty(commit)) { attributes["telemetry.distro.commit"] = commit; }

            return attributes;
        }

        private static void MergeInto(IDictionary<string, object> target, IDictionary<string, object> source)
        {
            if (source == null) { return; }
            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) { continue; }
                target[pair.Key] = pair.Value;
            }
        }
    }
}
