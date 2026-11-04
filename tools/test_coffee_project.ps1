param([switch]$SkipAssets)
$ErrorActionPreference = 'Stop'
$coffeeRoot = Split-Path -Parent $PSScriptRoot
$coffeeScratch = Join-Path $PSScriptRoot '.validation'
New-Item -ItemType Directory -Path $coffeeScratch -Force | Out-Null
$coffeeDotnet = (Get-Command dotnet -ErrorAction Stop).Source
$coffeeDotnetRoot = Split-Path -Parent $coffeeDotnet
$coffeeSdk = Get-ChildItem -LiteralPath (Join-Path $coffeeDotnetRoot 'sdk') -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$coffeeRefPack = Get-ChildItem -LiteralPath (Join-Path $coffeeDotnetRoot 'packs/Microsoft.NETCore.App.Ref') -Directory |
    Where-Object { $_.Name -match '^\d+\.\d+\.\d+$' } |
    Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$coffeeRefVersion = [version]$coffeeRefPack.Name
$coffeeTfm = 'net' + $coffeeRefVersion.Major + '.' + $coffeeRefVersion.Minor
$coffeeReferences = @(Get-ChildItem -LiteralPath (Join-Path $coffeeRefPack.FullName ('ref/' + $coffeeTfm)) -Filter '*.dll')
$coffeeCsc = Join-Path $coffeeSdk.FullName 'Roslyn/bincore/csc.dll'
if (!(Test-Path -LiteralPath $coffeeCsc) -or $coffeeReferences.Count -eq 0) { throw 'The installed .NET SDK/reference pack is incomplete.' }
$coffeeModels = @(Get-ChildItem -LiteralPath (Join-Path $coffeeRoot 'CloudCafeUnity/UnityProject/Assets/Scripts/Crafting') -Filter '*Model.cs')
$coffeeModels += Get-Item -LiteralPath (Join-Path $coffeeRoot 'CloudCafeUnity/UnityProject/Assets/Scripts/Coffee/EspressoBeanProfiles.cs')
$coffeeTests = @(Get-ChildItem -LiteralPath (Join-Path $coffeeRoot 'CloudCafeUnity/Tests') -Filter '*.cs' | Sort-Object Name)
$coffeeResults = @()
foreach ($coffeeTest in $coffeeTests) {
    $coffeeAssembly = Join-Path $coffeeScratch ($coffeeTest.BaseName + '.dll')
    $coffeeResponse = Join-Path $coffeeScratch ($coffeeTest.BaseName + '.rsp')
    $coffeeArguments = @('/nologo', '/target:exe', '/optimize+', '/deterministic+', '/nostdlib+', '/langversion:latest', ('/out:"' + $coffeeAssembly + '"'))
    $coffeeArguments += @($coffeeReferences | ForEach-Object { '/reference:"' + $_.FullName + '"' })
    $coffeeArguments += @($coffeeModels | ForEach-Object { '"' + $_.FullName + '"' })
    $coffeeArguments += '"' + $coffeeTest.FullName + '"'
    [IO.File]::WriteAllLines($coffeeResponse, $coffeeArguments, [Text.UTF8Encoding]::new($false))
    $coffeeCompileOutput = @(& $coffeeDotnet $coffeeCsc ('@' + $coffeeResponse) 2>&1 | ForEach-Object { $_.ToString() })
    $coffeeCompileExit = $LASTEXITCODE
    $coffeeRuntimeConfig = @{ runtimeOptions = @{ tfm = $coffeeTfm; framework = @{ name = 'Microsoft.NETCore.App'; version = $coffeeRefPack.Name }; rollForward = 'LatestPatch' } }
    $coffeeRuntimeConfig | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $coffeeScratch ($coffeeTest.BaseName + '.runtimeconfig.json')) -Encoding utf8
    $coffeeRunOutput = @()
    $coffeeRunExit = $null
    if ($coffeeCompileExit -eq 0) {
        $coffeeRunOutput = @(& $coffeeDotnet $coffeeAssembly 2>&1 | ForEach-Object { $_.ToString() })
        $coffeeRunExit = $LASTEXITCODE
    }
    $coffeeCount = 0
    $coffeeMarker = [regex]::Match(($coffeeRunOutput -join "`n"), '_TESTS_PASSED\s+(\d+)')
    if ($coffeeMarker.Success) { $coffeeCount = [int]$coffeeMarker.Groups[1].Value }
    $coffeePassed = $coffeeCompileExit -eq 0 -and $coffeeRunExit -eq 0 -and $coffeeMarker.Success
    $coffeeResult = [ordered]@{ name = $coffeeTest.BaseName; compiled = ($coffeeCompileExit -eq 0); executed = ($null -ne $coffeeRunExit); passed = $coffeePassed; assertion_count = $coffeeCount; output = ($coffeeRunOutput -join "`n").Replace($coffeeRoot, '<repo>') }
    if ($coffeeCompileExit -ne 0) { $coffeeResult.compile_output = ($coffeeCompileOutput -join "`n").Replace($coffeeRoot, '<repo>').Replace($coffeeDotnetRoot, '<dotnet>') }
    $coffeeResults += [pscustomobject]$coffeeResult
    Write-Output ($coffeeTest.BaseName + ': ' + $(if ($coffeePassed) { 'PASS (' + $coffeeCount + ' checks)' } else { 'FAIL' }))
}

# Parse actual game/editor source with Unity symbols enabled. This is syntax
# validation only; it deliberately does not pretend to bind UnityEngine APIs.
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.dll')
Add-Type -Path (Join-Path $PSHOME 'Microsoft.CodeAnalysis.CSharp.dll')
$coffeeParseOptions = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols([string[]]@('UNITY_EDITOR', 'UNITY_STANDALONE_WIN', 'UNITY_6000_0_OR_NEWER'))
$coffeeSyntaxFiles = @()
foreach ($coffeeFolder in @('Scripts', 'Editor')) {
    $coffeeSyntaxFiles += @(Get-ChildItem -LiteralPath (Join-Path $coffeeRoot ('CloudCafeUnity/UnityProject/Assets/' + $coffeeFolder)) -Filter '*.cs' -Recurse)
}
$coffeeDiagnostics = @()
foreach ($coffeeFile in $coffeeSyntaxFiles) {
    $coffeeTree = [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($coffeeFile.FullName), $coffeeParseOptions)
    foreach ($coffeeDiagnostic in $coffeeTree.GetDiagnostics()) {
        if ($coffeeDiagnostic.Severity.ToString() -ne 'Error') { continue }
        $coffeePosition = $coffeeDiagnostic.Location.GetLineSpan().StartLinePosition
        $coffeeDiagnostics += [ordered]@{ path = [IO.Path]::GetRelativePath($coffeeRoot, $coffeeFile.FullName).Replace('\', '/'); line = $coffeePosition.Line + 1; message = $coffeeDiagnostic.GetMessage() }
    }
}
$coffeeSyntax = [ordered]@{ passed = ($coffeeDiagnostics.Count -eq 0); file_count = $coffeeSyntaxFiles.Count; errors = @($coffeeDiagnostics); validation_type = 'Roslyn syntax only; UnityEngine/editor API binding was not executed' }
Write-Output ('C# source syntax: ' + $(if ($coffeeSyntax.passed) { 'PASS (' + $coffeeSyntaxFiles.Count + ' files)' } else { 'FAIL' }))
$coffeeAssetResult = $null
if (!$SkipAssets) {
    $coffeeAssetOutput = @(& python (Join-Path $PSScriptRoot 'test_coffee_project.py') --assets-only 2>&1 | ForEach-Object { $_.ToString() })
    try { $coffeeAssetResult = ($coffeeAssetOutput -join "`n") | ConvertFrom-Json }
    catch { $coffeeAssetResult = [ordered]@{ passed = $false; errors = @('Asset validator did not return JSON.'); output = $coffeeAssetOutput } }
    Write-Output ('Coffee asset registration: ' + $(if ($coffeeAssetResult.passed) { 'PASS' } else { 'FAIL' }))
}
$coffeeReport = [ordered]@{
    checked_at_utc = [DateTime]::UtcNow.ToString('o')
    passed = (@($coffeeResults | Where-Object { !$_.passed }).Count -eq 0 -and $coffeeSyntax.passed -and ($SkipAssets -or $coffeeAssetResult.passed))
    pure_csharp = [ordered]@{ executed = $true; test_count = $coffeeResults.Count; assertion_count = ($coffeeResults | Measure-Object -Property assertion_count -Sum).Sum; tests = @($coffeeResults) }
    source_syntax = $coffeeSyntax
    asset_registration = $coffeeAssetResult
    unity_editor = [ordered]@{ executed = $false; required_version = '6000.6.5f1'; note = 'Unity Editor import, engine API compilation, gameplay and player build were not run by this validation script.' }
}
$coffeeReport | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'validation.json') -Encoding utf8
if (!$coffeeReport.passed) { exit 1 }
