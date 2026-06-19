#requires -Version 5.1
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Integration tests for PSOTLP. These tests are skipped unless PSOTLP_ENDPOINT_URI is set.
# Required environment variables:
#   PSOTLP_ENDPOINT_URI
# Optional:
#   PSOTLP_LOGS_ENDPOINT_URI
#   PSOTLP_AUTHORIZATION_HEADER
#   PSOTLP_SERVICE_NAME

BeforeAll {
    $script:RepoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..\..')).Path
    $script:ModuleManifest = Join-Path -Path $script:RepoRoot -ChildPath 'Module/PSOTLP/PSOTLP.psd1'
    $script:EndpointUri = [System.Environment]::GetEnvironmentVariable('PSOTLP_ENDPOINT_URI')
    $script:RunIntegration = -not [string]::IsNullOrWhiteSpace($script:EndpointUri)
    if ($script:RunIntegration) {
        Import-Module -Name $script:ModuleManifest -Force
    }
}

AfterAll {
    if ($script:RunIntegration) {
        try { Disconnect-OTLP -ErrorAction SilentlyContinue } catch { }
        Remove-Module -Name PSOTLP -Force -ErrorAction SilentlyContinue
    }
}

Describe 'PSOTLP integration: Connect-OTLP' {
    It 'opens a connection to the configured endpoint' -Skip:(-not $script:RunIntegration) {
        $params = @{ EndpointUri = [Uri]$script:EndpointUri; ServiceName = ([System.Environment]::GetEnvironmentVariable('PSOTLP_SERVICE_NAME') ?? 'psotlp-integration') }
        $authHeader = [System.Environment]::GetEnvironmentVariable('PSOTLP_AUTHORIZATION_HEADER')
        if ($authHeader) { $params['Header'] = @{ Authorization = $authHeader } }
        $connection = Connect-OTLP @params -PassThru
        $connection | Should -Not -BeNullOrEmpty
        $connection.EndpointUri | Should -Be ([Uri]$script:EndpointUri)
    }
}

Describe 'PSOTLP integration: Write-OTLPLog' {
    It 'exports a single log record without throwing' -Skip:(-not $script:RunIntegration) {
        { Write-OTLPLog -Body 'integration-test-record' -Severity Information -Attribute @{ scenario = 'unit' } } | Should -Not -Throw
    }
}

Describe 'PSOTLP integration: Send-OTLPLogBatch' {
    It 'exports a small batch of log records without throwing' -Skip:(-not $script:RunIntegration) {
        $records = 1..3 | ForEach-Object {
            $record = New-Object PSOTLP.Models.OTLPLogRecord
            $record.Body = "integration-batch-$_"
            $record.Severity = [PSOTLP.Common.OTLPSeverity]::Information
            $record.TimestampUtc = [DateTimeOffset]::UtcNow
            $record.ObservedTimestampUtc = [DateTimeOffset]::UtcNow
            $record
        }
        { $records | Send-OTLPLogBatch } | Should -Not -Throw
    }
}

Describe 'PSOTLP integration: Invoke-OTLPScript' {
    It 'captures and exports script-block output without throwing' -Skip:(-not $script:RunIntegration) {
        { Invoke-OTLPScript -ScriptBlock { Write-Information 'integration-script' -InformationAction Continue } } | Should -Not -Throw
    }
}
