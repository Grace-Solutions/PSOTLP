# Invoke-OTLPScript

## SYNOPSIS
Executes a script block in a controlled runspace and captures every PowerShell stream as OTLP log records.

## SYNTAX

```powershell
Invoke-OTLPScript [-ScriptBlock] <ScriptBlock> [-ArgumentList <Object[]>]
    [-SessionName <String>] [-ServiceName <String>] [-Attribute <IDictionary>]
    [-BatchSize <Int32>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Invoke-OTLPScript` creates a fresh PowerShell runspace, attaches data-added handlers to every
stream (Output, Error, Warning, Verbose, Debug, Information), redacts each captured message, and
exports the resulting batch of OTLP log records.

## EXAMPLES

### Example 1: Capture the output of a script block
```powershell
Invoke-OTLPScript -ScriptBlock { Write-Host 'hello'; Write-Warning 'careful'; Get-Date } -SessionName 'demo'
```

### Example 2: Pass arguments to the script block
```powershell
Invoke-OTLPScript -ScriptBlock { param($name) Write-Information "hello $name" -InformationAction Continue } -ArgumentList 'world'
```

## RELATED LINKS
- Start-OTLPSession
- Write-OTLPLog
- about_PSOTLP
