using System.IO;
using System.IO.Compression;
using PSOTLP.Common;

namespace PSOTLP.Http
{
    /// <summary>
    /// Centralized gzip helper used by the HTTP client. No cmdlet may invoke gzip directly.
    /// </summary>
    public static class OTLPCompressionUtility
    {
        public const string ContentEncodingHeader = "Content-Encoding";
        public const string AcceptEncodingHeader = "Accept-Encoding";
        public const string GzipEncodingValue = "gzip";

        public static byte[] Compress(byte[] body, OTLPCompression compression)
        {
            if (body == null || body.Length == 0) { return body; }
            if (compression != OTLPCompression.Gzip) { return body; }

            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
                {
                    gzip.Write(body, 0, body.Length);
                }
                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] body)
        {
            if (body == null || body.Length == 0) { return body; }
            using (var input = new MemoryStream(body))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[8192];
                int read;
                while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                }
                return output.ToArray();
            }
        }
    }
}
