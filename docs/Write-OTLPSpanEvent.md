# Write-OTLPSpanEvent

## SYNOPSIS
Adds an event to an active OTLP span.

## SYNTAX

```powershell
Write-OTLPSpanEvent -Name <String> [-SpanId <String>] [-Attribute <IDictionary>] [<CommonParameters>]
```

## DESCRIPTION
`Write-OTLPSpanEvent` appends a structured event to the targeted span. When `-SpanId` is omitted,
the event is attached to the span at the top of the active span context stack.

## EXAMPLES

### Example 1: Add an event to the current span
```powershell
Write-OTLPSpanEvent -Name 'driver-located' -Attribute @{ path = 'C:\\Drivers\\foo.inf' }
```

## RELATED LINKS
- Start-OTLPSpan
- Stop-OTLPSpan
- about_PSOTLP
