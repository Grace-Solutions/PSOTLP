using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using PSOTLP.Common;
using PSOTLP.Connections;
using PSOTLP.Redaction;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "OTLPSession")]
    [OutputType(typeof(OTLPSession))]
    public sealed class StartOTLPSessionCommand : OTLPCmdletBase
    {
        [Parameter] public string SessionName { get; set; }
        [Parameter] public string ServiceName { get; set; }
        [Parameter] public OTLPSessionCaptureMode CaptureMode { get; set; } = OTLPSessionCaptureMode.Transcript;
        [Parameter] public FileInfo TranscriptPath { get; set; }

        [Parameter] [ValidateRange(1, 10000)] public int BatchSize { get; set; } = 100;
        [Parameter] [ValidateRange(1, 600)] public int FlushIntervalSeconds { get; set; } = 5;
        [Parameter] [ValidateRange(100, 1000000)] public int MaxQueueSize { get; set; } = 10000;
        [Parameter] public OTLPSessionDropPolicy DropPolicy { get; set; } = OTLPSessionDropPolicy.DropOldest;

        [Parameter] public bool RedactionEnabled { get; set; } = true;
        [Parameter] public string[] RedactPattern { get; set; }

        [Parameter] public IDictionary Attribute { get; set; }
        [Parameter] public SwitchParameter KeepTranscriptFile { get; set; }
        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var connection = OTLPSessionManager.RequireCurrentConnection();
                var userSuppliedTranscript = TranscriptPath != null;
                var transcriptFile = ResolveTranscriptPath();
                EnsureDirectory(transcriptFile);

                var session = new OTLPSession
                {
                    SessionId = Guid.NewGuid(),
                    SessionName = string.IsNullOrWhiteSpace(SessionName) ? "PSOTLPSession-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ") : SessionName,
                    ServiceName = string.IsNullOrWhiteSpace(ServiceName) ? connection.ServiceName : ServiceName,
                    CaptureMode = CaptureMode,
                    StartedAtUtc = DateTimeOffset.UtcNow,
                    TranscriptPath = transcriptFile,
                    IsActive = true,
                    OwnsTranscriptFile = !userSuppliedTranscript && !KeepTranscriptFile.IsPresent
                };

                WriteVerboseLine("Starting OTLP session capture (sessionId=" + session.SessionId + "). Please Wait...");
                session.SuspendedTranscriptPath = SuspendExistingTranscript();
                StartTranscript(transcriptFile);

                var queue = new OTLPSessionQueue(MaxQueueSize, DropPolicy);
                var redaction = RedactionEnabled ? new OTLPRedactionEngine(RedactPattern) : null;
                var exporter = OTLPSessionService.BuildDefaultExporter(Logger);
                var service = new OTLPSessionService(session, connection, queue, exporter, Logger, BatchSize);
                service.Start(transcriptFile, redaction, FlushIntervalSeconds);
                OTLPSessionRegistry.Register(service);

                WriteVerboseLine("OTLP session capture start was successful.");
                if (PassThru.IsPresent) { WriteObject(session); }
            }
            catch (Exception ex)
            {
                HandleException("StartSession", ex);
            }
        }

        private FileInfo ResolveTranscriptPath()
        {
            if (TranscriptPath != null) { return TranscriptPath; }
            var temp = Path.Combine(Path.GetTempPath(), "PSOTLP", "transcripts");
            var file = Path.Combine(temp, "session-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".txt");
            return new FileInfo(file);
        }

        private static void EnsureDirectory(FileInfo file)
        {
            var directory = file.Directory;
            if (directory != null && !directory.Exists) { directory.Create(); }
        }

        private void StartTranscript(FileInfo transcriptFile)
        {
            var quoted = "'" + transcriptFile.FullName.Replace("'", "''") + "'";
            var script = "Start-Transcript -Path " + quoted + " -Force -IncludeInvocationHeader | Out-Null";
            SessionState.InvokeCommand.InvokeScript(script);
        }

        private string SuspendExistingTranscript()
        {
            try
            {
                var script = "$msg = try { (Stop-Transcript -ErrorAction Stop) } catch { $null }; if ($msg) { [string]$msg } else { '' }";
                var output = SessionState.InvokeCommand.InvokeScript(script);
                if (output == null || output.Count == 0) { return null; }
                var text = output[0] != null ? output[0].BaseObject as string : null;
                if (string.IsNullOrWhiteSpace(text)) { return null; }
                var marker = "output file is ";
                var index = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (index < 0) { return null; }
                var path = text.Substring(index + marker.Length).Trim().Trim('\'', '"');
                WriteVerboseLine("Suspended an existing transcript so the OTLP session can capture cleanly.");
                return path;
            }
            catch { return null; }
        }
    }
}
