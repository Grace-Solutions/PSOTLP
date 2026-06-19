# Stop-OTLPSession

## SYNOPSIS
Stops a running OTLP capture session and drains the queued records.

## SYNTAX

```powershell
Stop-OTLPSession [-SessionId <Guid>] [-NoDrain] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Stop-OTLPSession` stops the transcript tailer, calls `Stop-Transcript`, and flushes remaining
queued records to the OTLP endpoint unless `-NoDrain` is specified.

## EXAMPLES

### Example 1: Stop the only active session and drain it
```powershell
Stop-OTLPSession
```

### Example 2: Stop a specific session without draining
```powershell
Stop-OTLPSession -SessionId $session.SessionId -NoDrain
```

## RELATED LINKS
- Start-OTLPSession
- Get-OTLPSession
- about_PSOTLP
