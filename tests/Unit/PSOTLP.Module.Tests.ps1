#requires -Version 5.1
#requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $script:RepoRoot = (Resolve-Path -Path (Join-Path -Path $PSScriptRoot -ChildPath '..\..')).Path
    $script:ModuleManifest = Join-Path -Path $script:RepoRoot -ChildPath 'Module/PSOTLP/PSOTLP.psd1'
    $script:ModuleLoader = Join-Path -Path $script:RepoRoot -ChildPath 'Module/PSOTLP/PSOTLP.psm1'
    $script:ModuleBinDir = Join-Path -Path $script:RepoRoot -ChildPath 'Module/PSOTLP/bin'
    $script:AssemblyPath = Join-Path -Path $script:ModuleBinDir -ChildPath 'PSOTLP.dll'

    if (-not (Test-Path -Path $script:ModuleManifest)) {
        throw "Module manifest not found. Run build.ps1 before running unit tests."
    }
    Import-Module -Name $script:ModuleManifest -Force
}

AfterAll {
    Remove-Module -Name PSOTLP -Force -ErrorAction SilentlyContinue
}

Describe 'PSOTLP module manifest' {
    It 'declares all required cmdlets' {
        $module = Get-Module -Name PSOTLP
        $expected = @(
            'Connect-OTLP','Disconnect-OTLP','Get-OTLPConnection',
            'Write-OTLPLog','Send-OTLPLogBatch',
            'Start-OTLPSession','Stop-OTLPSession','Get-OTLPSession','Invoke-OTLPScript',
            'Start-OTLPSpan','Stop-OTLPSpan','Write-OTLPSpanEvent','Send-OTLPTraceBatch'
        )
        foreach ($name in $expected) { $module.ExportedCmdlets.Keys | Should -Contain $name }
    }

    It 'manifest version matches assembly version' {
        $manifest = Import-PowerShellDataFile -Path $script:ModuleManifest
        $assembly = [System.Reflection.AssemblyName]::GetAssemblyName($script:AssemblyPath)
        $assembly.Version.ToString() | Should -Be $manifest.ModuleVersion
    }

    It 'manifest carries a non-empty commit hash' {
        $manifest = Import-PowerShellDataFile -Path $script:ModuleManifest
        $manifest.PrivateData.PSData.CommitHash | Should -Not -BeNullOrEmpty
    }

    It 'PSM1 references the binary under the bin folder' {
        $loader = Get-Content -Path $script:ModuleLoader -Raw
        $loader | Should -Match "bin"
        $loader | Should -Match "PSOTLP.dll"
    }
}

Describe 'OTLP endpoint registry' {
    It 'returns the ExportLogs endpoint definition' {
        $definition = [PSOTLP.Endpoints.OTLPEndpointRegistry]::Get('ExportLogs')
        $definition.DefaultPath | Should -Be '/v1/logs'
        $definition.Method | Should -Be 'POST'
    }

    It 'returns the ExportTraces endpoint definition' {
        $definition = [PSOTLP.Endpoints.OTLPEndpointRegistry]::Get('ExportTraces')
        $definition.DefaultPath | Should -Be '/v1/traces'
    }

    It 'returns the ExportMetrics endpoint definition' {
        $definition = [PSOTLP.Endpoints.OTLPEndpointRegistry]::Get('ExportMetrics')
        $definition.DefaultPath | Should -Be '/v1/metrics'
    }
}

Describe 'OTLP URI builder' {
    It 'builds /v1/logs from a base endpoint' {
        $base = [Uri]'https://otel.example.com'
        $definition = [PSOTLP.Endpoints.OTLPEndpointRegistry]::Get('ExportLogs')
        $uri = [PSOTLP.Http.OTLPUriBuilder]::Build($base, $definition, $null)
        $uri.AbsoluteUri | Should -Be 'https://otel.example.com/v1/logs'
    }

    It 'respects a signal-specific override' {
        $base = [Uri]'https://otel.example.com'
        $override = [Uri]'https://otel.example.com/custom/logs'
        $definition = [PSOTLP.Endpoints.OTLPEndpointRegistry]::Get('ExportLogs')
        $uri = [PSOTLP.Http.OTLPUriBuilder]::Build($base, $definition, $override)
        $uri.AbsoluteUri | Should -Be 'https://otel.example.com/custom/logs'
    }
}

Describe 'OTLP log formatter' {
    It 'produces the centralized timestamp format' {
        $line = [PSOTLP.Logging.OTLPLogFormatter]::Format('Info', 'Component', 'message')
        $line | Should -Match '^\[\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{7}Z\] - \[Info\] - \[Component\] - message$'
    }
}

Describe 'OTLP redaction engine' {
    It 'redacts password-like values' {
        $engine = New-Object PSOTLP.Redaction.OTLPRedactionEngine
        $output = $engine.Redact('password=hunter2 next')
        $output | Should -Not -Match 'hunter2'
        $output | Should -Match '\[REDACTED\]'
    }

    It 'redacts bearer tokens' {
        $engine = New-Object PSOTLP.Redaction.OTLPRedactionEngine
        $output = $engine.Redact('Authorization: Bearer ABC.DEF.GHI')
        $output | Should -Not -Match 'ABC\.DEF\.GHI'
    }

    It 'redacts api-key values' {
        $engine = New-Object PSOTLP.Redaction.OTLPRedactionEngine
        $output = $engine.Redact('api-key=topsecret123')
        $output | Should -Not -Match 'topsecret123'
    }
}

Describe 'OTLP severity mapper' {
    It 'maps Error stream to severity 17' {
        $severity = [PSOTLP.Common.OTLPSeverityMapper]::FromStreamName('Error')
        [PSOTLP.Common.OTLPSeverityMapper]::ToNumber($severity) | Should -Be 17
    }

    It 'maps Warning stream to severity 13' {
        $severity = [PSOTLP.Common.OTLPSeverityMapper]::FromStreamName('Warning')
        [PSOTLP.Common.OTLPSeverityMapper]::ToNumber($severity) | Should -Be 13
    }

    It 'maps Verbose stream to Debug severity' {
        $severity = [PSOTLP.Common.OTLPSeverityMapper]::FromStreamName('Verbose')
        $severity | Should -Be ([PSOTLP.Common.OTLPSeverity]::Debug)
    }
}

Describe 'OTLPSessionQueue drop policy' {
    It 'drops the oldest record when capacity is reached under DropOldest policy' {
        $queue = New-Object PSOTLP.Sessions.OTLPSessionQueue(2, [PSOTLP.Sessions.OTLPSessionDropPolicy]::DropOldest)
        for ($i = 0; $i -lt 4; $i++) {
            $record = New-Object PSOTLP.Models.OTLPLogRecord
            $record.Body = "item-$i"
            $null = $queue.Enqueue($record)
        }
        $queue.Count | Should -Be 2
        $queue.Dropped | Should -BeGreaterOrEqual 2
    }
}
