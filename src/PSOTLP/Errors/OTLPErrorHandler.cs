using System;
using System.Management.Automation;
using PSOTLP.Http;
using PSOTLP.Logging;

namespace PSOTLP.Errors
{
    /// <summary>
    /// Central error handler. All cmdlets route exceptions through this type so error lines and
    /// ErrorRecord shapes stay consistent across the module.
    /// </summary>
    public static class OTLPErrorHandler
    {
        public const string Component = "OTLPErrorHandler";

        public static void Log(IOTLPLogger logger, OTLPErrorDetails details, Exception exception)
        {
            if (logger == null || details == null) { return; }

            logger.Error(Component, "Operation failed: " + (details.Operation ?? "unspecified"));
            if (!string.IsNullOrEmpty(details.Component)) { logger.Error(Component, "Error Component: " + details.Component); }
            if (!string.IsNullOrEmpty(details.Message)) { logger.Error(Component, "Error Message: " + details.Message); }
            if (!string.IsNullOrEmpty(details.ExceptionType)) { logger.Error(Component, "Exception Type: " + details.ExceptionType); }
            if (!string.IsNullOrEmpty(details.InnerExceptionMessage)) { logger.Error(Component, "Inner Exception: " + details.InnerExceptionMessage); }
            if (details.HttpStatusCode.HasValue) { logger.Error(Component, "HTTP Status Code: " + details.HttpStatusCode.Value); }
            if (!string.IsNullOrEmpty(details.HttpReasonPhrase)) { logger.Error(Component, "HTTP Reason Phrase: " + details.HttpReasonPhrase); }
            if (details.RetryAttempt.HasValue) { logger.Error(Component, "Retry Attempts: " + details.RetryAttempt.Value); }
            if (!string.IsNullOrEmpty(details.EndpointName)) { logger.Error(Component, "Endpoint Name: " + details.EndpointName); }
            if (details.EndpointUri != null) { logger.Error(Component, "Endpoint URI: " + OTLPUriBuilder.Sanitize(details.EndpointUri)); }
            if (details.SignalType.HasValue) { logger.Error(Component, "Signal Type: " + details.SignalType.Value); }
            if (details.Encoding.HasValue) { logger.Error(Component, "Encoding: " + details.Encoding.Value); }
            if (details.Compression.HasValue) { logger.Error(Component, "Compression: " + details.Compression.Value); }
            if (details.SerializationErrorPosition.HasValue) { logger.Error(Component, "Serialization Position: " + details.SerializationErrorPosition.Value); }
        }

        public static ErrorRecord BuildErrorRecord(OTLPErrorDetails details, Exception exception, object targetObject = null)
        {
            if (details == null) { details = new OTLPErrorDetails(); }
            var ex = exception ?? new OTLPException(details.Message ?? "An OTLP operation failed.");
            var errorId = "PSOTLP." + (details.Component ?? "Module") + "." + (details.Operation ?? "Failure");
            var category = ResolveCategory(ex);
            var record = new ErrorRecord(ex, errorId, category, targetObject);

            if (!string.IsNullOrEmpty(details.Message))
            {
                record.ErrorDetails = new ErrorDetails(details.Message);
                if (details.HttpStatusCode.HasValue)
                {
                    record.ErrorDetails.RecommendedAction =
                        "HTTP status " + details.HttpStatusCode.Value +
                        (string.IsNullOrEmpty(details.HttpReasonPhrase) ? string.Empty : " (" + details.HttpReasonPhrase + ")");
                }
            }
            return record;
        }

        public static OTLPErrorDetails FromException(string component, string operation, Exception exception)
        {
            var details = new OTLPErrorDetails
            {
                Component = component,
                Operation = operation,
                Message = exception != null ? exception.Message : "Unknown error.",
                ExceptionType = exception != null ? exception.GetType().FullName : null,
                InnerExceptionMessage = exception != null && exception.InnerException != null ? exception.InnerException.Message : null
            };

            var http = exception as OTLPHttpException;
            if (http != null)
            {
                details.HttpStatusCode = http.StatusCode;
                details.HttpReasonPhrase = http.ReasonPhrase;
            }
            return details;
        }

        private static ErrorCategory ResolveCategory(Exception ex)
        {
            if (ex is OTLPConnectionException) { return ErrorCategory.ConnectionError; }
            if (ex is OTLPConfigurationException) { return ErrorCategory.InvalidArgument; }
            if (ex is OTLPHttpException) { return ErrorCategory.ConnectionError; }
            if (ex is OTLPSerializationException) { return ErrorCategory.InvalidData; }
            if (ex is OTLPSessionException) { return ErrorCategory.InvalidOperation; }
            if (ex is OTLPTranscriptException) { return ErrorCategory.ResourceUnavailable; }
            if (ex is OTLPStreamCaptureException) { return ErrorCategory.OperationStopped; }
            if (ex is OTLPRedactionException) { return ErrorCategory.InvalidData; }
            if (ex is OTLPExportException) { return ErrorCategory.WriteError; }
            return ErrorCategory.NotSpecified;
        }
    }
}
