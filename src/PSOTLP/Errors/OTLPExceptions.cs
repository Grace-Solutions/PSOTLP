using System;

namespace PSOTLP.Errors
{
    public class OTLPException : Exception
    {
        public OTLPException(string message) : base(message) { }
        public OTLPException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPConfigurationException : OTLPException
    {
        public OTLPConfigurationException(string message) : base(message) { }
        public OTLPConfigurationException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPConnectionException : OTLPException
    {
        public OTLPConnectionException(string message) : base(message) { }
        public OTLPConnectionException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPHttpException : OTLPException
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public OTLPHttpException(string message) : base(message) { }
        public OTLPHttpException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPSerializationException : OTLPException
    {
        public OTLPSerializationException(string message) : base(message) { }
        public OTLPSerializationException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPExportException : OTLPException
    {
        public int AttemptCount { get; set; }
        public OTLPExportException(string message) : base(message) { }
        public OTLPExportException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPSessionException : OTLPException
    {
        public OTLPSessionException(string message) : base(message) { }
        public OTLPSessionException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPTranscriptException : OTLPException
    {
        public OTLPTranscriptException(string message) : base(message) { }
        public OTLPTranscriptException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPStreamCaptureException : OTLPException
    {
        public OTLPStreamCaptureException(string message) : base(message) { }
        public OTLPStreamCaptureException(string message, Exception inner) : base(message, inner) { }
    }

    public class OTLPRedactionException : OTLPException
    {
        public OTLPRedactionException(string message) : base(message) { }
        public OTLPRedactionException(string message, Exception inner) : base(message, inner) { }
    }
}
