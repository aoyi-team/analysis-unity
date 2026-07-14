[CmdletBinding()]
param(
    [string]$ProjectRoot,
    [string]$UnityExe,
    [string]$BuildPath,
    [string]$ArtifactsRoot,
    [int]$Port = 0,
    [ValidateRange(1, 100)]
    [int]$Iterations = 1,
    [ValidateRange(10, 600)]
    [int]$TimeoutSeconds = 90,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
}

if ($Iterations -gt 1 -and $Port -ne 0) {
    throw 'Iterations greater than 1 cannot use an explicit non-zero Port.'
}

Import-Module (Join-Path $PSScriptRoot 'UnityCoverage.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'NetworkLoopback.psm1') -Force

function New-NetworkLoopbackRunId {
    return "loopback-$([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
}

function Invoke-NetworkLoopbackRun {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$BuildExe,

        [Parameter(Mandatory)]
        [string]$RunId,

        [Parameter(Mandatory)]
        [ValidateRange(1, 65535)]
        [int]$RunPort,

        [Parameter(Mandatory)]
        [string]$RunArtifacts,

        [ValidateRange(10, 600)]
        [int]$RunTimeoutSeconds
    )

    New-Item -ItemType Directory -Path $RunArtifacts -Force | Out-Null

    $hostLog = Join-Path $RunArtifacts 'host.log'
    $clientLog = Join-Path $RunArtifacts 'client.log'
    $hostArguments = Get-NetworkLoopbackPlayerArguments `
        -Role host `
        -RunId $RunId `
        -Port $RunPort `
        -ArtifactsRoot $RunArtifacts `
        -LogPath $hostLog `
        -TimeoutSeconds $RunTimeoutSeconds
    $clientArguments = Get-NetworkLoopbackPlayerArguments `
        -Role client `
        -RunId $RunId `
        -Port $RunPort `
        -ArtifactsRoot $RunArtifacts `
        -LogPath $clientLog `
        -TimeoutSeconds $RunTimeoutSeconds

    $hostProcess = $null
    $clientProcess = $null
    try {
        Write-Host "Run ID: $runId"
        Write-Host "UDP port: $RunPort"
        Write-Host "Artifacts: $RunArtifacts"

        $hostProcess = Start-Process `
            -FilePath $BuildExe `
            -ArgumentList (ConvertTo-WindowsCommandLine -Arguments $hostArguments) `
            -PassThru

        $hostCheckpointPath = Join-Path $RunArtifacts 'host-checkpoints.ndjson'
        $hostResultPath = Join-Path $RunArtifacts 'host-result.json'
        $readySeconds = [Math]::Min(30, $RunTimeoutSeconds)
        $readyDeadline = [DateTime]::UtcNow.AddSeconds($readySeconds)
        while (-not (Test-NetworkLoopbackCheckpoint -CheckpointPath $hostCheckpointPath -Checkpoint 'transport-started')) {
            if ($hostProcess.HasExited) {
                throw "Host exited before opening the KCP transport. See: $hostLog"
            }
            if (Test-Path -LiteralPath $hostResultPath) {
                $earlyResult = Read-NetworkLoopbackResult -ResultPath $hostResultPath
                throw "Host failed before client start: $($earlyResult.message)"
            }
            if ([DateTime]::UtcNow -ge $readyDeadline) {
                throw "Host did not open the KCP transport within $readySeconds seconds. See: $hostLog"
            }
            Start-Sleep -Milliseconds 100
        }

        $clientProcess = Start-Process `
            -FilePath $BuildExe `
            -ArgumentList (ConvertTo-WindowsCommandLine -Arguments $clientArguments) `
            -PassThru

        $clientResultPath = Join-Path $RunArtifacts 'client-result.json'
        $deadline = [DateTime]::UtcNow.AddSeconds($RunTimeoutSeconds + 10)
        while (-not ((Test-Path -LiteralPath $hostResultPath) -and (Test-Path -LiteralPath $clientResultPath))) {
            if ([DateTime]::UtcNow -ge $deadline) {
                throw 'Loopback run timed out waiting for result files.'
            }
            if ($hostProcess.HasExited -and -not (Test-Path -LiteralPath $hostResultPath)) {
                throw "Host exited without a result file. See: $hostLog"
            }
            if ($clientProcess.HasExited -and -not (Test-Path -LiteralPath $clientResultPath)) {
                throw "Client exited without a result file. See: $clientLog"
            }
            Start-Sleep -Milliseconds 100
        }

        $hostResult = Read-NetworkLoopbackResult -ResultPath $hostResultPath
        $clientResult = Read-NetworkLoopbackResult -ResultPath $clientResultPath
        if (-not $hostResult.success) {
            throw "Host loopback failed at $($hostResult.checkpoint): $($hostResult.message)"
        }
        if (-not $clientResult.success) {
            throw "Client loopback failed at $($clientResult.checkpoint): $($clientResult.message)"
        }
        if ($hostResult.checkpoint -ne 'lobby-scene' -or $clientResult.checkpoint -ne 'lobby-scene') {
            throw 'Both players must finish at the lobby-scene checkpoint.'
        }
        if ($hostResult.playerIndex -eq $clientResult.playerIndex) {
            throw "Loopback players must have distinct player indexes, but both reported $($hostResult.playerIndex)."
        }
        if ($hostResult.teamId -eq $clientResult.teamId) {
            throw "Loopback players must have distinct team IDs, but both reported $($hostResult.teamId)."
        }

        Write-Host "Host: success, player=$($hostResult.playerIndex), team=$($hostResult.teamId), scene=$($hostResult.scene)"
        Write-Host "Client: success, player=$($clientResult.playerIndex), team=$($clientResult.teamId), scene=$($clientResult.scene)"
        Write-Host 'Loopback result: PASS'

        return [pscustomobject]@{
            host = $hostResult
            client = $clientResult
        }
    }
    finally {
        foreach ($process in @($clientProcess, $hostProcess)) {
            if ($null -ne $process -and -not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

$projectPath = (Resolve-Path -LiteralPath $ProjectRoot).Path
$batchId = New-NetworkLoopbackRunId

if ([string]::IsNullOrWhiteSpace($BuildPath)) {
    if ($SkipBuild) {
        $BuildPath = Join-Path $projectPath 'Builds\LanTest\NetworkLoopback\AoyiLoopback.exe'
    }
    else {
        $BuildPath = Join-Path $projectPath "Builds\LanTest\NetworkLoopback\$batchId\AoyiLoopback.exe"
    }
}
$buildExe = [System.IO.Path]::GetFullPath($BuildPath)

if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = Join-Path $projectPath "artifacts\network-loopback\$batchId"
}
$batchArtifacts = [System.IO.Path]::GetFullPath($ArtifactsRoot)
New-Item -ItemType Directory -Path $batchArtifacts -Force | Out-Null

if (-not $SkipBuild) {
    $editorPath = Resolve-UnityEditorPath -ProjectRoot $projectPath -UnityExe $UnityExe
    $buildLog = Join-Path $batchArtifacts 'build.log'
    $buildArguments = @(
        '-batchmode'
        '-nographics'
        '-quit'
        '-projectPath'
        $projectPath
        '-executeMethod'
        'NetworkLoopbackBuild.BuildWindows'
        '-logFile'
        $buildLog
    )

    $previousBuildPath = $env:AOYI_LOOPBACK_BUILD_PATH
    try {
        $env:AOYI_LOOPBACK_BUILD_PATH = $buildExe
        $buildProcess = Start-Process `
            -FilePath $editorPath `
            -ArgumentList (ConvertTo-WindowsCommandLine -Arguments $buildArguments) `
            -Wait `
            -PassThru `
            -NoNewWindow
    }
    finally {
        $env:AOYI_LOOPBACK_BUILD_PATH = $previousBuildPath
    }

    if ($buildProcess.ExitCode -ne 0) {
        throw "Unity loopback build failed with exit code $($buildProcess.ExitCode). See: $buildLog"
    }
}

if (-not (Test-Path -LiteralPath $buildExe -PathType Leaf)) {
    throw "Loopback player build not found: $buildExe"
}

if ($Iterations -gt 1) {
    Write-Host "Batch ID: $batchId"
    Write-Host "Iterations: $Iterations"
    Write-Host "Batch artifacts: $batchArtifacts"
}

$runs = @()
$usedPorts = @()
$batchFailure = $null
for ($iteration = 1; $iteration -le $Iterations; $iteration++) {
    $runId = if ($Iterations -eq 1) { $batchId } else { New-NetworkLoopbackRunId }
    $runArtifacts = if ($Iterations -eq 1) {
        $batchArtifacts
    }
    else {
        Join-Path $batchArtifacts $runId
    }
    $runPort = if ($Port -ne 0) {
        $Port
    }
    else {
        Get-AvailableUdpPort -ExcludePorts $usedPorts
    }
    $usedPorts += $runPort

    if ($Iterations -gt 1) {
        Write-Host "Iteration $iteration/$Iterations"
    }

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $pairResult = Invoke-NetworkLoopbackRun `
            -BuildExe $buildExe `
            -RunId $runId `
            -RunPort $runPort `
            -RunArtifacts $runArtifacts `
            -RunTimeoutSeconds $TimeoutSeconds
        $stopwatch.Stop()
        $runs += [pscustomobject]@{
            iteration = $iteration
            runId = $runId
            port = $runPort
            success = $true
            durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
            artifacts = $runArtifacts
            host = $pairResult.host
            client = $pairResult.client
            error = $null
        }
    }
    catch {
        $stopwatch.Stop()
        $batchFailure = $_.Exception
        $hostResult = Read-NetworkLoopbackResultIfPresent `
            -ResultPath (Join-Path $runArtifacts 'host-result.json')
        $clientResult = Read-NetworkLoopbackResultIfPresent `
            -ResultPath (Join-Path $runArtifacts 'client-result.json')
        $runs += [pscustomobject]@{
            iteration = $iteration
            runId = $runId
            port = $runPort
            success = $false
            durationSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 3)
            artifacts = $runArtifacts
            host = $hostResult
            client = $clientResult
            error = $_.Exception.Message
        }
        break
    }
}

$summaryPath = Join-Path $batchArtifacts 'summary.json'
Write-NetworkLoopbackSummary `
    -SummaryPath $summaryPath `
    -BatchId $batchId `
    -RequestedIterations $Iterations `
    -Runs $runs
Write-Host "Summary: $summaryPath"

if ($null -ne $batchFailure) {
    throw "Network loopback batch failed: $($batchFailure.Message) Summary: $summaryPath"
}

if ($Iterations -gt 1) {
    Write-Host "Loopback batch result: PASS ($Iterations/$Iterations)"
}
