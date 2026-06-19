using System.Collections.Generic;

namespace PSOTLP.Models
{
    public sealed class OTLPResource
    {
        public IList<OTLPKeyValue> Attributes { get; set; } = new List<OTLPKeyValue>();
        public int DroppedAttributesCount { get; set; }
    }
}
