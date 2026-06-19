# Get-OTLPSession

## SYNOPSIS
Returns active or completed OTLP capture sessions.

## SYNTAX

```powershell
Get-OTLPSession [-SessionId <Guid>] [-IncludeCompleted] [<CommonParameters>]
```

## DESCRIPTION
`Get-OTLPSession` returns the registered `OTLPSession` objects from the in-process registry.
Transcript content is never returned; only session metadata is exposed.

## EXAMPLES

### Example 1: List all active sessions
```powershell
Get-OTLPSession
```

### Example 2: Include completed sessions
```powershell
Get-OTLPSession -IncludeCompleted
```

## RELATED LINKS
- Start-OTLPSession
- Stop-OTLPSession
- about_PSOTLP
