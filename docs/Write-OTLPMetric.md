---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Write-OTLPMetric

## SYNOPSIS
Sends a single OTLP metric record using the active connection.

## SYNTAX

### Value (Default)
```
Write-OTLPMetric [-Name] <String> [-Description <String>] [-Unit <String>] [-Type <OTLPMetricType>]
 [-Temporality <OTLPAggregationTemporality>] [-IsMonotonic] [-Value <Double>] [-IntValue <Int64>]
 [-AsInt] [-Attribute <IDictionary>] [-ResourceAttribute <IDictionary>]
 [-TimestampUtc <DateTimeOffset>] [-StartTimestampUtc <DateTimeOffset>] [-PassThru]
 [<CommonParameters>]
```

### InputObject
```
Write-OTLPMetric -InputObject <OTLPMetric> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Write-OTLPMetric` emits a single `OTLPMetric`. By default the cmdlet builds a `Gauge` with
`Cumulative` temporality from the supplied `-Value`. Pass `-AsInt` together with `-IntValue`
to emit integer points. Pass a pre-built `OTLPMetric` via `-InputObject` to bypass the
property mapping.


## EXAMPLES

### Example 1: Emit a metric via splat
```powershell
$WriteOTLPMetricParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $WriteOTLPMetricParameters.Name = 'ps.driver.install.duration'
    $WriteOTLPMetricParameters.Description = 'Driver installation duration'
    $WriteOTLPMetricParameters.Unit = 'ms'
    $WriteOTLPMetricParameters.Type = [PSOTLP.Common.OTLPMetricType]::Histogram
    $WriteOTLPMetricParameters.Temporality = [PSOTLP.Common.OTLPAggregationTemporality]::Cumulative
    $WriteOTLPMetricParameters.IsMonotonic = $False
    $WriteOTLPMetricParameters.Value = 1234.5
    $WriteOTLPMetricParameters.Attribute = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $WriteOTLPMetricParameters.Attribute['driver.name'] = 'foo.inf'
        $WriteOTLPMetricParameters.Attribute['phase'] = 'install'
    $WriteOTLPMetricParameters.ResourceAttribute = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $WriteOTLPMetricParameters.ResourceAttribute['deployment.environment'] = 'production'
    $WriteOTLPMetricParameters.TimestampUtc = [DateTimeOffset]::UtcNow
    $WriteOTLPMetricParameters.StartTimestampUtc = [DateTimeOffset]::UtcNow
    $WriteOTLPMetricParameters.PassThru = $True
    $WriteOTLPMetricParameters.Verbose = $True

$WriteOTLPMetricResult = Write-OTLPMetric @WriteOTLPMetricParameters

Write-Output -InputObject ($WriteOTLPMetricResult)
```

## PARAMETERS

### -Name
Metric name. Required in the `Value` parameter set.

```yaml
Type: System.String
Parameter Sets: Value
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Optional metric description shipped with the metric definition.

```yaml
Type: System.String
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
Optional metric unit (`ms`, `By`, `1`, etc.).

```yaml
Type: System.String
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
Metric instrument type. One of `Gauge`, `Sum`, `Histogram`.

```yaml
Type: PSOTLP.Common.OTLPMetricType
Parameter Sets: Value
Aliases:
Accepted values: Gauge, Sum, Histogram

Required: False
Position: Named
Default value: Gauge
Accept pipeline input: False
Accept wildcard characters: False
```

### -Temporality
Aggregation temporality for `Sum` and `Histogram` instruments. One of `Cumulative`, `Delta`.

```yaml
Type: PSOTLP.Common.OTLPAggregationTemporality
Parameter Sets: Value
Aliases:
Accepted values: Cumulative, Delta

Required: False
Position: Named
Default value: Cumulative
Accept pipeline input: False
Accept wildcard characters: False
```

### -IsMonotonic
Mark a `Sum` instrument as monotonic.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
`Double` data point value. Used when `-AsInt` is not supplied.

```yaml
Type: System.Double
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -IntValue
`Int64` data point value. Used when `-AsInt` is supplied.

```yaml
Type: System.Int64
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsInt
Emit the data point using `-IntValue` instead of `-Value`.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Attribute
`IDictionary` of metric attributes (`Hashtable`, `OrderedDictionary`, etc.).

```yaml
Type: System.Collections.IDictionary
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResourceAttribute
`IDictionary` of resource-level attributes that override the connection's resource for this
metric only.

```yaml
Type: System.Collections.IDictionary
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimestampUtc
Explicit data point timestamp. Defaults to `[DateTimeOffset]::UtcNow` at the moment of
invocation.

```yaml
Type: System.DateTimeOffset
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: [DateTimeOffset]::UtcNow
Accept pipeline input: False
Accept wildcard characters: False
```

### -StartTimestampUtc
Explicit data point start timestamp (used by `Sum` and `Histogram` instruments). Defaults
to `[DateTimeOffset]::UtcNow` at the moment of invocation.

```yaml
Type: System.DateTimeOffset
Parameter Sets: Value
Aliases:

Required: False
Position: Named
Default value: [DateTimeOffset]::UtcNow
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Pre-built `OTLPMetric` to emit. Required in the `InputObject` parameter set.

```yaml
Type: PSOTLP.Models.OTLPMetric
Parameter Sets: InputObject
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Emit the `OTLPMetric` after it is sent.

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

### PSOTLP.Models.OTLPMetric

## OUTPUTS

### PSOTLP.Models.OTLPMetric

## NOTES

## RELATED LINKS

[Send-OTLPMetricBatch]()

[Connect-OTLP]()
