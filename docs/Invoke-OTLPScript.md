---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Invoke-OTLPScript

## SYNOPSIS
Executes a script block in an isolated runspace and captures every PowerShell stream as OTLP
log records.

## SYNTAX

```
Invoke-OTLPScript [-ScriptBlock] <ScriptBlock> [-ArgumentList <Object[]>] [-SessionName <String>]
 [-ServiceName <String>] [-Attributes <IDictionary>]
 [-AttributeMergeMode <OTLPAttributeMergeMode>] [-BatchSize <Int32>] [-ImportFunctions]
 [-ImportVariables] [-SharedState <IDictionary>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Invoke-OTLPScript` creates a fresh PowerShell runspace, attaches data-added handlers to every
stream (Output, Error, Warning, Verbose, Debug, Information), redacts each captured message
through the active connection's redact patterns, and exports the resulting batch of
`OTLPLogRecord` objects.

Caller-scope functions and variables can be copied into the child runspace via
`-ImportFunctions` and `-ImportVariables`. A `-SharedState` dictionary (typically a
`[hashtable]::Synchronized(@{})`) is injected as `$SharedState` in the child runspace so the
parent and child can exchange data by reference without re-serialization.

When `-Verbose` is on, one count message is written for each of `-ArgumentList`,
`-ImportFunctions`, and `-ImportVariables` (only when those parameters are supplied), so the
caller can confirm what crossed the runspace boundary without exposing argument values.

## EXAMPLES

### Example 1: Run a captured script with shared state via splat
```powershell
$SharedState = [hashtable]::Synchronized(@{})
    $SharedState.Counter = 0

$InvokeOTLPScriptParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $InvokeOTLPScriptParameters.ScriptBlock = {
        param($Greeting)
        Write-Information "$Greeting from child" -InformationAction Continue
        Write-Warning  'careful'
        Write-Verbose  'verbose-line'
        $SharedState.Counter++
    }
    $InvokeOTLPScriptParameters.ArgumentList = ,'hello'
    $InvokeOTLPScriptParameters.SessionName = 'demo'
    $InvokeOTLPScriptParameters.Attributes = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $InvokeOTLPScriptParameters.Attributes['component'] = 'demo'
        $InvokeOTLPScriptParameters.Attributes['phase'] = 'PreExecution'
    $InvokeOTLPScriptParameters.BatchSize = 100
    $InvokeOTLPScriptParameters.ImportFunctions = $True
    $InvokeOTLPScriptParameters.ImportVariables = $True
    $InvokeOTLPScriptParameters.SharedState = $SharedState
    $InvokeOTLPScriptParameters.PassThru = $True
    $InvokeOTLPScriptParameters.Verbose = $True

$InvokeOTLPScriptResult = Invoke-OTLPScript @InvokeOTLPScriptParameters

Write-Output -InputObject ($InvokeOTLPScriptResult)
Write-Output -InputObject ("Counter after child run: $($SharedState.Counter)")
```

## PARAMETERS

### -ScriptBlock
Script block to execute in the isolated runspace. Required.

```yaml
Type: System.Management.Automation.ScriptBlock
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentList
Positional arguments passed to the script block. Bind to the script block's `param(...)`
declaration in order.

```yaml
Type: System.Object[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SessionName
Human-readable session name attached to every captured log record. Defaults to
`PSOTLPScript-yyyyMMddTHHmmssZ`.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: PSOTLPScript-<timestamp>
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServiceName
Override the `service.name` resource attribute on captured records. Defaults to the active
connection's `ServiceName`.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: (connection ServiceName)
Accept pipeline input: False
Accept wildcard characters: False
```

### -Attributes
`IDictionary` of attributes attached to every captured log record (`Hashtable`,
`OrderedDictionary`, etc.). Merged with (not replacing) module/connection defaults.

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
Overrides the connection's `AttributeMergeMode` for every record captured during this
invocation. When omitted, the connection-level mode (defaulting to `Merge`) is used. Accepted
values: `Merge`, `Replace`, `Skip`. See `Connect-OTLP` for the full semantics.

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

### -BatchSize
Maximum number of records exported per HTTP request when draining the captured queue.
Range 1-10000.

```yaml
Type: System.Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 100
Accept pipeline input: False
Accept wildcard characters: False
```

### -ImportFunctions
Copy every function from the caller's scope (`Get-ChildItem function:`) into the child
runspace before executing the script.

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

### -ImportVariables
Copy every non-automatic variable from the caller's scope (`Get-Variable`) into the child
runspace before executing the script. Automatic variables (`$_`, `$args`, `$PSScriptRoot`,
etc.) are filtered out.

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

### -SharedState
`IDictionary` (typically `[hashtable]::Synchronized(@{})`) injected as `$SharedState` in the
child runspace, providing a thread-safe reference channel between the parent and child.

```yaml
Type: System.Collections.IDictionary
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Emit the success-stream output from the captured script after the export completes.

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

### System.Management.Automation.PSObject

## NOTES

## RELATED LINKS

[Connect-OTLP]()

[Write-OTLPLog]()

[Send-OTLPLogBatch]()

