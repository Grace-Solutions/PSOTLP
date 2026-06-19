using System;
using System.Collections.Generic;
using System.Security;
using System.Text.RegularExpressions;
using PSOTLP.Common;

namespace PSOTLP.Connections
{
    public sealed class OTLPConnection
    {
        public Uri EndpointUri { get; set; }
        public Uri LogsEndpointUri { get; set; }
        public Uri TracesEndpointUri { get; set; }
        public Uri MetricsEndpointUri { get; set; }

        public OTLPTransport Transport { get; set; } = OTLPTransport.Http;
        public OTLPEncoding Encoding { get; set; } = OTLPEncoding.Json;
        public OTLPCompression Compression { get; set; } = OTLPCompression.None;
        public OTLPAuthenticationMode AuthenticationMode { get; set; } = OTLPAuthenticationMode.None;

        public string ServiceName { get; set; }
        public string ServiceNamespace { get; set; }
        public string ServiceInstanceId { get; set; }
        public string ScopeName { get; set; }
        public string ScopeVersion { get; set; }
        public string EnvironmentName { get; set; }

        public IDictionary<string, object> ResourceAttributes { get; set; }
        public IDictionary<string, object> LogAttributes { get; set; }

        public int RetryCount { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 30;

        public DateTimeOffset ConnectedAtUtc { get; set; }
        public bool IsConnected { get; set; }

        internal IDictionary<string, SecureString> Headers { get; set; }
        internal IList<Regex> RedactPatterns { get; set; }
    }
}
