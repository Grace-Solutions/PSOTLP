using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PSOTLP.Errors;
using PSOTLP.Models;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// OTLP/HTTP JSON serializer. Field names are emitted in lowerCamelCase. Null fields are
    /// dropped so OTLP backends accept the payload. Byte arrays are written as base64.
    /// </summary>
    public sealed class OTLPJsonSerializer : IOTLPSerializer
    {
        public string ContentType { get { return "application/json"; } }

        private readonly JsonSerializerSettings _settings;

        public OTLPJsonSerializer()
        {
            _settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            _settings.Converters.Add(new UnixNanoStringConverter());
        }

        public byte[] SerializeLogs(OTLPExportLogsServiceRequest request)
        {
            return SerializeInternal(request);
        }

        public byte[] SerializeTraces(OTLPExportTraceServiceRequest request)
        {
            return SerializeInternal(request);
        }

        public byte[] SerializeMetrics(OTLPExportMetricsServiceRequest request)
        {
            return SerializeInternal(request);
        }

        private byte[] SerializeInternal(object payload)
        {
            try
            {
                var json = JsonConvert.SerializeObject(payload, _settings);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                throw new OTLPSerializationException(
                    "OTLP JSON serialization failed: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// OTLP/JSON requires fixed64 fields (timeUnixNano, observedTimeUnixNano, etc.) to be
        /// serialized as decimal strings. This converter applies that rule to ulong values.
        /// </summary>
        private sealed class UnixNanoStringConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ulong) || objectType == typeof(ulong?);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null) { writer.WriteNull(); return; }
                writer.WriteValue(((ulong)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) { return null; }
                if (reader.Value is string s && ulong.TryParse(s, out var parsed)) { return parsed; }
                if (reader.Value is long l) { return (ulong)l; }
                return 0UL;
            }
        }
    }
}
