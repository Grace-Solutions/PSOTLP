using System.Collections.Generic;

namespace PSOTLP.Models
{
    /// <summary>
    /// Internal OTLP AnyValue representation used by the JSON serializer. Only one of the value
    /// fields is populated per instance; nulls map to OTLP NullValue when emitted.
    /// </summary>
    public sealed class OTLPAnyValue
    {
        public string StringValue { get; set; }
        public bool? BoolValue { get; set; }
        public long? IntValue { get; set; }
        public double? DoubleValue { get; set; }
        public byte[] BytesValue { get; set; }
        public IList<OTLPAnyValue> ArrayValue { get; set; }
        public IList<OTLPKeyValue> KvlistValue { get; set; }

        public static OTLPAnyValue FromString(string value) { return new OTLPAnyValue { StringValue = value ?? string.Empty }; }
    }
}
