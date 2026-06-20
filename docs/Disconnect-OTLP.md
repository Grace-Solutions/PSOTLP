---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Disconnect-OTLP

## SYNOPSIS
Closes the active OTLP connection and clears all cached header SecureString references.

## SYNTAX

```
Disconnect-OTLP [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Disconnect-OTLP` releases the stored `OTLPConnection` and any cached header `SecureString`
values. The cmdlet is a no-op when no connection is active.

## EXAMPLES

### Example 1: Close the current connection via splat
```powershell
$DisconnectOTLPParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $DisconnectOTLPParameters.PassThru = $True
    $DisconnectOTLPParameters.Verbose = $True

$DisconnectOTLPResult = Disconnect-OTLP @DisconnectOTLPParameters

Write-Output -InputObject ($DisconnectOTLPResult)
```

## PARAMETERS

### -PassThru
Emit the `OTLPConnectionView` of the connection that was disconnected (with `IsConnected` set
to `$false`) on the success path.

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

### PSOTLP.Connections.OTLPConnectionView

## NOTES

## RELATED LINKS

[Connect-OTLP]()

[Get-OTLPConnection]()

