using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text.RegularExpressions;
using PSOTLP.Authentication;
using PSOTLP.Common;
using PSOTLP.Connections;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsCommunications.Connect, "OTLP", SupportsShouldProcess = false)]
    [OutputType(typeof(OTLPConnectionView))]
    public sealed class ConnectOTLPCommand : OTLPCmdletBase
    {
        [Parameter] public Uri EndpointUri { get; set; }

        [Parameter] public Uri LogsEndpointUri { get; set; }
        [Parameter] public Uri TracesEndpointUri { get; set; }
        [Parameter] public Uri MetricsEndpointUri { get; set; }

        [Parameter] public IDictionary Header { get; set; }

        [Parameter] public OTLPTransport Transport { get; set; } = OTLPTransport.Http;
        [Parameter] public OTLPEncoding Encoding { get; set; } = OTLPEncoding.Json;
        [Parameter] public OTLPCompression Compression { get; set; } = OTLPCompression.None;

        [Parameter] public string ServiceName { get; set; }
        [Parameter] public string ServiceNamespace { get; set; }
        [Parameter] public string ServiceInstanceId { get; set; }
        [Parameter] public string ScopeName { get; set; }
        [Parameter] public string ScopeVersion { get; set; }
        [Parameter] public string EnvironmentName { get; set; }

        [Parameter] public IDictionary ResourceAttribute { get; set; }
        [Parameter] public IDictionary LogAttribute { get; set; }

        [Parameter] public Regex[] RedactPattern { get; set; }

        [Parameter] [ValidateRange(0, 10)] public int RetryCount { get; set; } = 3;
        [Parameter] [ValidateRange(1, 600)] public int TimeoutSeconds { get; set; } = 30;

        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var endpointUri = ResolveEndpointUri();
                var connection = new OTLPConnection
                {
                    EndpointUri = endpointUri,
                    LogsEndpointUri = LogsEndpointUri,
                    TracesEndpointUri = TracesEndpointUri,
                    MetricsEndpointUri = MetricsEndpointUri,
                    Transport = Transport,
                    Encoding = Encoding,
                    Compression = Compression,
                    ServiceName = OTLPAttributeConverter.NormalizeMissing(ResolveServiceName()),
                    ServiceNamespace = ResolveServiceNamespace(),
                    ServiceInstanceId = ResolveServiceInstanceId(),
                    ScopeName = ScopeName,
                    ScopeVersion = ScopeVersion,
                    EnvironmentName = EnvironmentName,
                    ResourceAttributes = HashtableToDictionary(ResourceAttribute),
                    LogAttributes = HashtableToDictionary(LogAttribute),
                    RedactPatterns = RedactPattern,
                    RetryCount = RetryCount,
                    TimeoutSeconds = TimeoutSeconds,
                    ConnectedAtUtc = DateTimeOffset.UtcNow,
                    IsConnected = true,
                    Headers = OTLPHeaderUtility.CreateEmpty(),
                    AuthenticationMode = ResolveAuthenticationMode()
                };

                ApplyAuthentication(connection);
                OTLPSessionManager.SetCurrentConnection(connection);

                WriteVerboseLine("OTLP connection established. Please Wait...");
                WriteVerboseLine("OTLP connection registered (endpoint=" + endpointUri + ", service=" + connection.ServiceName + ").");

                if (PassThru.IsPresent)
                {
                    WriteObject(OTLPConnectionView.From(connection));
                }
            }
            catch (Exception ex)
            {
                HandleException("Connect", ex);
            }
        }

        private Uri ResolveEndpointUri()
        {
            if (EndpointUri != null) { return EndpointUri; }
            var resolved = OTLPEnvironmentResolver.ResolveValue("PSOTLP_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_ENDPOINT");
            if (!string.IsNullOrWhiteSpace(resolved) && Uri.TryCreate(resolved, UriKind.Absolute, out var parsed)) { return parsed; }
            return new Uri("http://localhost:4318");
        }

        private string ResolveServiceName()
        {
            if (!string.IsNullOrWhiteSpace(ServiceName)) { return ServiceName; }
            var resolved = OTLPEnvironmentResolver.ResolveValue("PSOTLP_SERVICE_NAME", "OTEL_SERVICE_NAME");
            return !string.IsNullOrWhiteSpace(resolved) ? resolved : "powershell";
        }

        private string ResolveServiceNamespace()
        {
            if (!string.IsNullOrWhiteSpace(ServiceNamespace)) { return ServiceNamespace.Trim(); }
            return "PSOTLP";
        }

        private string ResolveServiceInstanceId()
        {
            if (!string.IsNullOrWhiteSpace(ServiceInstanceId)) { return ServiceInstanceId.Trim(); }
            return Guid.NewGuid().ToString();
        }

        private OTLPAuthenticationMode ResolveAuthenticationMode()
        {
            if (Header == null || Header.Count == 0) { return OTLPAuthenticationMode.None; }
            return OTLPAuthenticationMode.CustomHeader;
        }

        private void ApplyAuthentication(OTLPConnection connection)
        {
            if (Header == null) { return; }
            foreach (var pair in OTLPHeaderUtility.FromHashtable(Header)) { connection.Headers[pair.Key] = pair.Value; }
        }

        private static IDictionary<string, object> HashtableToDictionary(IDictionary source)
        {
            if (source == null) { return null; }
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in source) { if (entry.Key != null) { result[entry.Key.ToString()] = entry.Value; } }
            return result;
        }
    }
}
