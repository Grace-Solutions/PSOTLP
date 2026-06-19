#requires -Version 5.1
<#
.SYNOPSIS
    Helper functions backing build.ps1. Dot-sourced; not intended for direct invocation.
#>

function New-OTLPBuildContext {
    [CmdletBinding()] param([string]$RepoRoot, [string]$Configuration)
    $version = (Get-Date).ToString('yyyy.MM.dd.HHmm')
    $commitHash = ''
    try { $commitHash = (& git -C $RepoRoot rev-parse HEAD 2>$null).Trim() } catch { $commitHash = '' }
    if (-not $commitHash) { $commitHash = '0000000000000000000000000000000000000000' }

    return [pscustomobject]@{
        RepoRoot       = [System.IO.DirectoryInfo]$RepoRoot
        Configuration  = $Configuration
        Version        = $version
        CommitHash     = $commitHash
        Project        = [System.IO.FileInfo]([System.IO.Path]::Combine($RepoRoot, 'src', 'PSOTLP', 'PSOTLP.csproj'))
        Solution       = [System.IO.FileInfo]([System.IO.Path]::Combine($RepoRoot, 'PSOTLP.sln'))
        ModuleDir      = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($RepoRoot, 'Module', 'PSOTLP'))
        ModuleBinDir   = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($RepoRoot, 'Module', 'PSOTLP', 'bin'))
        TestsDir       = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($RepoRoot, 'tests'))
        ReleasesDir    = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($RepoRoot, 'Releases'))
        ChangeLogFile  = [System.IO.FileInfo]([System.IO.Path]::Combine($RepoRoot, 'CHANGELOG.md'))
        ModuleGuid     = '0fae6770-1d6a-4f1d-9a3a-1a93b96a3a01'
    }
}

function Invoke-OTLPBuildClean {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    $targets = @(
        [System.IO.Path]::Combine($Context.RepoRoot.FullName, 'src', 'PSOTLP', 'bin')
        [System.IO.Path]::Combine($Context.RepoRoot.FullName, 'src', 'PSOTLP', 'obj')
        $Context.ModuleBinDir.FullName
    )
    foreach ($target in $targets) {
        if ([System.IO.Directory]::Exists($target)) {
            Write-Verbose "Removing $target"
            Remove-Item -Path $target -Recurse -Force
        }
    }
}

function Invoke-OTLPBuildRestore {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context, [switch]$Always)
    if (-not $Always) { return }
    Write-Host "Restoring NuGet packages. Please Wait..."
    & dotnet restore $Context.Project.FullName | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed (exit code $LASTEXITCODE)." }
}

function Invoke-OTLPBuildCompile {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    Write-Host "Compiling PSOTLP ($($Context.Configuration)). Please Wait..."
    $informationalVersion = "$($Context.Version)+$($Context.CommitHash)"
    $assemblyVersion = $Context.Version
    $buildArgs = @(
        'build', $Context.Project.FullName,
        '-c', $Context.Configuration,
        '-nologo',
        "/p:Version=$assemblyVersion",
        "/p:AssemblyVersion=$assemblyVersion",
        "/p:FileVersion=$assemblyVersion",
        "/p:InformationalVersion=$informationalVersion",
        "/p:CommitHash=$($Context.CommitHash)"
    )
    & dotnet @buildArgs | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit code $LASTEXITCODE)." }
}

function Publish-OTLPModuleAssets {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context, [switch]$Force)
    if (-not $Context.ModuleBinDir.Exists) { $Context.ModuleBinDir.Create() | Out-Null }
    $sourceCandidates = @(
        [System.IO.Path]::Combine($Context.RepoRoot.FullName, 'src', 'PSOTLP', 'bin', $Context.Configuration, 'netstandard2.0')
        [System.IO.Path]::Combine($Context.RepoRoot.FullName, 'src', 'PSOTLP', 'bin', $Context.Configuration)
    )
    $source = $sourceCandidates | Where-Object { [System.IO.Directory]::Exists($_) } | Select-Object -First 1
    if (-not $source) { throw "Build output not found in any of: $($sourceCandidates -join ', ')" }

    Get-ChildItem -Path $source -Filter '*.dll' -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination ([System.IO.Path]::Combine($Context.ModuleBinDir.FullName, $_.Name)) -Force
    }
    Get-ChildItem -Path $source -Filter '*.pdb' -File -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination ([System.IO.Path]::Combine($Context.ModuleBinDir.FullName, $_.Name)) -Force
    }
}

function Write-OTLPModuleManifest {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    $manifestPath = [System.IO.Path]::Combine($Context.ModuleDir.FullName, 'PSOTLP.psd1')
    $contents = @"
@{
    RootModule = 'PSOTLP.psm1'
    ModuleVersion = '$($Context.Version)'
    GUID = '$($Context.ModuleGuid)'
    Author = 'Grace Solutions'
    CompanyName = 'Grace Solutions'
    Copyright = '(c) Grace Solutions. All rights reserved.'
    Description = 'PowerShell module for emitting OpenTelemetry Protocol (OTLP) telemetry over HTTP/JSON.'
    PowerShellVersion = '5.1'
    CompatiblePSEditions = @('Desktop', 'Core')
    DotNetFrameworkVersion = '4.7.2'
    FunctionsToExport = @()
    CmdletsToExport = @(
        'Connect-OTLP',
        'Disconnect-OTLP',
        'Get-OTLPConnection',
        'Write-OTLPLog',
        'Send-OTLPLogBatch',
        'Start-OTLPSession',
        'Stop-OTLPSession',
        'Get-OTLPSession',
        'Invoke-OTLPScript',
        'Start-OTLPSpan',
        'Stop-OTLPSpan',
        'Write-OTLPSpanEvent',
        'Send-OTLPTraceBatch'
    )
    AliasesToExport = @()
    FormatsToProcess = @('PSOTLP.Format.ps1xml')
    TypesToProcess = @('PSOTLP.Types.ps1xml')
    PrivateData = @{
        PSData = @{
            Tags = @('OpenTelemetry','OTLP','Logs','Tracing','PowerShell','Observability','GraceSolutions')
            ProjectUri = ''
            ReleaseNotes = ''
            CommitHash = '$($Context.CommitHash)'
        }
    }
}
"@
    [System.IO.File]::WriteAllText($manifestPath, $contents, [System.Text.UTF8Encoding]::new($false))
}

function Write-OTLPModuleLoader {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context, [switch]$Force)
    $loaderPath = [System.IO.Path]::Combine($Context.ModuleDir.FullName, 'PSOTLP.psm1')
    if ([System.IO.File]::Exists($loaderPath) -and -not $Force) { return }

    $contents = @'
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
'@
    [System.IO.File]::WriteAllText($loaderPath, $contents, [System.Text.UTF8Encoding]::new($false))
}

function Update-OTLPChangeLog {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    if (-not $Context.ChangeLogFile.Exists) { return }
    $stamp = [System.IO.File]::ReadAllText($Context.ChangeLogFile.FullName)
    $marker = "[$($Context.Version)]"
    if ($stamp.Contains($marker)) { return }
    $today = (Get-Date).ToString('yyyy-MM-dd')
    $entry = "## $marker - $today`r`n`r`n- Automated build for commit $($Context.CommitHash).`r`n`r`n"
    $updated = $entry + $stamp
    [System.IO.File]::WriteAllText($Context.ChangeLogFile.FullName, $updated, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-OTLPUnitTests {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    $unitDir = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($Context.TestsDir.FullName, 'Unit'))
    if (-not $unitDir.Exists) { Write-Host "No unit tests directory found at $($unitDir.FullName). Skipping."; return }
    if (-not (Get-Module -ListAvailable -Name Pester)) { Write-Warning "Pester is not installed. Skipping unit tests."; return }
    Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop
    $result = Invoke-Pester -Path $unitDir.FullName -PassThru
    if ($result.FailedCount -gt 0) { throw "Unit tests failed: $($result.FailedCount) failing test(s)." }
}

function Invoke-OTLPIntegrationTests {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    $intDir = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($Context.TestsDir.FullName, 'Integration'))
    if (-not $intDir.Exists) { Write-Host "No integration tests directory found at $($intDir.FullName). Skipping."; return }
    if (-not (Get-Module -ListAvailable -Name Pester)) { Write-Warning "Pester is not installed. Skipping integration tests."; return }
    Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop
    $result = Invoke-Pester -Path $intDir.FullName -PassThru
    if ($result.FailedCount -gt 0) { throw "Integration tests failed: $($result.FailedCount) failing test(s)." }
}

function Test-OTLPModuleImport {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    $manifest = [System.IO.Path]::Combine($Context.ModuleDir.FullName, 'PSOTLP.psd1')
    if (-not [System.IO.File]::Exists($manifest)) { return }
    try {
        $module = Import-Module -Name $manifest -Force -PassThru -ErrorAction Stop
        Write-Host "Imported $($module.Name) version $($module.Version)."
        Remove-Module -Name $module.Name -Force -ErrorAction SilentlyContinue
    }
    catch { throw "Module import validation failed: $($_.Exception.Message)" }
}

function New-OTLPReleasePackage {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context, [switch]$Force)
    if (-not $Context.ReleasesDir.Exists) { $Context.ReleasesDir.Create() | Out-Null }
    $releaseDir = [System.IO.DirectoryInfo]([System.IO.Path]::Combine($Context.ReleasesDir.FullName, $Context.Version))
    if ($releaseDir.Exists) {
        if (-not $Force) { throw "Release folder already exists: $($releaseDir.FullName). Use -Force to overwrite." }
        Remove-Item -Path $releaseDir.FullName -Recurse -Force
    }
    $releaseDir.Create() | Out-Null
    $destination = [System.IO.Path]::Combine($releaseDir.FullName, 'PSOTLP')
    Copy-Item -Path $Context.ModuleDir.FullName -Destination $destination -Recurse -Force
    Write-Host "Release packaged to $destination."
}

function Invoke-OTLPBuildSign {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    $certBase64 = [System.Environment]::GetEnvironmentVariable('SIGNING_CERTIFICATE_BASE64')
    $certPassword = [System.Environment]::GetEnvironmentVariable('SIGNING_CERTIFICATE_PASSWORD')
    if ([string]::IsNullOrWhiteSpace($certBase64)) {
        throw "Signing was requested but the repository secret SIGNING_CERTIFICATE_BASE64 is not available."
    }
    if ([string]::IsNullOrWhiteSpace($certPassword)) {
        throw "Signing was requested but the repository secret SIGNING_CERTIFICATE_PASSWORD is not available."
    }
    Write-Host "Signing PSOTLP release artifacts. Please Wait..."
    $bytes = [Convert]::FromBase64String($certBase64)
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($bytes, $certPassword, 'Exportable,PersistKeySet')
    $releaseDir = [System.IO.Path]::Combine($Context.ReleasesDir.FullName, $Context.Version, 'PSOTLP')
    if (-not [System.IO.Directory]::Exists($releaseDir)) { throw "Release staging folder not found: $releaseDir. Run with -CreateRelease first." }
    $timestampServer = [System.Environment]::GetEnvironmentVariable('TIMESTAMP_SERVER_URI')
    Get-ChildItem -Path $releaseDir -Recurse -File -Include *.dll,*.ps1,*.psm1,*.psd1 | ForEach-Object {
        $signParams = @{ FilePath = $_.FullName; Certificate = $cert; HashAlgorithm = 'SHA256' }
        if ($timestampServer) { $signParams['TimestampServer'] = $timestampServer }
        Set-AuthenticodeSignature @signParams | Out-Null
    }
    Write-Host "PSOTLP release artifact signing was successful."
}

function Publish-OTLPRelease {
    [CmdletBinding()] param(
        [Parameter(Mandatory)] $Context,
        [switch]$PublishPowerShellGallery,
        [switch]$PublishNuGet
    )
    $releaseModule = [System.IO.Path]::Combine($Context.ReleasesDir.FullName, $Context.Version, 'PSOTLP')
    if (-not [System.IO.Directory]::Exists($releaseModule)) {
        throw "Release staging folder not found: $releaseModule. Run with -CreateRelease first."
    }
    if ($PublishPowerShellGallery) {
        $key = [System.Environment]::GetEnvironmentVariable('POWERSHELL_GALLERY_API_KEY')
        if ([string]::IsNullOrWhiteSpace($key)) {
            throw "The PowerShell Gallery publish step could not start because the repository secret POWERSHELL_GALLERY_API_KEY is not available."
        }
        Write-Host "Publishing PSOTLP $($Context.Version) to the PowerShell Gallery. Please Wait..."
        Publish-Module -Path $releaseModule -NuGetApiKey $key -Repository 'PSGallery' -Force -ErrorAction Stop
        Write-Host "PSOTLP PowerShell Gallery publish was successful."
    }
    if ($PublishNuGet) {
        $key = [System.Environment]::GetEnvironmentVariable('NUGET_API_KEY')
        $source = [System.Environment]::GetEnvironmentVariable('NUGET_SOURCE_URI')
        if ([string]::IsNullOrWhiteSpace($key)) {
            throw "The NuGet publish step could not start because the repository secret NUGET_API_KEY is not available."
        }
        if ([string]::IsNullOrWhiteSpace($source)) {
            throw "The NuGet publish step could not start because the repository secret NUGET_SOURCE_URI is not available."
        }
        Write-Host "Publishing PSOTLP $($Context.Version) to NuGet feed $source. Please Wait..."
        $repoName = 'PSOTLPPublishFeed'
        if (-not (Get-PSRepository -Name $repoName -ErrorAction SilentlyContinue)) {
            Register-PSRepository -Name $repoName -SourceLocation $source -PublishLocation $source -InstallationPolicy Trusted | Out-Null
        }
        Publish-Module -Path $releaseModule -NuGetApiKey $key -Repository $repoName -Force -ErrorAction Stop
        Write-Host "PSOTLP NuGet publish was successful."
    }
}

function Invoke-OTLPBuildCommit {
    [CmdletBinding()] param([Parameter(Mandatory)] $Context)
    Push-Location $Context.RepoRoot.FullName
    try {
        & git add -A | Out-Null
        $status = (& git status --porcelain).Trim()
        if (-not $status) { Write-Host "No changes to commit."; return }
        $message = "Build $($Context.Version) ($($Context.CommitHash.Substring(0, [Math]::Min(7,$Context.CommitHash.Length))))"
        & git commit -m $message | Write-Host
    }
    finally { Pop-Location }
}
