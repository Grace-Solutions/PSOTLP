using System;

namespace PSOTLP.Logging
{
    /// <summary>
    /// Centralized log message formatter. All internal log lines flow through this type so the
    /// shape "[UTC Timestamp] - [Level] - [Component] - Message" is enforced in one place.
    /// </summary>
    public static class OTLPLogFormatter
    {
        public static string Format(string level, string component, string message)
        {
            var timestamp = DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            return "[" + timestamp + "] - [" + (level ?? "Info") + "] - [" + (component ?? "PSOTLP") + "] - " + (message ?? string.Empty);
        }
    }
}
