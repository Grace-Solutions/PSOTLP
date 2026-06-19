using System;
using System.Security.Cryptography;
using System.Text;

namespace PSOTLP.Common
{
    /// <summary>
    /// Generates OTLP-compatible trace and span identifiers. Trace IDs are 16-byte (32 hex char)
    /// random values and span IDs are 8-byte (16 hex char) random values, per the OTLP/W3C
    /// trace-context specification.
    /// </summary>
    public static class OTLPIdGenerator
    {
        private const string HexChars = "0123456789abcdef";
        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
        private static readonly object _lock = new object();

        public static string NewTraceId()
        {
            return NewHex(16);
        }

        public static string NewSpanId()
        {
            return NewHex(8);
        }

        public static bool IsValidTraceId(string value)
        {
            return IsHex(value, 32);
        }

        public static bool IsValidSpanId(string value)
        {
            return IsHex(value, 16);
        }

        private static string NewHex(int byteCount)
        {
            var bytes = new byte[byteCount];
            lock (_lock) { Random.GetBytes(bytes); }
            var builder = new StringBuilder(byteCount * 2);
            for (var i = 0; i < bytes.Length; i++)
            {
                builder.Append(HexChars[(bytes[i] >> 4) & 0x0F]);
                builder.Append(HexChars[bytes[i] & 0x0F]);
            }
            return builder.ToString();
        }

        private static bool IsHex(string value, int expectedLength)
        {
            if (string.IsNullOrEmpty(value)) { return false; }
            if (value.Length != expectedLength) { return false; }
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                var isDigit = c >= '0' && c <= '9';
                var isLower = c >= 'a' && c <= 'f';
                var isUpper = c >= 'A' && c <= 'F';
                if (!isDigit && !isLower && !isUpper) { return false; }
            }
            return true;
        }
    }
}
