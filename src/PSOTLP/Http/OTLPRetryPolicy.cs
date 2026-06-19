using System;
using System.Collections.Generic;
using System.Threading;

namespace PSOTLP.Http
{
    /// <summary>
    /// Synchronous retry policy with exponential backoff and jitter. Honors a server-provided
    /// Retry-After header when present (seconds or HTTP-date). 400 is never retried.
    /// </summary>
    public sealed class OTLPRetryPolicy
    {
        private static readonly HashSet<int> DefaultRetryable = new HashSet<int> { 429, 502, 503, 504 };
        private static readonly Random _jitter = new Random();
        private static readonly object _jitterLock = new object();

        public int MaxAttempts { get; set; } = 3;
        public int InitialDelayMilliseconds { get; set; } = 500;
        public int MaximumDelayMilliseconds { get; set; } = 10000;
        public HashSet<int> RetryableStatusCodes { get; set; } = new HashSet<int>(DefaultRetryable);

        public bool ShouldRetry(int statusCode, int attemptCount)
        {
            if (statusCode == 400) { return false; }
            if (!RetryableStatusCodes.Contains(statusCode)) { return false; }
            return attemptCount < MaxAttempts;
        }

        public TimeSpan GetDelay(int attemptCount, OTLPHttpResponse response)
        {
            var retryAfter = TryParseRetryAfter(response);
            if (retryAfter.HasValue) { return retryAfter.Value; }

            var exponent = Math.Min(attemptCount, 10);
            var baseDelay = InitialDelayMilliseconds * Math.Pow(2, exponent);
            var capped = Math.Min(baseDelay, MaximumDelayMilliseconds);
            int jitter;
            lock (_jitterLock) { jitter = _jitter.Next(0, Math.Max(1, (int)(capped * 0.2))); }
            return TimeSpan.FromMilliseconds(capped + jitter);
        }

        public void Sleep(TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero) { return; }
            Thread.Sleep(delay);
        }

        private static TimeSpan? TryParseRetryAfter(OTLPHttpResponse response)
        {
            if (response == null || response.Headers == null) { return null; }
            string value;
            if (!response.Headers.TryGetValue("Retry-After", out value) || string.IsNullOrWhiteSpace(value)) { return null; }

            int seconds;
            if (int.TryParse(value, out seconds) && seconds >= 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }

            DateTimeOffset when;
            if (DateTimeOffset.TryParse(value, out when))
            {
                var delta = when - DateTimeOffset.UtcNow;
                return delta > TimeSpan.Zero ? delta : (TimeSpan?)null;
            }
            return null;
        }
    }
}
