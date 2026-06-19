using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PSOTLP.Common;
using PSOTLP.Logging;
using PSOTLP.Models;
using PSOTLP.Redaction;

namespace PSOTLP.Sessions
{
    /// <summary>
    /// Background worker that tails a transcript file and converts newly appended lines to
    /// <see cref="OTLPLogRecord"/> entries. Uses a single dedicated thread and a sleep loop
    /// because the codebase prohibits async/await.
    /// </summary>
    public sealed class OTLPTranscriptTailer
    {
        public const string Component = "OTLPTranscriptTailer";

        private readonly FileInfo _transcriptFile;
        private readonly OTLPSessionQueue _queue;
        private readonly OTLPRedactionEngine _redaction;
        private readonly IOTLPLogger _logger;
        private readonly OTLPSession _session;

        private Thread _worker;
        private volatile bool _stopRequested;
        private long _position;

        public OTLPTranscriptTailer(FileInfo transcriptFile, OTLPSessionQueue queue, OTLPRedactionEngine redaction, IOTLPLogger logger, OTLPSession session)
        {
            _transcriptFile = transcriptFile ?? throw new ArgumentNullException(nameof(transcriptFile));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _redaction = redaction;
            _logger = logger;
            _session = session;
        }

        public void Start()
        {
            _stopRequested = false;
            _worker = new Thread(Loop) { IsBackground = true, Name = "PSOTLP-TranscriptTailer" };
            _worker.Start();
        }

        public void Stop()
        {
            _stopRequested = true;
            try { if (_worker != null) { _worker.Join(TimeSpan.FromSeconds(5)); } } catch { }
            DrainOnce();
        }

        private void Loop()
        {
            while (!_stopRequested)
            {
                try { DrainOnce(); } catch (Exception ex) { if (_logger != null) { _logger.Warning(Component, "Transcript tail iteration failed: " + ex.Message); } }
                Thread.Sleep(250);
            }
        }

        private void DrainOnce()
        {
            _transcriptFile.Refresh();
            if (!_transcriptFile.Exists) { return; }

            using (var stream = new FileStream(_transcriptFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream))
            {
                if (_position > stream.Length) { _position = 0; }
                stream.Seek(_position, SeekOrigin.Begin);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    HandleLine(line);
                }
                _position = stream.Position;
            }
        }

        private void HandleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) { return; }
            var trimmed = line.TrimEnd('\r');
            if (IsTranscriptMetadata(trimmed)) { return; }
            var streamName = DetectStream(trimmed, out var body);
            if (string.IsNullOrWhiteSpace(body)) { return; }
            var redacted = _redaction != null ? _redaction.Redact(body) : body;
            var record = new OTLPLogRecord
            {
                Body = redacted,
                Severity = OTLPSeverityMapper.FromStreamName(streamName),
                ObservedTimestampUtc = DateTimeOffset.UtcNow,
                TimestampUtc = DateTimeOffset.UtcNow,
                Attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["powershell.stream"] = streamName,
                    ["powershell.session.id"] = _session != null ? _session.SessionId.ToString() : "n/a",
                    ["powershell.session.name"] = _session != null && _session.SessionName != null ? _session.SessionName : "n/a",
                    ["powershell.capture.mode"] = "Transcript"
                }
            };

            if (_queue.Enqueue(record) && _session != null) { _session.RecordsCaptured++; }
        }

        private static readonly string[] MetadataPrefixes =
        {
            "**********************",
            "Windows PowerShell transcript start",
            "PowerShell transcript start",
            "Windows PowerShell transcript end",
            "PowerShell transcript end",
            "Start time:",
            "End time:",
            "Stop time:",
            "Username:",
            "RunAs User:",
            "Configuration Name:",
            "Machine:",
            "Host Application:",
            "Process ID:",
            "PSVersion:",
            "PSEdition:",
            "PSCompatibleVersions:",
            "BuildVersion:",
            "CLRVersion:",
            "WSManStackVersion:",
            "PSRemotingProtocolVersion:",
            "SerializationVersion:",
            "Transcript started, output file is",
            "Transcript stopped, output file is"
        };

        private static bool IsTranscriptMetadata(string line)
        {
            if (line == null) { return false; }
            for (int i = 0; i < MetadataPrefixes.Length; i++)
            {
                if (line.StartsWith(MetadataPrefixes[i], StringComparison.Ordinal)) { return true; }
            }
            return false;
        }

        private static string DetectStream(string line, out string body)
        {
            body = line;
            if (StartsWith(line, "VERBOSE: ", out body)) { return "Verbose"; }
            if (StartsWith(line, "WARNING: ", out body)) { return "Warning"; }
            if (StartsWith(line, "DEBUG: ", out body)) { return "Debug"; }
            if (StartsWith(line, "INFORMATION: ", out body)) { return "Information"; }
            if (StartsWith(line, "INFO: ", out body)) { return "Information"; }
            if (StartsWith(line, "ERROR: ", out body)) { return "Error"; }
            return "Output";
        }

        private static bool StartsWith(string line, string prefix, out string body)
        {
            if (line != null && line.StartsWith(prefix, StringComparison.Ordinal)) { body = line.Substring(prefix.Length); return true; }
            body = line;
            return false;
        }
    }
}
