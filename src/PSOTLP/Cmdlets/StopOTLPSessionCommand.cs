using System;
using System.Management.Automation;
using PSOTLP.Sessions;

namespace PSOTLP.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Stop, "OTLPSession")]
    [OutputType(typeof(OTLPSession))]
    public sealed class StopOTLPSessionCommand : OTLPCmdletBase
    {
        [Parameter] public Guid SessionId { get; set; }
        [Parameter] public SwitchParameter NoDrain { get; set; }
        [Parameter] public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var service = SessionId != Guid.Empty
                    ? OTLPSessionRegistry.Get(SessionId)
                    : OTLPSessionRegistry.GetSingleActive();
                if (service == null) { throw new InvalidOperationException("No active OTLP session was found."); }

                WriteVerboseLine("Stopping OTLP session capture (sessionId=" + service.Session.SessionId + "). Please Wait...");
                StopTranscript();
                service.Stop(!NoDrain.IsPresent);
                CleanUpTranscriptFile(service.Session);
                ResumeSuspendedTranscript(service.Session.SuspendedTranscriptPath);
                OTLPSessionRegistry.Complete(service.Session.SessionId);
                WriteVerboseLine("OTLP session capture stop was successful.");

                if (PassThru.IsPresent) { WriteObject(service.Session); }
            }
            catch (Exception ex)
            {
                HandleException("StopSession", ex);
            }
        }

        private void StopTranscript()
        {
            try { SessionState.InvokeCommand.InvokeScript("Stop-Transcript -ErrorAction SilentlyContinue | Out-Null"); }
            catch { }
        }

        private void CleanUpTranscriptFile(Sessions.OTLPSession session)
        {
            if (session == null || !session.OwnsTranscriptFile || session.TranscriptPath == null) { return; }
            try
            {
                session.TranscriptPath.Refresh();
                if (session.TranscriptPath.Exists) { session.TranscriptPath.Delete(); }
            }
            catch (Exception ex) { WriteVerboseLine("Unable to delete OTLP session transcript file: " + ex.Message); }
        }

        private void ResumeSuspendedTranscript(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { return; }
            try
            {
                var quoted = "'" + path.Replace("'", "''") + "'";
                SessionState.InvokeCommand.InvokeScript("Start-Transcript -Path " + quoted + " -Append -ErrorAction SilentlyContinue | Out-Null");
                WriteVerboseLine("Resumed previously suspended transcript.");
            }
            catch (Exception ex) { WriteVerboseLine("Unable to resume previously suspended transcript: " + ex.Message); }
        }
    }
}
