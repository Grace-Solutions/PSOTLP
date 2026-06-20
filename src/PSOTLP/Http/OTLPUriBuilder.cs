using System;
using PSOTLP.Common;
using PSOTLP.Endpoints;

namespace PSOTLP.Http
{
    /// <summary>
    /// Centralized URI composition. All callers must use this type to compose signal endpoints
    /// from a base endpoint plus an endpoint definition path, or a signal-specific override.
    /// </summary>
    public static class OTLPUriBuilder
    {
        public static Uri Build(Uri baseEndpoint, OTLPEndpointDefinition definition, Uri signalOverride = null, OTLPEncoding encoding = OTLPEncoding.Json)
        {
            if (definition == null) { throw new ArgumentNullException(nameof(definition)); }
            if (signalOverride != null) { return signalOverride; }
            if (baseEndpoint == null) { throw new ArgumentNullException(nameof(baseEndpoint)); }

            var builder = new UriBuilder(baseEndpoint);
            var basePath = builder.Path ?? string.Empty;
            var signalPath = definition.ResolvePath(encoding) ?? string.Empty;

            if (basePath.EndsWith("/", StringComparison.Ordinal) && signalPath.StartsWith("/", StringComparison.Ordinal))
            {
                builder.Path = basePath + signalPath.Substring(1);
            }
            else if (!basePath.EndsWith("/", StringComparison.Ordinal) && !signalPath.StartsWith("/", StringComparison.Ordinal))
            {
                builder.Path = basePath + "/" + signalPath;
            }
            else
            {
                builder.Path = basePath + signalPath;
            }

            return builder.Uri;
        }

        public static Uri BuildForSignal(Uri baseEndpoint, OTLPSignalType signalType, Uri signalOverride = null)
        {
            return Build(baseEndpoint, OTLPEndpointRegistry.GetForSignal(signalType), signalOverride);
        }

        public static Uri Sanitize(Uri uri)
        {
            if (uri == null) { return null; }
            var builder = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty, Query = string.Empty };
            return builder.Uri;
        }
    }
}
