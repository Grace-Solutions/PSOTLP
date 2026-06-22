using System;
using System.Collections.Generic;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Models;

namespace PSOTLP.Resources
{
    /// <summary>
    /// Centralized resource attribute merger. Built-in system inventory and module identity
    /// always form the always-on baseline; connection-level resource attributes and any per-
    /// record overrides are combined under the requested <see cref="OTLPAttributeMergeMode"/>:
    ///   * Merge   - caller keys overlay the baseline; caller wins on collision.
    ///   * Replace - caller dictionary replaces the connection-level layer; system inventory
    ///               and module identity are always retained.
    ///   * Skip    - baseline wins on collision; caller fills missing keys only.
    /// </summary>
    public static class OTLPResourceBuilder
    {
        private static readonly OTLPSystemInformationProvider SystemInfo = new OTLPSystemInformationProvider();

        public static OTLPResource Build(OTLPConnection connection, IDictionary<string, object> recordOverrides)
        {
            return Build(connection, recordOverrides, ResolveMode(connection, null));
        }

        public static OTLPResource Build(OTLPConnection connection, IDictionary<string, object> recordOverrides, OTLPAttributeMergeMode mode)
        {
            var identity = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            MergeInto(identity, SystemInfo.GetHostAttributes());
            MergeInto(identity, SystemInfo.GetHardwareAttributes());
            MergeInto(identity, SystemInfo.GetNetworkAttributes());
            MergeInto(identity, GetModuleAttributes(connection));

            var connectionLayer = connection != null ? connection.ResourceAttributes : null;
            var userLayer = OTLPAttributeMerger.Apply(connectionLayer, recordOverrides, mode);

            var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            MergeInto(merged, identity);
            MergeInto(merged, userLayer);

            return new OTLPResource { Attributes = OTLPAttributeConverter.ToKeyValueList(merged) };
        }

        public static OTLPAttributeMergeMode ResolveMode(OTLPConnection connection, OTLPAttributeMergeMode? recordMode)
        {
            if (recordMode.HasValue) { return recordMode.Value; }
            return connection != null ? connection.AttributeMergeMode : OTLPAttributeMergeMode.Merge;
        }

        public static OTLPInstrumentationScope BuildScope(OTLPConnection connection)
        {
            var scope = new OTLPInstrumentationScope
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

            if (connection != null && connection.ScopeAttributes != null && connection.ScopeAttributes.Count > 0)
            {
                scope.Attributes = OTLPAttributeConverter.ToKeyValueList(connection.ScopeAttributes);
            }

            return scope;
        }

        private static IDictionary<string, object> GetModuleAttributes(OTLPConnection connection)
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["service.name"] = OTLPAttributeConverter.NormalizeMissing(connection != null ? connection.ServiceName : null)
            };

            if (connection != null)
            {
                if (!string.IsNullOrWhiteSpace(connection.ServiceNamespace))
                {
                    attributes["service.namespace"] = connection.ServiceNamespace.Trim();
                }
                if (!string.IsNullOrWhiteSpace(connection.ServiceInstanceId))
                {
                    attributes["service.instance.id"] = connection.ServiceInstanceId.Trim();
                }
                if (!string.IsNullOrWhiteSpace(connection.EnvironmentName))
                {
                    attributes["deployment.environment"] = connection.EnvironmentName.Trim();
                }
            }

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
