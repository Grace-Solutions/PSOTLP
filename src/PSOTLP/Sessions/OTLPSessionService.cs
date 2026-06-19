using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Errors;
using PSOTLP.Exporters;
using PSOTLP.Http;
using PSOTLP.Logging;
using PSOTLP.Redaction;
using PSOTLP.Serialization;

namespace PSOTLP.Sessions
{
    /// <summary>
    /// Encapsulates one running OTLP transcript-capture session: the session model, the queue, the
    /// transcript tailer, and a flush timer that drains the queue in batches to the log exporter.
    /// </summary>
    public sealed class OTLPSessionService
    {
        public const string Component = "OTLPSessionService";

        private readonly OTLPConnection _connection;
        private readonly IOTLPLogExporter _exporter;
        private readonly IOTLPLogger _logger;
        private readonly int _batchSize;

        private readonly object _flushLock = new object();
        private Timer _flushTimer;
        private OTLPTranscriptTailer _tailer;

        public OTLPSession Session { get; }
        public OTLPSessionQueue Queue { get; }

        public OTLPSessionService(OTLPSession session, OTLPConnection connection, OTLPSessionQueue queue, IOTLPLogExporter exporter, IOTLPLogger logger, int batchSize)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _logger = logger;
            _batchSize = batchSize <= 0 ? 100 : batchSize;
        }

        public void Start(FileInfo transcriptFile, OTLPRedactionEngine redaction, int flushIntervalSeconds)
        {
            _tailer = new OTLPTranscriptTailer(transcriptFile, Queue, redaction, _logger, Session);
            _tailer.Start();

            var interval = TimeSpan.FromSeconds(flushIntervalSeconds <= 0 ? 5 : flushIntervalSeconds);
            _flushTimer = new Timer(OnFlush, null, interval, interval);
            if (_logger != null) { _logger.Info(Component, "OTLP session capture started (sessionId=" + Session.SessionId + ", transcript=" + transcriptFile.FullName + ")."); }
        }

        public void Stop(bool drain)
        {
            try { if (_flushTimer != null) { _flushTimer.Dispose(); _flushTimer = null; } } catch { }
            try { if (_tailer != null) { _tailer.Stop(); } } catch { }

            if (drain)
            {
                if (_logger != null) { _logger.Info(Component, "Draining OTLP session queue. Please Wait..."); }
                FlushRemaining();
                if (_logger != null) { _logger.Info(Component, "OTLP session queue drain was successful."); }
            }

            Session.StoppedAtUtc = DateTimeOffset.UtcNow;
            Session.IsActive = false;
            Session.RecordsDropped = Queue.Dropped;
        }

        private void OnFlush(object state)
        {
            if (!Monitor.TryEnter(_flushLock)) { return; }
            try { FlushBatch(); }
            catch (Exception ex) { if (_logger != null) { _logger.Warning(Component, "OTLP session flush iteration failed: " + ex.Message); } }
            finally { Monitor.Exit(_flushLock); }
        }

        private void FlushBatch()
        {
            var batch = Queue.DrainBatch(_batchSize);
            if (batch.Count == 0) { return; }
            try
            {
                _exporter.Export(_connection, batch);
                Session.RecordsExported += batch.Count;
            }
            catch (OTLPException ex)
            {
                if (_logger != null) { _logger.Warning(Component, "OTLP session batch export failed: " + ex.Message); }
            }
        }

        private void FlushRemaining()
        {
            lock (_flushLock)
            {
                while (Queue.Count > 0)
                {
                    var batch = Queue.DrainBatch(_batchSize);
                    if (batch.Count == 0) { break; }
                    try { _exporter.Export(_connection, batch); Session.RecordsExported += batch.Count; }
                    catch (OTLPException ex) { if (_logger != null) { _logger.Warning(Component, "OTLP session drain flush failed: " + ex.Message); } break; }
                }
            }
        }

        public static IOTLPLogExporter BuildDefaultExporter(IOTLPLogger logger)
        {
            var http = new OTLPHttpClient(logger, new OTLPRetryPolicy());
            var serializer = new OTLPJsonSerializer();
            var redaction = new OTLPRedactionEngine();
            return new OTLPLogExporter(http, serializer, redaction, logger);
        }
    }
}
