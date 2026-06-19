using System;
using System.Management.Automation;
using PSOTLP.Errors;
using PSOTLP.Logging;

namespace PSOTLP.Cmdlets
{
    /// <summary>
    /// Base class for every PSOTLP cmdlet. Provides centralized logger creation and consistent
    /// error reporting so individual cmdlets never construct loggers or error records directly.
    /// </summary>
    public abstract class OTLPCmdletBase : PSCmdlet
    {
        private IOTLPLogger _logger;

        protected IOTLPLogger Logger
        {
            get
            {
                if (_logger == null) { _logger = new OTLPCmdletLogger(this); }
                return _logger;
            }
        }

        protected string CmdletComponent
        {
            get { return GetType().Name; }
        }

        protected void WriteVerboseLine(string message)
        {
            Logger.Info(CmdletComponent, message);
        }

        protected void WriteWarningLine(string message)
        {
            Logger.Warning(CmdletComponent, message);
        }

        protected void HandleException(string operation, Exception exception, object targetObject = null)
        {
            var details = OTLPErrorHandler.FromException(CmdletComponent, operation, exception);
            OTLPErrorHandler.Log(Logger, details, exception);
            var record = OTLPErrorHandler.BuildErrorRecord(details, exception, targetObject);
            ThrowTerminatingError(record);
        }

        protected void WriteNonTerminatingException(string operation, Exception exception, object targetObject = null)
        {
            var details = OTLPErrorHandler.FromException(CmdletComponent, operation, exception);
            OTLPErrorHandler.Log(Logger, details, exception);
            var record = OTLPErrorHandler.BuildErrorRecord(details, exception, targetObject);
            WriteError(record);
        }
    }
}
