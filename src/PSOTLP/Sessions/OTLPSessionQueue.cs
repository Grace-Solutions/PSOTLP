using System.Collections.Generic;
using PSOTLP.Common;
using PSOTLP.Models;

namespace PSOTLP.Sessions
{
    /// <summary>
    /// Bounded FIFO queue used by the session transcript pipeline. Producer threads call
    /// <see cref="Enqueue"/>; the flush worker calls <see cref="DrainBatch"/>. Synchronization is
    /// done with a simple lock so that no async/await is required.
    /// </summary>
    public sealed class OTLPSessionQueue
    {
        private readonly object _lock = new object();
        private readonly LinkedList<OTLPLogRecord> _records = new LinkedList<OTLPLogRecord>();
        private readonly int _maxQueueSize;
        private readonly OTLPSessionDropPolicy _dropPolicy;

        public int Dropped { get; private set; }

        public OTLPSessionQueue(int maxQueueSize, OTLPSessionDropPolicy dropPolicy)
        {
            _maxQueueSize = maxQueueSize <= 0 ? 10000 : maxQueueSize;
            _dropPolicy = dropPolicy;
        }

        public int Count { get { lock (_lock) { return _records.Count; } } }

        public bool Enqueue(OTLPLogRecord record)
        {
            if (record == null) { return false; }
            lock (_lock)
            {
                if (_records.Count >= _maxQueueSize)
                {
                    switch (_dropPolicy)
                    {
                        case OTLPSessionDropPolicy.DropNewest:
                            Dropped++;
                            return false;
                        case OTLPSessionDropPolicy.Block:
                            return false;
                        case OTLPSessionDropPolicy.DropOldest:
                        default:
                            _records.RemoveFirst();
                            Dropped++;
                            break;
                    }
                }
                _records.AddLast(record);
                return true;
            }
        }

        public List<OTLPLogRecord> DrainBatch(int batchSize)
        {
            if (batchSize <= 0) { batchSize = 100; }
            var batch = new List<OTLPLogRecord>(batchSize);
            lock (_lock)
            {
                while (batch.Count < batchSize && _records.Count > 0)
                {
                    batch.Add(_records.First.Value);
                    _records.RemoveFirst();
                }
            }
            return batch;
        }

        public List<OTLPLogRecord> DrainAll()
        {
            lock (_lock)
            {
                var all = new List<OTLPLogRecord>(_records);
                _records.Clear();
                return all;
            }
        }
    }
}
