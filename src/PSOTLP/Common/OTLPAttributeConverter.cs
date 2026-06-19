using System;
using System.Collections;
using System.Collections.Generic;
using PSOTLP.Models;

namespace PSOTLP.Common
{
    /// <summary>
    /// Centralized converter from arbitrary .NET / PowerShell values to OTLP AnyValue/KeyValue.
    /// </summary>
    public static class OTLPAttributeConverter
    {
        public const string MissingValue = "n/a";

        public static OTLPAnyValue ToAnyValue(object value)
        {
            if (value == null) { return new OTLPAnyValue { StringValue = MissingValue }; }
            if (value is OTLPAnyValue any) { return any; }
            if (value is string s) { return new OTLPAnyValue { StringValue = string.IsNullOrEmpty(s) ? MissingValue : s }; }
            if (value is bool b) { return new OTLPAnyValue { BoolValue = b }; }
            if (value is byte[] bytes) { return new OTLPAnyValue { BytesValue = bytes }; }
            if (value is double d) { return new OTLPAnyValue { DoubleValue = d }; }
            if (value is float f) { return new OTLPAnyValue { DoubleValue = f }; }
            if (value is decimal dec) { return new OTLPAnyValue { DoubleValue = (double)dec }; }
            if (value is long l) { return new OTLPAnyValue { IntValue = l }; }
            if (value is int i) { return new OTLPAnyValue { IntValue = i }; }
            if (value is short sh) { return new OTLPAnyValue { IntValue = sh }; }
            if (value is byte by) { return new OTLPAnyValue { IntValue = by }; }
            if (value is uint ui) { return new OTLPAnyValue { IntValue = ui }; }
            if (value is ulong ul) { return new OTLPAnyValue { IntValue = (long)ul }; }
            if (value is DateTime dt) { return new OTLPAnyValue { StringValue = dt.ToUniversalTime().ToString("o") }; }
            if (value is DateTimeOffset dto) { return new OTLPAnyValue { StringValue = dto.UtcDateTime.ToString("o") }; }
            if (value is Guid g) { return new OTLPAnyValue { StringValue = g.ToString() }; }
            if (value is Uri u) { return new OTLPAnyValue { StringValue = u.ToString() }; }

            if (value is IDictionary dict)
            {
                var kv = new List<OTLPKeyValue>();
                foreach (DictionaryEntry entry in dict)
                {
                    if (entry.Key == null) { continue; }
                    kv.Add(new OTLPKeyValue { Key = entry.Key.ToString(), Value = ToAnyValue(entry.Value) });
                }
                return new OTLPAnyValue { KvlistValue = kv };
            }

            if (value is IEnumerable en && !(value is string))
            {
                var arr = new List<OTLPAnyValue>();
                foreach (var item in en) { arr.Add(ToAnyValue(item)); }
                return new OTLPAnyValue { ArrayValue = arr };
            }

            var text = value.ToString();
            return new OTLPAnyValue { StringValue = string.IsNullOrEmpty(text) ? MissingValue : text };
        }

        public static IList<OTLPKeyValue> ToKeyValueList(IDictionary<string, object> attributes)
        {
            var result = new List<OTLPKeyValue>();
            if (attributes == null) { return result; }
            foreach (var pair in attributes)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) { continue; }
                result.Add(new OTLPKeyValue { Key = pair.Key, Value = ToAnyValue(pair.Value) });
            }
            return result;
        }

        public static string NormalizeMissing(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? MissingValue : value.Trim();
        }
    }
}
