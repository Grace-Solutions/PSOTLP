using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using PSOTLP.Common;

namespace PSOTLP.Authentication
{
    /// <summary>
    /// Centralized utilities for the single header collection used by the module. Every header
    /// value is stored as SecureString. Plaintext input is converted at the boundary and the
    /// dictionary uses an OrdinalIgnoreCase comparer so case-insensitive lookups are reliable.
    /// </summary>
    public static class OTLPHeaderUtility
    {
        public static IDictionary<string, SecureString> CreateEmpty()
        {
            return new Dictionary<string, SecureString>(StringComparer.OrdinalIgnoreCase);
        }

        public static IDictionary<string, SecureString> FromHashtable(IDictionary source)
        {
            var result = CreateEmpty();
            if (source == null) { return result; }

            foreach (DictionaryEntry entry in source)
            {
                if (entry.Key == null) { continue; }
                var name = entry.Key.ToString();
                if (string.IsNullOrWhiteSpace(name)) { continue; }
                result[name] = CoerceValue(entry.Value);
            }
            return result;
        }

        public static SecureString CoerceValue(object value)
        {
            if (value == null) { return null; }
            if (value is SecureString s) { return s; }
            return OTLPSecureStringUtility.ToSecureString(value.ToString());
        }

        /// <summary>
        /// Materializes plaintext header values for a single HTTP request. The caller must clear
        /// references to the returned dictionary as quickly as practical.
        /// </summary>
        public static IDictionary<string, string> MaterializeForRequest(IDictionary<string, SecureString> headers)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) { return result; }

            foreach (var pair in headers)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) { continue; }
                result[pair.Key] = pair.Value == null ? string.Empty : OTLPSecureStringUtility.ToPlainText(pair.Value);
            }
            return result;
        }

        /// <summary>
        /// Returns the sanitized header names only. Values are never returned for logging.
        /// </summary>
        public static IEnumerable<string> SafeHeaderNames(IDictionary<string, SecureString> headers)
        {
            if (headers == null) { return Enumerable.Empty<string>(); }
            return headers.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
        }
    }
}
