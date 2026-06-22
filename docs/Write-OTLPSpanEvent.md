---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Write-OTLPSpanEvent

## SYNOPSIS
Adds a structured event to an active OTLP span.

## SYNTAX

```
Write-OTLPSpanEvent [-Name] <String> [-Attributes <IDictionary>]
 [-AttributeMergeMode <OTLPAttributeMergeMode>] [-TimestampUtc <DateTimeOffset>]
 [-SpanId <String>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Write-OTLPSpanEvent` appends an `OTLPSpanEvent` to the targeted span. When `-SpanId` is
omitted, the event is attached to the span at the top of the active span context stack.
Throws when no active span is available.

## EXAMPLES

### Example 1: Add an event with attributes to the current span via splat
```powershell
$WriteOTLPSpanEventParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $WriteOTLPSpanEventParameters.Name = 'driver-located'
    $WriteOTLPSpanEventParameters.Attributes = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $WriteOTLPSpanEventParameters.Attributes['path'] = 'C:\Drivers\foo.inf'
        $WriteOTLPSpanEventParameters.Attributes['size.bytes'] = 1048576
    $WriteOTLPSpanEventParameters.TimestampUtc = [DateTimeOffset]::UtcNow
    $WriteOTLPSpanEventParameters.PassThru = $True
    $WriteOTLPSpanEventParameters.Verbose = $True

$WriteOTLPSpanEventResult = Write-OTLPSpanEvent @WriteOTLPSpanEventParameters

Write-Output -InputObject ($WriteOTLPSpanEventResult)
```

## PARAMETERS

### -Name
Event name. Required.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Attributes
`IDictionary` of event attributes (`Hashtable`, `OrderedDictionary`, etc.).

```yaml
Type: System.Collections.IDictionary
Parameter Sets: (All)
Aliases: Attribute

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AttributeMergeMode
Overrides the connection's `AttributeMergeMode` for this event only. When omitted, the
connection-level mode (defaulting to `Merge`) is used. Accepted values: `Merge`, `Replace`,
`Skip`. See `Connect-OTLP` for the full semantics.

```yaml
Type: PSOTLP.Common.OTLPAttributeMergeMode
Parameter Sets: (All)
Aliases:
Accepted values: Merge, Replace, Skip

Required: False
Position: Named
Default value: Merge
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimestampUtc
Explicit event timestamp. Defaults to `[DateTimeOffset]::UtcNow` at the moment of invocation.

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

### -SpanId
Explicit span id to attach the event to. Defaults to the span at the top of the active
context stack.

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

### -PassThru
Emit the new `OTLPSpanEvent`.

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

### None

## OUTPUTS

### PSOTLP.Models.OTLPSpanEvent

## NOTES

## RELATED LINKS

[Start-OTLPSpan]()

[Stop-OTLPSpan]()

