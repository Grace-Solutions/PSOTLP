using System;

namespace PSOTLP.Sessions
{
    public sealed class OTLPSession
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; }
        public string ServiceName { get; set; }
        public DateTimeOffset StartedAtUtc { get; set; }
        public DateTimeOffset? StoppedAtUtc { get; set; }
        public int RecordsCaptured { get; set; }
        public int RecordsExported { get; set; }
        public int RecordsDropped { get; set; }
        public bool IsActive { get; set; }
    }
}
