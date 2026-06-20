---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Write-OTLPLog

## SYNOPSIS
Sends a single OTLP log record using the active connection.

## SYNTAX

### Body (Default)
```
Write-OTLPLog [-Body] <String> [-Severity <OTLPSeverity>] [-Attribute <IDictionary>]
 [-ResourceAttribute <IDictionary>] [-LogAttribute <IDictionary>] [-EventName <String>]
 [-TimestampUtc <DateTimeOffset>] [-TraceId <String>] [-SpanId <String>] [-PassThru]
 [<CommonParameters>]
```

### InputObject
```
Write-OTLPLog -InputObject <OTLPLogRecord> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Write-OTLPLog` emits one structured `OTLPLogRecord`. When called inside an active
`Start-OTLPSpan` / `Stop-OTLPSpan` pair, the active span's `TraceId` and `SpanId` are attached
to the record automatically when they are not supplied explicitly. Accepts either parameter
values or a pre-built `OTLPLogRecord` via `-InputObject` (pipeline-friendly).

## EXAMPLES

### Example 1: Emit a structured log record via splat
```powershell
$WriteOTLPLogParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $WriteOTLPLogParameters.Body = 'Driver installation completed'
    $WriteOTLPLogParameters.Severity = [PSOTLP.Common.OTLPSeverity]::Information
    $WriteOTLPLogParameters.EventName = 'driver.install.completed'
    $WriteOTLPLogParameters.Attribute = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $WriteOTLPLogParameters.Attribute['driver.name'] = 'foo.inf'
        $WriteOTLPLogParameters.Attribute['phase'] = 'PostExecution'
    $WriteOTLPLogParameters.TimestampUtc = [DateTimeOffset]::UtcNow
    $WriteOTLPLogParameters.PassThru = $True
    $WriteOTLPLogParameters.Verbose = $True

$WriteOTLPLogResult = Write-OTLPLog @WriteOTLPLogParameters

Write-Output -InputObject ($WriteOTLPLogResult)
```

## PARAMETERS

### -Body
Log record body. Required in the `Body` parameter set.

```yaml
Type: System.String
Parameter Sets: Body
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Severity
OTLP severity. One of `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`.

```yaml
Type: PSOTLP.Common.OTLPSeverity
Parameter Sets: Body
Aliases:
Accepted values: Trace, Debug, Information, Warning, Error, Fatal

Required: False
Position: Named
Default value: Information
Accept pipeline input: False
Accept wildcard characters: False
```

### -Attribute
`IDictionary` of record attributes (`Hashtable`, `OrderedDictionary`, etc.).

```yaml
Type: System.Collections.IDictionary
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResourceAttribute
`IDictionary` of resource-level attributes that override the connection's resource for this
record only.

```yaml
Type: System.Collections.IDictionary
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogAttribute
`IDictionary` of log-scope attributes that override the connection's log attributes for this
record only.

```yaml
Type: System.Collections.IDictionary
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EventName
Optional `event.name` attribute. Useful for semantic-convention events.

```yaml
Type: System.String
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimestampUtc
Explicit record timestamp. Defaults to `[DateTimeOffset]::UtcNow` at the moment of invocation.

```yaml
Type: System.DateTimeOffset
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: [DateTimeOffset]::UtcNow
Accept pipeline input: False
Accept wildcard characters: False
```

### -TraceId
Explicit trace id (32-char hex). Defaults to the trace id of the active span, when present.

```yaml
Type: System.String
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SpanId
Explicit span id (16-char hex). Defaults to the id of the active span, when present.

```yaml
Type: System.String
Parameter Sets: Body
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Pre-built `OTLPLogRecord` to emit. Required in the `InputObject` parameter set.

```yaml
Type: PSOTLP.Models.OTLPLogRecord
Parameter Sets: InputObject
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Emit the `OTLPLogRecord` after it is queued.

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

### PSOTLP.Models.OTLPLogRecord

## OUTPUTS

### PSOTLP.Models.OTLPLogRecord

## NOTES

## RELATED LINKS

[Send-OTLPLogBatch]()

[Start-OTLPSpan]()

[Connect-OTLP]()

