using System;
using System.Collections.Generic;
using System.Management;
using PSOTLP.Common;

namespace PSOTLP.Resources
{
    /// <summary>
    /// Windows-only WMI/CIM access used by the system information provider. This type may only
    /// be invoked from a Windows runtime context.
    /// </summary>
    internal static class OTLPWindowsInventory
    {
        public static void Populate(IDictionary<string, object> attributes)
        {
            QueryFirst("SELECT Manufacturer, Model FROM Win32_ComputerSystem", obj =>
            {
                attributes["device.manufacturer"] = OTLPAttributeConverter.NormalizeMissing(GetString(obj, "Manufacturer"));
                attributes["device.model"] = OTLPAttributeConverter.NormalizeMissing(GetString(obj, "Model"));
            });

            QueryFirst("SELECT Product FROM Win32_BaseBoard", obj =>
                attributes["device.systemID"] = OTLPAttributeConverter.NormalizeMissing(GetString(obj, "Product")));

            QueryFirst("SELECT UUID FROM Win32_ComputerSystemProduct", obj =>
                attributes["device.uuid"] = OTLPAttributeConverter.NormalizeMissing(GetString(obj, "UUID")));

            QueryFirst("SELECT Caption, OSArchitecture, ProductType, Version FROM Win32_OperatingSystem", obj =>
            {
                var caption = GetString(obj, "Caption");
                if (!string.IsNullOrEmpty(caption) && caption.StartsWith("Microsoft ", StringComparison.OrdinalIgnoreCase))
                {
                    caption = caption.Substring("Microsoft ".Length).Trim();
                }
                attributes["os.caption"] = OTLPAttributeConverter.NormalizeMissing(caption);
                attributes["os.architecture"] = OTLPAttributeConverter.NormalizeMissing(NormalizeArchitecture(GetString(obj, "OSArchitecture")));

                var productType = GetUInt(obj, "ProductType");
                attributes["os.type"] = productType == 1
                    ? "Workstation"
                    : (productType == 2 || productType == 3 ? "Server" : OTLPAttributeConverter.MissingValue);
            });
        }

        private static void QueryFirst(string query, Action<ManagementObject> apply)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(query))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject item in collection) { apply(item); item.Dispose(); break; }
                }
            }
            catch { /* leave attribute defaults */ }
        }

        private static string GetString(ManagementObject item, string property)
        {
            try { var value = item[property]; return value != null ? value.ToString() : null; }
            catch { return null; }
        }

        private static uint GetUInt(ManagementObject item, string property)
        {
            try { var value = item[property]; return value is uint u ? u : (uint.TryParse(value?.ToString(), out var p) ? p : 0); }
            catch { return 0; }
        }

        private static string NormalizeArchitecture(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) { return null; }
            if (raw.IndexOf("arm", StringComparison.OrdinalIgnoreCase) >= 0 && raw.IndexOf("64", StringComparison.OrdinalIgnoreCase) >= 0) { return "ARM64"; }
            if (raw.IndexOf("64", StringComparison.OrdinalIgnoreCase) >= 0) { return "x64"; }
            if (raw.IndexOf("32", StringComparison.OrdinalIgnoreCase) >= 0) { return "x86"; }
            return raw;
        }
    }
}
