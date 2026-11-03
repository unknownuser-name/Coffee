param(
    [ValidateSet('coffeeBeanSmoke', 'frontSmoke', 'latteSmoke')]
    [string]$Mode = 'coffeeBeanSmoke',
    [string]$PlayerExecutable = '',
    [switch]$SkipCapture
)
$ErrorActionPreference = 'Stop'
$coffeeRoot = Split-Path -Parent $PSScriptRoot
$coffeeBuildRoot = Join-Path $coffeeRoot 'CloudCafeUnity/CoffeeCraftLatteMouse'
if (!$PlayerExecutable) { $PlayerExecutable = Join-Path $coffeeBuildRoot 'Build/CoffeeCraftLatteMouse.exe' }
$coffeePlayer = (Resolve-Path -LiteralPath $PlayerExecutable).Path
$coffeeScratch = Join-Path $PSScriptRoot '.validation'
$coffeeReview = Join-Path $coffeeRoot 'CloudCafeUnity/Review/CoffeeAssets'
New-Item -ItemType Directory -Path $coffeeScratch, $coffeeReview -Force | Out-Null
$coffeeLog = Join-Path $coffeeScratch ('player-' + $Mode + '.log')
$coffeeArguments = @('-force-d3d11', '-screen-width', '1440', '-screen-height', '810', '-screen-fullscreen', '0', ('-' + $Mode), '-logFile', ('"' + $coffeeLog + '"'))
if ($SkipCapture) {
    if ($Mode -ne 'coffeeBeanSmoke') { throw '-SkipCapture applies only to the bean selection check.' }
    $coffeeArguments += '-coffeeBeanSkipCapture'
}
$coffeeStarted = [DateTime]::UtcNow
# Render the normal game view; batch mode can suppress the GUI backbuffer.
$coffeeProcess = Start-Process -FilePath $coffeePlayer -ArgumentList $coffeeArguments -WindowStyle Hidden -PassThru
if (!$coffeeProcess.WaitForExit(180000)) {
    Stop-Process -Id $coffeeProcess.Id -ErrorAction SilentlyContinue
    throw ('Game smoke check timed out: ' + $Mode)
}
$coffeeProcess.Refresh()
if ($coffeeProcess.ExitCode -ne 0) {
    Get-Content -LiteralPath $coffeeLog -Tail 50
    throw ('Game smoke check failed: ' + $Mode + ', exit ' + $coffeeProcess.ExitCode)
}

$coffeeRuntimeReview = Join-Path (Split-Path -Parent (Split-Path -Parent $coffeePlayer)) 'Review'
if ($Mode -eq 'coffeeBeanSmoke') {
    $coffeeSource = Join-Path $coffeeRuntimeReview 'CoffeeAssets/selection-validation.json'
    $coffeeDestination = Join-Path $coffeeReview 'selection-validation.json'
} elseif ($Mode -eq 'latteSmoke') {
    $coffeeSource = Join-Path $coffeeRuntimeReview 'Practice/result.json'
    $coffeeDestination = Join-Path $coffeeReview 'latte-validation.json'
} else {
    $coffeeSource = Join-Path $coffeeRuntimeReview 'result.json'
    $coffeeDestination = Join-Path $coffeeReview 'front-grind-validation.json'
}
$coffeeResultFile = Get-Item -LiteralPath $coffeeSource
if ($coffeeResultFile.LastWriteTimeUtc -lt $coffeeStarted.AddSeconds(-2)) {
    throw 'The player did not produce a fresh validation report.'
}
$coffeeResult = Get-Content -LiteralPath $coffeeSource -Raw | ConvertFrom-Json
if (!$coffeeResult.passed) { throw ('The player report did not pass: ' + $Mode) }
if ($Mode -eq 'coffeeBeanSmoke') {
    if (!$SkipCapture) {
        if (!$coffeeResult.screenshot_verified_nonblank) { throw 'The game-view screenshot has not passed the nonblank check.' }
        $coffeeScreenshot = Join-Path $coffeeRuntimeReview 'CoffeeAssets/selection.png'
        Copy-Item -LiteralPath $coffeeScreenshot -Destination (Join-Path $coffeeReview 'selection.png') -Force
    } elseif ($coffeeResult.visual_capture_executed -or $coffeeResult.screenshot_verified_nonblank) {
        throw 'A capture-skipped report must not claim visual verification.'
    }
}
Copy-Item -LiteralPath $coffeeSource -Destination $coffeeDestination -Force
[pscustomobject]@{
    mode = $Mode
    passed = $true
    exit_code = $coffeeProcess.ExitCode
    visual_capture_requested = ($Mode -eq 'coffeeBeanSmoke' -and !$SkipCapture)
    report = [IO.Path]::GetRelativePath($coffeeRoot, $coffeeDestination).Replace('\', '/')
    scope = 'Scripted game runtime API; not physical OS mouse/keyboard input.'
} | ConvertTo-Json
