using System;
using System.Collections.Generic;
using PSOTLP.Common;
using PSOTLP.Connections;

namespace PSOTLP.Connections
{
    /// <summary>
    /// Sanitized projection of an <see cref="OTLPConnection"/> returned by Get-OTLPConnection
    /// and Connect-OTLP -PassThru. Header values, tokens, and API keys are never included; only
    /// the configured header names are exposed for diagnostics.
    /// </summary>
    public sealed class OTLPConnectionView
    {
        public Uri EndpointUri { get; set; }
        public Uri LogsEndpointUri { get; set; }
        public Uri TracesEndpointUri { get; set; }
        public Uri MetricsEndpointUri { get; set; }
        public bool NoSignalPath { get; set; }

        public OTLPTransport Transport { get; set; }
        public OTLPEncoding Encoding { get; set; }
        public OTLPCompression Compression { get; set; }
        public OTLPAuthenticationMode AuthenticationMode { get; set; }

        public string ServiceName { get; set; }
        public string ServiceNamespace { get; set; }
        public string ServiceInstanceId { get; set; }
        public string ScopeName { get; set; }
        public string ScopeVersion { get; set; }
        public string EnvironmentName { get; set; }

        public IDictionary<string, object> ResourceAttributes { get; set; }
        public IDictionary<string, object> LogAttributes { get; set; }

        public int RetryCount { get; set; }
        public int TimeoutSeconds { get; set; }

        public DateTimeOffset ConnectedAtUtc { get; set; }
        public bool IsConnected { get; set; }

        public IList<string> HeaderNames { get; set; } = new List<string>();

        public static OTLPConnectionView From(OTLPConnection connection)
        {
            if (connection == null) { return null; }
            var view = new OTLPConnectionView
            {
                EndpointUri = connection.EndpointUri,
                LogsEndpointUri = connection.LogsEndpointUri,
                TracesEndpointUri = connection.TracesEndpointUri,
                MetricsEndpointUri = connection.MetricsEndpointUri,
                NoSignalPath = connection.NoSignalPath,
                Transport = connection.Transport,
                Encoding = connection.Encoding,
                Compression = connection.Compression,
                AuthenticationMode = connection.AuthenticationMode,
                ServiceName = connection.ServiceName,
                ServiceNamespace = connection.ServiceNamespace,
                ServiceInstanceId = connection.ServiceInstanceId,
                ScopeName = connection.ScopeName,
                ScopeVersion = connection.ScopeVersion,
                EnvironmentName = connection.EnvironmentName,
                ResourceAttributes = connection.ResourceAttributes,
                LogAttributes = connection.LogAttributes,
                RetryCount = connection.RetryCount,
                TimeoutSeconds = connection.TimeoutSeconds,
                ConnectedAtUtc = connection.ConnectedAtUtc,
                IsConnected = connection.IsConnected
            };

            if (connection.Headers != null)
            {
                foreach (var name in connection.Headers.Keys) { view.HeaderNames.Add(name); }
            }
            return view;
        }
    }
}
