using PSOTLP.Errors;

namespace PSOTLP.Connections
{
    /// <summary>
    /// Process-wide holder for the active OTLP connection. Cmdlets must read and write the
    /// active connection only through this manager.
    /// </summary>
    public static class OTLPSessionManager
    {
        private static readonly object _lock = new object();
        private static OTLPConnection _current;

        public static OTLPConnection CurrentConnection
        {
            get { lock (_lock) { return _current; } }
        }

        public static void SetCurrentConnection(OTLPConnection connection)
        {
            lock (_lock) { _current = connection; }
        }

        public static OTLPConnection RequireCurrentConnection()
        {
            var current = CurrentConnection;
            if (current == null || !current.IsConnected)
            {
                throw new OTLPConnectionException(
                    "No active OTLP connection exists. Call Connect-OTLP before invoking telemetry cmdlets.");
            }
            return current;
        }

        public static void Disconnect()
        {
            lock (_lock)
            {
                if (_current != null && _current.Headers != null) { _current.Headers.Clear(); }
                _current = null;
            }
        }
    }
}
