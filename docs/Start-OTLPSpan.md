---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Start-OTLPSpan

## SYNOPSIS
Starts a new OTLP span and pushes it onto the active span context stack.

## SYNTAX

```
Start-OTLPSpan [-Name] <String> [-Kind <OTLPSpanKind>] [-TraceId <String>] [-SpanId <String>]
 [-ParentSpanId <String>] [-Attributes <IDictionary>]
 [-AttributeMergeMode <OTLPAttributeMergeMode>] [-StartTimeUtc <DateTimeOffset>] [-PassThru]
 [<CommonParameters>]
```

## DESCRIPTION
`Start-OTLPSpan` creates an `OTLPSpan` record, pushes it onto the per-thread span context stack,
and links subsequent `Write-OTLPLog` and `Write-OTLPSpanEvent` calls to it automatically. When a
parent span is already on the stack, the new span inherits its `TraceId` and links to it as the
parent. Returns the span when `-PassThru` is supplied.

## EXAMPLES

### Example 1: Start a span with attributes via splat
```powershell
$StartOTLPSpanParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $StartOTLPSpanParameters.Name = 'install-driver'
    $StartOTLPSpanParameters.Kind = [PSOTLP.Common.OTLPSpanKind]::Internal
    $StartOTLPSpanParameters.Attributes = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $StartOTLPSpanParameters.Attributes['component'] = 'driver-installer'
        $StartOTLPSpanParameters.Attributes['driver.name'] = 'foo.inf'
    $StartOTLPSpanParameters.StartTimeUtc = [DateTimeOffset]::UtcNow
    $StartOTLPSpanParameters.PassThru = $True
    $StartOTLPSpanParameters.Verbose = $True

$StartOTLPSpanResult = Start-OTLPSpan @StartOTLPSpanParameters

Write-Output -InputObject ($StartOTLPSpanResult)
```

## PARAMETERS

### -Name
Human-readable span name. Required.

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

### -Kind
Span kind. One of `Internal`, `Server`, `Client`, `Producer`, `Consumer`.

```yaml
Type: PSOTLP.Common.OTLPSpanKind
Parameter Sets: (All)
Aliases:
Accepted values: Internal, Server, Client, Producer, Consumer

Required: False
Position: Named
Default value: Internal
Accept pipeline input: False
Accept wildcard characters: False
```

### -TraceId
Explicit trace id (32-char hex). Defaults to the parent span's trace id when one is on the
stack, otherwise a freshly generated id.

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

### -SpanId
Explicit span id (16-char hex). Defaults to a freshly generated id.

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

### -ParentSpanId
Explicit parent span id. Defaults to the id of the span at the top of the context stack.

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

### -Attributes
`IDictionary` of span attributes (`Hashtable`, `OrderedDictionary`, etc.). Keys become
attribute names; values are normalized to OTLP `AnyValue` kinds.

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
Overrides the connection's `AttributeMergeMode` for this span only. When omitted, the
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

### -StartTimeUtc
Explicit span start time. Defaults to `[DateTimeOffset]::UtcNow` at the moment of invocation.

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

### -PassThru
Emit the freshly created `OTLPSpan`.

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

### PSOTLP.Models.OTLPSpan

## NOTES

## RELATED LINKS

[Stop-OTLPSpan]()

[Write-OTLPSpanEvent]()

[Send-OTLPTraceBatch]()

