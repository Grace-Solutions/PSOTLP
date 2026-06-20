using System;
using PSOTLP.Common;

namespace PSOTLP.Serialization
{
    /// <summary>
    /// Centralized factory for OTLP serializers. No exporter or cmdlet may instantiate a
    /// serializer directly; all callers resolve the implementation here so encoding selection
    /// stays consistent across log and trace pipelines.
    /// </summary>
    public static class OTLPSerializerFactory
    {
        public static IOTLPSerializer Create(OTLPEncoding encoding)
        {
            switch (encoding)
            {
                case OTLPEncoding.Json: return new OTLPJsonSerializer();
                case OTLPEncoding.Protobuf: return new OTLPProtobufSerializer();
                case OTLPEncoding.NDJson: return new OTLPNDJSONSerializer();
                default: throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unsupported OTLP encoding.");
            }
        }
    }
}
