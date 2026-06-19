using System;
using System.IO;

namespace PSOTLP.Common
{
    /// <summary>
    /// Centralized FileInfo/DirectoryInfo helpers. No raw string concatenation for paths.
    /// </summary>
    public static class OTLPPathUtility
    {
        public static FileInfo File(params string[] parts)
        {
            if (parts == null || parts.Length == 0) { throw new ArgumentException("At least one path segment is required.", nameof(parts)); }
            return new FileInfo(Path.Combine(parts));
        }

        public static DirectoryInfo Directory(params string[] parts)
        {
            if (parts == null || parts.Length == 0) { throw new ArgumentException("At least one path segment is required.", nameof(parts)); }
            return new DirectoryInfo(Path.Combine(parts));
        }

        public static DirectoryInfo EnsureDirectory(DirectoryInfo directory)
        {
            if (directory == null) { throw new ArgumentNullException(nameof(directory)); }
            if (!directory.Exists) { directory.Create(); }
            return directory;
        }

        public static string GetShortName(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { return null; }
            try { return Path.GetFileNameWithoutExtension(path); }
            catch { return null; }
        }
    }
}
