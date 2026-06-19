# Send-OTLPLogBatch

## SYNOPSIS
Sends an explicit batch of OTLP log records.

## SYNTAX

```powershell
Send-OTLPLogBatch [-InputObject] <OTLPLogRecord[]> [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Send-OTLPLogBatch` accepts records from the pipeline or as an explicit array, buffers them in
`ProcessRecord`, and exports them as a single resourceLogs payload in `EndProcessing`.

## EXAMPLES

### Example 1: Pipe log records into the cmdlet
```powershell
$records = 1..3 | ForEach-Object {
    $record = New-Object PSOTLP.Models.OTLPLogRecord
    $record.Body = "log-$_"
    $record.Severity = [PSOTLP.Common.OTLPSeverity]::Information
    $record
}
$records | Send-OTLPLogBatch
```

## RELATED LINKS
- Write-OTLPLog
- about_PSOTLP
