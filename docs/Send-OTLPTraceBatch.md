---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Send-OTLPTraceBatch

## SYNOPSIS
Sends an explicit batch of OTLP spans using the active connection.

## SYNTAX

```
Send-OTLPTraceBatch [-InputObject] <OTLPSpan[]> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Send-OTLPTraceBatch` accepts `OTLPSpan` instances from the pipeline or as an explicit array,
buffers them in `ProcessRecord`, and exports them as a single `resourceSpans` payload in
`EndProcessing`. Requires an active connection established with `Connect-OTLP`.

## EXAMPLES

### Example 1: Send a captured span as an explicit batch via splat
```powershell
$Span = Start-OTLPSpan -Name 'install-driver' -PassThru
Stop-OTLPSpan -SpanId $Span.SpanId -NoExport | Out-Null

$SendOTLPTraceBatchParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $SendOTLPTraceBatchParameters.InputObject = ,$Span
    $SendOTLPTraceBatchParameters.PassThru = $True
    $SendOTLPTraceBatchParameters.Verbose = $True

$SendOTLPTraceBatchResult = Send-OTLPTraceBatch @SendOTLPTraceBatchParameters

Write-Output -InputObject ($SendOTLPTraceBatchResult)
```

## PARAMETERS

### -InputObject
One or more `OTLPSpan` instances to flush. Accepts pipeline input.

```yaml
Type: PSOTLP.Models.OTLPSpan[]
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Emit the `OTLPExportResult` describing the batch (status code, span count, retry attempts).

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

### PSOTLP.Models.OTLPSpan[]

## OUTPUTS

### PSOTLP.Exporters.OTLPExportResult

## NOTES

## RELATED LINKS

[Start-OTLPSpan]()

[Stop-OTLPSpan]()

