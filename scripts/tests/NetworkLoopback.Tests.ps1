$modulePath = Join-Path $PSScriptRoot '..\NetworkLoopback.psm1'
$runnerPath = Join-Path $PSScriptRoot '..\Run-NetworkLoopback.ps1'
Import-Module $modulePath -Force

Describe 'Network loopback orchestration helpers' {
    It 'exposes an Iterations parameter constrained to 1 through 100' {
        $scriptPath = Join-Path $PSScriptRoot '..\Run-NetworkLoopback.ps1'
        $command = Get-Command -Name $scriptPath
        $parameter = $command.Parameters['Iterations']

        $parameter | Should Not Be $null
        $range = @($parameter.Attributes | Where-Object {
            $_ -is [System.Management.Automation.ValidateRangeAttribute]
        })
        $range.Count | Should Be 1
        $range[0].MinRange | Should Be 1
        $range[0].MaxRange | Should Be 100
    }

    It 'rejects an explicit fixed port before preparing a multi-round run' {
        $scriptPath = Join-Path $PSScriptRoot '..\Run-NetworkLoopback.ps1'
        $missingBuildPath = Join-Path $TestDrive 'missing-build\AoyiLoopback.exe'

        {
            & $scriptPath `
                -Iterations 2 `
                -Port 18888 `
                -SkipBuild `
                -BuildPath $missingBuildPath
        } | Should Throw 'Iterations greater than 1 cannot use an explicit non-zero Port.'
    }

    It 'resolves the default project root in Windows PowerShell 5.1' {
        $missingBuildPath = Join-Path $TestDrive 'missing-windows-powershell-build\AoyiLoopback.exe'
        $artifactsPath = Join-Path $TestDrive 'windows-powershell-artifacts'

        $output = & powershell.exe `
            -NoProfile `
            -ExecutionPolicy Bypass `
            -File $runnerPath `
            -SkipBuild `
            -BuildPath $missingBuildPath `
            -ArtifactsRoot $artifactsPath 2>&1
        $combinedOutput = $output -join [Environment]::NewLine

        $LASTEXITCODE | Should Be 1
        $combinedOutput | Should Match 'Loopback player build not found:'
    }

    It 'allocates a bindable UDP port' {
        $port = Get-AvailableUdpPort

        $port | Should BeGreaterThan 0
        $port | Should BeLessThan 65536

        $udp = [System.Net.Sockets.UdpClient]::new($port)
        $udp.Dispose()
    }

    It 'excludes ports already used by the current batch' {
        $firstPort = Get-AvailableUdpPort
        $secondPort = Get-AvailableUdpPort -ExcludePorts @($firstPort)

        $secondPort | Should Not Be $firstPort
    }

    It 'builds isolated host player arguments' {
        $arguments = Get-NetworkLoopbackPlayerArguments `
            -Role host `
            -RunId run-123 `
            -Port 18888 `
            -ArtifactsRoot 'D:\tmp\run-123' `
            -LogPath 'D:\tmp\run-123\host.log' `
            -TimeoutSeconds 45

        ($arguments -contains '-networkTestRole') | Should Be $true
        ($arguments -contains 'host') | Should Be $true
        ($arguments -contains '-networkTestPort') | Should Be $true
        ($arguments -contains '18888') | Should Be $true
        ($arguments -contains '-logFile') | Should Be $true
        ($arguments -contains 'D:\tmp\run-123\host.log') | Should Be $true
    }

    It 'detects an exact checkpoint in NDJSON' {
        $checkpointPath = Join-Path $TestDrive 'host-checkpoints.ndjson'
        @'
{"checkpoint":"bootstrap"}
{"checkpoint":"transport-started"}
'@ | Set-Content -LiteralPath $checkpointPath

        Test-NetworkLoopbackCheckpoint `
            -CheckpointPath $checkpointPath `
            -Checkpoint 'transport-started' | Should Be $true
        Test-NetworkLoopbackCheckpoint `
            -CheckpointPath $checkpointPath `
            -Checkpoint 'battle-scene' | Should Be $false
    }

    It 'reads a successful player result' {
        $resultPath = Join-Path $TestDrive 'host-result.json'
        '{"runId":"run-123","role":"host","success":true,"checkpoint":"battle-scene"}' |
            Set-Content -LiteralPath $resultPath

        $result = Read-NetworkLoopbackResult -ResultPath $resultPath

        $result.runId | Should Be 'run-123'
        $result.role | Should Be 'host'
        $result.success | Should Be $true
        $result.checkpoint | Should Be 'battle-scene'
    }

    It 'reads an available player result without failing when it is absent' {
        $resultPath = Join-Path $TestDrive 'available-host-result.json'
        '{"runId":"run-available","role":"host","success":false,"checkpoint":"battle-scene"}' |
            Set-Content -LiteralPath $resultPath

        $availableResult = Read-NetworkLoopbackResultIfPresent -ResultPath $resultPath
        $missingResult = Read-NetworkLoopbackResultIfPresent -ResultPath (Join-Path $TestDrive 'missing-result.json')

        $availableResult.runId | Should Be 'run-available'
        $availableResult.role | Should Be 'host'
        $availableResult.success | Should Be $false
        $missingResult | Should Be $null
    }

    It 'preserves available role results in a failed batch run record' {
        $runnerSource = Get-Content -LiteralPath $runnerPath -Raw

        $runnerSource | Should Match '(?s)catch\s*\{.*Read-NetworkLoopbackResultIfPresent.*host\s*=\s*\$hostResult.*client\s*=\s*\$clientResult.*break'
    }

    It 'writes an aggregate network loopback summary' {
        $summaryPath = Join-Path $TestDrive 'batch-summary.json'
        $runs = @(
            [pscustomobject]@{
                runId = 'run-001'
                success = $true
            }
            [pscustomobject]@{
                runId = 'run-002'
                success = $false
            }
        )

        Write-NetworkLoopbackSummary `
            -SummaryPath $summaryPath `
            -BatchId 'batch-001' `
            -RequestedIterations 2 `
            -Runs $runs

        $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json

        $summary.requestedIterations | Should Be 2
        $summary.completedIterations | Should Be 2
        $summary.passedIterations | Should Be 1
        $summary.failedIterations | Should Be 1
        $summary.success | Should Be $false
        (@($summary.runs | ForEach-Object { $_.runId }) -contains 'run-001') | Should Be $true
        (@($summary.runs | ForEach-Object { $_.runId }) -contains 'run-002') | Should Be $true
    }

    It 'atomically replaces an existing summary without temporary files' {
        $summaryDirectory = Join-Path $TestDrive 'replacement'
        $summaryPath = Join-Path $summaryDirectory 'summary.json'

        Write-NetworkLoopbackSummary `
            -SummaryPath $summaryPath `
            -BatchId 'batch-old' `
            -RequestedIterations 1 `
            -Runs @([pscustomobject]@{ runId = 'run-old'; success = $true })
        Write-NetworkLoopbackSummary `
            -SummaryPath $summaryPath `
            -BatchId 'batch-new' `
            -RequestedIterations 1 `
            -Runs @([pscustomobject]@{ runId = 'run-new'; success = $true })

        $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json

        $summary.batchId | Should Be 'batch-new'
        $summary.runs[0].runId | Should Be 'run-new'
        @(Get-ChildItem -LiteralPath $summaryDirectory -Filter 'summary.json.tmp*').Count | Should Be 0
    }

    It 'writes an incomplete summary for an empty run collection' {
        $summaryPath = Join-Path $TestDrive 'empty-summary.json'

        Write-NetworkLoopbackSummary `
            -SummaryPath $summaryPath `
            -BatchId 'batch-empty' `
            -RequestedIterations 2 `
            -Runs @()

        $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json

        $summary.completedIterations | Should Be 0
        $summary.passedIterations | Should Be 0
        $summary.failedIterations | Should Be 0
        $summary.success | Should Be $false
    }

}
