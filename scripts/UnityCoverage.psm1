Set-StrictMode -Version Latest

function Get-UnityProjectVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectRoot
    )

    $versionFile = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "Unity project version file not found: $versionFile"
    }

    $match = Select-String -LiteralPath $versionFile -Pattern '^m_EditorVersion:\s*(\S+)\s*$' |
        Select-Object -First 1
    if ($null -eq $match) {
        throw "Unity editor version is missing from: $versionFile"
    }

    return $match.Matches[0].Groups[1].Value
}

function Resolve-UnityEditorPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectRoot,

        [string]$UnityExe
    )

    if (-not [string]::IsNullOrWhiteSpace($UnityExe)) {
        if (-not (Test-Path -LiteralPath $UnityExe -PathType Leaf)) {
            throw "Unity executable not found: $UnityExe"
        }

        return (Resolve-Path -LiteralPath $UnityExe).Path
    }

    $version = Get-UnityProjectVersion -ProjectRoot $ProjectRoot
    $candidates = @(
        (Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"),
        (Join-Path ${env:ProgramFiles} "Unity Hub\Editor\$version\Editor\Unity.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw "Unity $version was not found. Pass -UnityExe with the full Unity.exe path."
}

function Get-UnityCoverageArguments {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectRoot,

        [Parameter(Mandatory)]
        [string]$ArtifactsRoot,

        [string]$AssemblyFilters = '+Aoyi.*,+Assembly-CSharp'
    )

    $resolvedProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
    $resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($ArtifactsRoot)
    $coverageRoot = Join-Path $resolvedArtifactsRoot 'coverage'
    $testResults = Join-Path $resolvedArtifactsRoot 'editmode-results.xml'
    $editorLog = Join-Path $resolvedArtifactsRoot 'unity-editmode.log'
    $coverageOptions = "generateHtmlReport;generateAdditionalMetrics;assemblyFilters:$AssemblyFilters;pathFilters:-/Tests/,-/Mirror/,-/Photon/"

    return @(
        '-batchmode'
        '-nographics'
        '-projectPath'
        $resolvedProjectRoot
        '-runTests'
        '-testPlatform'
        'EditMode'
        '-testResults'
        $testResults
        '-debugCodeOptimization'
        '-enableCodeCoverage'
        '-coverageResultsPath'
        $coverageRoot
        '-coverageOptions'
        $coverageOptions
        '-logFile'
        $editorLog
    )
}

function ConvertTo-WindowsCommandLine {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Arguments
    )

    $quoted = foreach ($argument in $Arguments) {
        if ($null -eq $argument -or $argument.Length -eq 0) {
            '""'
            continue
        }

        if ($argument -notmatch '[\s"]') {
            $argument
            continue
        }

        $escaped = [regex]::Replace($argument, '(\\*)"', '$1$1\"')
        $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
        '"' + $escaped + '"'
    }

    return ($quoted -join ' ')
}

function Get-UnityTestSummary {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TestResultsPath
    )

    if (-not (Test-Path -LiteralPath $TestResultsPath -PathType Leaf)) {
        throw "Unity test result not found: $TestResultsPath"
    }

    [xml]$resultXml = Get-Content -LiteralPath $TestResultsPath -Raw
    $testRun = $resultXml.'test-run'
    if ($null -eq $testRun) {
        throw "Unexpected NUnit result format: $TestResultsPath"
    }

    return [pscustomobject]@{
        Total = [int]$testRun.total
        Passed = [int]$testRun.passed
        Failed = [int]$testRun.failed
        Skipped = [int]$testRun.skipped
        Result = [string]$testRun.result
    }
}

Export-ModuleMember -Function @(
    'Get-UnityProjectVersion'
    'Resolve-UnityEditorPath'
    'Get-UnityCoverageArguments'
    'ConvertTo-WindowsCommandLine'
    'Get-UnityTestSummary'
)
