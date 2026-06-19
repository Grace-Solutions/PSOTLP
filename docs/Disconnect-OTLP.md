# Disconnect-OTLP

## SYNOPSIS
Closes the active OTLP connection and clears all header and token references from memory.

## SYNTAX

```powershell
Disconnect-OTLP [<CommonParameters>]
```

## DESCRIPTION
`Disconnect-OTLP` releases the stored `OTLPConnection` and any cached header `SecureString`
values. The cmdlet is a no-op when no connection is active.

## EXAMPLES

### Example 1: Close the current connection
```powershell
Disconnect-OTLP
```

## RELATED LINKS
- Connect-OTLP
- Get-OTLPConnection
- about_PSOTLP
