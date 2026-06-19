# Start-OTLPSession

## SYNOPSIS
Starts a transcript-based OTLP capture session.

## SYNTAX

```powershell
Start-OTLPSession [-SessionName <String>] [-ServiceName <String>]
    [-CaptureMode <OTLPSessionCaptureMode>] [-TranscriptPath <FileInfo>]
    [-BatchSize <Int32>] [-FlushIntervalSeconds <Int32>] [-MaxQueueSize <Int32>]
    [-DropPolicy <OTLPSessionDropPolicy>] [-RedactionEnabled <Boolean>]
    [-RedactPattern <String[]>] [-Attribute <IDictionary>] [-KeepTranscriptFile]
    [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Start-OTLPSession` invokes `Start-Transcript`, attaches a transcript tailer to the resulting
file, and starts a background flush timer that drains a bounded queue into the log exporter.

If a transcript is already running, it is suspended for the duration of the session and
restored by `Stop-OTLPSession`. When `-TranscriptPath` is not supplied, the session owns the
temporary transcript file and `Stop-OTLPSession` deletes it on stop; pass `-KeepTranscriptFile`
to retain the temp file for later inspection.

## EXAMPLES

### Example 1: Start a default session
```powershell
Start-OTLPSession -ServiceName 'powershell-session' -SessionName 'CloudInit-Bootstrap' -PassThru
```

## RELATED LINKS
- Stop-OTLPSession
- Get-OTLPSession
- about_PSOTLP
