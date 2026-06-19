using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using PSOTLP.Common;

namespace PSOTLP.Resources
{
    /// <summary>
    /// Resolves host, OS, process, and (Windows only) WMI/CIM device attributes. All values
    /// fall back to "n/a" so the "default attributes always present" rule holds.
    /// </summary>
    public sealed class OTLPSystemInformationProvider
    {
        private static readonly object _lock = new object();
        private static IDictionary<string, object> _cachedHardware;

        public IDictionary<string, object> GetHostAttributes()
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            attributes["host.name"] = OTLPAttributeConverter.NormalizeMissing(TryGet(() => Dns.GetHostName()));
            attributes["user.name"] = OTLPAttributeConverter.NormalizeMissing(TryGet(() => Environment.UserName));
            attributes["os.platform"] = OTLPAttributeConverter.NormalizeMissing(GetOsPlatform());
            attributes["os.version"] = OTLPAttributeConverter.NormalizeMissing(TryGet(() => Environment.OSVersion.Version.ToString()));
            attributes["os.architecture"] = OTLPAttributeConverter.NormalizeMissing(NormalizeArchitecture(RuntimeInformation.OSArchitecture));
            attributes["process.architecture"] = OTLPAttributeConverter.NormalizeMissing(NormalizeArchitecture(RuntimeInformation.ProcessArchitecture));
            attributes["process.runtime.name"] = ".NET";
            attributes["process.runtime.version"] = OTLPAttributeConverter.NormalizeMissing(TryGet(() => RuntimeInformation.FrameworkDescription));
            return attributes;
        }

        public IDictionary<string, object> GetHardwareAttributes()
        {
            lock (_lock)
            {
                if (_cachedHardware != null) { return _cachedHardware; }
                var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["device.manufacturer"] = OTLPAttributeConverter.MissingValue,
                    ["device.model"] = OTLPAttributeConverter.MissingValue,
                    ["device.systemID"] = OTLPAttributeConverter.MissingValue,
                    ["device.uuid"] = OTLPAttributeConverter.MissingValue,
                    ["os.type"] = OTLPAttributeConverter.MissingValue,
                    ["os.caption"] = OTLPAttributeConverter.MissingValue
                };

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    OTLPWindowsInventory.Populate(attributes);
                }

                _cachedHardware = attributes;
                return attributes;
            }
        }

        private static string GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return "Windows"; }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return "Linux"; }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return "macOS"; }
            return null;
        }

        private static string NormalizeArchitecture(Architecture architecture)
        {
            switch (architecture)
            {
                case Architecture.X64: return "x64";
                case Architecture.X86: return "x86";
                case Architecture.Arm64: return "ARM64";
                case Architecture.Arm: return "ARM";
                default: return architecture.ToString();
            }
        }

        private static string TryGet(Func<string> getter)
        {
            try { return getter(); } catch { return null; }
        }
    }
}
