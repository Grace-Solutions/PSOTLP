using System.Collections.Generic;
using System.Threading;
using PSOTLP.Models;

namespace PSOTLP.Sessions
{
    /// <summary>
    /// Per-thread active span stack. Cmdlets push a span on Start-OTLPSpan and pop it on
    /// Stop-OTLPSpan so that nested spans inherit the correct parent and so that logs written
    /// during the span automatically pick up TraceId / SpanId.
    /// </summary>
    public static class OTLPSpanContextStack
    {
        private static readonly ThreadLocal<Stack<OTLPSpan>> Stacks =
            new ThreadLocal<Stack<OTLPSpan>>(() => new Stack<OTLPSpan>());

        public static void Push(OTLPSpan span)
        {
            if (span == null) { return; }
            Stacks.Value.Push(span);
        }

        public static OTLPSpan Pop()
        {
            var stack = Stacks.Value;
            if (stack.Count == 0) { return null; }
            return stack.Pop();
        }

        public static OTLPSpan Peek()
        {
            var stack = Stacks.Value;
            if (stack.Count == 0) { return null; }
            return stack.Peek();
        }

        public static OTLPSpan FindById(string spanId)
        {
            if (string.IsNullOrEmpty(spanId)) { return null; }
            foreach (var span in Stacks.Value)
            {
                if (span != null && string.Equals(span.SpanId, spanId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return span;
                }
            }
            return null;
        }

        public static bool RemoveById(string spanId)
        {
            if (string.IsNullOrEmpty(spanId)) { return false; }
            var stack = Stacks.Value;
            if (stack.Count == 0) { return false; }
            var temp = new Stack<OTLPSpan>();
            var removed = false;
            while (stack.Count > 0)
            {
                var span = stack.Pop();
                if (!removed && span != null && string.Equals(span.SpanId, spanId, System.StringComparison.OrdinalIgnoreCase))
                {
                    removed = true;
                    continue;
                }
                temp.Push(span);
            }
            while (temp.Count > 0) { stack.Push(temp.Pop()); }
            return removed;
        }

        public static int Depth { get { return Stacks.Value.Count; } }

        public static void Clear()
        {
            Stacks.Value.Clear();
        }
    }
}
