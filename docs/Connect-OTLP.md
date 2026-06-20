---
external help file: PSOTLP.dll-Help.xml
Module Name: PSOTLP
online version:
schema: 2.0.0
---

# Connect-OTLP

## SYNOPSIS
Opens a connection to an OTLP endpoint and stores it as the active connection for the session.

## SYNTAX

```
Connect-OTLP [-EndpointUri <Uri>] [-LogsEndpointUri <Uri>] [-TracesEndpointUri <Uri>]
 [-MetricsEndpointUri <Uri>] [-Header <IDictionary>] [-Transport <OTLPTransport>]
 [-Encoding <OTLPEncoding>] [-Compression <OTLPCompression>] [-ServiceName <String>]
 [-ServiceNamespace <String>] [-ServiceInstanceId <String>] [-ScopeName <String>]
 [-ScopeVersion <String>] [-EnvironmentName <String>] [-ResourceAttribute <IDictionary>]
 [-LogAttribute <IDictionary>] [-RedactPattern <Regex[]>] [-RetryCount <Int32>]
 [-TimeoutSeconds <Int32>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
`Connect-OTLP` builds and stores an `OTLPConnection` that subsequent cmdlets reuse. All
authentication headers (bearer tokens, API keys, vendor-specific values) are supplied through
the single `-Header` dictionary. `-Header` accepts any `IDictionary` implementation
(`Hashtable`, `OrderedDictionary`, etc.); values may be `String` or `SecureString`.
Plain-string values are converted to `SecureString` at parameter binding, and header values
are only materialized when a request is sent.

When `-ServiceNamespace` is not supplied, `Connect-OTLP` defaults `service.namespace` to
`PSOTLP`. When `-ServiceInstanceId` is not supplied, a fresh GUID is generated for the
connection lifetime. When `-EndpointUri` is not supplied, the value is resolved from
`$env:PSOTLP_OTLP_ENDPOINT` or `$env:OTEL_EXPORTER_OTLP_ENDPOINT`, falling back to
`http://localhost:4318`.

## EXAMPLES

### Example 1: Open an authenticated connection via splat
```powershell
$ConnectOTLPParameters = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
    $ConnectOTLPParameters.EndpointUri = [Uri]'https://otel.example.com'
    $ConnectOTLPParameters.Header = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $ConnectOTLPParameters.Header['Authorization'] = ConvertTo-SecureString -String ('Bearer ' + $env:OTEL_BEARER_TOKEN) -AsPlainText -Force
        $ConnectOTLPParameters.Header['x-tenant-id'] = 'personal'
    $ConnectOTLPParameters.ServiceName = 'powershell-installer'
    $ConnectOTLPParameters.ServiceNamespace = 'GraceSolutions'
    $ConnectOTLPParameters.EnvironmentName = 'production'
    $ConnectOTLPParameters.Transport = [PSOTLP.Common.OTLPTransport]::Http
    $ConnectOTLPParameters.Encoding = [PSOTLP.Common.OTLPEncoding]::Json
    $ConnectOTLPParameters.Compression = [PSOTLP.Common.OTLPCompression]::Gzip
    $ConnectOTLPParameters.ResourceAttribute = New-Object -TypeName 'System.Collections.Specialized.OrderedDictionary' -ArgumentList ([System.StringComparer]::OrdinalIgnoreCase)
        $ConnectOTLPParameters.ResourceAttribute['deployment.environment'] = 'production'
        $ConnectOTLPParameters.ResourceAttribute['team'] = 'observability'
    $ConnectOTLPParameters.RedactPattern = New-Object -TypeName 'System.Collections.Generic.List[System.Text.RegularExpressions.Regex]'
        $ConnectOTLPParameters.RedactPattern.Add([regex]'(?i)x-internal-secret\s*=\s*\S+')
    $ConnectOTLPParameters.RetryCount = 3
    $ConnectOTLPParameters.TimeoutSeconds = 30
    $ConnectOTLPParameters.PassThru = $True
    $ConnectOTLPParameters.Verbose = $True

$ConnectOTLPResult = Connect-OTLP @ConnectOTLPParameters

Write-Output -InputObject ($ConnectOTLPResult)
```

## PARAMETERS

### -EndpointUri
Base OTLP endpoint. When omitted, falls back to `$env:PSOTLP_OTLP_ENDPOINT`,
`$env:OTEL_EXPORTER_OTLP_ENDPOINT`, then `http://localhost:4318`.

```yaml
Type: System.Uri
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: http://localhost:4318
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogsEndpointUri
Signal-specific override for the `/v1/logs` endpoint.

```yaml
Type: System.Uri
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TracesEndpointUri
Signal-specific override for the `/v1/traces` endpoint.

```yaml
Type: System.Uri
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MetricsEndpointUri
Signal-specific override for the `/v1/metrics` endpoint.

```yaml
Type: System.Uri
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Header
`IDictionary` of HTTP headers applied to every OTLP request. Use this for any authentication
header (`Authorization`, `x-api-key`, etc.) and for vendor-specific routing headers. Values
may be `String` or `SecureString`; plain-string values are converted to `SecureString` at
parameter binding.

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

### -Transport
Transport protocol. Only `Http` is supported today.

```yaml
Type: PSOTLP.Common.OTLPTransport
Parameter Sets: (All)
Aliases:
Accepted values: Http, Grpc

Required: False
Position: Named
Default value: Http
Accept pipeline input: False
Accept wildcard characters: False
```

### -Encoding
Wire encoding. One of `Json`, `Protobuf`, `NDJson`.

```yaml
Type: PSOTLP.Common.OTLPEncoding
Parameter Sets: (All)
Aliases:
Accepted values: Json, Protobuf, NDJson

Required: False
Position: Named
Default value: Json
Accept pipeline input: False
Accept wildcard characters: False
```

### -Compression
Wire compression. One of `None`, `Gzip`.

```yaml
Type: PSOTLP.Common.OTLPCompression
Parameter Sets: (All)
Aliases:
Accepted values: None, Gzip

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServiceName
`service.name` resource attribute. Falls back to `$env:PSOTLP_SERVICE_NAME`,
`$env:OTEL_SERVICE_NAME`, then `powershell`.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: powershell
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServiceNamespace
`service.namespace` resource attribute. Defaults to `PSOTLP`.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: PSOTLP
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServiceInstanceId
`service.instance.id` resource attribute. Defaults to a freshly generated GUID.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: (new GUID)
Accept pipeline input: False
Accept wildcard characters: False
```

### -ScopeName
Instrumentation-scope name on emitted records.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ScopeVersion
Instrumentation-scope version on emitted records.

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EnvironmentName
`deployment.environment` resource attribute (e.g. `production`, `staging`).

```yaml
Type: System.String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ResourceAttribute
`IDictionary` of additional resource attributes that ship on every emitted record.

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

### -LogAttribute
`IDictionary` of default log-scope attributes that ship on every emitted log record.

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

### -RedactPattern
Array of `[regex]` patterns evaluated against captured stream bodies and outgoing log
payloads. Matched substrings are replaced with `[REDACTED]`.

```yaml
Type: System.Text.RegularExpressions.Regex[]
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RetryCount
Maximum number of HTTP retries per export call. Range 0-10.

```yaml
Type: System.Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 3
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeoutSeconds
HTTP timeout in seconds per export call. Range 1-600.

```yaml
Type: System.Int32
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: 30
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Emit the non-sensitive `OTLPConnectionView` for the new connection.

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

[Disconnect-OTLP]()

[Get-OTLPConnection]()

[Invoke-OTLPScript]()

