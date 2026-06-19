# Connect-OTLP

## SYNOPSIS
Opens a connection to an OTLP endpoint and stores it as the active connection for the session.

## SYNTAX

```powershell
Connect-OTLP -EndpointUri <Uri> [-ServiceName <String>] [-Header <Hashtable>]
    [-BearerToken <SecureString>] [-ApiKey <SecureString>] [-ApiKeyHeaderName <String>]
    [-LogsEndpointUri <Uri>] [-TracesEndpointUri <Uri>] [-TimeoutSeconds <Int32>]
    [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Connect-OTLP` builds and stores an `OTLPConnection` that subsequent cmdlets reuse. All header
values are stored as `SecureString` and are only materialized when a request is sent.

## EXAMPLES

### Example 1: Open a connection with a bearer token
```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -Header @{ Authorization = 'Bearer token' }
```

### Example 2: Open a connection with a service name and explicit signal endpoint
```powershell
$secure = ConvertTo-SecureString 'token' -AsPlainText -Force
Connect-OTLP -EndpointUri 'https://otel.example.com' -ServiceName 'powershell-installer' -BearerToken $secure
```

## RELATED LINKS
- Disconnect-OTLP
- Get-OTLPConnection
- about_PSOTLP
