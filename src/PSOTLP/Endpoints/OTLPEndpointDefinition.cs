using PSOTLP.Common;

namespace PSOTLP.Endpoints
{
    public sealed class OTLPEndpointDefinition
    {
        public string Name { get; set; }
        public OTLPSignalType SignalType { get; set; }
        public string Method { get; set; }
        public string DefaultPath { get; set; }
        public string NDJsonPath { get; set; }
        public string DefaultContentType { get; set; }
        public bool SupportsCompression { get; set; }
        public bool RequiresAuthorization { get; set; }

        public string ResolvePath(OTLPEncoding encoding)
        {
            if (encoding == OTLPEncoding.NDJson && !string.IsNullOrEmpty(NDJsonPath)) { return NDJsonPath; }
            return DefaultPath;
        }
    }
}
