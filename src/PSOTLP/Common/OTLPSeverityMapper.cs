namespace PSOTLP.Common
{
    /// <summary>
    /// Centralized severity mapping. No cmdlet may translate severities inline.
    /// </summary>
    public static class OTLPSeverityMapper
    {
        public static int ToNumber(OTLPSeverity severity)
        {
            switch (severity)
            {
                case OTLPSeverity.Trace: return 1;
                case OTLPSeverity.Debug: return 5;
                case OTLPSeverity.Information: return 9;
                case OTLPSeverity.Warning: return 13;
                case OTLPSeverity.Error: return 17;
                case OTLPSeverity.Fatal: return 21;
                default: return 9;
            }
        }

        public static string ToText(OTLPSeverity severity)
        {
            switch (severity)
            {
                case OTLPSeverity.Trace: return "TRACE";
                case OTLPSeverity.Debug: return "DEBUG";
                case OTLPSeverity.Information: return "INFO";
                case OTLPSeverity.Warning: return "WARN";
                case OTLPSeverity.Error: return "ERROR";
                case OTLPSeverity.Fatal: return "FATAL";
                default: return "INFO";
            }
        }

        /// <summary>
        /// Maps a PowerShell stream name into a severity. Used by transcript tailing and hosted
        /// runspace stream capture.
        /// </summary>
        public static OTLPSeverity FromStreamName(string streamName)
        {
            if (string.IsNullOrEmpty(streamName)) { return OTLPSeverity.Information; }
            switch (streamName.ToLowerInvariant())
            {
                case "output":
                case "success":
                case "information":
                case "progress":
                case "host":
                case "native_stdout":
                    return OTLPSeverity.Information;
                case "verbose":
                case "debug":
                    return OTLPSeverity.Debug;
                case "warning":
                    return OTLPSeverity.Warning;
                case "error":
                case "native_stderr":
                    return OTLPSeverity.Error;
                default:
                    return OTLPSeverity.Information;
            }
        }
    }
}
