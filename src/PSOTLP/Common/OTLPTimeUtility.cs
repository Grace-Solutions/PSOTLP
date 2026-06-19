using System;

namespace PSOTLP.Common
{
    /// <summary>
    /// Time conversions used by the OTLP serializers. OTLP uses nanoseconds since Unix epoch as
    /// fixed64 values.
    /// </summary>
    public static class OTLPTimeUtility
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static ulong ToUnixNano(DateTimeOffset value)
        {
            var ticks = value.UtcDateTime.Ticks - UnixEpoch.Ticks;
            if (ticks < 0) { return 0UL; }
            return (ulong)ticks * 100UL;
        }

        public static DateTimeOffset FromUnixNano(ulong unixNano)
        {
            var ticks = (long)(unixNano / 100UL);
            return new DateTimeOffset(UnixEpoch.AddTicks(ticks), TimeSpan.Zero);
        }
    }
}
