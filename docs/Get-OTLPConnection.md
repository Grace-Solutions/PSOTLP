# Get-OTLPConnection

## SYNOPSIS
Returns a non-sensitive view of the active OTLP connection.

## SYNTAX

```powershell
Get-OTLPConnection [<CommonParameters>]
```

## DESCRIPTION
`Get-OTLPConnection` returns a `OTLPConnectionView` object that exposes endpoint information,
service name, and which header keys are configured. Header values themselves are never returned.

## EXAMPLES

### Example 1: View the current connection
```powershell
Get-OTLPConnection
```

## RELATED LINKS
- Connect-OTLP
- Disconnect-OTLP
- about_PSOTLP
