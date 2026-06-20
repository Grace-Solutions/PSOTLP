---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Send-OTLPLogBatch

## SYNOPSIS
Sends an explicit batch of OTLP log records using the active connection.

## SYNTAX

```
Send-OTLPLogBatch [-InputObject] <OTLPLogRecord[]> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Send-OTLPLogBatch` accepts `OTLPLogRecord` instances from the pipeline or as an explicit
array, buffers them in `ProcessRecord`, and exports them as a single `resourceLogs` payload
in `EndProcessing`. Requires an active connection established with `Connect-OTLP`.

## EXAMPLES

### Example 1: Build a batch and send it via splat
```powershell
$RecordList = New-Object -TypeName 'System.Collections.Generic.List[PSOTLP.Models.OTLPLogRecord]'
    foreach ($Index in 1..3) {
        $Record = New-Object -TypeName 'PSOTLP.Models.OTLPLogRecord'
        $Record.Body = "log-$Index"
        $Record.Severity = [PSOTLP.Common.OTLPSeverity]::Information
        $RecordList.Add($Record)
    }

$SendOTLPLogBatchParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SendOTLPLogBatchParameters.InputObject = $RecordList.ToArray()
    $SendOTLPLogBatchParameters.PassThru = $True
    $SendOTLPLogBatchParameters.Verbose = $True

$SendOTLPLogBatchResult = Send-OTLPLogBatch @SendOTLPLogBatchParameters

Write-Output -InputObject ($SendOTLPLogBatchResult)
```

## PARAMETERS

### -InputObject
One or more `OTLPLogRecord` instances. Accepts pipeline input.

```yaml
Type: PSOTLP.Models.OTLPLogRecord[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Emit the `OTLPExportResult` describing the batch (status code, record count, retry attempts).

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

### PSOTLP.Models.OTLPLogRecord[]

## OUTPUTS

### PSOTLP.Exporters.OTLPExportResult

## NOTES

## RELATED LINKS

[Write-OTLPLog]()

[Connect-OTLP]()

