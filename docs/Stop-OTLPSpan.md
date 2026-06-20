---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Stop-OTLPSpan

## SYNOPSIS
Stops an active OTLP span, records its end time and status, and (by default) exports it.

## SYNTAX

```
Stop-OTLPSpan [[-SpanId] <String>] [-StatusCode <OTLPStatusCode>] [-StatusMessage <String>]
 [-EndTimeUtc <DateTimeOffset>] [-NoExport] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Stop-OTLPSpan` removes a span from the active span context stack (either by `-SpanId` or by
popping the top of the stack), stamps it with an end time and status, and ships it to the
trace exporter unless `-NoExport` is supplied. Returns the span when `-PassThru` is used,
which is useful for combining with `Send-OTLPTraceBatch`.

## EXAMPLES

### Example 1: Stop a span with Error status via splat
```powershell
$Span = Start-OTLPSpan -Name 'install-driver' -PassThru

$StopOTLPSpanParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $StopOTLPSpanParameters.SpanId = $Span.SpanId
    $StopOTLPSpanParameters.StatusCode = [PSOTLP.Common.OTLPStatusCode]::Error
    $StopOTLPSpanParameters.StatusMessage = 'driver-install-failed'
    $StopOTLPSpanParameters.EndTimeUtc = [DateTimeOffset]::UtcNow
    $StopOTLPSpanParameters.NoExport = $False
    $StopOTLPSpanParameters.PassThru = $True
    $StopOTLPSpanParameters.Verbose = $True

$StopOTLPSpanResult = Stop-OTLPSpan @StopOTLPSpanParameters

Write-Output -InputObject ($StopOTLPSpanResult)
```

## PARAMETERS

### -SpanId
Explicit span id to stop. When omitted, the span at the top of the active context stack is
popped. Throws when no match is found.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -StatusCode
Span status. One of `Unset`, `Ok`, `Error`. Defaults to `Unset`.

```yaml
Type: PSOTLP.Common.OTLPStatusCode
Parameter Sets: (All)
Aliases:
Accepted values: Unset, Ok, Error

Required: False
Position: Named
Default value: Unset
Accept pipeline input: False
Accept wildcard characters: False
```

### -StatusMessage
Optional human-readable status message. Only honored when set together with `-StatusCode`.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EndTimeUtc
Explicit span end time. Defaults to `[DateTimeOffset]::UtcNow` at the moment of invocation.

```yaml
Type: System.DateTimeOffset
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: [DateTimeOffset]::UtcNow
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoExport
Skip the per-span export call. Useful when batching multiple spans through
`Send-OTLPTraceBatch`.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Emit the finalized `OTLPSpan`.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable,
-Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String

## OUTPUTS

### PSOTLP.Models.OTLPSpan

## NOTES

## RELATED LINKS

[Start-OTLPSpan]()

[Send-OTLPTraceBatch]()

