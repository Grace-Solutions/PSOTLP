# Send-OTLPTraceBatch

## SYNOPSIS
Sends queued OTLP spans to the active connection.

## SYNTAX

```powershell
Send-OTLPTraceBatch [-InputObject <OTLPSpan[]>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Send-OTLPTraceBatch` flushes the pending span queue (or an explicit span collection) as a single
OTLP `resourceSpans` payload.

## EXAMPLES

### Example 1: Flush all completed spans
```powershell
Send-OTLPTraceBatch
```

## RELATED LINKS
- Start-OTLPSpan
- Stop-OTLPSpan
- about_PSOTLP
