using System;
using System.Management.Automation;

namespace PSOTLP.Logging
{
    /// <summary>
    /// Routes internal module log messages through the PowerShell cmdlet streams using the
    /// required mapping: Info -> Verbose, Warning -> Warning, Error -> Error. No internal log
    /// message is visible by default; callers must use -Verbose to opt in to Info output.
    /// </summary>
    public sealed class OTLPCmdletLogger : IOTLPLogger
    {
        private readonly Cmdlet _cmdlet;

        public OTLPCmdletLogger(Cmdlet cmdlet)
        {
            if (cmdlet == null) { throw new ArgumentNullException(nameof(cmdlet)); }
            _cmdlet = cmdlet;
        }

        public void Info(string component, string message)
        {
            _cmdlet.WriteVerbose(OTLPLogFormatter.Format("Info", component, message));
        }

        public void Warning(string component, string message)
        {
            _cmdlet.WriteWarning(OTLPLogFormatter.Format("Warning", component, message));
        }

        public void Error(string component, string message)
        {
            Error(component, message, null);
        }

        public void Error(string component, string message, Exception exception)
        {
            var formatted = OTLPLogFormatter.Format("Error", component, message);
            var ex = exception ?? new InvalidOperationException(formatted);
            var record = new ErrorRecord(ex, component ?? "PSOTLP", ErrorCategory.NotSpecified, null)
            {
                ErrorDetails = new ErrorDetails(formatted)
            };
            _cmdlet.WriteError(record);
        }
    }

    /// <summary>
    /// Logger used by background threads / non-cmdlet contexts. Captures messages into PowerShell
    /// data streams attached to the owning cmdlet so the cmdlet can drain them on its main thread.
    /// </summary>
    public sealed class OTLPDeferredLogger : IOTLPLogger
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<DeferredEntry> _entries =
            new System.Collections.Concurrent.ConcurrentQueue<DeferredEntry>();

        public void Info(string component, string message)
        {
            _entries.Enqueue(new DeferredEntry { Level = "Info", Component = component, Message = message });
        }

        public void Warning(string component, string message)
        {
            _entries.Enqueue(new DeferredEntry { Level = "Warning", Component = component, Message = message });
        }

        public void Error(string component, string message) { Error(component, message, null); }

        public void Error(string component, string message, Exception exception)
        {
            _entries.Enqueue(new DeferredEntry { Level = "Error", Component = component, Message = message, Exception = exception });
        }

        public void Drain(IOTLPLogger target)
        {
            if (target == null) { return; }
            DeferredEntry entry;
            while (_entries.TryDequeue(out entry))
            {
                if (entry.Level == "Error") { target.Error(entry.Component, entry.Message, entry.Exception); }
                else if (entry.Level == "Warning") { target.Warning(entry.Component, entry.Message); }
                else { target.Info(entry.Component, entry.Message); }
            }
        }

        private sealed class DeferredEntry
        {
            public string Level;
            public string Component;
            public string Message;
            public Exception Exception;
        }
    }
}
