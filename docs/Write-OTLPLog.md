# Write-OTLPLog

## SYNOPSIS
Sends a single OTLP log record using the active connection.

## SYNTAX

```powershell
Write-OTLPLog [-Body] <String> [-Severity <OTLPSeverity>] [-Attribute <Hashtable>]
    [-ResourceAttribute <Hashtable>] [-LogAttribute <Hashtable>] [-TraceId <String>]
    [-SpanId <String>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Write-OTLPLog` emits one structured log record. When called inside `Start-OTLPSpan` / `Stop-OTLPSpan`,
the active span's `TraceId` and `SpanId` are attached automatically.

## EXAMPLES

### Example 1: Emit an informational log
```powershell
Write-OTLPLog -Body 'Starting device onboarding' -Severity Information -Attribute @{ Phase = 'PreExecution' }
```

### Example 2: Emit an error log inside a span
```powershell
$span = Start-OTLPSpan -Name 'install-driver'
Write-OTLPLog -Body 'Driver installation failed' -Severity Error
Stop-OTLPSpan -SpanId $span.SpanId
```

## RELATED LINKS
- Send-OTLPLogBatch
- Start-OTLPSpan
- about_PSOTLP
