param([ValidateSet('Build','Test','Release','InstallTest')][string]$Task='Build')
$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$settingsPath=Join-Path $repoRoot 'local.settings.json'
if(-not(Test-Path -LiteralPath $settingsPath)){throw 'local.settings.example.json als local.settings.json kopieren und die lokalen Pfade eintragen.'}
$local=Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
$profile=[IO.Path]::GetFullPath($local.ProfileDirectory)
$game=[IO.Path]::GetFullPath($local.GameDirectory)
$core=Join-Path $profile 'BepInEx\core'
$managed=Join-Path $game 'Lethal Company_Data\Managed'
$artifacts=Join-Path $repoRoot 'artifacts'
$bin=Join-Path $artifacts 'bin'
$reports=Join-Path $artifacts 'tests'
New-Item -ItemType Directory -Path $bin,$reports -Force | Out-Null
function Hash([string]$Path){return (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash}
function SourceHash {
 $inputs=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Filter '*.cs' -File)
 $inputs+=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'scripts') -Filter '*.ps1' -File)
 $inputs+=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests') -Filter '*.cs' -File)
 $inputs+=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'tests\UnityHarness\Assets\Editor') -Filter '*.cs' -File)
 $parts=@($inputs | Sort-Object FullName | ForEach-Object {Hash $_.FullName})
 $parts+=Hash (Join-Path $profile 'BepInEx\plugins\AuroraBytes-The_Fiend\TheFiend.dll')
 foreach($reference in @((Join-Path $core 'BepInEx.dll'),(Join-Path $core '0Harmony.dll'),(Join-Path $managed 'Assembly-CSharp.dll'),(Join-Path $managed 'Unity.Netcode.Runtime.dll'),(Join-Path $profile 'BepInEx\plugins\Evaisa-LethalLib\LethalLib\LethalLib.dll'))){$parts+=Hash $reference}
 return ($parts -join ':')
}
$dll=Join-Path $bin 'FiendGetConfigured.dll'
$proofPath=Join-Path $reports 'verification.json'
if(($Task -eq 'Release' -or $Task -eq 'InstallTest') -and (Test-Path -LiteralPath $proofPath) -and (Test-Path -LiteralPath $dll)){
 $cached=Get-Content -LiteralPath $proofPath -Raw | ConvertFrom-Json
 $unityAvailable=$local.UnityEditor -and (Test-Path -LiteralPath $local.UnityEditor)
 if($cached.SourceHash -eq (SourceHash) -and $cached.ArtifactSHA256 -eq (Hash $dll) -and $cached.CoreTests -eq 'Passed' -and $cached.InstalledReferences -eq 'Passed' -and ($cached.UnityRuntime -eq 'Passed' -or -not $unityAvailable)){
  if($Task -eq 'Release'){& (Join-Path $PSScriptRoot 'Package.ps1') -RepoRoot $repoRoot -Dll $dll}
  else {& (Join-Path $PSScriptRoot 'Install-Test.ps1') -RepoRoot $repoRoot -ProfileDirectory $profile -Dll $dll}
  exit 0
 }
}
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(-not(Test-Path -LiteralPath $compiler)){$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'}
if(-not(Test-Path -LiteralPath $compiler)){throw 'Der vorhandene Windows-C#-Compiler fehlt. Es wurde nichts installiert.'}
& (Join-Path $PSScriptRoot 'Verify-References.ps1') -ProfileDirectory $profile
$references=@((Join-Path $core 'BepInEx.dll'),(Join-Path $core '0Harmony.dll'),(Join-Path $managed 'UnityEngine.dll'),(Join-Path $managed 'UnityEngine.CoreModule.dll'),(Join-Path $managed 'UnityEngine.AIModule.dll'),(Join-Path $managed 'UnityEngine.PhysicsModule.dll'),(Join-Path $managed 'netstandard.dll'))
foreach($reference in $references){if(-not(Test-Path -LiteralPath $reference)){throw "Lokale Referenz fehlt: $reference"}}
$compileArgs=@('/nologo','/target:library','/optimize+','/langversion:5',"/out:$dll")
$compileArgs+=@($references | ForEach-Object {"/reference:$_"})
$compileArgs+=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'src') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object {$_.FullName})
& $compiler @compileArgs
if($LASTEXITCODE -ne 0){throw 'DLL-Kompilierung fehlgeschlagen.'}
Write-Host "Gebaut: $dll"
if($Task -eq 'Build'){exit 0}
$unit=Join-Path $reports 'CoreTests.exe'
& $compiler /nologo /target:exe /optimize+ /langversion:5 "/out:$unit" (Join-Path $repoRoot 'src\Settings.cs') (Join-Path $repoRoot 'tests\CoreTests.cs')
if($LASTEXITCODE -ne 0){throw 'Kompilierung der Logiktests fehlgeschlagen.'}
$unitOutput=& $unit 2>&1
if($LASTEXITCODE -ne 0){throw ($unitOutput -join "`n")}
$unitOutput | Set-Content -LiteralPath (Join-Path $reports 'core-tests.log') -Encoding UTF8
$unitOutput | ForEach-Object {Write-Host $_}
$unityPassed=$false
if($local.UnityEditor -and (Test-Path -LiteralPath $local.UnityEditor)){
 $project=Join-Path $repoRoot 'tests\UnityHarness'
 $unityLog=Join-Path $reports 'unity-runtime.log'
 $arguments=@('-batchmode','-nographics','-quit','-noUpm','-projectPath',('"'+$project+'"'),'-executeMethod','FiendRuntimeTests.Run','-pluginPath',('"'+$dll+'"'),'-corePath',('"'+$core+'"'),'-managedPath',('"'+$managed+'"'),'-fiendPath',('"'+(Join-Path $profile 'BepInEx\plugins\AuroraBytes-The_Fiend\TheFiend.dll')+'"'),'-lethalLibPath',('"'+(Join-Path $profile 'BepInEx\plugins\Evaisa-LethalLib\LethalLib\LethalLib.dll')+'"'),'-logFile',('"'+$unityLog+'"'))
 $process=Start-Process -FilePath $local.UnityEditor -ArgumentList $arguments -PassThru -WindowStyle Hidden
 while(-not $process.WaitForExit(1000)) {if((Get-Date)-$process.StartTime -gt [TimeSpan]::FromMinutes(3)){$process.Kill();throw 'Unity-Test hat das Zeitlimit ueberschritten.'}}
 $unityText=Get-Content -LiteralPath $unityLog -Raw
 if($process.ExitCode -ne 0 -or -not $unityText.Contains('RUNTIME_TESTS_OK')){
  $unityText -split "`r?`n" | Where-Object {$_ -match 'Exception|error CS|FAIL'} | ForEach-Object {Write-Host $_}
  throw "Unity-Laufzeittest fehlgeschlagen. Siehe $unityLog"
 }
 $unityPassed=$true
 $unityText -split "`r?`n" | Where-Object {$_ -match 'RUNTIME_TESTS_OK'} | ForEach-Object {Write-Host $_}
} else {Write-Warning 'Unity-Laufzeittest nicht ausgefuehrt: kein vorhandener Editor in local.settings.json eingetragen.'}
$proof=[ordered]@{ArtifactSHA256=(Hash $dll);SourceHash=(SourceHash);CoreTests='Passed';InstalledReferences='Passed';UnityRuntime=if($unityPassed){'Passed'}else{'NotRun'};LobbyTest='Pending';Utc=(Get-Date).ToUniversalTime().ToString('o')}
$proof | ConvertTo-Json | Set-Content -LiteralPath $proofPath -Encoding UTF8
if($Task -eq 'Release'){& (Join-Path $PSScriptRoot 'Package.ps1') -RepoRoot $repoRoot -Dll $dll}
if($Task -eq 'InstallTest'){& (Join-Path $PSScriptRoot 'Install-Test.ps1') -RepoRoot $repoRoot -ProfileDirectory $profile -Dll $dll}
