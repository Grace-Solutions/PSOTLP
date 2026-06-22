using System;
using System.Collections.Generic;
using PSOTLP.Common;

namespace PSOTLP.Resources
{
    /// <summary>
    /// Single source of truth for combining a default attribute baseline with a caller-supplied
    /// override dictionary under the configured <see cref="OTLPAttributeMergeMode"/>. All
    /// resource and log/metric/span attribute pipelines flow through <see cref="Apply"/> so the
    /// semantics of Merge/Replace/Skip are identical wherever attributes are layered.
    /// </summary>
    public static class OTLPAttributeMerger
    {
        public static IDictionary<string, object> Apply(
            IDictionary<string, object> defaults,
            IDictionary<string, object> overrides,
            OTLPAttributeMergeMode mode)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (mode == OTLPAttributeMergeMode.Replace)
            {
                CopyInto(result, overrides, false);
                return result;
            }

            CopyInto(result, defaults, false);
            CopyInto(result, overrides, mode == OTLPAttributeMergeMode.Skip);
            return result;
        }

        private static void CopyInto(IDictionary<string, object> target, IDictionary<string, object> source, bool keepExisting)
        {
            if (source == null) { return; }
            foreach (var pair in source)
            {
                if (string.IsNullOrWhiteSpace(pair.Key)) { continue; }
                if (keepExisting && target.ContainsKey(pair.Key)) { continue; }
                target[pair.Key] = pair.Value;
            }
        }
    }
}
