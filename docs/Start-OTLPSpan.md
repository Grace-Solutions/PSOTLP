# Start-OTLPSpan

## SYNOPSIS
Starts a new OTLP span and pushes it onto the active span context stack.

## SYNTAX

```powershell
Start-OTLPSpan -Name <String> [-Kind <OTLPSpanKind>] [-TraceId <String>] [-SpanId <String>]
    [-ParentSpanId <String>] [-Attribute <IDictionary>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Start-OTLPSpan` creates an `OTLPSpan` record, pushes it onto the per-thread span context stack,
and links subsequent `Write-OTLPLog` and `Write-OTLPSpanEvent` calls to it automatically.

## EXAMPLES

### Example 1: Start a span and stop it
```powershell
$span = Start-OTLPSpan -Name 'install-driver' -Kind Internal -PassThru
Stop-OTLPSpan -SpanId $span.SpanId
```

## RELATED LINKS
- Stop-OTLPSpan
- Write-OTLPSpanEvent
- about_PSOTLP
