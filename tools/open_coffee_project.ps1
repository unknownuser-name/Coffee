param([string]$UnityEditor = '', [switch]$DryRun)
$ErrorActionPreference = 'Stop'
$coffeeRoot = Split-Path -Parent $PSScriptRoot
$coffeeProject = Join-Path $coffeeRoot 'CloudCafeUnity/UnityProject'
$coffeeVersionFile = Join-Path $coffeeProject 'ProjectSettings/ProjectVersion.txt'
$coffeeVersionMatch = [regex]::Match([IO.File]::ReadAllText($coffeeVersionFile), '(?m)^m_EditorVersion:\s*(\S+)')
if (!$coffeeVersionMatch.Success) { throw 'Could not read the game project Unity version.' }
$coffeeVersion = $coffeeVersionMatch.Groups[1].Value
$coffeeCandidates = @()
if ($UnityEditor) { $coffeeCandidates += $UnityEditor }
if ($env:UNITY_EDITOR_PATH) { $coffeeCandidates += $env:UNITY_EDITOR_PATH }
$coffeeHubPathFile = Join-Path $env:APPDATA 'UnityHub/secondaryInstallPath.json'
if (Test-Path -LiteralPath $coffeeHubPathFile) {
    try {
        $coffeeHubRoot = Get-Content -LiteralPath $coffeeHubPathFile -Raw | ConvertFrom-Json
        if ($coffeeHubRoot -is [string] -and $coffeeHubRoot) {
            $coffeeCandidates += Join-Path $coffeeHubRoot ($coffeeVersion + '/Editor/Unity.exe')
        }
    } catch { Write-Verbose 'Unity Hub secondary path could not be read.' }
}
$coffeeCandidates += Join-Path $env:ProgramFiles ('Unity/Hub/Editor/' + $coffeeVersion + '/Editor/Unity.exe')
$coffeeEditorPath = $coffeeCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (!$coffeeEditorPath) {
    throw ('Unity ' + $coffeeVersion + ' was not found. Add ' + $coffeeProject + ' in Unity Hub, or pass -UnityEditor with Unity.exe.')
}
$coffeeEditorPath = (Resolve-Path -LiteralPath $coffeeEditorPath).Path
$coffeeArguments = '-projectPath "' + $coffeeProject + '"'
if ($DryRun) {
    [pscustomobject]@{ unity_editor = $coffeeEditorPath; version = $coffeeVersion; project = $coffeeProject; arguments = $coffeeArguments; scene = 'Assets/Scenes/FrontGrind.unity' } | ConvertTo-Json
    exit 0
}
# This is a user-invoked interactive Editor launcher, not a background helper.
Start-Process -FilePath $coffeeEditorPath -ArgumentList $coffeeArguments -WindowStyle Normal | Out-Null
