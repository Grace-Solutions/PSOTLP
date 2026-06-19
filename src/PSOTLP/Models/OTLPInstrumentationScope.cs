using System.Collections.Generic;

namespace PSOTLP.Models
{
    public sealed class OTLPInstrumentationScope
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public int DroppedAttributesCount { get; set; }
    }
}
