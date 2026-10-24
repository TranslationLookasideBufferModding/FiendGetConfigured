param([Parameter(Mandatory=$true)][string]$RepoRoot,[Parameter(Mandatory=$true)][string]$ProfileDirectory,[Parameter(Mandatory=$true)][string]$Dll)
$ErrorActionPreference='Stop'
if(Get-Process -Name 'Lethal Company' -ErrorAction SilentlyContinue){throw 'Bitte zuerst Lethal Company schliessen.'}
$plugins=[IO.Path]::GetFullPath((Join-Path $ProfileDirectory 'BepInEx\plugins'))
if(-not(Test-Path -LiteralPath $plugins -PathType Container)){throw 'Der Profil-Pluginordner wurde nicht gefunden.'}
$destination=[IO.Path]::GetFullPath((Join-Path $plugins 'FiendGetConfigured-LocalTest\FiendGetConfigured.dll'))
if(-not $destination.StartsWith($plugins+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unsicherer Zielpfad.'}
$duplicates=@(Get-ChildItem -LiteralPath $plugins -Filter 'FiendGetConfigured.dll' -Recurse -File | Where-Object {$_.FullName -ne $destination})
if($duplicates.Count -gt 0){throw ('Es gibt bereits eine andere Installation. Zuerst diese im Modmanager deaktivieren oder den alten lokalen Testordner entfernen: '+($duplicates.FullName -join ', '))}
$proof=Get-Content -LiteralPath (Join-Path $RepoRoot 'artifacts\tests\verification.json') -Raw | ConvertFrom-Json
if($proof.ArtifactSHA256 -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $Dll).Hash){throw 'Die DLL passt nicht zum Testnachweis. Test.cmd ausfuehren.'}
if(Test-Path -LiteralPath $destination){
 $backup=Join-Path $RepoRoot ('artifacts\backups\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
 New-Item -ItemType Directory -Path $backup -Force | Out-Null
 Copy-Item -LiteralPath $destination -Destination $backup
}
New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($destination)) -Force | Out-Null
Copy-Item -LiteralPath $Dll -Destination $destination -Force
Write-Host "Lokal installiert: $destination"
Write-Host 'Die eigene Config wird beim naechsten Spielstart erzeugt. Bestehende Config-Dateien wurden nicht ueberschrieben.'
Write-Host 'Nach der Thunderstore-Installation nur den Ordner FiendGetConfigured-LocalTest entfernen, damit keine doppelte DLL bleibt.'
