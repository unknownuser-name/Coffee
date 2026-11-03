param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityEditor)) { throw 'Unity 6000.6.5f1 editor not found. Pass -UnityEditor with its path.' }
$cafeProject = Join-Path $PSScriptRoot 'UnityProject'
$cafeLog = Join-Path $PSScriptRoot 'CoffeeCraft-build.log'
$cafeArgs = '-batchmode -nographics -quit -projectPath "' + $cafeProject + '" -executeMethod FrontGrindBuild.BuildLatteMouse -logFile "' + $cafeLog + '"'
$cafeProcess = Start-Process -FilePath $UnityEditor -ArgumentList $cafeArgs -WindowStyle Hidden -PassThru
# Wait for the Editor itself; a Unity Hub helper can outlive a batch build.
$cafeProcess.WaitForExit()
$cafeProcess.Refresh()
if ($cafeProcess.ExitCode -ne 0) { throw "Unity build failed. See $cafeLog" }
$cafeOutput = Join-Path $PSScriptRoot 'CoffeeCraftLatteMouse/Build/CoffeeCraftLatteMouse.exe'
if (!(Test-Path -LiteralPath $cafeOutput)) { throw 'Build reported success but output is missing.' }
Write-Output $cafeOutput
