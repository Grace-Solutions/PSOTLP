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
            'Write-OTLPLog','Send-OTLPLogBatch','Invoke-OTLPScript',
            'Start-OTLPSpan','Stop-OTLPSpan','Write-OTLPSpanEvent','Send-OTLPTraceBatch',
            'Write-OTLPMetric','Send-OTLPMetricBatch'
        )
        foreach ($name in $expected) { $module.ExportedCmdlets.Keys | Should -Contain $name }
    }

    It 'manifest version matches assembly version' {
        $manifest = Import-PowerShellDataFile -Path $script:ModuleManifest
        $assembly = [System.Reflection.AssemblyName]::GetAssemblyName($script:AssemblyPath)
        $assembly.Version | Should -Be ([Version]$manifest.ModuleVersion)
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

    It 'returns the base endpoint unchanged when noSignalPath is set' {
        $base = [Uri]'https://proxy.example.com/ingest'
        $definition = [PSOTLP.Endpoints.OTLPEndpointRegistry]::Get('ExportLogs')
        $uri = [PSOTLP.Http.OTLPUriBuilder]::Build($base, $definition, $null, [PSOTLP.Common.OTLPEncoding]::Json, $true)
        $uri.AbsoluteUri | Should -Be 'https://proxy.example.com/ingest'
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
        $queue = New-Object PSOTLP.Sessions.OTLPSessionQueue(2, [PSOTLP.Common.OTLPSessionDropPolicy]::DropOldest)
        for ($i = 0; $i -lt 4; $i++) {
            $record = New-Object PSOTLP.Models.OTLPLogRecord
            $record.Body = "item-$i"
            $null = $queue.Enqueue($record)
        }
        $queue.Count | Should -Be 2
        $queue.Dropped | Should -BeGreaterOrEqual 2
    }
}

Describe 'OTLPStreamHook capture' {
    BeforeAll {
        $script:Queue = New-Object PSOTLP.Sessions.OTLPSessionQueue(1000, [PSOTLP.Common.OTLPSessionDropPolicy]::DropOldest)
        $script:Session = New-Object PSOTLP.Sessions.OTLPSession
        $script:Session.SessionId = [Guid]::NewGuid()
        $script:Session.SessionName = 'unit-stream-hook'
        $script:Session.IsActive = $true
        $patterns = [System.Text.RegularExpressions.Regex[]]@([regex]'(?i)custom-secret\s*=\s*\S+')
        $script:Redaction = [PSOTLP.Redaction.OTLPRedactionEngine]::new([System.Collections.Generic.IEnumerable[System.Text.RegularExpressions.Regex]]$patterns)
        $script:Hook = New-Object PSOTLP.Sessions.OTLPStreamHook($script:Queue, $script:Redaction, $script:Session, $null)

        $runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace(
            [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault())
        $runspace.Open()
        $script:PSInstance = [System.Management.Automation.PowerShell]::Create()
        $script:PSInstance.Runspace = $runspace
        $script:Hook.Attach($script:PSInstance)

        $null = $script:PSInstance.AddScript(@'
$VerbosePreference = 'Continue'
$DebugPreference = 'Continue'
$InformationPreference = 'Continue'
Write-Verbose 'verbose-line custom-secret=hunter2'
Write-Debug 'debug-line'
Write-Warning 'warning-line'
Write-Information 'info-line'
Write-Error 'error-line' -ErrorAction Continue
'plain-output'
'@)
        $script:Results = $script:PSInstance.Invoke()
        $script:Hook.Detach($script:PSInstance)
        $script:Hook.DrainOutput($script:Results)
        foreach ($err in $script:PSInstance.Streams.Error) { $script:Hook.HandleError($err) }
        $script:Bodies = $script:Queue.DrainBatch(1000)
    }

    AfterAll {
        if ($script:PSInstance) { $script:PSInstance.Dispose() }
    }

    It 'captures a record for every PowerShell stream' {
        $streams = $script:Bodies | ForEach-Object { $_.Attributes['powershell.stream'] } | Sort-Object -Unique
        foreach ($name in @('Verbose','Debug','Warning','Information','Error','Output')) {
            $streams | Should -Contain $name
        }
    }

    It 'maps Error stream records to severity 17' {
        $err = $script:Bodies | Where-Object { $_.Attributes['powershell.stream'] -eq 'Error' } | Select-Object -First 1
        [PSOTLP.Common.OTLPSeverityMapper]::ToNumber($err.Severity) | Should -Be 17
    }

    It 'applies connection-level RedactPattern to captured bodies' {
        $verbose = $script:Bodies | Where-Object { $_.Attributes['powershell.stream'] -eq 'Verbose' } | Select-Object -First 1
        $verbose.Body | Should -Not -Match 'hunter2'
        $verbose.Body | Should -Match '\[REDACTED\]'
    }

    It 'tags every record with the originating session id' {
        $expected = $script:Session.SessionId.ToString()
        foreach ($record in $script:Bodies) {
            $record.Attributes['powershell.session.id'] | Should -Be $expected
        }
    }
}

Describe 'OTLPConnection redaction wiring' {
    It 'stores Regex[] patterns supplied to Connect-OTLP on the active connection' {
        $pattern = [regex]'(?i)x-test-secret\s*=\s*\S+'
        Connect-OTLP -EndpointUri ([Uri]'http://localhost:4318') -RedactPattern @($pattern)
        try {
            $connection = [PSOTLP.Connections.OTLPSessionManager]::CurrentConnection
            $prop = [PSOTLP.Connections.OTLPConnection].GetProperty('RedactPatterns', [System.Reflection.BindingFlags]'Instance,NonPublic')
            $patterns = $prop.GetValue($connection)
            $patterns.Count | Should -BeGreaterOrEqual 1
            $patterns[0].ToString() | Should -Be $pattern.ToString()
        }
        finally {
            Disconnect-OTLP -ErrorAction SilentlyContinue
        }
    }
}
