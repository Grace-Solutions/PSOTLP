using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PSOTLP.Redaction
{
    /// <summary>
    /// Centralized redaction for log lines, transcript content, and string-like attribute values.
    /// Default patterns cover Authorization headers, bearer tokens, API keys, secret-like
    /// key/value pairs, connection strings, and private key blocks.
    /// </summary>
    public sealed class OTLPRedactionEngine
    {
        public const string Replacement = "[REDACTED]";

        private static readonly Regex[] DefaultPatterns = new[]
        {
            new Regex(@"(?i)(authorization\s*[:=]\s*)(bearer\s+)?[A-Za-z0-9\-_.~+/=]+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"(?i)\bbearer\s+[A-Za-z0-9\-_.~+/=]+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"(?i)(api[-_ ]?key\s*[:=]\s*)[^\s,;""']+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"(?i)(x-api-key\s*[:=]\s*)[^\s,;""']+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"(?i)((?:password|passwd|pwd|secret|client[-_ ]?secret|token)\s*[:=]\s*)[^\s,;""']+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"-----BEGIN (?:RSA |EC |DSA |OPENSSH |PGP |)PRIVATE KEY-----[\s\S]*?-----END (?:RSA |EC |DSA |OPENSSH |PGP |)PRIVATE KEY-----",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"(?i)(connection[-_ ]?string\s*[:=]\s*).+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant),
            new Regex(@"(?i)((?:Server|Host|Data Source)\s*=\s*[^;]+;\s*(?:Password|Pwd)\s*=\s*)[^;]+",
                RegexOptions.Compiled | RegexOptions.CultureInvariant)
        };

        private readonly List<Regex> _patterns;

        public OTLPRedactionEngine() : this((IEnumerable<string>)null) { }

        public OTLPRedactionEngine(IEnumerable<string> additionalPatterns)
        {
            _patterns = new List<Regex>(DefaultPatterns);
            if (additionalPatterns == null) { return; }
            foreach (var pattern in additionalPatterns)
            {
                if (string.IsNullOrWhiteSpace(pattern)) { continue; }
                _patterns.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant));
            }
        }

        public bool Enabled { get; set; } = true;

        public string Redact(string input)
        {
            if (!Enabled || string.IsNullOrEmpty(input)) { return input; }
            var output = input;
            for (int i = 0; i < _patterns.Count; i++)
            {
                output = _patterns[i].Replace(output, m =>
                {
                    var prefix = m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1].Value : string.Empty;
                    return prefix + Replacement;
                });
            }
            return output;
        }
    }
}
