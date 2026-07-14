[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$UnityExe,
    [string]$ArtifactsRoot,
    [string]$AssemblyFilters = '+Aoyi.*,+Assembly-CSharp'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'UnityCoverage.psm1') -Force

$projectPath = (Resolve-Path -LiteralPath $ProjectRoot).Path
if ([string]::IsNullOrWhiteSpace($ArtifactsRoot)) {
    $ArtifactsRoot = Join-Path $projectPath 'artifacts\unity-coverage'
}
$artifactsPath = [System.IO.Path]::GetFullPath($ArtifactsRoot)

$activeProjectEditors = @(
    Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_.CommandLine) -and
            $_.CommandLine.IndexOf($projectPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0
        }
)
if ($activeProjectEditors.Count -gt 0) {
    $processIds = ($activeProjectEditors.ProcessId -join ', ')
    throw "The Unity project is already open (PID: $processIds). Close it before running coverage."
}

$lockFile = Join-Path $projectPath 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $lockFile) {
    Write-Warning "A stale Unity lock file may exist: $lockFile"
}

$editorPath = Resolve-UnityEditorPath -ProjectRoot $projectPath -UnityExe $UnityExe
New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null

$arguments = Get-UnityCoverageArguments `
    -ProjectRoot $projectPath `
    -ArtifactsRoot $artifactsPath `
    -AssemblyFilters $AssemblyFilters
$argumentLine = ConvertTo-WindowsCommandLine -Arguments $arguments

Write-Host "Unity: $editorPath"
Write-Host "Project: $projectPath"
Write-Host "Artifacts: $artifactsPath"

$process = Start-Process `
    -FilePath $editorPath `
    -ArgumentList $argumentLine `
    -Wait `
    -PassThru `
    -NoNewWindow

$testResults = Join-Path $artifactsPath 'editmode-results.xml'
$editorLog = Join-Path $artifactsPath 'unity-editmode.log'

if (-not (Test-Path -LiteralPath $testResults -PathType Leaf)) {
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode) before writing test results. See: $editorLog"
    }
    throw "Unity did not create the NUnit test result: $testResults"
}

$summary = Get-UnityTestSummary -TestResultsPath $testResults

$htmlReport = Get-ChildItem -Path (Join-Path $artifactsPath 'coverage') -Filter 'index.htm*' -File -Recurse |
    Select-Object -First 1
if ($null -ne $htmlReport) {
    Write-Host "Coverage report: $($htmlReport.FullName)"
}

Write-Host "Tests: $($summary.Total) total, $($summary.Passed) passed, $($summary.Failed) failed"

if ($summary.Failed -gt 0) {
    throw "Unity tests failed: $($summary.Failed). See: $testResults"
}
if ($process.ExitCode -ne 0) {
    throw "Unity exited with code $($process.ExitCode). See: $editorLog"
}
if ($null -eq $htmlReport) {
    throw "Unity tests passed but no HTML coverage report was generated under: $artifactsPath\coverage"
}
