using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using PSOTLP.Common;
using PSOTLP.Models;
using PSOTLP.Redaction;

namespace PSOTLP.Sessions
{
    /// <summary>
    /// Attaches DataAdded handlers to every PSDataCollection on a hosted PowerShell instance and
    /// converts each emitted record into a redacted <see cref="OTLPLogRecord"/> on the session
    /// queue. Used by Invoke-OTLPScript.
    /// </summary>
    public sealed class OTLPStreamHook
    {
        private readonly OTLPSessionQueue _queue;
        private readonly OTLPRedactionEngine _redaction;
        private readonly OTLPSession _session;
        private readonly IDictionary<string, object> _extraAttributes;
        private readonly OTLPAttributeMergeMode? _attributeMergeMode;

        private EventHandler<DataAddedEventArgs> _errorHandler;
        private EventHandler<DataAddedEventArgs> _warningHandler;
        private EventHandler<DataAddedEventArgs> _verboseHandler;
        private EventHandler<DataAddedEventArgs> _debugHandler;
        private EventHandler<DataAddedEventArgs> _informationHandler;

        public OTLPStreamHook(OTLPSessionQueue queue, OTLPRedactionEngine redaction, OTLPSession session, IDictionary<string, object> extraAttributes)
            : this(queue, redaction, session, extraAttributes, null) { }

        public OTLPStreamHook(OTLPSessionQueue queue, OTLPRedactionEngine redaction, OTLPSession session, IDictionary<string, object> extraAttributes, OTLPAttributeMergeMode? attributeMergeMode)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _redaction = redaction;
            _session = session;
            _extraAttributes = extraAttributes;
            _attributeMergeMode = attributeMergeMode;
        }

        public void Attach(PowerShell ps)
        {
            _errorHandler = (s, e) => Capture("Error", ((PSDataCollection<ErrorRecord>)s)[e.Index]?.ToString());
            _warningHandler = (s, e) => Capture("Warning", ((PSDataCollection<WarningRecord>)s)[e.Index]?.Message);
            _verboseHandler = (s, e) => Capture("Verbose", ((PSDataCollection<VerboseRecord>)s)[e.Index]?.Message);
            _debugHandler = (s, e) => Capture("Debug", ((PSDataCollection<DebugRecord>)s)[e.Index]?.Message);
            _informationHandler = (s, e) => Capture("Information", ((PSDataCollection<InformationRecord>)s)[e.Index]?.ToString());

            ps.Streams.Error.DataAdded += _errorHandler;
            ps.Streams.Warning.DataAdded += _warningHandler;
            ps.Streams.Verbose.DataAdded += _verboseHandler;
            ps.Streams.Debug.DataAdded += _debugHandler;
            ps.Streams.Information.DataAdded += _informationHandler;
        }

        public void Detach(PowerShell ps)
        {
            try { ps.Streams.Error.DataAdded -= _errorHandler; } catch { }
            try { ps.Streams.Warning.DataAdded -= _warningHandler; } catch { }
            try { ps.Streams.Verbose.DataAdded -= _verboseHandler; } catch { }
            try { ps.Streams.Debug.DataAdded -= _debugHandler; } catch { }
            try { ps.Streams.Information.DataAdded -= _informationHandler; } catch { }
        }

        public void DrainOutput(Collection<PSObject> output)
        {
            if (output == null) { return; }
            foreach (var item in output)
            {
                if (item == null) { continue; }
                Capture("Output", item.ToString());
            }
        }

        public void HandleError(ErrorRecord error)
        {
            if (error == null) { return; }
            Capture("Error", error.ToString());
        }

        private void Capture(string streamName, string message)
        {
            if (string.IsNullOrEmpty(message)) { return; }
            var body = _redaction != null ? _redaction.Redact(message) : message;
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["powershell.stream"] = streamName,
                ["powershell.session.id"] = _session != null ? _session.SessionId.ToString() : "n/a",
                ["powershell.session.name"] = _session != null && _session.SessionName != null ? _session.SessionName : "n/a",
                ["powershell.capture.mode"] = "HostedScript"
            };
            if (_extraAttributes != null)
            {
                foreach (var pair in _extraAttributes) { attributes[pair.Key] = pair.Value; }
            }

            var record = new OTLPLogRecord
            {
                Body = body,
                Severity = OTLPSeverityMapper.FromStreamName(streamName),
                TimestampUtc = DateTimeOffset.UtcNow,
                ObservedTimestampUtc = DateTimeOffset.UtcNow,
                Attributes = attributes,
                AttributeMergeMode = _attributeMergeMode
            };
            if (_queue.Enqueue(record) && _session != null) { _session.RecordsCaptured++; }
        }
    }
}
