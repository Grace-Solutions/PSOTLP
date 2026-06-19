using System;
using System.Collections.Generic;
using System.Linq;

namespace PSOTLP.Sessions
{
    /// <summary>
    /// Process-wide registry of active and completed OTLP capture sessions. Cmdlets must register
    /// new sessions here and look up sessions only through this registry.
    /// </summary>
    public static class OTLPSessionRegistry
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<Guid, OTLPSessionService> _active = new Dictionary<Guid, OTLPSessionService>();
        private static readonly List<OTLPSession> _completed = new List<OTLPSession>();

        public static void Register(OTLPSessionService service)
        {
            if (service == null) { return; }
            lock (_lock) { _active[service.Session.SessionId] = service; }
        }

        public static OTLPSessionService Get(Guid sessionId)
        {
            lock (_lock)
            {
                OTLPSessionService service;
                return _active.TryGetValue(sessionId, out service) ? service : null;
            }
        }

        public static OTLPSessionService GetSingleActive()
        {
            lock (_lock) { return _active.Values.Count == 1 ? _active.Values.First() : null; }
        }

        public static IList<OTLPSession> ListActive()
        {
            lock (_lock) { return _active.Values.Select(s => s.Session).ToList(); }
        }

        public static IList<OTLPSession> ListCompleted()
        {
            lock (_lock) { return _completed.ToList(); }
        }

        public static void Complete(Guid sessionId)
        {
            lock (_lock)
            {
                if (_active.TryGetValue(sessionId, out var service))
                {
                    _active.Remove(sessionId);
                    _completed.Add(service.Session);
                    if (_completed.Count > 100) { _completed.RemoveAt(0); }
                }
            }
        }
    }
}
