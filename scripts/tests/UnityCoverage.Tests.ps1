$modulePath = Join-Path $PSScriptRoot '..\UnityCoverage.psm1'
Import-Module $modulePath -Force

Describe 'UnityCoverage helpers' {
    BeforeEach {
        $testRoot = Join-Path $TestDrive 'Project'
        $projectSettings = Join-Path $testRoot 'ProjectSettings'
        New-Item -ItemType Directory -Path $projectSettings -Force | Out-Null
        @"
m_EditorVersion: 2022.3.61f1c1
m_EditorVersionWithRevision: 2022.3.61f1c1 (327989805ccf)
"@ | Set-Content -LiteralPath (Join-Path $projectSettings 'ProjectVersion.txt')
    }

    It 'reads the exact Unity editor version from ProjectVersion.txt' {
        Get-UnityProjectVersion -ProjectRoot $testRoot | Should Be '2022.3.61f1c1'
    }

    It 'uses an explicit Unity executable when it exists' {
        $unityExe = Join-Path $TestDrive 'Unity.exe'
        New-Item -ItemType File -Path $unityExe | Out-Null

        Resolve-UnityEditorPath -ProjectRoot $testRoot -UnityExe $unityExe |
            Should Be (Resolve-Path $unityExe).Path
    }

    It 'rejects an explicit Unity executable that does not exist' {
        $action = { Resolve-UnityEditorPath -ProjectRoot $testRoot -UnityExe (Join-Path $TestDrive 'missing.exe') }
        $action | Should Throw 'Unity executable not found'
    }

    It 'builds EditMode coverage arguments with reports and assembly filters' {
        $artifacts = Join-Path $TestDrive 'artifacts'
        $arguments = Get-UnityCoverageArguments `
            -ProjectRoot $testRoot `
            -ArtifactsRoot $artifacts `
            -AssemblyFilters '+Aoyi.*,+Assembly-CSharp'

        ($arguments -contains '-runTests') | Should Be $true
        ($arguments -contains '-testPlatform') | Should Be $true
        ($arguments -contains 'EditMode') | Should Be $true
        ($arguments -contains '-debugCodeOptimization') | Should Be $true
        ($arguments -contains '-enableCodeCoverage') | Should Be $true
        ($arguments -contains '-coverageResultsPath') | Should Be $true
        ($arguments -contains '-coverageOptions') | Should Be $true
        ($arguments -join ' ') | Should Match 'generateHtmlReport'
        ($arguments -join ' ') | Should Match 'assemblyFilters:\+Aoyi\.\*,\+Assembly-CSharp'
    }

    It 'quotes command line arguments that contain spaces' {
        $commandLine = ConvertTo-WindowsCommandLine -Arguments @(
            '-projectPath'
            'D:\Desktop\game\aoyi team2'
            '-runTests'
        )

        $commandLine | Should Be '-projectPath "D:\Desktop\game\aoyi team2" -runTests'
    }

    It 'parses NUnit totals from Unity test results' {
        $resultPath = Join-Path $TestDrive 'editmode-results.xml'
        @"
<?xml version="1.0" encoding="utf-8"?>
<test-run total="16" passed="12" failed="4" skipped="0" result="Failed(Child)" />
"@ | Set-Content -LiteralPath $resultPath

        $summary = Get-UnityTestSummary -TestResultsPath $resultPath

        $summary.Total | Should Be 16
        $summary.Passed | Should Be 12
        $summary.Failed | Should Be 4
        $summary.Result | Should Be 'Failed(Child)'
    }
}
