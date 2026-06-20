# PSOTLP

PowerShell binary module (C#, .NET Standard 2.0) that emits OpenTelemetry
Protocol (OTLP) logs, traces, and metric stubs to any OTLP/HTTP endpoint from
Windows PowerShell 5.1 or PowerShell 7+.

## Highlights

- Synchronous C# (no `async`/`await`).
- Single `-Header` dictionary on `Connect-OTLP` carries every authentication
  header (bearer tokens, API keys, custom headers); every value is stored as
  `SecureString`.
- Centralized redaction with default patterns plus user-supplied `Regex[]`.
- In-memory script capture via `Invoke-OTLPScript` — no transcript, no file
  locks, no interference with any transcript the caller may already have
  running.
- Spans, span events, and trace batch export.
- Pluggable wire encoding: `Json` (OTLP/HTTP), `Protobuf` (OTLP/HTTP), or
  `NDJson` (Rootprint `/api/ingest/ndjson`).
- Single `build.ps1` drives build, package, release, and publish.

## Install

From the PowerShell Gallery (once the next release is published):

```powershell
Install-Module -Name PSOTLP -Scope CurrentUser
```

From source:

```powershell
git clone https://prod.git.gracesolution.info/gsadmin/PSOTLP.git
cd PSOTLP
./build.ps1 -Configuration Release
Import-Module ./Module/PSOTLP/PSOTLP.psd1 -Force
```

## Quick start

```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -ServiceName 'my-script'

Write-OTLPLog -Body 'Bootstrap started' -Severity Information

Invoke-OTLPScript -ScriptBlock {
    Write-Verbose 'Configuring services'
    Get-Service | Select-Object -First 5
}

Disconnect-OTLP
```

Custom redaction:

```powershell
$patterns = @([regex]'(?i)x-custom-secret\s*[:=]\s*\S+')
Connect-OTLP -EndpointUri 'https://otel.example.com' -RedactPattern $patterns
```

See [`docs/`](docs) for per-cmdlet reference and
[`docs/DesignSpec.md`](docs/DesignSpec.md) for the full design specification.

## Backends

Any OTLP/HTTP-compatible backend works. Two are called out below because their
authentication and content-type rules differ slightly.

### HyperDX

HyperDX accepts standard OTLP/HTTP at `https://in-otel.hyperdx.io/v1/logs`
and authenticates with the `authorization` header set to the raw API key
(no `Bearer ` prefix). See the
[HyperDX OpenTelemetry docs](https://www.hyperdx.io/docs/install/opentelemetry)
and the
[cURL walkthrough](https://www.hyperdx.io/blog/testing-sending-opentelemetry-events-curl).

```powershell
$apiKey = Read-Host -AsSecureString -Prompt 'HyperDX API key'

Connect-OTLP `
    -EndpointUri 'https://in-otel.hyperdx.io' `
    -Header @{ authorization = $apiKey } `
    -ServiceName 'my-script' `
    -Compression Gzip

Invoke-OTLPScript -ScriptBlock {
    Write-Information 'Bootstrap started' -InformationAction Continue
    Get-Service | Select-Object -First 5
}

Disconnect-OTLP
```

### Rootprint

Rootprint's OTLP endpoint is `POST https://<your-rootprint>/v1/logs` and
authenticates with `Authorization: Bearer <ingest-token>`. The target index
is pinned by the ingest API key (create one in **Settings → API keys**); the
exporter does not pick the index. See the
[Rootprint OTLP reference](https://docs.rootprint.io/send-logs/otlp) and
[Manage indexes](https://docs.rootprint.io/configuration/manage-indexes).

Rootprint requires `Content-Type: application/x-protobuf` at `/v1/logs` and
rejects JSON with HTTP 415. Use `-Encoding Protobuf`:

```powershell
$ingestToken = Read-Host -AsSecureString -Prompt 'Rootprint ingest token'
$bearer      = ConvertTo-SecureString -String ('Bearer ' +
    [System.Net.NetworkCredential]::new('', $ingestToken).Password) -AsPlainText -Force

Connect-OTLP `
    -EndpointUri 'https://rootprint.example.com' `
    -Header @{ Authorization = $bearer } `
    -ServiceName 'my-script' `
    -Encoding Protobuf `
    -Compression Gzip

Invoke-OTLPScript -ScriptBlock {
    Write-Information 'Bootstrap started' -InformationAction Continue
}

Disconnect-OTLP
```

Rootprint also accepts a flat NDJSON document per line at
`POST /api/ingest/ndjson` with `Content-Type: application/x-ndjson`. Select
it with `-Encoding NDJson`; the URI builder routes the logs signal to the
`/api/ingest/ndjson` path automatically and each log record is emitted as a
single snake_case JSON document terminated by `\n`. NDJSON is logs-only and
will throw on trace export.

```powershell
$ingestToken = Read-Host -AsSecureString -Prompt 'Rootprint ingest token'
$bearer      = ConvertTo-SecureString -String ('Bearer ' +
    [System.Net.NetworkCredential]::new('', $ingestToken).Password) -AsPlainText -Force

Connect-OTLP `
    -EndpointUri 'https://rootprint.example.com' `
    -Header @{ Authorization = $bearer } `
    -ServiceName 'my-script' `
    -Encoding NDJson

Write-OTLPLog -Body 'Bootstrap started' -Severity Information
Send-OTLPLogBatch -InputObject (Get-Content .\events.json | ConvertFrom-Json)

Disconnect-OTLP
```

## Encoding reference

| `-Encoding` | Content-Type | Default logs path | Traces | Metrics |
| --- | --- | --- | --- | --- |
| `Json` (default) | `application/json` | `/v1/logs` | Supported | Supported |
| `Protobuf` | `application/x-protobuf` | `/v1/logs` | Supported | Supported |
| `NDJson` | `application/x-ndjson` | `/api/ingest/ndjson` | Not supported | Not supported |

The OpenTelemetry `.proto` files live under `src/PSOTLP/Proto/` and are compiled into the
`PSOTLP.dll` at build time by `Grpc.Tools`. They are not shipped with the module and do not need
to be distributed alongside it.

## Metrics

Metrics use the standard OTLP `/v1/metrics` path and support `Gauge` and `Sum` instrument types
with `Delta` or `Cumulative` aggregation temporality.

```powershell
Connect-OTLP -EndpointUri 'https://otel.example.com' -ServiceName 'cloudbase-init'

# Gauge (point-in-time value)
Write-OTLPMetric -Name 'system.memory.usage' -Unit 'By' -Value 1.42e9 -Attribute @{ state = 'used' }

# Monotonic cumulative counter
Write-OTLPMetric -Name 'driver.install.count' -Type Sum -Temporality Cumulative -IsMonotonic `
    -IntValue 1 -AsInt -Attribute @{ result = 'success' }

# Batch
$metrics = 1..5 | ForEach-Object {
    $m = New-Object PSOTLP.Models.OTLPMetric
    $m.Name = 'sample.gauge'; $m.DoubleValue = $_; $m
}
$metrics | Send-OTLPMetricBatch
```

## Cmdlets

| Cmdlet | Purpose |
| --- | --- |
| `Connect-OTLP` | Establish a reusable OTLP connection. |
| `Disconnect-OTLP` | Clear the current connection. |
| `Get-OTLPConnection` | Return sanitized connection metadata. |
| `Write-OTLPLog` | Emit a single structured log record. |
| `Send-OTLPLogBatch` | Send a batch of log records over OTLP/HTTP. |
| `Invoke-OTLPScript` | Run a script block in a hosted runspace and emit every captured stream as an OTLP log. |
| `Start-OTLPSpan` / `Stop-OTLPSpan` | Manage trace spans. |
| `Write-OTLPSpanEvent` | Attach an event to the current span. |
| `Send-OTLPTraceBatch` | Send a batch of trace spans. |
| `Write-OTLPMetric` | Emit a single Gauge or Sum metric data point. |
| `Send-OTLPMetricBatch` | Send a batch of metric data points. |

## Build

```powershell
./build.ps1 -Configuration Release              # build + stage module
./build.ps1 -Configuration Release -RunTests    # build + run Pester 5 unit tests
./build.ps1 -Configuration Release -CreateRelease -Force
./build.ps1 -Configuration Release -CreateRelease -Publish -PublishPowerShellGallery -Sign -Force
```

Signing is opt-in and fail-close: `-Sign` requires
`SIGNING_CERTIFICATE_BASE64` and `SIGNING_CERTIFICATE_PASSWORD`; if either is
missing the build aborts before publishing.

## CI / Release

The `.github/workflows/release.yml` workflow:

1. Builds and packages on every push to `main` and every `v*` tag.
2. Publishes to the PowerShell Gallery on tag pushes, or on
   `workflow_dispatch` with `publish=true`.
3. Optionally signs when `workflow_dispatch` is invoked with `sign=true` and
   the signing secrets are configured.

Tests are not executed in CI; run them locally before opening a PR.

## Contributing

`main` is protected — direct pushes are disabled and all changes must land
through a pull request. Before opening a PR:

1. Run `./build.ps1 -Configuration Release -RunTests` and confirm tests pass.
2. Keep commits scoped and use descriptive messages.
3. Update `docs/` when the public surface changes.

### Branch protection policy

Apply the following rules to `main` in the Git server admin UI:

- Disable direct pushes (pull requests only).
- Require at least one approving review.
- Require the `PSOTLP Release / Build and Package` check to pass before
  merge.
- Allow repository administrators to bypass the above for break-glass
  scenarios.

## License

Copyright (c) Grace Solutions. All rights reserved.
