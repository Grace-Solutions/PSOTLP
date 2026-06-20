#requires -Version 5.1
<#
.SYNOPSIS
    Idempotent build, package, test, and release driver for the PSOTLP module.

.DESCRIPTION
    Generates the build version once, embeds the git commit hash, restores and
    compiles the C# binary module, generates the manifest and loader, stages
    output under Module/PSOTLP/bin, and (optionally) runs tests, packages a
    release, validates the module import, and creates a git commit.

.PARAMETER Configuration
    Build configuration. Debug or Release. Defaults to Release.

.PARAMETER Clean
    Remove generated output before building.

.PARAMETER Restore
    Run dotnet restore before building.

.PARAMETER RunTests
    Execute the Pester unit tests after the build.

.PARAMETER RunIntegrationTests
    Execute the Pester integration tests after the build (requires endpoint env vars).

.PARAMETER CreateRelease
    Stage a release folder under Releases/<version>.

.PARAMETER CommitOnSuccess
    Commit the build outputs only after every requested step succeeded.

.PARAMETER Publish
    Publish the staged release package to one or more destinations.

.PARAMETER PublishPowerShellGallery
    Push the release to the PowerShell Gallery (requires POWERSHELL_GALLERY_API_KEY).

.PARAMETER PublishNuGet
    Push the release to a NuGet feed (requires NUGET_API_KEY and NUGET_SOURCE_URI).

.PARAMETER Sign
    Sign the release artifacts (requires SIGNING_CERTIFICATE_BASE64 and SIGNING_CERTIFICATE_PASSWORD).

.PARAMETER CI
    Indicates the build is running inside a CI/CD pipeline. Tightens validation and disables prompts.

.PARAMETER Force
    Overwrite an existing release with the same version. Also regenerates PSM1.
#>
[CmdletBinding()]
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
    [switch]$CommitOnSuccess,
    [switch]$CI,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$RepoRoot = [System.IO.DirectoryInfo](Split-Path -Path $PSCommandPath -Parent)
$BuildModuleDir = [System.IO.DirectoryInfo][System.IO.Path]::Combine($RepoRoot.FullName, 'build')

if (-not $BuildModuleDir.Exists) { $BuildModuleDir.Create() | Out-Null }

. ([System.IO.Path]::Combine($BuildModuleDir.FullName, 'BuildHelpers.ps1'))

$Context = New-OTLPBuildContext -RepoRoot $RepoRoot.FullName -Configuration $Configuration

Write-Host "PSOTLP build version: $($Context.Version)"
Write-Host "PSOTLP commit hash : $($Context.CommitHash)"

if ($Clean) { Invoke-OTLPBuildClean -Context $Context }

Invoke-OTLPBuildRestore -Context $Context -Always:$Restore
Invoke-OTLPBuildCompile -Context $Context
Publish-OTLPModuleAssets -Context $Context -Force:$Force
Write-OTLPModuleManifest -Context $Context
Write-OTLPModuleLoader -Context $Context -Force:$Force
Update-OTLPExternalHelp -Context $Context
Update-OTLPChangeLog -Context $Context

if ($RunTests) { Invoke-OTLPUnitTests -Context $Context }
if ($RunIntegrationTests) { Invoke-OTLPIntegrationTests -Context $Context }

Test-OTLPModuleImport -Context $Context

if ($CreateRelease) { New-OTLPReleasePackage -Context $Context -Force:$Force }

if ($Sign) { Invoke-OTLPBuildSign -Context $Context }

if ($Publish) {
    Publish-OTLPRelease -Context $Context -PublishPowerShellGallery:$PublishPowerShellGallery -PublishNuGet:$PublishNuGet
}

if ($CommitOnSuccess) { Invoke-OTLPBuildCommit -Context $Context }

Write-Host "PSOTLP build complete."
