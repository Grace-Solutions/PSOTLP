---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Send-OTLPMetricBatch

## SYNOPSIS
Sends an explicit batch of OTLP metric records using the active connection.

## SYNTAX

```
Send-OTLPMetricBatch [-InputObject] <OTLPMetric[]> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Send-OTLPMetricBatch` accepts `OTLPMetric` instances from the pipeline or as an explicit
array, buffers them in `ProcessRecord`, and exports them as a single `resourceMetrics`
payload in `EndProcessing`. Requires an active connection established with `Connect-OTLP`.

## EXAMPLES

### Example 1: Send a metric batch via splat
```powershell
$MetricList = New-Object -TypeName 'System.Collections.Generic.List[PSOTLP.Models.OTLPMetric]'
    foreach ($Index in 1..3) {
        $Metric = New-Object -TypeName 'PSOTLP.Models.OTLPMetric'
        $Metric.Name = "ps.demo.gauge.$Index"
        $Metric.Type = [PSOTLP.Common.OTLPMetricType]::Gauge
        $Metric.DoubleValue = [double]$Index
        $MetricList.Add($Metric)
    }

$SendOTLPMetricBatchParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SendOTLPMetricBatchParameters.InputObject = $MetricList.ToArray()
    $SendOTLPMetricBatchParameters.PassThru = $True
    $SendOTLPMetricBatchParameters.Verbose = $True

$SendOTLPMetricBatchResult = Send-OTLPMetricBatch @SendOTLPMetricBatchParameters

Write-Output -InputObject ($SendOTLPMetricBatchResult)
```

## PARAMETERS

### -InputObject
One or more `OTLPMetric` instances to flush. Accepts pipeline input.

```yaml
Type: PSOTLP.Models.OTLPMetric[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Emit the `OTLPExportResult` describing the batch (status code, metric count, retry attempts).

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

### PSOTLP.Models.OTLPMetric[]

## OUTPUTS

### PSOTLP.Exporters.OTLPExportResult

## NOTES

## RELATED LINKS

[Write-OTLPMetric]()

[Connect-OTLP]()
