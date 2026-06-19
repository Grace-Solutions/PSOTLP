# Stop-OTLPSpan

## SYNOPSIS
Stops an active OTLP span and queues it for export.

## SYNTAX

```powershell
Stop-OTLPSpan [-SpanId <String>] [-Status <OTLPSpanStatusCode>] [-StatusMessage <String>]
    [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Stop-OTLPSpan` pops the matching span from the active span context stack, records its end
timestamp and status, and queues it for the next `Send-OTLPTraceBatch` flush.

## EXAMPLES

### Example 1: Stop the most recently started span
```powershell
Stop-OTLPSpan
```

### Example 2: Stop a span with an error status
```powershell
Stop-OTLPSpan -SpanId $span.SpanId -Status Error -StatusMessage 'driver-install-failed'
```

## RELATED LINKS
- Start-OTLPSpan
- Send-OTLPTraceBatch
- about_PSOTLP
