using System;
using System.Reflection;

namespace PSOTLP.Common
{
    /// <summary>
    /// Centralized accessor for module identity and assembly version metadata.
    /// </summary>
    public static class OTLPModuleInfo
    {
        public const string ModuleName = "PSOTLP";
        public const string TelemetrySdkName = "PSOTLP";
        public const string TelemetrySdkLanguage = "dotnet";
        public const string DistroName = "PSOTLP";

        private static string _cachedVersion;
        private static string _cachedInformationalVersion;
        private static string _cachedCommitHash;

        public static string Version
        {
            get
            {
                if (_cachedVersion != null) { return _cachedVersion; }
                var asm = typeof(OTLPModuleInfo).Assembly;
                _cachedVersion = asm.GetName().Version != null ? asm.GetName().Version.ToString() : "0.0.0.0";
                return _cachedVersion;
            }
        }

        public static string InformationalVersion
        {
            get
            {
                if (_cachedInformationalVersion != null) { return _cachedInformationalVersion; }
                var attr = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(
                    typeof(OTLPModuleInfo).Assembly, typeof(AssemblyInformationalVersionAttribute));
                _cachedInformationalVersion = attr != null ? attr.InformationalVersion : Version;
                return _cachedInformationalVersion;
            }
        }

        public static string CommitHash
        {
            get
            {
                if (_cachedCommitHash != null) { return _cachedCommitHash; }
                var asm = typeof(OTLPModuleInfo).Assembly;
                foreach (var attribute in (AssemblyMetadataAttribute[])
                    Attribute.GetCustomAttributes(asm, typeof(AssemblyMetadataAttribute)))
                {
                    if (string.Equals(attribute.Key, "CommitHash", StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedCommitHash = attribute.Value ?? string.Empty;
                        return _cachedCommitHash;
                    }
                }

                var info = InformationalVersion;
                var plus = info != null ? info.IndexOf('+') : -1;
                _cachedCommitHash = plus >= 0 ? info.Substring(plus + 1) : string.Empty;
                return _cachedCommitHash;
            }
        }
    }
}
