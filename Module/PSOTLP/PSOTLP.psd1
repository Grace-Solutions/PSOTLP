@{
    RootModule = 'PSOTLP.psm1'
    ModuleVersion = '2026.06.20.1826'
    GUID = '0fae6770-1d6a-4f1d-9a3a-1a93b96a3a01'
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
        'Invoke-OTLPScript',
        'Start-OTLPSpan',
        'Stop-OTLPSpan',
        'Write-OTLPSpanEvent',
        'Send-OTLPTraceBatch',
        'Write-OTLPMetric',
        'Send-OTLPMetricBatch'
    )
    AliasesToExport = @()
    FormatsToProcess = @('PSOTLP.Format.ps1xml')
    TypesToProcess = @('PSOTLP.Types.ps1xml')
    PrivateData = @{
        PSData = @{
            Tags = @('OpenTelemetry','OTLP','Logs','Tracing','PowerShell','Observability','GraceSolutions')
            ProjectUri = ''
            ReleaseNotes = ''
            CommitHash = '3702fe1a037e2fa2255db9a8bdbe7dcf3b4bb15d'
        }
    }
}