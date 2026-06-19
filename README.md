# PSOTLP

PowerShell binary module (C#, .NET Standard 2.0) that emits OpenTelemetry
Protocol (OTLP) logs, traces, and metric stubs to any OTLP/HTTP endpoint from
Windows PowerShell 5.1 or PowerShell 7+.

## Highlights

- Synchronous, dependency-free C# (no `async`/`await`).
- Bearer / API key / custom header authentication; every header value stored
  as `SecureString`.
- Centralized redaction with default patterns plus user-supplied `Regex[]`.
- In-memory script capture via `Invoke-OTLPScript` — no transcript, no file
  locks, no interference with any transcript the caller may already have
  running.
- Spans, span events, and trace batch export.
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
