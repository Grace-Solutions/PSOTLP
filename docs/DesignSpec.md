# PSOTLP Full Module Specification

## 1. Project Summary

`PSOTLP` is a C# binary PowerShell module for emitting OpenTelemetry Protocol telemetry from PowerShell.

The module must follow the same implementation discipline as the `PSInfisicalAPI` specification:

* C# binary module
* `.NET Standard 2.0`
* Windows PowerShell 5.1 and PowerShell 7+ support
* Centralized logging
* Centralized error handling
* Centralized endpoint definitions
* Centralized URI construction
* Centralized HTTP request execution
* No async/await
* No duplicated logic
* Strong output types
* Pipeline-friendly cmdlets
* Approved PowerShell verbs
* PSD1 and PSM1
* Idempotent build script
* Version format `yyyy.MM.dd.HHmm`
* Commit hash embedded in module metadata

The module is not meant to be a generic wrapper around every OpenTelemetry feature on day one. The initial goal is to establish a strong OTLP framework and implement PowerShell-focused log/session telemetry first.

Initial primary focus:

```powershell
Connect-OTLP
Disconnect-OTLP
Get-OTLPConnection
Write-OTLPLog
Send-OTLPLogBatch
Invoke-OTLPScript
```

Future supported signal areas:

```text
Logs
Traces
Metrics
Profiles, only if/when the OpenTelemetry profile signal becomes stable enough to justify support
```

Initial supported signal:

```text
Logs
```

Initial transport:

```text
OTLP/HTTP
```

Future transport:

```text
OTLP/gRPC
```

---

## 2. Design Goal

`PSOTLP` should allow PowerShell users to emit structured telemetry without rewriting every script, function, module, or CLI call.

The main user experience is connection-scoped explicit logging and on-demand script capture:

```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ Authorization = (ConvertTo-SecureString 'Bearer token' -AsPlainText -Force) }

Write-OTLPLog -Body 'Starting device onboarding' -Severity Information -Attribute @{ Phase = 'PreExecution'; Customer = 'Personal' }

Invoke-OTLPScript -ScriptBlock {
    Get-Service
    Write-Verbose 'Service query completed'
} -ServiceName 'powershell-script' -SessionName 'ServiceAudit' -Verbose

Disconnect-OTLP
```

The module also supports explicit structured batches:

```powershell
$records | Send-OTLPLogBatch
```

---

## 3. Non-Negotiable Requirements

## 3.1 Runtime

The module must target:

```text
.NET Standard 2.0
PowerShellStandard.Library
Windows PowerShell 5.1
PowerShell 7+
```

## 3.2 No Async/Await

The source code must contain no usage of:

```csharp
async
await
```

All operations must be synchronous:

```text
HTTP execution
Queue draining
Batch export
File tailing
Serialization
Session capture
Build script actions
```

If background behavior is needed, use explicit synchronous worker/thread constructs, timers, or runspace-safe designs without `async` or `await`.

## 3.3 Centralized Reuse

The following must be centralized:

```text
Logging
Error handling
Endpoint definitions
URI construction
HTTP request execution
Request retry logic
Header sanitization
Payload serialization
Payload compression
Stream capture mapping
Queue management
Batching
Redaction
Resource attribute creation
Scope attribute creation
Signal model creation
Version metadata
Build/release actions
```

No cmdlet should independently build OTLP payloads, URLs, retry logic, or request headers.

## 3.4 Approved Verbs

Public cmdlets must use approved PowerShell verbs.

Initial cmdlets:

```powershell
Connect-OTLP
Disconnect-OTLP
Get-OTLPConnection
Write-OTLPLog
Send-OTLPLogBatch
Invoke-OTLPScript
```

Reserved future cmdlets:

```powershell
Start-OTLPSpan
Stop-OTLPSpan
Write-OTLPMetric
Send-OTLPTraceBatch
Send-OTLPMetricBatch
Export-OTLPPayload
Import-OTLPConfiguration
Set-OTLPConfiguration
Get-OTLPConfiguration
Test-OTLPConnection
```

---

## 4. Initial Scope

The initial implementation must support:

```text
OTLP/HTTP log export
JSON Protobuf encoding
Optional gzip compression
Bearer/API-key/custom header authentication
Connection reuse
PowerShell session capture
PowerShell stream-to-log mapping
Manual structured log records
Batching
Retry logic
Redaction rules
Centralized logging/error handling
Idempotent build/release flow
```

The initial implementation does not need to support:

```text
OTLP/gRPC
Full metrics
Full traces
Profiles
Automatic .NET Activity instrumentation
Automatic PowerShell function-level tracing
Automatic CLI child-process stream capture beyond what the host/session can observe
```

Those should remain future extension points.

---

## 5. OTLP Transport Design

## 5.1 Initial Transport

Initial transport must be:

```text
OTLP/HTTP
```

Default endpoint:

```text
http://localhost:4318
```

Default signal paths:

```text
Logs:    /v1/logs
Traces:  /v1/traces
Metrics: /v1/metrics
```

Initial implementation should export logs to:

```text
{EndpointUri}/v1/logs
```

Unless overridden by:

```powershell
-LogsEndpointUri
```

Or unless signal-path suffixing is suppressed by:

```powershell
-NoSignalPath
```

When `-NoSignalPath` is set, every signal sends to `{EndpointUri}` exactly as supplied
(per-signal overrides still win when present). Use this when the endpoint is a proxy,
collector, or gateway that already routes to the correct signal sink.

## 5.2 Supported Payload Encoding

Initial encoding:

```text
JSON Protobuf encoding
```

Required header:

```text
Content-Type: application/json
```

Future encoding:

```text
Binary Protobuf encoding
```

Future header:

```text
Content-Type: application/x-protobuf
```

## 5.3 Compression

Supported compression modes:

```text
None
Gzip
```

Default:

```text
None
```

When gzip is enabled:

```text
Content-Encoding: gzip
Accept-Encoding: gzip
```

## 5.4 Retry Behavior

Retryable HTTP status codes:

```text
429
502
503
504
```

Retry strategy:

```text
Synchronous retry loop
Exponential backoff
Jitter
Retry-After header respected when present
Maximum retry count configurable
Maximum retry delay configurable
```

Default retry values:

```text
RetryCount: 3
InitialRetryDelayMilliseconds: 500
MaximumRetryDelayMilliseconds: 10000
```

HTTP `400 Bad Request` must not be retried.

Other `4xx` responses must not be retried by default.

---

## 6. Repository Structure

```text
PSOTLP/
├── Artifacts/
├── Module/
│   └── PSOTLP/
│       ├── PSOTLP.psd1
│       ├── PSOTLP.psm1
│       ├── PSOTLP.Format.ps1xml
│       ├── PSOTLP.Types.ps1xml
│       └── bin/
│           ├── PSOTLP.dll
│           ├── Newtonsoft.Json.dll
│           └── Google.Protobuf.dll
├── Releases/
│   └── yyyy.MM.dd.HHmm/
├── docs/
│   ├── about_PSOTLP.help.txt
│   ├── Connect-OTLP.md
│   ├── Disconnect-OTLP.md
│   ├── Get-OTLPConnection.md
│   ├── Write-OTLPLog.md
│   ├── Send-OTLPLogBatch.md
│   └── Invoke-OTLPScript.md
├── src/
│   ├── PSOTLP/
│   │   ├── Authentication/
│   │   ├── Batching/
│   │   ├── Cmdlets/
│   │   ├── Common/
│   │   ├── Connections/
│   │   ├── Endpoints/
│   │   ├── Errors/
│   │   ├── Exporters/
│   │   ├── Http/
│   │   ├── Logging/
│   │   ├── Models/
│   │   ├── Redaction/
│   │   ├── Resources/
│   │   ├── Serialization/
│   │   ├── Sessions/
│   │   ├── Signals/
│   │   └── Streams/
│   └── PSOTLP.Tests/
├── build.ps1
├── CHANGELOG.md
└── README.md
```

Source must start under `/src`.

Namespaces should follow folder responsibility:

```text
PSOTLP.Authentication
PSOTLP.Batching
PSOTLP.Cmdlets
PSOTLP.Connections
PSOTLP.Endpoints
PSOTLP.Exporters
PSOTLP.Redaction
PSOTLP.Sessions
PSOTLP.Streams
```

---

## 7. Module Manifest and Loader

## 7.1 PSD1

The `.psd1` must be generated by the build script.

Required shape:

```powershell
@{
    RootModule = 'PSOTLP.psm1'
    ModuleVersion = 'yyyy.MM.dd.HHmm'
    GUID = '<stable-guid>'
    Author = 'Alphaeus Mote'
    CompanyName = ''
    Copyright = ''
    PowerShellVersion = '5.1'
    CompatiblePSEditions = @('Desktop', 'Core')
    FunctionsToExport = @()
    CmdletsToExport = @(
        'Connect-OTLP',
        'Disconnect-OTLP',
        'Get-OTLPConnection',
        'Write-OTLPLog',
        'Send-OTLPLogBatch',
        'Invoke-OTLPScript'
    )
    AliasesToExport = @()
    PrivateData = @{
        PSData = @{
            Tags = @('OpenTelemetry', 'OTLP', 'Logs', 'Tracing', 'PowerShell', 'Observability')
            ProjectUri = ''
            ReleaseNotes = ''
            CommitHash = '<git-commit-hash>'
        }
    }
}
```

## 7.2 PSM1

The `.psm1` must only load the binary and optional type/format data.

```powershell
$BinaryPath = [System.IO.FileInfo][System.IO.Path]::Combine($PSScriptRoot, 'bin', 'PSOTLP.dll')

Import-Module -Name $BinaryPath.FullName

$TypesPath = [System.IO.FileInfo][System.IO.Path]::Combine($PSScriptRoot, 'PSOTLP.Types.ps1xml')
$FormatPath = [System.IO.FileInfo][System.IO.Path]::Combine($PSScriptRoot, 'PSOTLP.Format.ps1xml')

if ([System.IO.File]::Exists($TypesPath.FullName)) {
    Update-TypeData -PrependPath $TypesPath.FullName -ErrorAction SilentlyContinue
}

if ([System.IO.File]::Exists($FormatPath.FullName)) {
    Update-FormatData -PrependPath $FormatPath.FullName -ErrorAction SilentlyContinue
}
```

---

## 8. Versioning

Version format:

```text
yyyy.MM.dd.HHmm
```

Example:

```text
2026.06.19.1425
```

The version must be generated once per build and applied to:

```text
PSD1 ModuleVersion
AssemblyVersion
AssemblyFileVersion
AssemblyInformationalVersion
Release folder
CHANGELOG.md
Generated docs if applicable
```

Commit hash must be embedded separately:

```text
PSD1 PrivateData.PSData.CommitHash
AssemblyMetadata("CommitHash", "<commit-hash>")
AssemblyInformationalVersion = "yyyy.MM.dd.HHmm+<commit-hash>"
```

---

## 9. Idempotent Build Script

The build script must be repeatable and safe to run multiple times.

## 9.1 Build Parameters

```powershell
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean,

    [switch]$Restore,

    [switch]$RunTests,

    [switch]$RunIntegrationTests,

    [switch]$CreateRelease,

    [switch]$CommitOnSuccess,

    [switch]$Force
)
```

## 9.2 Required Build Behavior

```text
Create missing folders.
Clean generated output when -Clean is used.
Never delete source files.
Generate version once.
Read current git commit hash.
Restore packages when requested.
Build the C# project.
Run unit tests when requested.
Run integration tests only when explicitly requested.
Generate PSD1.
Generate PSM1 if missing or when -Force is used.
Copy compiled DLLs to Module/PSOTLP/bin.
Copy dependency DLLs to Module/PSOTLP/bin.
Copy type and format files.
Update CHANGELOG.md.
Create Releases/yyyy.MM.dd.HHmm when requested.
Overwrite same-version release only when -Force is used.
Validate module import in Windows PowerShell 5.1 where available.
Validate module import in PowerShell 7+ where available.
Commit only after successful build when -CommitOnSuccess is specified.
Never commit failed builds.
```

## 9.3 Example Build Commands

```powershell
.\build.ps1 -Clean -Restore -RunTests -CreateRelease
```

```powershell
.\build.ps1 -Clean -Restore -RunTests -RunIntegrationTests -CreateRelease -CommitOnSuccess
```

---

## 10. Test Configuration

Integration tests should support environment-variable based configuration.

Recommended variables:

```text
PSOTLP_ENDPOINT_URI
PSOTLP_LOGS_ENDPOINT_URI
PSOTLP_HEADERS
PSOTLP_AUTHORIZATION_HEADER
PSOTLP_SERVICE_NAME
PSOTLP_SERVICE_NAMESPACE
PSOTLP_SERVICE_INSTANCE_ID
PSOTLP_ENVIRONMENT
PSOTLP_COMPRESSION
PSOTLP_EXPORT_PROTOCOL
```

Optional HyperDX-style values:

```text
PSOTLP_HYPERDX_ENDPOINT_URI
PSOTLP_HYPERDX_API_KEY
```

Integration tests must not run by default.

Integration tests run only when:

```powershell
.\build.ps1 -RunIntegrationTests
```

Secrets and tokens from test environment variables must never be logged.

---

## 11. Logging Specification

Internal module logging must be centralized.

Format:

```text
[UTC Timestamp] - [Level] - [Component] - Message
```

Example:

```text
[2026-06-19T18:44:22.1830000Z] - [Information] - [OTLPHttpExporter] - Attempting to export OTLP log batch. Please Wait...
[2026-06-19T18:44:22.9290000Z] - [Information] - [OTLPHttpExporter] - OTLP log batch export was successful.
[2026-06-19T18:44:22.9330000Z] - [Error] - [OTLPHttpExporter] - OTLP log batch export failed.
```

## 11.1 Rules

```text
Log before meaningful operations.
Log after successful operations.
Log after failed operations.
Use Please Wait... where applicable.
Respect -Verbose.
Never log authentication headers.
Never log bearer tokens.
Never log API keys.
Never log sensitive telemetry attributes.
Never log raw telemetry payloads by default.
```

## 11.2 PowerShell Channels

```text
Verbose -> WriteVerbose
Debug -> WriteDebug
Warning -> WriteWarning
Error -> WriteError
```

Warning exists as a level but should not be used for intentional export/send operations.

---

## 12. Error Handling Specification

All errors must flow through centralized error handling.

Required types:

```text
OTLPException
OTLPConfigurationException
OTLPConnectionException
OTLPHttpException
OTLPSerializationException
OTLPExportException
OTLPSessionException
OTLPStreamCaptureException
OTLPRedactionException
OTLPErrorDetails
OTLPErrorHandler
```

## 12.1 Error Details

Errors should preserve:

```text
Component
Operation
Message
Exception type
Inner exception message
HTTP status code
HTTP reason phrase
Retry attempt count
Endpoint name
Endpoint URI without sensitive query/header values
Signal type
Encoding
Compression mode
Serialization error position if available
```

## 12.2 Error Logging

Example:

```text
[UTC] - [Error] - [OTLPErrorHandler] - Operation failed: ExportLogs
[UTC] - [Error] - [OTLPErrorHandler] - Error Component: OTLPHttpExporter
[UTC] - [Error] - [OTLPErrorHandler] - Error Message: The OTLP endpoint returned Service Unavailable.
[UTC] - [Error] - [OTLPErrorHandler] - HTTP Status Code: 503
[UTC] - [Error] - [OTLPErrorHandler] - Retry Attempts: 3
```

Cmdlets must emit proper `ErrorRecord` objects.

---

## 13. Endpoint Registry

Endpoint definitions must be centralized.

No cmdlet may hard-code signal paths.

## 13.1 Endpoint Definition Model

```csharp
public sealed class OTLPEndpointDefinition
{
    public string Name { get; set; }
    public OTLPSignalType SignalType { get; set; }
    public string Method { get; set; }
    public string DefaultPath { get; set; }
    public string DefaultContentType { get; set; }
    public bool SupportsCompression { get; set; }
    public bool RequiresAuthorization { get; set; }
}
```

## 13.2 Initial Endpoint Definitions

```text
ExportLogs:
  SignalType: Logs
  Method: POST
  DefaultPath: /v1/logs
  DefaultContentType: application/json
  SupportsCompression: true

ExportTraces:
  SignalType: Traces
  Method: POST
  DefaultPath: /v1/traces
  DefaultContentType: application/json
  SupportsCompression: true
  Future: true

ExportMetrics:
  SignalType: Metrics
  Method: POST
  DefaultPath: /v1/metrics
  DefaultContentType: application/json
  SupportsCompression: true
  Future: true
```

---

## 14. URI and Path Handling

## 14.1 URI Rules

All URLs must use:

```csharp
System.Uri
System.UriBuilder
```

URI construction must be centralized:

```text
OTLPUriBuilder
```

Responsibilities:

```text
Combine base endpoint and signal path.
Respect signal-specific endpoint overrides.
Preserve scheme, host, port, and path.
Avoid duplicate slashes.
Escape query values if query values are ever used.
Avoid manual URL string concatenation in cmdlets.
```

## 14.2 Path Rules

All internal paths must use:

```csharp
System.IO.FileInfo
System.IO.DirectoryInfo
System.IO.Path.Combine(...)
```

PowerShell scripts must use:

```powershell
[System.IO.FileInfo][System.IO.Path]::Combine(...)
[System.IO.DirectoryInfo][System.IO.Path]::Combine(...)
```

Public examples may use simple paths and strings.

---

## 15. Authentication and Headers

`PSOTLP` should not assume one vendor authentication pattern.

Supported authentication/header patterns:

```text
No authentication
Bearer token
API key header
Custom headers
Environment variable driven headers
```

## 15.1 Connect-OTLP Parameter Surface

`Connect-OTLP` exposes a single parameter set. All authentication headers — bearer tokens,
API keys, vendor-specific values — are supplied through the `-Headers` dictionary, which accepts
any `IDictionary` implementation (`Hashtable`, `OrderedDictionary`, etc.). Values may be
`String` or `SecureString`; plain strings are converted to `SecureString` at parameter binding.

```powershell
Connect-OTLP `
    -EndpointUri <Uri> `
    [-Headers <IDictionary>] `
    [-LogsEndpointUri <Uri>] `
    [-TracesEndpointUri <Uri>] `
    [-MetricsEndpointUri <Uri>] `
    [-NoSignalPath] `
    [-ServiceName <string>] `
    [-ServiceNamespace <string>] `
    [-ServiceInstanceId <string>] `
    [-ScopeName <string>] `
    [-ScopeVersion <string>] `
    [-ScopeAttributes <IDictionary>] `
    [-EnvironmentName <string>] `
    [-Compression <None|Gzip>] `
    [-Encoding <Json|Protobuf|NDJson>] `
    [-PassThru]
```

Common header shapes:

```powershell
# No authentication
Connect-OTLP -EndpointUri 'https://otel.example.com'

# Bearer token (callers prepend "Bearer " themselves so the dictionary is verbatim)
$bearer = ConvertTo-SecureString -String 'Bearer eyJhbGciOi...' -AsPlainText -Force
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ Authorization = $bearer }

# Vendor API key (HyperDX style)
$key = Read-Host -AsSecureString
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ authorization = $key }

# Multiple headers (ordered, vendor + tenant)
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers ([ordered]@{
    Authorization = $bearer
    'x-tenant-id' = 'personal'
})
```

## 15.2 Header Rules

```text
All header values are treated as potentially sensitive.
Header values are stored as SecureString.
Plaintext values supplied through -Headers are converted to SecureString at parameter binding.
Never log header values.
Never display header values.
Convert SecureString to plaintext only at the HTTP request creation boundary.
Clear temporary plaintext as aggressively as practical.
Bearer tokens, API keys, and other authentication values share the same Headers collection.
```

---

## 16. Connection Model

## 16.1 Session Manager

```csharp
public static class OTLPSessionManager
{
    public static OTLPConnection CurrentConnection { get; }

    public static void SetCurrentConnection(OTLPConnection connection);

    public static OTLPConnection RequireCurrentConnection();

    public static void Disconnect();
}
```

## 16.2 Connection Object

```csharp
public sealed class OTLPConnection
{
    public Uri EndpointUri { get; set; }
    public Uri LogsEndpointUri { get; set; }
    public Uri TracesEndpointUri { get; set; }
    public Uri MetricsEndpointUri { get; set; }
    public bool NoSignalPath { get; set; }

    public OTLPTransport Transport { get; set; }
    public OTLPEncoding Encoding { get; set; }
    public OTLPCompression Compression { get; set; }
    public OTLPAuthenticationMode AuthenticationMode { get; set; }

    public string ServiceName { get; set; }
    public string ServiceNamespace { get; set; }
    public string ServiceInstanceId { get; set; }
    public string EnvironmentName { get; set; }

    public DateTimeOffset ConnectedAtUtc { get; set; }
    public bool IsConnected { get; set; }

    internal IDictionary<string, SecureString> Headers { get; set; }
}
```

Header values may not be displayed or serialized.

---

## 17. Resource Attributes

Each exported payload must include resource attributes.

Default resource attributes:

```text
service.name
service.namespace
service.instance.id
deployment.environment.name
host.name
os.type
os.description
process.pid
process.executable.name
process.command
process.runtime.name
process.runtime.version
telemetry.sdk.name
telemetry.sdk.language
telemetry.sdk.version
telemetry.distro.name
telemetry.distro.version
```

PowerShell-specific resource attributes:

```text
powershell.version
powershell.edition
powershell.host.name
powershell.host.version
powershell.runspace.id
powershell.process.architecture
```

Custom resource attributes must be supported:

```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -ServiceName 'cloudinit' -ResourceAttribute @{ Customer = 'Personal'; Site = 'Lab' }
```

---

## 18. Instrumentation Scope

Initial scope defaults:

```text
scope.name: PSOTLP
scope.version: module version
```

For session capture:

```text
scope.name: PSOTLP.Session
scope.version: module version
```

For manual logs:

```text
scope.name: PSOTLP.Manual
scope.version: module version
```

For script invocation:

```text
scope.name: PSOTLP.Invoke
scope.version: module version
```

---

## 19. Log Record Model

## 19.1 Public Model

```csharp
public sealed class OTLPLogRecord
{
    public DateTimeOffset TimestampUtc { get; set; }
    public DateTimeOffset ObservedTimestampUtc { get; set; }
    public OTLPSeverity Severity { get; set; }
    public string SeverityText { get; set; }
    public int SeverityNumber { get; set; }
    public string Body { get; set; }
    public Dictionary<string, object> Attributes { get; set; }
    public string TraceId { get; set; }
    public string SpanId { get; set; }
    public string EventName { get; set; }
}
```

## 19.2 Severity Enum

```csharp
public enum OTLPSeverity
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Fatal
}
```

Severity mapping should be centralized.

Suggested default mapping:

```text
Trace       -> TRACE / 1
Debug       -> DEBUG / 5
Information -> INFO  / 9
Warning     -> WARN  / 13
Error       -> ERROR / 17
Fatal       -> FATAL / 21
```

---

## 20. PowerShell Stream Mapping

Stream-to-log mapping must be centralized.

Initial mapping:

```text
Success/Output -> Information
Information    -> Information
Verbose        -> Debug
Debug          -> Debug
Warning        -> Warning
Error          -> Error
Progress       -> Information
Host           -> Information
Native stdout  -> Information
Native stderr  -> Error
```

Required attributes:

```text
powershell.stream.name
powershell.stream.id
powershell.command.name
powershell.script.path
powershell.pipeline.id
powershell.runspace.id
powershell.session.id
powershell.session.name
```

---

## 21. Session Capture Strategy

The module captures PowerShell activity only when the user explicitly opts in
through `Invoke-OTLPScript`. The cmdlet creates a fresh hosted runspace and
attaches `PSDataCollection.DataAdded` handlers to every stream so that capture
is fully in-memory, cleans up automatically, and never interferes with any
transcript the caller may already have running.

`Invoke-OTLPScript` captures:

```text
Output
Information
Verbose
Debug
Warning
Error
Progress where practical
Native stdout where practical
Native stderr where practical
```

No transcript file, transcript tailer, or process-wide session registry is
created. Each invocation owns its queue and disposes it before returning.

---

## 22. Redaction

Redaction must be centralized.

Required type:

```text
OTLPRedactionEngine
```

Default redaction should support:

```text
Authorization headers
Bearer tokens
API keys
Password-like key/value pairs
Secret-like key/value pairs
Connection strings
Cloud tokens
Private keys
Environment variables matching sensitive names
```

Default sensitive name patterns:

```text
password
passwd
pwd
secret
token
apikey
api_key
accesskey
access_key
clientsecret
client_secret
authorization
credential
sas
connectionstring
connection_string
```

Configurable redaction:

Additional redaction patterns are supplied to `Connect-OTLP` as a
`System.Text.RegularExpressions.Regex[]` and are stored on the active
`OTLPConnection`. Every exporter that the module builds reads the
connection's `RedactPatterns` collection and seeds the centralized
`OTLPRedactionEngine` with them in addition to the built-in defaults.

```powershell
$patterns = @(
    [regex]'(?i)x-custom-secret\s*[:=]\s*[^\s;]+',
    [regex]'(?i)mytenant_[A-Z0-9]{32}'
)
Connect-OTLP -EndpointUri 'https://otel.example.com' -RedactPattern $patterns
```

Redaction replacement:

```text
[REDACTED]
```

No raw unredacted log bodies should be exported when redaction is enabled.

Default:

```text
Redaction enabled
```

---

## 23. Queue and Batch Design

Telemetry should not send one HTTP request per line by default.

Required components:

```text
OTLPLogQueue
OTLPBatchBuilder
OTLPBatchExporter
OTLPRetryPolicy
```

Defaults:

```text
BatchSize: 100
FlushIntervalSeconds: 5
MaxQueueSize: 10000
DropPolicy: DropOldest
RetryCount: 3
```

Drop policies:

```csharp
public enum OTLPQueueDropPolicy
{
    DropOldest,
    DropNewest,
    Block
}
```

Since async/await is forbidden, the queue worker must use synchronous logic.

`Invoke-OTLPScript` drains its in-memory queue before returning. `Send-OTLPLogBatch` flushes the supplied batch synchronously.

---

## 24. Cmdlet Specifications

# 24.1 Connect-OTLP

## Purpose

Create and store a reusable OTLP connection.

## Parameters

```powershell
Connect-OTLP `
    -EndpointUri <Uri> `
    [-LogsEndpointUri <Uri>] `
    [-TracesEndpointUri <Uri>] `
    [-MetricsEndpointUri <Uri>] `
    [-NoSignalPath] `
    [-Headers <IDictionary>] `
    [-ServiceName <string>] `
    [-ServiceNamespace <string>] `
    [-ServiceInstanceId <string>] `
    [-ScopeName <string>] `
    [-ScopeVersion <string>] `
    [-ScopeAttributes <IDictionary>] `
    [-EnvironmentName <string>] `
    [-ResourceAttribute <IDictionary>] `
    [-LogAttribute <IDictionary>] `
    [-RedactPattern <Regex[]>] `
    [-Compression <None|Gzip>] `
    [-Encoding <Json|Protobuf|NDJson>] `
    [-RetryCount <int>] `
    [-TimeoutSeconds <int>] `
    [-PassThru]
```

## Defaults

```text
EndpointUri: http://localhost:4318
LogsEndpointUri: EndpointUri + /v1/logs
Compression: None
Encoding: Json
ServiceName: powershell
ServiceNamespace: PSOTLP
ServiceInstanceId: generated GUID
RetryCount: 3
TimeoutSeconds: 30
```

## Behavior

```text
Validate EndpointUri.
Build signal endpoints centrally.
Store connection in current connection manager.
Store all header values as SecureString.
Create default resource attributes.
Return connection only with -PassThru.
```

## Example

```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -ServiceName 'powershell-session' -EnvironmentName 'production' -Verbose
```

Bearer token example (callers prepend `Bearer ` themselves so the dictionary is stored as-is):

```powershell
$Token  = Read-Host -Prompt 'Bearer Token' -AsSecureString
$bearer = ConvertTo-SecureString -String ('Bearer ' +
    [System.Net.NetworkCredential]::new('', $Token).Password) -AsPlainText -Force
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ Authorization = $bearer } -ServiceName 'cloudinit'
```

Custom header example (all header values are stored as `SecureString`):

```powershell
$ApiKey = Read-Host -Prompt 'API Key' -AsSecureString
Connect-OTLP -EndpointUri 'https://in-otel.example.com' -Headers @{ Authorization = $ApiKey } -ServiceName 'powershell-session'
```

Custom redaction example:

```powershell
$patterns = @([regex]'(?i)x-custom-secret\s*[:=]\s*[^\s;]+')
Connect-OTLP -EndpointUri 'https://otel.example.com' -RedactPattern $patterns
```

---

# 24.2 Disconnect-OTLP

## Purpose

Clear the current OTLP connection.

## Parameters

```powershell
Disconnect-OTLP [-PassThru]
```

## Behavior

```text
Clear current connection.
Clear header/token references.
Return nothing by default.
```

---

# 24.3 Get-OTLPConnection

## Purpose

Return current OTLP connection metadata.

## Parameters

```powershell
Get-OTLPConnection
```

## Behavior

```text
Return current connection metadata.
Do not include header values.
Do not include tokens.
```

---

# 24.4 Write-OTLPLog

## Purpose

Emit one structured OTLP log record.

## Parameters

```powershell
Write-OTLPLog `
    -Body <string> `
    [-Severity <Trace|Debug|Information|Warning|Error|Fatal>] `
    [-Attribute <hashtable>] `
    [-EventName <string>] `
    [-TimestampUtc <DateTimeOffset>] `
    [-TraceId <string>] `
    [-SpanId <string>] `
    [-PassThru]
```

## Defaults

```text
Severity: Information
TimestampUtc: DateTimeOffset.UtcNow
ObservedTimestampUtc: DateTimeOffset.UtcNow
```

## Behavior

```text
Require current connection.
Create OTLPLogRecord.
Apply redaction.
Queue record.
Flush according to batching policy.
Return record only with -PassThru.
```

Example:

```powershell
Write-OTLPLog -Body 'Starting bootstrap' -Severity Information -Attribute @{ Phase = 'PreExecution'; Script = 'CloudbaseInit' }
```

---

# 24.5 Send-OTLPLogBatch

## Purpose

Send an explicit batch of OTLP log records.

## Parameters

```powershell
Send-OTLPLogBatch `
    -InputObject <OTLPLogRecord[]> `
    [-PassThru]
```

## Pipeline

```text
InputObject accepts pipeline input.
```

## Behavior

```text
Require current connection.
Serialize records to OTLP payload.
Send payload to logs endpoint.
Apply retry policy.
Return export result only with -PassThru.
```

---

# 24.6 Invoke-OTLPScript

## Purpose

Execute a script block in an isolated hosted runspace and emit each captured
PowerShell stream record as an OTLP log. Capture is fully in-memory and never
writes a transcript or interferes with any transcript the caller may already
have running.

## Parameters

```powershell
Invoke-OTLPScript `
    -ScriptBlock <scriptblock> `
    [-ArgumentList <object[]>] `
    [-SessionName <string>] `
    [-ServiceName <string>] `
    [-Attribute <IDictionary>] `
    [-BatchSize <int>] `
    [-PassThru]
```

## Behavior

```text
Require current connection.
Create a fresh hosted PowerShell runspace.
Attach DataAdded handlers to every stream.
Map each captured record to an OTLP log record.
Apply connection-level redaction patterns.
Drain the in-memory queue to the log exporter before returning.
Preserve normal script output behavior where practical.
```

Example:

```powershell
Invoke-OTLPScript -SessionName 'ServiceAudit' -ScriptBlock {
    Get-Service
    Write-Verbose 'Completed service query'
} -Verbose
```

---

## 25. Payload Model

Initial internal payload should follow OTLP logs JSON shape.

Top-level layout:

```text
resourceLogs[]
  resource
    attributes[]
  scopeLogs[]
    scope
    logRecords[]
```

Required internal model types:

```text
OTLPAnyValue
OTLPKeyValue
OTLPResource
OTLPInstrumentationScope
OTLPResourceLogs
OTLPScopeLogs
OTLPLogRecordPayload
OTLPExportLogsServiceRequest
OTLPExportLogsServiceResponse
OTLPPartialSuccess
```

JSON serialization must use lowerCamelCase field names.

64-bit integer timestamp fields must be encoded as decimal strings when required by OTLP JSON rules.

---

## 26. Serialization

Required serializers:

```text
IOTLPSerializer
OTLPJsonSerializer
OTLPProtobufSerializer, future
```

Initial:

```text
OTLPJsonSerializer
```

Rules:

```text
No raw payload logging by default.
Optional payload dump only through explicit diagnostic switch in test/dev builds.
Payload dump must pass through redaction.
Serialization errors must identify component and operation.
```

---

## 27. HTTP Client

Required interface:

```csharp
public interface IOTLPHttpClient
{
    OTLPHttpResponse Send(OTLPHttpRequest request);
}
```

Request model:

```csharp
public sealed class OTLPHttpRequest
{
    public string OperationName { get; set; }
    public string EndpointName { get; set; }
    public OTLPSignalType SignalType { get; set; }
    public string Method { get; set; }
    public Uri Uri { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public byte[] Body { get; set; }
    public bool BodyMayContainSensitiveData { get; set; }
}
```

Response model:

```csharp
public sealed class OTLPHttpResponse
{
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; }
    public byte[] Body { get; set; }
    public Dictionary<string, string> Headers { get; set; }

    public void Clear()
    {
        Body = null;
        Headers?.Clear();
    }
}
```

Rules:

```text
Use synchronous HTTP calls.
Keep connection alive where practical.
Respect timeout.
Support gzip request body.
Support gzip response body where practical.
Clear request/response bodies as practical.
```

---

## 28. Session Objects

## 28.1 OTLPSession

The `OTLPSession` type is an internal accounting record used by
`Invoke-OTLPScript` to track how many stream events were captured, exported,
and dropped during a single hosted invocation. It is not exposed through any
public cmdlet.

```csharp
public sealed class OTLPSession
{
    public Guid SessionId { get; set; }
    public string SessionName { get; set; }
    public string ServiceName { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? StoppedAtUtc { get; set; }
    public int RecordsCaptured { get; set; }
    public int RecordsExported { get; set; }
    public int RecordsDropped { get; set; }
    public bool IsActive { get; set; }
}
```

---

## 29. Export Result Object

```csharp
public sealed class OTLPExportResult
{
    public OTLPSignalType SignalType { get; set; }
    public Uri EndpointUri { get; set; }
    public int StatusCode { get; set; }
    public string ReasonPhrase { get; set; }
    public int AttemptCount { get; set; }
    public int RecordCount { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset ExportedAtUtc { get; set; }
}
```

---

## 30. Type and Format Data

`OTLPConnection` default display:

```text
EndpointUri
LogsEndpointUri
ServiceName
EnvironmentName
Compression
Encoding
IsConnected
ConnectedAtUtc
```

`OTLPExportResult` default display:

```text
SignalType
EndpointUri
StatusCode
RecordCount
AttemptCount
Success
ExportedAtUtc
```

Do not display:

```text
Headers
RawPayload
```

---

## 31. Documentation Requirements

Every public cmdlet must have help and examples.

Required docs:

```text
about_PSOTLP.help.txt
Connect-OTLP.md
Disconnect-OTLP.md
Get-OTLPConnection.md
Write-OTLPLog.md
Send-OTLPLogBatch.md
Invoke-OTLPScript.md
```

Each help page must include:

```text
Synopsis
Description
Parameters
Inputs
Outputs
Examples
Notes
```

Examples should be clean and user-friendly.

Do not force examples to show internal `FileInfo`, `DirectoryInfo`, `Path.Combine`, `UriBuilder`, or serializer internals.

---

## 32. Testing Requirements

## 32.1 Unit Tests

Minimum tests:

```text
Build script is idempotent.
Manifest version matches assembly version.
Manifest commit hash exists.
PSM1 imports binary from bin folder.
Endpoint registry returns ExportLogs endpoint.
Endpoint registry returns ExportTraces endpoint.
Endpoint registry returns ExportMetrics endpoint.
URI builder builds /v1/logs correctly.
URI builder respects custom LogsEndpointUri.
URI builder returns base endpoint unchanged when NoSignalPath is set.
Logger uses required UTC format.
Logger does not log Authorization header.
Logger does not log API key header.
Error handler preserves HTTP status code.
Error handler sanitizes headers.
Redaction engine redacts password-like values.
Redaction engine redacts token-like values.
Severity mapper maps PowerShell streams correctly.
Write-OTLPLog creates OTLPLogRecord.
Send-OTLPLogBatch serializes resourceLogs.
Send-OTLPLogBatch uses POST.
Send-OTLPLogBatch retries 429.
Send-OTLPLogBatch retries 502.
Send-OTLPLogBatch retries 503.
Send-OTLPLogBatch retries 504.
Send-OTLPLogBatch does not retry 400.
Invoke-OTLPScript drains its in-memory queue before returning.
Invoke-OTLPScript captures records from every PowerShell stream.
```

## 32.2 Integration Tests

Integration tests must be explicit.

Required test variables:

```text
PSOTLP_ENDPOINT_URI
```

Optional:

```text
PSOTLP_LOGS_ENDPOINT_URI
PSOTLP_AUTHORIZATION_HEADER
PSOTLP_SERVICE_NAME
PSOTLP_HYPERDX_ENDPOINT_URI
PSOTLP_HYPERDX_API_KEY
```

Integration tests should verify:

```text
Connect-OTLP works.
Write-OTLPLog exports successfully.
Send-OTLPLogBatch exports successfully.
Invoke-OTLPScript exports captured stream records.
Retry behavior handles a temporary failing endpoint.
Header values are never logged.
```

---

## 33. Implementation Milestones

## Milestone 1: Skeleton

```text
Repository structure
C# project
Test project
Initial build.ps1
PSD1 generation
PSM1 generation
Version generation
Commit hash embedding
CHANGELOG.md
```

## Milestone 2: Core Infrastructure

```text
Central logger
Central error types
Central error handler
Endpoint registry
URI builder
Header sanitizer
SecureString utility
Redaction engine
Path utility
```

## Milestone 3: HTTP and Serialization

```text
Synchronous HTTP client
Request/response models
JSON serializer
Gzip compression
Retry policy
Export result model
```

## Milestone 4: Connection

```text
OTLPConnection model
Connection manager
Connect-OTLP
Disconnect-OTLP
Get-OTLPConnection
Authentication/header modes
```

## Milestone 5: Manual Logs

```text
OTLPLogRecord model
Severity mapping
Resource attributes
Instrumentation scope
Write-OTLPLog
Send-OTLPLogBatch
```

## Milestone 6: Script Invocation Capture

```text
Invoke-OTLPScript
In-memory stream capture
Stream-to-severity mapping
Script output preservation
Final batch flushing
```

## Milestone 7: Docs and Release

```text
External help
README examples
Type formatting
Format views
Unit tests
Integration tests
Import validation in PS 5.1
Import validation in PS 7+
Release packaging
Commit-on-success workflow
```

---

## 34. Acceptance Criteria

The project is acceptable only when all are true:

```text
The module is named PSOTLP.
The module is C# based.
The module targets .NET Standard 2.0.
The module works in Windows PowerShell 5.1.
The module works in PowerShell 7+.
No async keyword exists in source.
No await keyword exists in source.
All public cmdlets use approved verbs.
All public cmdlets have help.
All public cmdlets have examples.
Connect-OTLP creates a reusable current connection.
Disconnect-OTLP clears the current connection.
Get-OTLPConnection returns sanitized connection metadata.
Write-OTLPLog emits a structured log record.
Send-OTLPLogBatch sends log records to OTLP/HTTP.
Invoke-OTLPScript provides in-memory controlled stream capture.
Endpoint paths are centralized.
URI building is centralized.
HTTP request execution is centralized.
Retry policy is centralized.
Redaction is centralized.
Stream-to-severity mapping is centralized.
Resource attribute creation is centralized.
All URLs use System.Uri or UriBuilder.
All internal paths use FileInfo, DirectoryInfo, and Path.Combine.
Logging is centralized.
Logging uses [UTC Timestamp] - [Level] - [Component] - Message.
Operations log before and after.
Failures log sanitized error detail.
Authorization headers are never logged.
Bearer tokens are never logged.
API keys are never logged.
Header values are never logged.
Raw payloads are not logged by default.
Redaction is enabled by default.
Default OTLP/HTTP endpoint is supported.
Custom logs endpoint is supported.
JSON OTLP payload export works.
Gzip compression works.
Retryable status codes are retried.
HTTP 400 is not retried.
Version format is yyyy.MM.dd.HHmm.
Commit hash is embedded.
Build script is idempotent.
Build script generates manifest.
Build script copies binaries to Module/PSOTLP/bin.
Build script creates release folders.
Build script can run unit tests.
Build script can run integration tests only when explicitly requested.
Successful milestones are committed when -CommitOnSuccess is used.
Failed builds are never committed.
```

---

## 35. Final Design Principle

`PSOTLP` should not be a pile of ad hoc HTTP posts to `/v1/logs`.

It should be a reusable, strongly typed, PowerShell-aware OTLP framework that starts with logs and session capture, while keeping the design open for traces, metrics, additional transports, richer stream capture, hosted runspaces, and future OpenTelemetry enhancements.


## 1. Ownership and Manifest Identity

The module must use **Grace Solutions** as the author/vendor identity.

The manifest must not use an individual name.

Required PSD1 values:

```powershell
Author = 'Grace Solutions'
CompanyName = 'Grace Solutions'
Copyright = '(c) Grace Solutions. All rights reserved.'
```

The module remains:

```text
Module Name: PSOTLP
```

The version format remains:

```text
yyyy.MM.dd.HHmm
```

The commit hash must still be embedded in:

```text
PSD1 PrivateData.PSData.CommitHash
AssemblyMetadata("CommitHash", "<commit-hash>")
AssemblyInformationalVersion = "yyyy.MM.dd.HHmm+<commit-hash>"
```

---

## 2. Transport and Encoding Support

PSOTLP must support both OTLP/HTTP and OTLP/gRPC from the beginning.

Supported transports:

```text
OTLP/HTTP
OTLP/gRPC
```

Supported encodings:

```text
JSON
Protobuf
```

Valid combinations:

```text
HTTP + JSON
HTTP + Protobuf
gRPC + Protobuf
```

Invalid combination:

```text
gRPC + JSON
```

The module should reject `gRPC + JSON` during validation because gRPC transport uses Protobuf service contracts.

Default transport:

```text
HTTP
```

Default encoding:

```text
JSON
```

Default endpoints:

```text
HTTP Endpoint:  http://localhost:4318
gRPC Endpoint:  http://localhost:4317

HTTP Logs:      /v1/logs
HTTP Traces:    /v1/traces
HTTP Metrics:   /v1/metrics
```

Initial implementation must support:

```text
Logs over OTLP/HTTP JSON
Logs over OTLP/HTTP Protobuf
Logs over OTLP/gRPC Protobuf
Traces over OTLP/HTTP JSON
Traces over OTLP/HTTP Protobuf
Traces over OTLP/gRPC Protobuf
```

Metrics remain a future extension unless explicitly enabled in a later milestone.

---

## 3. Dictionary Type Requirements

All attribute and header collections must use `IDictionary`.

Do not expose implementation-specific dictionary types in public models or public cmdlet parameters unless required internally.

Required public-facing types:

```csharp
IDictionary<string, object> Attributes
IDictionary<string, object> ResourceAttributes
IDictionary<string, object> LogAttributes
IDictionary<string, object> SpanAttributes
IDictionary<string, SecureString> Headers
```

For C# model properties:

```csharp
public IDictionary<string, object> Attributes { get; set; }

public IDictionary<string, object> ResourceAttributes { get; set; }

public IDictionary<string, object> LogAttributes { get; set; }

internal IDictionary<string, SecureString> Headers { get; set; }
```

Internally, use a case-insensitive comparer where appropriate:

```csharp
StringComparer.OrdinalIgnoreCase
```

For OpenTelemetry attribute keys, preserve the original key casing when serialized, but duplicate detection should be case-insensitive inside PowerShell-friendly helpers.

---

## 4. Logging Behavior

PSOTLP must not show internal module logging messages unless the caller explicitly asks for them through PowerShell streams.

Rules:

```text
No internal informational messages should appear by default.
Operational messages must use WriteVerbose.
Diagnostic messages must use WriteDebug.
Warnings must only be emitted for actual warning conditions and must respect WarningAction / WarningPreference.
Errors must use WriteError.
No regular success message should appear unless -Verbose is used.
No “Please Wait...” message should appear unless -Verbose is used.
```

Example:

```powershell
Write-OTLPLog -Body 'Starting install'
```

Expected default console output:

```text
No output, unless -PassThru is used or an error occurs.
```

Verbose example:

```powershell
Write-OTLPLog -Body 'Starting install' -Verbose
```

Verbose output may include:

```text
[UTC Timestamp] - [Verbose] - [WriteOTLPLogCommand] - Attempting to queue OTLP log record. Please Wait...
[UTC Timestamp] - [Verbose] - [WriteOTLPLogCommand] - OTLP log record queue operation was successful.
```

Internal logger format remains:

```text
[UTC Timestamp] - [Level] - [Component] - Message
```

---

## 5. Easy Baseline Logging With Advanced Support

The logging cmdlets must be easy to call at baseline but still support intermediate and advanced payloads.

Baseline call:

```powershell
Write-OTLPLog 'Install started'
```

Equivalent explicit call:

```powershell
Write-OTLPLog -Body 'Install started'
```

Intermediate call:

```powershell
Write-OTLPLog -Body 'Package installed' -Severity Information -Attribute @{ PackageName = 'Git'; ExitCode = 0 }
```

Advanced call:

```powershell
Write-OTLPLog `
    -Body 'Install completed' `
    -Severity Information `
    -TraceId $TraceId `
    -SpanId $SpanId `
    -ResourceAttribute @{ 'deployment.environment' = 'production'; 'device.manufacturer' = 'Dell Inc.' } `
    -LogAttribute @{ 'process.pid' = $PID; 'error.code' = 0 }
```

Object-based advanced call:

```powershell
$Record = [PSOTLP.Models.OTLPLogRecord]::new()
$Record.Body = 'Advanced structured event'
$Record.Severity = 'Information'
$Record.Attributes = @{ Phase = 'Install'; Result = 'Success' }

Write-OTLPLog -InputObject $Record
```

`Write-OTLPLog` must support:

```text
Simple string body
Body + severity
Body + IDictionary attributes
TraceId / SpanId correlation
ResourceAttributes
LogAttributes
InputObject model
Pipeline input
PassThru
```

---

## 6. Default Attribute Enrichment

PSOTLP must add rich default attributes automatically.

Default log attributes should include where available:

```text
process.pid
thread.id
module.name
module.path
module.version
command.name
script.name
script.path
function.name
error.code
powershell.stream.name
powershell.runspace.id
powershell.pipeline.id
powershell.host.name
powershell.host.version
powershell.version
powershell.edition
```

Default resource attributes should include where available:

```text
service.name
service.namespace
service.instance.id
service.version
deployment.environment
host.name
user.name
os.type
os.description
os.version
process.runtime.name
process.runtime.version
device.manufacturer
device.model
device.systemID
device.uuid
```

Default trace/span attributes should include where available:

```text
trace.id
span.id
parent.span.id
span.name
span.kind
script.name
script.path
function.name
command.name
process.pid
thread.id
error.code
```

The module should distinguish between:

```text
ResourceAttributes: describe the thing producing telemetry.
LogAttributes: describe the individual log event.
SpanAttributes: describe the individual trace span.
Attributes: generic user-provided attributes merged into the correct signal-level collection.
```

---

## 7. Attribute Merge Precedence

Attributes must be merged predictably.

Precedence from lowest to highest:

```text
Built-in defaults
Environment-derived attributes
Connection-level ResourceAttributes
Session-level Attributes
Cmdlet-level ResourceAttribute / LogAttribute / SpanAttribute
Cmdlet-level Attribute
InputObject explicit attributes
```

Higher precedence values overwrite lower precedence values.

Example:

```text
Built-in service.name = powershell
Environment service.name = cloudinit
Connect-OTLP -ServiceName device-bootstrap
Write-OTLPLog -ResourceAttribute @{ 'service.name' = 'custom-event-service' }

Final service.name = custom-event-service
```

---

## 8. Default Service, Script, and Function Resolution

PSOTLP must resolve script and function context by best effort.

Resolution order for `service.name`:

```text
Explicit -ServiceName
PSOTLP_SERVICE_NAME environment variable
OTEL_SERVICE_NAME environment variable
Calling script file base name
Current command/function name
Current process name
powershell
```

Resolution order for `scope.name`:

```text
Explicit -ScopeName
Calling function name
Calling command name
Calling script file base name
PSOTLP
```

Resolution order for `scope.version`:

```text
Explicit -ScopeVersion
Calling script version, if discoverable
Module version, if command is from a module
PSOTLP module version
```

Resolution order for `script.name`:

```text
Calling script path base name
Current command source file base name
Current process name
```

Resolution order for `function.name`:

```text
Calling function name if discoverable
Current command name
Current process name
```

The module should use PowerShell invocation metadata where available:

```text
PSCmdlet.MyInvocation
InvocationInfo.ScriptName
InvocationInfo.MyCommand.Name
InvocationInfo.InvocationName
InvocationInfo.PSScriptRoot
InvocationInfo.ScriptLineNumber
InvocationInfo.OffsetInLine
```

If function/script context cannot be resolved, fall back cleanly without error.

---

## 9. Hardware and OS Enrichment

The module must use C# APIs and CIM/WMI providers directly where possible.

No shelling out.

Forbidden:

```text
wmic.exe
powershell.exe Get-CimInstance
pwsh Get-CimInstance
cmd.exe
bash
external process calls for inventory enrichment
```

Allowed on Windows:

```text
System.Management
Microsoft.Management.Infrastructure, if compatible and practical
WMI/CIM classes directly from C#
Registry APIs where appropriate
Environment APIs
RuntimeInformation APIs
Dns APIs
Process APIs
Thread APIs
```

Windows hardware enrichment should use WMI/CIM classes directly from C#.

Suggested mappings:

```text
Win32_ComputerSystem.Manufacturer       -> device.manufacturer
Win32_ComputerSystem.Model              -> device.model
Win32_BaseBoard.Product                 -> device.systemID
Win32_ComputerSystemProduct.UUID        -> device.uuid
Win32_OperatingSystem.Caption           -> os.description
Win32_OperatingSystem.Version           -> os.version
Computer name / DNS host name           -> host.name
Current identity                         -> user.name
```

Cross-platform behavior:

```text
On Windows, use WMI/CIM directly from C#.
On Linux/macOS, use .NET runtime APIs first.
Do not fail telemetry if hardware details are unavailable.
Missing enrichment values should simply be omitted.
```

---

## 10. Environment Variable Configuration

PSOTLP must support automatic environment variable detection with scope precedence.

Explicit cmdlet parameters must always win.

Environment scope precedence:

```text
Explicit cmdlet parameter
Process environment variable
User environment variable
Machine environment variable
Built-in default
```

On platforms where User or Machine environment variable targets are unsupported or unavailable, the module must skip them safely.

The environment resolver must track source metadata.

Example source metadata:

```text
Name: PSOTLP_ENDPOINT_URI
ValueSource: Process
AppliedTo: EndpointUri
```

Do not expose sensitive values in source metadata.

Supported standard OpenTelemetry environment variables:

```text
OTEL_SERVICE_NAME
OTEL_RESOURCE_ATTRIBUTES
OTEL_EXPORTER_OTLP_ENDPOINT
OTEL_EXPORTER_OTLP_LOGS_ENDPOINT
OTEL_EXPORTER_OTLP_TRACES_ENDPOINT
OTEL_EXPORTER_OTLP_PROTOCOL
OTEL_EXPORTER_OTLP_LOGS_PROTOCOL
OTEL_EXPORTER_OTLP_TRACES_PROTOCOL
OTEL_EXPORTER_OTLP_HEADERS
OTEL_EXPORTER_OTLP_LOGS_HEADERS
OTEL_EXPORTER_OTLP_TRACES_HEADERS
OTEL_EXPORTER_OTLP_COMPRESSION
OTEL_EXPORTER_OTLP_TIMEOUT
```

Supported PSOTLP-specific environment variables:

```text
PSOTLP_ENDPOINT_URI
PSOTLP_LOGS_ENDPOINT_URI
PSOTLP_TRACES_ENDPOINT_URI
PSOTLP_PROTOCOL
PSOTLP_TRANSPORT
PSOTLP_ENCODING
PSOTLP_HEADERS
PSOTLP_LOGS_HEADERS
PSOTLP_TRACES_HEADERS
PSOTLP_SERVICE_NAME
PSOTLP_SERVICE_NAMESPACE
PSOTLP_SERVICE_INSTANCE_ID
PSOTLP_SCOPE_NAME
PSOTLP_SCOPE_VERSION
PSOTLP_DEPLOYMENT_ENVIRONMENT
PSOTLP_RESOURCE_ATTRIBUTES
PSOTLP_LOG_ATTRIBUTES
PSOTLP_TRACE_ID
PSOTLP_SPAN_ID
PSOTLP_COMPRESSION
PSOTLP_TIMEOUT_SECONDS
```

Environment name precedence when both OTEL and PSOTLP variables exist:

```text
Explicit cmdlet parameter
PSOTLP signal-specific variable
OTEL signal-specific variable
PSOTLP common variable
OTEL common variable
Built-in default
```

Scope precedence applies inside each name lookup.

Example:

```text
Process PSOTLP_ENDPOINT_URI beats User PSOTLP_ENDPOINT_URI.
User PSOTLP_ENDPOINT_URI beats Machine PSOTLP_ENDPOINT_URI.
Process PSOTLP_ENDPOINT_URI beats Process OTEL_EXPORTER_OTLP_ENDPOINT.
Explicit -EndpointUri beats all environment variables.
```

---

## 11. Headers

Headers must use `IDictionary`.

All header values are stored as `SecureString` because any header may contain
authentication or other sensitive material:

```csharp
IDictionary<string, SecureString> Headers
```

A single `Headers` collection holds every header, including `Authorization`,
API key headers, and any custom headers. There is no separate sensitive-header
collection. Plaintext values supplied through `-Headers` (for example a
hashtable of strings) are converted to `SecureString` at parameter binding.

Parameter examples:

```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ 'x-tenant-id' = 'personal' }
```

```powershell
$Token  = Read-Host -AsSecureString
$bearer = ConvertTo-SecureString -String ('Bearer ' +
    [System.Net.NetworkCredential]::new('', $Token).Password) -AsPlainText -Force
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ Authorization = $bearer }
```

```powershell
$ApiKey = Read-Host -AsSecureString
Connect-OTLP -EndpointUri 'https://otel.example.com' -Headers @{ authorization = $ApiKey }
```

Rules:

```text
All header values are treated as sensitive.
Header values must never be logged.
Header values must never be displayed.
Header values must only be converted to plaintext at the HTTP request creation boundary.
Plaintext supplied through -Headers is converted to SecureString at parameter binding.
Bearer tokens and API keys are stored in the same Headers collection.
```

---

## 12. Trace Support

Trace support moves from future scope into initial scope.

Initial trace cmdlets:

```powershell
Start-OTLPSpan
Stop-OTLPSpan
Write-OTLPSpanEvent
Send-OTLPTraceBatch
```

Trace support must integrate with log support.

Logs should be able to carry:

```text
TraceId
SpanId
TraceFlags
```

Trace ID resolution order:

```text
Explicit -TraceId
InputObject.TraceId
Active OTLP span context
PSOTLP_TRACE_ID environment variable
Generated TraceId
```

Span ID resolution order:

```text
Explicit -SpanId
InputObject.SpanId
Active OTLP span context
Generated SpanId when creating a span
Null for uncorrelated logs
```

Example:

```powershell
$Span = Start-OTLPSpan -Name 'Install-Git' -Attribute @{ PackageName = 'Git' } -PassThru

Write-OTLPLog -Body 'Installing Git' -Severity Information -TraceId $Span.TraceId -SpanId $Span.SpanId

Stop-OTLPSpan -SpanId $Span.SpanId
```

Simpler usage:

```powershell
Start-OTLPSpan -Name 'CloudInit-PostExecution'

Write-OTLPLog 'Starting post execution'
Write-OTLPLog 'Finished post execution'

Stop-OTLPSpan
```

The module must maintain an active span stack per session/runspace where practical.

---

## 13. Span Model

Required public span model:

```csharp
public sealed class OTLPSpan
{
    public string TraceId { get; set; }
    public string SpanId { get; set; }
    public string ParentSpanId { get; set; }
    public string Name { get; set; }
    public OTLPSpanKind Kind { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }
    public IDictionary<string, object> Attributes { get; set; }
    public IList<OTLPSpanEvent> Events { get; set; }
    public OTLPStatus Status { get; set; }
}
```

Span kind enum:

```csharp
public enum OTLPSpanKind
{
    Internal,
    Server,
    Client,
    Producer,
    Consumer
}
```

Default span kind:

```text
Internal
```

---

## 14. Span Event Model

```csharp
public sealed class OTLPSpanEvent
{
    public string Name { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public IDictionary<string, object> Attributes { get; set; }
}
```

Example:

```powershell
Write-OTLPSpanEvent -Name 'PackageDownloaded' -Attribute @{ Package = 'Git'; Source = 'Winget' }
```

---

## 15. Stream Capture Without Rewriting Cmdlets

PSOTLP must not require rewriting existing PowerShell cmdlets, functions, or
scripts. The design goal is:

```text
Wrap the script in Invoke-OTLPScript.
Run normal PowerShell inside.
Return.
```

Example:

```powershell
Invoke-OTLPScript -SessionName 'Device-Onboarding' -ScriptBlock {
    Write-Verbose 'Starting onboarding'
    winget install -e --id Git.Git
    Get-Service
    Write-Warning 'Example warning'
}
```

Capture strategy:

```text
PowerShell SDK stream collection inside a hosted runspace owned by Invoke-OTLPScript.
DataAdded stream events on every PowerShell stream.
No transcript file, no transcript tailer, no process-wide session registry.
No interference with any transcript the caller may already be running.
```

Important limitation:

```text
An imported module cannot transparently intercept every stream from every arbitrary command in the caller's live runspace. Capture is only guaranteed when PSOTLP owns the invocation through Invoke-OTLPScript.
```

---

## 16. Invoke-OTLPScript Stream Capture

When PSOTLP owns the invocation, it should tap into PowerShell streams directly.

Capture from PowerShell SDK stream collections:

```text
Output
Error
Warning
Verbose
Debug
Information
Progress where practical
```

Use stream events where practical:

```text
PSDataCollection<T>.DataAdded
PowerShell.Streams.Error.DataAdded
PowerShell.Streams.Warning.DataAdded
PowerShell.Streams.Verbose.DataAdded
PowerShell.Streams.Debug.DataAdded
PowerShell.Streams.Information.DataAdded
PowerShell.Streams.Progress.DataAdded
```

Do not rewrite commands.

Do not require scripts to call `Write-OTLPLog`.

The wrapper should preserve normal output behavior where practical.

---

## 17. Invocation Capture Defaults

`Invoke-OTLPScript` should enrich every captured record with:

```text
powershell.session.id
powershell.session.name
service.name
scope.name
scope.version
script.name
function.name
process.pid
thread.id
host.name
user.name
deployment.environment
```

If a trace is active, captured logs should also include:

```text
trace.id
span.id
```

---

## 18. Cmdlet Surface Updates

Keep the existing cmdlets:

```powershell
Connect-OTLP
Disconnect-OTLP
Get-OTLPConnection
Write-OTLPLog
Send-OTLPLogBatch
Invoke-OTLPScript
```

Add trace cmdlets:

```powershell
Start-OTLPSpan
Stop-OTLPSpan
Write-OTLPSpanEvent
Send-OTLPTraceBatch
```

Do not remove or rename existing cmdlets.

Do not design the module around rewriting the user’s existing cmdlets.

---

## 19. Updated Connection Parameters

`Connect-OTLP` must support:

```powershell
Connect-OTLP `
    [-EndpointUri <Uri>] `
    [-LogsEndpointUri <Uri>] `
    [-TracesEndpointUri <Uri>] `
    [-MetricsEndpointUri <Uri>] `
    [-NoSignalPath] `
    [-Transport <Http|Grpc>] `
    [-Encoding <Json|Protobuf|NDJson>] `
    [-Headers <IDictionary>] `
    [-ServiceName <string>] `
    [-ServiceNamespace <string>] `
    [-ServiceInstanceId <string>] `
    [-ScopeName <string>] `
    [-ScopeVersion <string>] `
    [-ScopeAttributes <IDictionary>] `
    [-EnvironmentName <string>] `
    [-ResourceAttribute <IDictionary>] `
    [-LogAttribute <IDictionary>] `
    [-Compression <None|Gzip>] `
    [-RetryCount <int>] `
    [-TimeoutSeconds <int>] `
    [-PassThru]
```

Defaults are loaded from:

```text
Environment resolver
Then built-in defaults
Then explicit params override all
```

---

## 20. Updated Log Record Model

```csharp
public sealed class OTLPLogRecord
{
    public DateTimeOffset TimestampUtc { get; set; }
    public DateTimeOffset ObservedTimestampUtc { get; set; }

    public OTLPSeverity Severity { get; set; }
    public string SeverityText { get; set; }
    public int SeverityNumber { get; set; }

    public string Body { get; set; }

    public string TraceId { get; set; }
    public string SpanId { get; set; }

    public string EventName { get; set; }

    public IDictionary<string, object> Attributes { get; set; }
    public IDictionary<string, object> ResourceAttributes { get; set; }
    public IDictionary<string, object> LogAttributes { get; set; }
}
```

Rules:

```text
Attributes are generic user-provided attributes.
LogAttributes are specific to the log event.
ResourceAttributes describe the resource producing the event.
Final serialized log attributes are produced by the centralized attribute merger.
```

---

## 21. Updated Connection Model

```csharp
public sealed class OTLPConnection
{
    public Uri EndpointUri { get; set; }
    public Uri LogsEndpointUri { get; set; }
    public Uri TracesEndpointUri { get; set; }
    public Uri MetricsEndpointUri { get; set; }
    public bool NoSignalPath { get; set; }

    public OTLPTransport Transport { get; set; }
    public OTLPEncoding Encoding { get; set; }
    public OTLPCompression Compression { get; set; }
    public OTLPAuthenticationMode AuthenticationMode { get; set; }

    public string ServiceName { get; set; }
    public string ServiceNamespace { get; set; }
    public string ServiceInstanceId { get; set; }
    public string ScopeName { get; set; }
    public string ScopeVersion { get; set; }
    public string EnvironmentName { get; set; }

    public IDictionary<string, object> ResourceAttributes { get; set; }
    public IDictionary<string, object> LogAttributes { get; set; }

    public DateTimeOffset ConnectedAtUtc { get; set; }
    public bool IsConnected { get; set; }

    internal IDictionary<string, SecureString> Headers { get; set; }
}
```

All authentication headers (bearer tokens, API keys, vendor-specific values)
are supplied through the single `-Headers` dictionary and stored in `Headers`
as `SecureString`. There is no separate sensitive-header collection and no
dedicated `-BearerToken`/`-ApiKey` parameters; callers compose the value
themselves (for example, `@{ Authorization = 'Bearer <token>' }`).

---

## 22. Attribute Collection Services

Required centralized services:

```text
OTLPDefaultAttributeProvider
OTLPResourceAttributeProvider
OTLPLogAttributeProvider
OTLPSpanAttributeProvider
OTLPAttributeMerger
OTLPInvocationContextResolver
OTLPEnvironmentResolver
OTLPSystemInformationProvider
```

Responsibilities:

```text
Collect process defaults.
Collect thread defaults.
Collect PowerShell invocation defaults.
Collect script/function defaults.
Collect WMI/CIM hardware defaults.
Collect environment variable attributes.
Merge attributes by precedence.
Prevent duplicate handling in cmdlets.
```

No cmdlet should manually assemble default attributes.

---

## 23. Updated Acceptance Criteria

Additional acceptance criteria:

```text
PSD1 Author is Grace Solutions.
PSD1 CompanyName is Grace Solutions.
OTLP/HTTP is supported.
OTLP/gRPC is supported.
HTTP JSON encoding is supported.
HTTP Protobuf encoding is supported.
gRPC Protobuf encoding is supported.
gRPC JSON is rejected as invalid.
Trace support is included in initial scope.
Start-OTLPSpan exists.
Stop-OTLPSpan exists.
Write-OTLPSpanEvent exists.
Send-OTLPTraceBatch exists.
Logs can include TraceId and SpanId.
Active span context can enrich logs automatically.
Attributes use IDictionary.
ResourceAttributes use IDictionary.
LogAttributes use IDictionary.
SpanAttributes use IDictionary.
Headers use IDictionary.
Logging cmdlets are simple at baseline.
Logging cmdlets support advanced structured attributes.
No internal module logs display unless verbose/debug/warning/error streams are explicitly used.
Default resource attributes are rich.
Default log attributes are rich.
Script name is resolved by default where possible.
Function name is resolved by default where possible.
Process name is used as fallback where needed.
Hardware enrichment uses C# WMI/CIM access where available.
No shelling out for hardware/system enrichment.
Environment variables are detected by Process, User, Machine precedence.
Explicit cmdlet parameters override environment variables.
PSOTLP-specific environment variables override OTEL common variables where appropriate.
No user cmdlet rewriting is required for invocation capture.
Invoke-OTLPScript taps streams directly through a hosted runspace.
```

## Default Attribute Presence Rule

PSOTLP must not omit expected default attributes simply because the value cannot be resolved.

All default attributes must always be present.

When a value cannot be resolved, the value must be:

```text
n/a
```

This applies to:

```text
ResourceAttributes
LogAttributes
SpanAttributes
TraceAttributes
SessionAttributes
```

Example:

```text
device.manufacturer = n/a
device.model = n/a
device.uuid = n/a
function.name = n/a
module.name = n/a
error.code = n/a
```

The goal is consistent filtering and dashboards. Consumers should not need to handle missing fields.

---

## Script Path Rule

Do not include full script paths in default attributes.

Full paths are too long, create poor filter values, and make telemetry harder to group.

Do not include these by default:

```text
script.path
module.path
file.path
powershell.script.path
```

Use short names instead:

```text
script.name
module.name
command.name
function.name
```

If a user explicitly wants full paths later, that should be an opt-in advanced option, not a default.

Optional future parameter:

```powershell
-IncludePathAttributes
```

Default:

```text
false
```

---

## Operating System Attribute Requirements

PSOTLP must enrich operating system attributes consistently.

Required OS resource attributes:

```text
os.type
os.caption
os.architecture
os.version
os.platform
```

### os.type

`os.type` must resolve to one of these normalized values:

```text
Workstation
Server
n/a
```

On Windows, determine this through C# WMI/CIM access, not shell commands.

Suggested Windows source:

```text
Win32_OperatingSystem.ProductType
```

Mapping:

```text
1 = Workstation
2 = Server
3 = Server
Unknown = n/a
```

### os.caption

`os.caption` should be the friendly OS name without the leading Microsoft branding.

Example raw WMI value:

```text
Microsoft Windows 11 Pro
```

Normalized value:

```text
Windows 11 Pro
```

Example raw WMI value:

```text
Microsoft Windows Server 2022 Standard
```

Normalized value:

```text
Windows Server 2022 Standard
```

Normalization rule:

```text
Remove leading "Microsoft " from the caption.
Trim whitespace.
If empty or unavailable, use "n/a".
```

### os.architecture

`os.architecture` must represent the operating system architecture.

Examples:

```text
x64
ARM64
x86
n/a
```

Suggested Windows source:

```text
Win32_OperatingSystem.OSArchitecture
```

Suggested normalization:

```text
64-bit -> x64
32-bit -> x86
ARM64 -> ARM64
Unknown -> n/a
```

### process.architecture

PSOTLP must also include process architecture separately.

Required log/resource attribute:

```text
process.architecture
```

Examples:

```text
x64
x86
ARM64
n/a
```

This is not the same as OS architecture. A 32-bit PowerShell process can run on a 64-bit OS.

Use .NET runtime APIs where possible.

---

## Required Default Resource Attributes

These attributes must always exist. If the value cannot be resolved, use `n/a`.

```text
service.name
service.namespace
service.instance.id
service.version
deployment.environment
host.name
user.name
os.type
os.caption
os.architecture
os.version
os.platform
process.architecture
process.runtime.name
process.runtime.version
device.manufacturer
device.model
device.systemID
device.uuid
```

Example default values:

```text
service.name = Calling script name, function name, process name, or powershell
service.namespace = n/a
service.instance.id = generated GUID or n/a
service.version = n/a
deployment.environment = n/a
host.name = computer name or n/a
user.name = current user or n/a
os.type = Workstation, Server, or n/a
os.caption = Windows 11 Pro, Windows Server 2022 Standard, Ubuntu 24.04, or n/a
os.architecture = x64, x86, ARM64, or n/a
os.version = OS version or n/a
os.platform = Windows, Linux, macOS, or n/a
process.architecture = x64, x86, ARM64, or n/a
process.runtime.name = .NET or n/a
process.runtime.version = runtime version or n/a
device.manufacturer = manufacturer or n/a
device.model = model or n/a
device.systemID = baseboard/system identifier or n/a
device.uuid = UUID or n/a
```

---

## Required Default Log Attributes

These log attributes must always exist. If the value cannot be resolved, use `n/a`.

```text
process.pid
thread.id
module.name
module.version
command.name
script.name
function.name
error.code
powershell.stream.name
powershell.runspace.id
powershell.pipeline.id
powershell.host.name
powershell.host.version
powershell.version
powershell.edition
```

Do not include full script path by default.

Example:

```text
process.pid = 4524
thread.id = 12
module.name = powershell
module.version = n/a
command.name = Invoke-DeviceBootstrap
script.name = Invoke-DeviceBootstrap
function.name = Install-Git
error.code = 0
powershell.stream.name = Output
```

If `error.code` is not applicable, use:

```text
n/a
```

Do not omit it.

---

## Required Default Span Attributes

These span attributes must always exist. If the value cannot be resolved, use `n/a`.

```text
trace.id
span.id
parent.span.id
span.name
span.kind
script.name
function.name
command.name
process.pid
thread.id
error.code
```

Do not include full script path by default.

---

## C# System Inventory Rules

PSOTLP must not shell out for system enrichment.

Forbidden:

```text
wmic.exe
powershell.exe
pwsh
cmd.exe
bash
Get-CimInstance through a child process
Get-WmiObject through a child process
external command execution for inventory enrichment
```

Allowed:

```text
C# WMI access
C# CIM access
System.Management where compatible
Microsoft.Management.Infrastructure where compatible
System.Environment
System.Diagnostics.Process
System.Threading.Thread
System.Runtime.InteropServices.RuntimeInformation
System.Security.Principal
System.Net.Dns
Registry APIs where appropriate
```

Windows mappings:

```text
Win32_ComputerSystem.Manufacturer -> device.manufacturer
Win32_ComputerSystem.Model -> device.model
Win32_BaseBoard.Product -> device.systemID
Win32_ComputerSystemProduct.UUID -> device.uuid
Win32_OperatingSystem.Caption -> os.caption
Win32_OperatingSystem.Version -> os.version
Win32_OperatingSystem.OSArchitecture -> os.architecture
Win32_OperatingSystem.ProductType -> os.type
```

All values must be normalized and defaulted to `n/a` when unavailable.

---

## Object Creation Examples

All PowerShell examples must use `New-Object`.

Do not use:

```powershell
[Some.Type]::new()
```

Do use:

```powershell
$Record = New-Object -TypeName 'PSOTLP.Models.OTLPLogRecord'
$Record.Body = 'Advanced structured event'
$Record.Severity = 'Information'
$Record.Attributes = @{
    Phase = 'Install'
    Result = 'Success'
}

Write-OTLPLog -InputObject $Record
```

This applies to:

```text
README examples
comment-based help examples
external help examples
docs
test snippets shown in documentation
```

C# source code can use normal C# constructors internally.

The restriction applies to PowerShell examples and module-facing documentation.

---

## Internal Module Logging Levels

PSOTLP internal module logging must use only these logical levels:

```text
Info
Warning
Error
```

These map to PowerShell streams as follows:

```text
Info -> Verbose
Warning -> Warning
Error -> Error
```

No internal module logging should display by default.

### Info

Info messages must be written only when `-Verbose` is used.

Example:

```text
[2026-06-19T18:44:22.1830000Z] - [Info] - [OTLPHttpExporter] - Attempting to export an OTLP log batch with 100 records to https://otel.example.com/v1/logs.
```

Example success:

```text
[2026-06-19T18:44:22.9290000Z] - [Info] - [OTLPHttpExporter] - The OTLP log batch was exported successfully. Status code: 200. Records exported: 100.
```

### Warning

Warning messages must be used only for real warning conditions.

Example:

```text
[2026-06-19T18:44:22.9290000Z] - [Warning] - [OTLPQueueManager] - The OTLP queue reached its maximum size. The oldest record was dropped because the configured drop policy is DropOldest.
```

### Error

Error messages must clearly describe what failed and what detail can help resolve it.

Example:

```text
[2026-06-19T18:44:22.9330000Z] - [Error] - [OTLPHttpExporter] - The OTLP log batch export failed after 3 attempts. The endpoint returned HTTP 503 Service Unavailable.
```

---

## Internal Logging Message Quality

Logging messages must be plain English, specific, and useful for troubleshooting.

Avoid vague messages.

Do not use:

```text
Failed.
Error happened.
Request bad.
Could not send.
Something went wrong.
```

Use:

```text
The OTLP log batch export failed after 3 attempts. The endpoint returned HTTP 503 Service Unavailable.
The OTLP connection could not be created because the endpoint URI is not valid.
The OTLP trace batch was not sent because no active OTLP connection exists.
The Invoke-OTLPScript invocation could not drain its queue because no active OTLP connection exists.
The default device UUID could not be resolved from Win32_ComputerSystemProduct. The value was set to n/a.
```

Messages must include enough context to identify:

```text
What operation was attempted
What component failed
What endpoint or signal was involved when safe
What status code or exception occurred when available
What fallback was applied when applicable
```

Messages must not include:

```text
Bearer tokens
API keys
Authorization headers
Header values
Raw telemetry payloads
Secrets
Full script paths by default
```

---

## Updated Logger Contract

Required interface shape:

```csharp
public interface IOTLPLogger
{
    void Info(string component, string message);
    void Warning(string component, string message);
    void Error(string component, string message);
    void Error(string component, string message, Exception exception);
}
```

PowerShell stream mapping:

```text
Info -> Cmdlet.WriteVerbose()
Warning -> Cmdlet.WriteWarning()
Error -> Cmdlet.WriteError()
```

The logger must not write directly to console.

Forbidden:

```text
Console.WriteLine
Console.Error.WriteLine
Debug.WriteLine for user-facing logs
Trace.WriteLine for user-facing logs
Write-Host
```

---

## Attribute Example

An enriched log should produce predictable attributes like this:

```text
ResourceAttributes:
  service.name = Invoke-DeviceBootstrap
  service.namespace = n/a
  service.instance.id = 8f21c247-50dd-4192-b989-f5ea6d3c86aa
  service.version = 2026.06.19.1844
  deployment.environment = production
  host.name = DEVICE-001
  user.name = SYSTEM
  os.type = Workstation
  os.caption = Windows 11 Pro
  os.architecture = x64
  os.version = 10.0.26100
  os.platform = Windows
  process.architecture = x64
  process.runtime.name = .NET
  process.runtime.version = 8.0.0
  device.manufacturer = Dell Inc.
  device.model = Latitude 5520
  device.systemID = 0A1B2C
  device.uuid = 4C4C4544-0031-5810-8033-C7C04F4D5933

LogAttributes:
  process.pid = 4524
  thread.id = 12
  module.name = Invoke-DeviceBootstrap
  module.version = 2026.06.19.1844
  command.name = Install-Git
  script.name = Invoke-DeviceBootstrap
  function.name = Install-Git
  error.code = 0
  powershell.stream.name = Information
  powershell.runspace.id = n/a
  powershell.pipeline.id = n/a
  powershell.host.name = ConsoleHost
  powershell.host.version = 5.1.26100.1
  powershell.version = 5.1.26100.1
  powershell.edition = Desktop
```

No full script path is included by default.

---

## Updated Acceptance Criteria

Additional acceptance criteria:

```text
Grace Solutions is used as the manifest Author.
Grace Solutions is used as the manifest CompanyName.
All default attributes are always present.
Unresolved default attributes use n/a.
Full script paths are not included by default.
os.type resolves to Workstation, Server, or n/a.
os.caption removes leading Microsoft branding.
os.architecture is included.
process.architecture is included.
PowerShell examples use New-Object instead of ::new().
Internal logging levels are Info, Warning, and Error only.
Info maps to Verbose.
Warning maps to Warning.
Error maps to Error.
Internal module logs do not display by default.
Log messages are plain English.
Log messages are descriptive enough to guide troubleshooting.
Log messages do not expose secrets, tokens, header values, raw payloads, or full script paths by default.
System enrichment uses C# APIs, WMI, or CIM directly.
System enrichment does not shell out.
```

## CI/CD Goal

PSOTLP must have identical CI/CD behavior for GitHub Actions and Gitea Actions.

The workflow files should be structurally identical and should differ only when a platform-specific limitation makes a difference unavoidable.

Required workflow locations:

```text
.github/workflows/release.yml
.gitea/workflows/release.yml
```

The preferred design is to keep both files identical.

If one workflow is updated, the other workflow must be updated in the same commit.

---

## CI/CD Platform Requirements

Supported platforms:

```text
GitHub Actions
Gitea Actions
```

Both platforms must use:

```text
Repository secrets
Repository variables only when values are not sensitive
Host runners by default
No container job requirement
Identical secret names
Identical build command
Identical test command
Identical release command
Identical publish command
```

The workflow should not require Docker.

The runner labels are forge-specific by design, so each workflow file targets the runner pool that its host actually provides.

Active runner labels:

```text
Gitea:  powershell-linux
GitHub: ubuntu-latest
```

The Gitea runner pool advertises `powershell-linux` for the host runners. The GitHub workflow uses the GitHub-hosted `ubuntu-latest` image so the mirrored repository remains usable without provisioning a self-hosted GitHub runner. Both targets must provide PowerShell 7, the .NET SDK, and Git.

---

## Repository Secret Names

The following repository secrets must be supported.

### PowerShell Gallery Publishing

```text
PSGALLERY_API_KEY
```

### NuGet Publishing, Optional

```text
NUGET_API_KEY
NUGET_SOURCE_URI
```

### OTLP Integration Testing

```text
PSOTLP_ENDPOINT_URI
PSOTLP_LOGS_ENDPOINT_URI
PSOTLP_TRACES_ENDPOINT_URI
PSOTLP_AUTHORIZATION_HEADER
PSOTLP_BEARER_TOKEN
PSOTLP_API_KEY
PSOTLP_API_KEY_HEADER_NAME
```

### Optional Signing

```text
SIGNING_CERTIFICATE_BASE64
SIGNING_CERTIFICATE_PASSWORD
TIMESTAMP_SERVER_URI
```

### Optional Release Automation

```text
RELEASE_TOKEN
```

No secret value may be hard-coded in workflow YAML.

No secret value may be echoed.

No secret value may be written to logs.

No secret value may be passed as a plain command argument when an environment variable can be used instead.

---

## Repository Variables

Repository variables may be used only for non-sensitive values.

Supported repository variables:

```text
PSGALLERY_PROJECT_URI
PSOTLP_SERVICE_NAME
PSOTLP_SERVICE_NAMESPACE
PSOTLP_DEPLOYMENT_ENVIRONMENT
PSOTLP_DEFAULT_ENDPOINT_URI
DOTNET_VERSION
POWERSHELL_REQUIRED_VERSION
```

`PSGALLERY_PROJECT_URI` is read by `Write-OTLPModuleManifest` and stamped into `PrivateData.PSData.ProjectUri` of the generated `PSOTLP.psd1`. The PowerShell Gallery rejects packages with an empty `ProjectUrl`, so this variable must be set on any repository that publishes the module. The publish job fails with the Gallery's own `'ProjectUrl cannot be empty.'` error if the value is missing at build time.

Repository variables must not contain:

```text
Tokens
API keys
Passwords
Certificates
Authorization headers
Client secrets
Signing passwords
```

---

## Secret Precedence in CI/CD

CI/CD secrets must be passed into the build script as environment variables.

The build script must then apply the same precedence rules as the module:

```text
Explicit build parameter
Process environment variable
User environment variable
Machine environment variable
Built-in default
```

In CI/CD, repository secrets become process environment variables.

Example mapping:

```yaml
env:
  PSOTLP_ENDPOINT_URI: ${{ secrets.PSOTLP_ENDPOINT_URI }}
  PSOTLP_LOGS_ENDPOINT_URI: ${{ secrets.PSOTLP_LOGS_ENDPOINT_URI }}
  PSOTLP_TRACES_ENDPOINT_URI: ${{ secrets.PSOTLP_TRACES_ENDPOINT_URI }}
  PSOTLP_BEARER_TOKEN: ${{ secrets.PSOTLP_BEARER_TOKEN }}
  PSGALLERY_API_KEY: ${{ secrets.PSGALLERY_API_KEY }}
```

The build script must never print these values.

---

## Build Script CI/CD Parameters

The idempotent `build.ps1` script must support CI/CD use directly.

Required parameters:

```powershell
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean,

    [switch]$Restore,

    [switch]$RunTests,

    [switch]$RunIntegrationTests,

    [switch]$CreateRelease,

    [switch]$Publish,

    [switch]$PublishPowerShellGallery,

    [switch]$PublishNuGet,

    [switch]$Sign,

    [switch]$CI,

    [switch]$Force
)
```

CI usage:

```powershell
.\build.ps1 -CI -Clean -Restore -RunTests -Configuration Release
```

Integration test usage:

```powershell
.\build.ps1 -CI -Clean -Restore -RunTests -RunIntegrationTests -Configuration Release
```

Release usage:

```powershell
.\build.ps1 -CI -Clean -Restore -RunTests -CreateRelease -Configuration Release
```

Publish usage:

```powershell
.\build.ps1 -CI -Clean -Restore -RunTests -CreateRelease -Publish -PublishPowerShellGallery -Configuration Release
```

---

## CI/CD Trigger Rules

The release workflow is driven entirely by pull requests merged into `main`.

Trigger:

```yaml
on:
  pull_request:
    types: [closed]
    branches: [main]
    paths-ignore:
      - 'README.md'
      - 'CHANGELOG.md'
      - 'LICENSE'
      - 'docs/**'
      - 'tests/**'
      - 'build.ps1'
      - 'build/**'
      - 'Module/**'
      - '**/*.sln'
      - '**/*.csproj'
      - '.gitea/workflows/**'
      - '.github/workflows/**'
      - '.gitignore'
      - '.gitattributes'
```

Every job in the workflow is guarded by:

```yaml
if: github.event.pull_request.merged == true
```

so the workflow never runs for a closed-but-unmerged pull request and never runs on direct pushes to `main`.

The `paths-ignore` filter restricts release triggering to source-code changes under `src/**`. Documentation, tests, the build script, the staged module directory, project and solution files, the CI workflow definitions, and repository metadata are all excluded. A merged pull request that touches only the listed paths does not run the build, release, or publish jobs. A pull request that touches any non-ignored path (in practice, any file under `src/**`) runs the full pipeline. When the pipeline runs, the runner builds the module from the current tip of `main`, so all current tracked files (including the latest `*.ps1xml`, `*.csproj`, and `Module/**` content) are packaged into the release regardless of whether they themselves can independently trigger the workflow.

Pull request (open, synchronize, reopen):

```text
No action - the release workflow is not subscribed to these events.
Validation of pull request commits is delegated to the developer's local build and any future PR validation workflow.
```

Pull request merged into main:

```text
Build the module from source on the merge commit.
Validate the module manifest.
Upload the staged module directory as a workflow artifact.
Create a forge release at the module's version tag, with a CHANGELOG-derived body and the module zip as an asset.
Publish the staged module directory to the PowerShell Gallery.
```

Direct push to main, push to development branches, tag push, and manual dispatch:

```text
No action - the release workflow does not subscribe to these events.
```

Release tag pattern:

```text
The release tag equals the module version reported by Test-ModuleManifest, with no prefix.
```

Example:

```text
2026.06.19.1844
```

---

## Secret Safety Rules for CI/CD

CI/CD workflows must follow these rules:

```text
Secrets are read only from repository secrets.
Secrets are injected only as environment variables.
Secrets are never written to files unless the operation requires it.
Temporary secret files are deleted before the job ends.
Secrets are never echoed.
Secrets are never passed to Write-Host.
Secrets are never included in artifact names.
Secrets are never included in release notes.
Secrets are never included in test output.
Secrets are never included in build logs.
Secrets are never included in telemetry payload dumps.
```

If a secret is missing, the build script must fail with a plain English message that names the missing secret but not its value.

Example:

```text
The PowerShell Gallery publish step could not start because the repository secret PSGALLERY_API_KEY is not available.
```

---

## Identical Workflow Requirement

The same CI/CD file content should be used for both:

```text
.github/workflows/release.yml
.gitea/workflows/release.yml
```

The files must use the same:

```text
Workflow name
Triggers
Job names
Environment variable names
Secret names
Module name parameterization (env.MODULE_NAME)
Release tag derivation
Release body shape
Publish command
Artifact name
```

The two files may differ only on forge-specific surface:

```text
Runner label (Gitea: powershell-linux, GitHub: ubuntu-latest)
Artifact upload and download action (Gitea: christopherhx/gitea-upload-artifact@v4 and christopherhx/gitea-download-artifact@v4, GitHub: actions/upload-artifact@v4 and actions/download-artifact@v4)
Release REST API authentication header (Gitea: token, GitHub: Bearer + X-GitHub-Api-Version)
Release accept header (Gitea: application/json, GitHub: application/vnd.github+json)
Pull request URL path segment (Gitea: /pulls/<id>, GitHub: /pull/<id>)
Release asset upload URI (Gitea: /releases/<id>/assets?name=..., GitHub: upload_url template stripped + ?name=...)
PSResourceGet bootstrap policy (Gitea: required pre-installed on the host runner, GitHub: installed on demand for CurrentUser)
GitHub-only permissions block (contents: write on the release job)
```

No other difference is permitted. The build script must not be split across the two forges.

---

## Required Workflow File

The workflow should be named:

```text
Publish to PowerShell Gallery
```

The workflow file should support:

```text
A pull_request closed trigger filtered to the main branch.
A merged == true guard on every job so closed-but-unmerged pull requests do not run the workflow.
A top-level env.MODULE_NAME entry that names the module once and is referenced by all jobs and steps.
Three jobs in order: build, release, publish.
A Linux host runner for all three jobs (powershell-linux on Gitea, ubuntu-latest on GitHub).
A PowerShell 7 build path.
A forge release created through the forge REST API with the module zip uploaded as an asset.
A PowerShell Gallery publish that consumes the staged module directory without re-running the build.
```

---

## Reference Workflow

The active workflow files are the source of truth:

```text
.github/workflows/release.yml
.gitea/workflows/release.yml
```

The workflow has three jobs that run on a merged pull request into `main`:

```text
build
  Restore NuGet packages.
  Run build.ps1 to compile the binary module and stage Module/<MODULE_NAME>.
  Validate Module/<MODULE_NAME>/<MODULE_NAME>.psd1 with Test-ModuleManifest.
  Upload Module/<MODULE_NAME>/ as the <MODULE_NAME>-module workflow artifact.

release
  Download the <MODULE_NAME>-module artifact into Module/<MODULE_NAME>/.
  Resolve the module version and the release tag from the manifest.
  Compress Module/<MODULE_NAME>/* into <MODULE_NAME>-<VERSION>.zip.
  Look up the release at the tag; skip creation if it already exists.
  Build a release body containing a metadata table, the CHANGELOG section for the version, and an Install snippet.
  Create the forge release through the forge REST API.
  Upload the zip as a release asset.

publish
  Download the <MODULE_NAME>-module artifact into Module/<MODULE_NAME>/.
  Bootstrap Microsoft.PowerShell.PSResourceGet and register PSGallery as Trusted with ApiVersion v2.
  Verify the PSGALLERY_API_KEY secret is present and fail with a plain English message if not.
  Re-validate the downloaded manifest.
  Publish-PSResource -Path Module/<MODULE_NAME> -Repository PSGallery -ApiKey $env:PSGALLERY_API_KEY.
```

Skeleton (canonical Gitea form; the GitHub form is byte-identical except for the forge-specific surface enumerated in the Identical Workflow Requirement section):

```yaml
name: Publish to PowerShell Gallery

on:
  pull_request:
    types: [closed]
    branches: [main]
    paths-ignore:
      - 'README.md'
      - 'CHANGELOG.md'
      - 'LICENSE'
      - 'docs/**'
      - 'tests/**'
      - 'build.ps1'
      - 'build/**'
      - 'Module/**'
      - '**/*.sln'
      - '**/*.csproj'
      - '.gitea/workflows/**'
      - '.github/workflows/**'
      - '.gitignore'
      - '.gitattributes'

env:
  MODULE_NAME: PSOTLP

jobs:
  build:
    if: github.event.pull_request.merged == true
    runs-on: powershell-linux
    steps:
      - uses: actions/checkout@v4
      - name: Build module
        shell: pwsh
        run: ./build.ps1
      - name: Validate module manifest
        shell: pwsh
        run: Test-ModuleManifest "Module/${env:MODULE_NAME}/${env:MODULE_NAME}.psd1"
      - name: Upload module artifact
        uses: christopherhx/gitea-upload-artifact@v4
        with:
          name: ${{ env.MODULE_NAME }}-module
          path: Module/${{ env.MODULE_NAME }}
          if-no-files-found: error
          retention-days: 7

  release:
    needs: build
    if: ${{ success() && github.event.pull_request.merged == true }}
    runs-on: powershell-linux
    outputs:
      version: ${{ steps.meta.outputs.version }}
      tag: ${{ steps.meta.outputs.tag }}
    steps:
      - uses: actions/checkout@v4
      - uses: christopherhx/gitea-download-artifact@v4
        with:
          name: ${{ env.MODULE_NAME }}-module
          path: Module/${{ env.MODULE_NAME }}
      - id: meta
        shell: pwsh
        run: |
          $version = (Test-ModuleManifest "Module/${env:MODULE_NAME}/${env:MODULE_NAME}.psd1").Version.ToString()
          "version=$version" | Out-File $env:GITHUB_OUTPUT -Append
          "tag=$version"     | Out-File $env:GITHUB_OUTPUT -Append
      - name: Package and create release
        shell: pwsh
        env:
          GITEA_TOKEN: ${{ github.token }}
          API_URL:    ${{ github.api_url }}
          REPO:       ${{ github.repository }}
          TAG:        ${{ steps.meta.outputs.tag }}
          VERSION:    ${{ steps.meta.outputs.version }}
        run: |
          # Compress Module/<MODULE_NAME>/* into <MODULE_NAME>-<VERSION>.zip,
          # build a release body from the CHANGELOG section for $env:VERSION,
          # look up the existing release by tag (skip if present),
          # POST a new release to $API_URL/repos/$REPO/releases,
          # upload the zip to /releases/<id>/assets?name=<MODULE_NAME>-<VERSION>.zip.

  publish:
    needs: release
    if: ${{ success() && github.event.pull_request.merged == true }}
    runs-on: powershell-linux
    steps:
      - uses: christopherhx/gitea-download-artifact@v4
        with:
          name: ${{ env.MODULE_NAME }}-module
          path: Module/${{ env.MODULE_NAME }}
      - name: Publish to PowerShell Gallery
        shell: pwsh
        env:
          PSGALLERY_API_KEY: ${{ secrets.PSGALLERY_API_KEY }}
        run: |
          if ([string]::IsNullOrWhiteSpace($env:PSGALLERY_API_KEY)) {
              throw "The PowerShell Gallery publish step could not start because the repository secret PSGALLERY_API_KEY is not available."
          }
          Publish-PSResource `
              -Path (Join-Path $PWD "Module/${env:MODULE_NAME}") `
              -Repository PSGallery `
              -ApiKey $env:PSGALLERY_API_KEY `
              -Verbose
```

The full step-by-step content of each job, including the host prerequisite checks, the manifest validation steps, the CHANGELOG extraction logic, and the PSResourceGet bootstrap, lives in the workflow files themselves and is the binding source.

---

## Gitea Compatibility Rule

Gitea should use the same workflow syntax as GitHub wherever possible. The only differences allowed are the forge-specific surface enumerated in the Identical Workflow Requirement section.

The Gitea host runner must provide:

```text
PowerShell 7
.NET SDK
Git
Microsoft.PowerShell.PSResourceGet pre-installed for AllUsers (the Gitea publish job does not bootstrap PSResourceGet at runtime)
```

The Gitea runner pool must advertise the label that the workflow targets:

```text
powershell-linux
```

---

## GitHub Runner Rule

The GitHub workflow targets the GitHub-hosted Linux image so the mirrored repository remains runnable without provisioning a self-hosted GitHub runner:

```text
ubuntu-latest
```

The publish job bootstraps Microsoft.PowerShell.PSResourceGet on demand for the CurrentUser scope, because the GitHub-hosted image does not preinstall it for AllUsers.

If the repository switches to a self-hosted GitHub runner, the runner label and PSResourceGet provisioning policy must be updated to match the Gitea contract.

---

## Publish Rules

Publishing must occur only when:

```text
The pull request was closed with merged == true.
The pull request targeted the main branch.
The PSGALLERY_API_KEY repository secret is present.
The release job created or confirmed the forge release for the module version.
```

PowerShell Gallery publishing requires:

```text
PSGALLERY_API_KEY
```

If missing, fail with:

```text
The PowerShell Gallery publish step could not start because the repository secret PSGALLERY_API_KEY is not available.
```

Do not publish from pull requests that were closed without being merged.

Do not publish from branches other than main.

Do not publish from direct pushes to main, tag pushes, or manual dispatch (the workflow does not subscribe to those events).

---

## Artifact Rules

Build artifacts must not contain secrets.

Artifacts may contain:

```text
Compiled module
PSD1
PSM1
Type files
Format files
Docs
Release notes
Test results
Code coverage results
```

Artifacts must not contain:

```text
Repository secrets
Generated secret files
Temporary signing certificate files
Telemetry payload dumps
Raw environment dumps
```

---

## Acceptance Criteria Additions

```text
GitHub Actions workflow exists at .github/workflows/release.yml.
Gitea Actions workflow exists at .gitea/workflows/release.yml.
Both workflows are named "Publish to PowerShell Gallery".
Both workflows subscribe to pull_request closed on the main branch only.
Both workflows guard every job with github.event.pull_request.merged == true.
Both workflows have the same three jobs in the same order: build, release, publish.
Both workflows reference the module name through env.MODULE_NAME at the top level.
Both workflows use the same secret names.
Both workflows use host runners by default and avoid container jobs.
The Gitea workflow targets runs-on: powershell-linux.
The GitHub workflow targets runs-on: ubuntu-latest.
The two workflows differ only on the forge-specific surface documented in the Identical Workflow Requirement section.
The build job uploads Module/<MODULE_NAME>/ as the <MODULE_NAME>-module artifact.
The release job creates a forge release at the module version, with the <MODULE_NAME>-<VERSION>.zip uploaded as a release asset.
The release job skips creation when the release at the tag already exists.
The publish job consumes the downloaded module artifact and calls Publish-PSResource against the staged directory.
The publish job fails with a plain English error when PSGALLERY_API_KEY is missing, and never reveals the secret value.
Publishing does not run for pull requests that were closed without being merged.
Publishing does not run for direct pushes to main, tag pushes, or manual dispatch.
PowerShell Gallery publishing uses PSGALLERY_API_KEY.
Secrets are passed as environment variables.
Secrets are never echoed.
Secrets are never stored in artifacts.
Missing secrets produce plain English errors that name the missing secret but never reveal a value.
```
