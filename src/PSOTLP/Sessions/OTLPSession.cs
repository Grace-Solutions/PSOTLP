using System;
using System.IO;
using PSOTLP.Common;

namespace PSOTLP.Sessions
{
    public sealed class OTLPSession
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; }
        public string ServiceName { get; set; }
        public OTLPSessionCaptureMode CaptureMode { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset? StoppedAtUtc { get; set; }
        public FileInfo TranscriptPath { get; set; }
        public int RecordsCaptured { get; set; }
        public int RecordsExported { get; set; }
        public int RecordsDropped { get; set; }
        public bool IsActive { get; set; }
        internal string SuspendedTranscriptPath { get; set; }
        internal bool OwnsTranscriptFile { get; set; }
    }
}
