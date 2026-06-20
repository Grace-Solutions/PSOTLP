using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using PSOTLP.Common;

namespace PSOTLP.Resources
{
    /// <summary>
    /// Resolves host, OS, process, network, and (Windows only) WMI/CIM device attributes.
    /// Host/OS/process values fall back to "n/a" so the "default attributes always present"
    /// rule holds. Network attributes are emitted per active non-loopback adapter as
    /// host.network.{i}.interface, host.network.{i}.mac, host.network.{i}.ipv4, and
    /// host.network.{i}.ipv6, with the default-route adapter (if any) placed at index 0.
    /// </summary>
    public sealed class OTLPSystemInformationProvider
    {
        private static readonly object _lock = new object();
        private static IDictionary<string, object> _cachedHardware;
        private static IDictionary<string, object> _cachedNetwork;

        public IDictionary<string, object> GetHostAttributes()
        {
            var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            attributes["host.name"] = OTLPAttributeConverter.NormalizeMissing(TryGet(() => Dns.GetHostName()));
            attributes["os.platform"] = OTLPAttributeConverter.NormalizeMissing(GetOsPlatform());
            attributes["os.version"] = OTLPAttributeConverter.NormalizeMissing(TryGet(() => Environment.OSVersion.Version.ToString()));
            attributes["os.architecture"] = OTLPAttributeConverter.NormalizeMissing(NormalizeArchitecture(RuntimeInformation.OSArchitecture));
            attributes["process.architecture"] = OTLPAttributeConverter.NormalizeMissing(NormalizeArchitecture(RuntimeInformation.ProcessArchitecture));

            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    attributes["process.id"] = proc.Id;
                    attributes["process.name"] = OTLPAttributeConverter.NormalizeMissing(proc.ProcessName);
                }
            }
            catch
            {
                attributes["process.id"] = OTLPAttributeConverter.MissingValue;
                attributes["process.name"] = OTLPAttributeConverter.MissingValue;
            }
            attributes["process.user"] = OTLPAttributeConverter.NormalizeMissing(TryGet(GetProcessUser));
            return attributes;
        }

        public IDictionary<string, object> GetNetworkAttributes()
        {
            lock (_lock)
            {
                if (_cachedNetwork != null) { return _cachedNetwork; }
                var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    var ordered = new List<NetworkInterface>();
                    NetworkInterface defaultRoute = null;
                    foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        if (nic == null) { continue; }
                        if (nic.OperationalStatus != OperationalStatus.Up) { continue; }
                        if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) { continue; }
                        IPInterfaceProperties props;
                        try { props = nic.GetIPProperties(); } catch { continue; }
                        if (props == null) { continue; }
                        if (!HasRoutableUnicast(props)) { continue; }
                        if (defaultRoute == null && HasDefaultGateway(props)) { defaultRoute = nic; }
                        ordered.Add(nic);
                    }
                    if (defaultRoute != null && ordered.Count > 1 && !ReferenceEquals(ordered[0], defaultRoute))
                    {
                        ordered.Remove(defaultRoute);
                        ordered.Insert(0, defaultRoute);
                    }

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        var nic = ordered[i];
                        var prefix = "host.network." + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".";
                        attributes[prefix + "interface"] = nic.Name;

                        var macText = FormatMac(nic);
                        if (!string.IsNullOrEmpty(macText)) { attributes[prefix + "mac"] = macText; }

                        var ipv4 = new List<string>();
                        var ipv6 = new List<string>();
                        IPInterfaceProperties ipProps;
                        try { ipProps = nic.GetIPProperties(); } catch { ipProps = null; }
                        if (ipProps != null && ipProps.UnicastAddresses != null)
                        {
                            foreach (var addr in ipProps.UnicastAddresses)
                            {
                                if (addr == null || addr.Address == null) { continue; }
                                if (IPAddress.IsLoopback(addr.Address)) { continue; }
                                if (addr.Address.AddressFamily == AddressFamily.InterNetwork) { ipv4.Add(addr.Address.ToString()); }
                                else if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6) { ipv6.Add(addr.Address.ToString()); }
                            }
                        }
                        if (ipv4.Count > 0) { attributes[prefix + "ipv4"] = ipv4.ToArray(); }
                        if (ipv6.Count > 0) { attributes[prefix + "ipv6"] = ipv6.ToArray(); }
                    }
                }
                catch { }

                _cachedNetwork = attributes;
                return attributes;
            }
        }

        private static bool HasDefaultGateway(IPInterfaceProperties props)
        {
            if (props == null || props.GatewayAddresses == null) { return false; }
            foreach (var gw in props.GatewayAddresses)
            {
                if (gw == null || gw.Address == null) { continue; }
                if (IPAddress.IsLoopback(gw.Address)) { continue; }
                if (gw.Address.Equals(IPAddress.Any) || gw.Address.Equals(IPAddress.IPv6Any)) { continue; }
                return true;
            }
            return false;
        }

        private static bool HasRoutableUnicast(IPInterfaceProperties props)
        {
            if (props == null || props.UnicastAddresses == null) { return false; }
            foreach (var addr in props.UnicastAddresses)
            {
                if (addr == null || addr.Address == null) { continue; }
                if (IPAddress.IsLoopback(addr.Address)) { continue; }
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork
                    || addr.Address.AddressFamily == AddressFamily.InterNetworkV6) { return true; }
            }
            return false;
        }

        private static string FormatMac(NetworkInterface nic)
        {
            try
            {
                var mac = nic.GetPhysicalAddress();
                if (mac == null) { return null; }
                var bytes = mac.GetAddressBytes();
                if (bytes == null || bytes.Length == 0) { return null; }
                var parts = new string[bytes.Length];
                for (int i = 0; i < bytes.Length; i++) { parts[i] = bytes[i].ToString("X2"); }
                return string.Join(":", parts);
            }
            catch { return null; }
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

        private static string GetProcessUser()
        {
            var user = Environment.UserName;
            if (string.IsNullOrEmpty(user)) { return null; }
            var domain = Environment.UserDomainName;
            if (!string.IsNullOrEmpty(domain) && !string.Equals(domain, user, StringComparison.OrdinalIgnoreCase))
            {
                return domain + "\\" + user;
            }
            return user;
        }
    }
}
