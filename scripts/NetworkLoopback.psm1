Set-StrictMode -Version Latest

function Get-AvailableUdpPort {
    [CmdletBinding()]
    param(
        [int[]]$ExcludePorts = @()
    )

    for ($attempt = 0; $attempt -lt 1000; $attempt++) {
        $udp = [System.Net.Sockets.UdpClient]::new(0)
        try {
            $port = ([System.Net.IPEndPoint]$udp.Client.LocalEndPoint).Port
        }
        finally {
            $udp.Dispose()
        }

        if ($ExcludePorts -notcontains $port) {
            return $port
        }
    }

    throw 'Unable to allocate a UDP port outside the excluded set.'
}

function Get-NetworkLoopbackPlayerArguments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('host', 'client')]
        [string]$Role,

        [Parameter(Mandatory)]
        [string]$RunId,

        [Parameter(Mandatory)]
        [ValidateRange(1, 65535)]
        [int]$Port,

        [Parameter(Mandatory)]
        [string]$ArtifactsRoot,

        [Parameter(Mandatory)]
        [string]$LogPath,

        [ValidateRange(5, 600)]
        [int]$TimeoutSeconds = 60
    )

    return @(
        '-batchmode'
        '-nographics'
        '-logFile'
        [System.IO.Path]::GetFullPath($LogPath)
        '-networkTestRole'
        $Role
        '-networkTestRunId'
        $RunId
        '-networkTestPort'
        $Port.ToString([System.Globalization.CultureInfo]::InvariantCulture)
        '-networkTestArtifacts'
        [System.IO.Path]::GetFullPath($ArtifactsRoot)
        '-networkTestTimeout'
        $TimeoutSeconds.ToString([System.Globalization.CultureInfo]::InvariantCulture)
    )
}

function Test-NetworkLoopbackCheckpoint {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$CheckpointPath,

        [Parameter(Mandatory)]
        [string]$Checkpoint
    )

    if (-not (Test-Path -LiteralPath $CheckpointPath -PathType Leaf)) {
        return $false
    }

    foreach ($line in Get-Content -LiteralPath $CheckpointPath) {
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }

        try {
            $record = $line | ConvertFrom-Json
            if ($record.checkpoint -eq $Checkpoint) {
                return $true
            }
        }
        catch {
            continue
        }
    }

    return $false
}

function Read-NetworkLoopbackResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ResultPath
    )

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        throw "Network loopback result not found: $ResultPath"
    }

    return Get-Content -LiteralPath $ResultPath -Raw | ConvertFrom-Json
}

function Read-NetworkLoopbackResultIfPresent {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ResultPath
    )

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        return $null
    }

    try {
        return Read-NetworkLoopbackResult -ResultPath $ResultPath
    }
    catch {
        return $null
    }
}

function Write-NetworkLoopbackSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$SummaryPath,

        [Parameter(Mandatory)]
        [string]$BatchId,

        [Parameter(Mandatory)]
        [ValidateRange(1, 100)]
        [int]$RequestedIterations,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]$Runs
    )

    $runRecords = @($Runs)
    $passedIterations = @($runRecords | Where-Object { $_.success -eq $true }).Count
    $completedIterations = $runRecords.Count
    $failedIterations = $completedIterations - $passedIterations
    $summary = [ordered]@{
        batchId = $BatchId
        requestedIterations = $RequestedIterations
        completedIterations = $completedIterations
        passedIterations = $passedIterations
        failedIterations = $failedIterations
        success = ($completedIterations -eq $RequestedIterations -and $failedIterations -eq 0)
        runs = $runRecords
    }

    $fullSummaryPath = [System.IO.Path]::GetFullPath($SummaryPath)
    $summaryDirectory = Split-Path -Parent $fullSummaryPath
    New-Item -ItemType Directory -Path $summaryDirectory -Force | Out-Null

    $temporaryPath = '{0}.tmp.{1}' -f $fullSummaryPath, [Guid]::NewGuid().ToString('N')
    try {
        $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $temporaryPath -Encoding UTF8
        if ([System.IO.File]::Exists($fullSummaryPath)) {
            $nullBackupPath = [System.Management.Automation.Language.NullString]::Value
            [System.IO.File]::Replace($temporaryPath, $fullSummaryPath, $nullBackupPath)
        }
        else {
            [System.IO.File]::Move($temporaryPath, $fullSummaryPath)
        }
    }
    finally {
        if ([System.IO.File]::Exists($temporaryPath)) {
            [System.IO.File]::Delete($temporaryPath)
        }
    }
}

Export-ModuleMember -Function @(
    'Get-AvailableUdpPort'
    'Get-NetworkLoopbackPlayerArguments'
    'Test-NetworkLoopbackCheckpoint'
    'Read-NetworkLoopbackResult'
    'Read-NetworkLoopbackResultIfPresent'
    'Write-NetworkLoopbackSummary'
)
