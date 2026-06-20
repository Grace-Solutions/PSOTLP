---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Get-OTLPConnection

## SYNOPSIS
Returns a non-sensitive view of the active OTLP connection.

## SYNTAX

```
Get-OTLPConnection [<CommonParameters>]
```

## DESCRIPTION
`Get-OTLPConnection` returns an `OTLPConnectionView` object that exposes endpoint information,
service name, transport / encoding / compression, scope, environment, and which header keys
are configured. Header values themselves are never returned. When no connection is active,
nothing is emitted and a verbose message is written.

## EXAMPLES

### Example 1: Inspect the current connection via splat
```powershell
$GetOTLPConnectionParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $GetOTLPConnectionParameters.Verbose = $True

$GetOTLPConnectionResult = Get-OTLPConnection @GetOTLPConnectionParameters

Write-Output -InputObject ($GetOTLPConnectionResult)
```

## PARAMETERS

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable,
-InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable,
-Verbose, -WarningAction, and -WarningVariable. For more information, see
[about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### PSOTLP.Connections.OTLPConnectionView

## NOTES

## RELATED LINKS

[Connect-OTLP]()

[Disconnect-OTLP]()

