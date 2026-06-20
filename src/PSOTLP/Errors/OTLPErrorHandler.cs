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

            var builder = new System.Text.StringBuilder();
            builder.Append("Operation failed: ").Append(details.Operation ?? "unspecified");
            if (!string.IsNullOrEmpty(details.Component)) { builder.Append(Environment.NewLine).Append("Error Component: ").Append(details.Component); }
            if (!string.IsNullOrEmpty(details.Message)) { builder.Append(Environment.NewLine).Append("Error Message: ").Append(details.Message); }
            if (!string.IsNullOrEmpty(details.ExceptionType)) { builder.Append(Environment.NewLine).Append("Exception Type: ").Append(details.ExceptionType); }
            if (!string.IsNullOrEmpty(details.InnerExceptionMessage)) { builder.Append(Environment.NewLine).Append("Inner Exception: ").Append(details.InnerExceptionMessage); }
            if (details.HttpStatusCode.HasValue) { builder.Append(Environment.NewLine).Append("HTTP Status Code: ").Append(details.HttpStatusCode.Value); }
            if (!string.IsNullOrEmpty(details.HttpReasonPhrase)) { builder.Append(Environment.NewLine).Append("HTTP Reason Phrase: ").Append(details.HttpReasonPhrase); }
            if (details.RetryAttempt.HasValue) { builder.Append(Environment.NewLine).Append("Retry Attempts: ").Append(details.RetryAttempt.Value); }
            if (!string.IsNullOrEmpty(details.EndpointName)) { builder.Append(Environment.NewLine).Append("Endpoint Name: ").Append(details.EndpointName); }
            if (details.EndpointUri != null) { builder.Append(Environment.NewLine).Append("Endpoint URI: ").Append(OTLPUriBuilder.Sanitize(details.EndpointUri)); }
            if (details.SignalType.HasValue) { builder.Append(Environment.NewLine).Append("Signal Type: ").Append(details.SignalType.Value); }
            if (details.Encoding.HasValue) { builder.Append(Environment.NewLine).Append("Encoding: ").Append(details.Encoding.Value); }
            if (details.Compression.HasValue) { builder.Append(Environment.NewLine).Append("Compression: ").Append(details.Compression.Value); }
            if (details.SerializationErrorPosition.HasValue) { builder.Append(Environment.NewLine).Append("Serialization Position: ").Append(details.SerializationErrorPosition.Value); }

            logger.Error(Component, builder.ToString(), exception);
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
