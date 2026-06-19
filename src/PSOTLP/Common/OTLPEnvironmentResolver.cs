using System;

namespace PSOTLP.Common
{
    /// <summary>
    /// Resolves configuration values from environment variables using
    /// Process -> User -> Machine precedence. PSOTLP-prefixed names are preferred over the
    /// equivalent OTEL_-prefixed names when both are provided to a single resolve call.
    /// User/Machine targets are skipped safely on non-Windows platforms.
    /// </summary>
    public static class OTLPEnvironmentResolver
    {
        public sealed class ResolvedValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Source { get; set; }
        }

        public static ResolvedValue Resolve(params string[] candidateNames)
        {
            if (candidateNames == null) { return null; }
            foreach (var name in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(name)) { continue; }
                var processValue = GetSafe(name, EnvironmentVariableTarget.Process);
                if (!string.IsNullOrEmpty(processValue))
                {
                    return new ResolvedValue { Name = name, Value = processValue, Source = "Process" };
                }
            }

            foreach (var name in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(name)) { continue; }
                var userValue = GetSafe(name, EnvironmentVariableTarget.User);
                if (!string.IsNullOrEmpty(userValue))
                {
                    return new ResolvedValue { Name = name, Value = userValue, Source = "User" };
                }
            }

            foreach (var name in candidateNames)
            {
                if (string.IsNullOrWhiteSpace(name)) { continue; }
                var machineValue = GetSafe(name, EnvironmentVariableTarget.Machine);
                if (!string.IsNullOrEmpty(machineValue))
                {
                    return new ResolvedValue { Name = name, Value = machineValue, Source = "Machine" };
                }
            }

            return null;
        }

        public static string ResolveValue(params string[] candidateNames)
        {
            var resolved = Resolve(candidateNames);
            return resolved != null ? resolved.Value : null;
        }

        private static string GetSafe(string name, EnvironmentVariableTarget target)
        {
            try { return Environment.GetEnvironmentVariable(name, target); }
            catch { return null; }
        }
    }
}
