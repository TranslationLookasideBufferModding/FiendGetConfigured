param([Parameter(Mandatory=$true)][string]$RepoRoot,[Parameter(Mandatory=$true)][string]$Dll)
$ErrorActionPreference='Stop'
$manifestPath=Join-Path $RepoRoot 'package\manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
if($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$' -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$' -or $manifest.description.Length -gt 250){throw 'Ungueltiges Thunderstore-Manifest.'}
if(([Reflection.AssemblyName]::GetAssemblyName($Dll)).Version.ToString(3) -ne $manifest.version_number){throw 'DLL- und Manifest-Version stimmen nicht ueberein.'}
$proof=Get-Content -LiteralPath (Join-Path $RepoRoot 'artifacts\tests\verification.json') -Raw | ConvertFrom-Json
if($proof.ArtifactSHA256 -ne (Get-FileHash -Algorithm SHA256 -LiteralPath $Dll).Hash -or $proof.CoreTests -ne 'Passed' -or $proof.InstalledReferences -ne 'Passed'){throw 'Der Testnachweis passt nicht zur DLL.'}
$icon=Join-Path $RepoRoot 'package\icon.png'
Add-Type -AssemblyName System.Drawing
$picture=[Drawing.Image]::FromFile($icon)
try {if($picture.Width -ne 256 -or $picture.Height -ne 256){throw 'icon.png muss 256 x 256 Pixel haben.'}}finally{$picture.Dispose()}
$release=Join-Path $RepoRoot 'artifacts\releases'
New-Item -ItemType Directory -Path $release -Force | Out-Null
$destination=Join-Path $release ($manifest.name+'-'+$manifest.version_number+'.zip')
$temporary=$destination+'.tmp'
if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary}
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$zip=[IO.Compression.ZipFile]::Open($temporary,[IO.Compression.ZipArchiveMode]::Create)
try {
 $files=@{
  'README.md'=(Join-Path $RepoRoot 'README.md');'manifest.json'=$manifestPath;'icon.png'=$icon;
  'CHANGELOG.md'=(Join-Path $RepoRoot 'CHANGELOG.md');'LICENSE.txt'=(Join-Path $RepoRoot 'LICENSE.txt');
  'plugins/FiendGetConfigured/FiendGetConfigured.dll'=$Dll
 }
 foreach($name in $files.Keys){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$files[$name],$name,[IO.Compression.CompressionLevel]::Optimal) | Out-Null}
}finally{$zip.Dispose()}
Move-Item -LiteralPath $temporary -Destination $destination -Force
Write-Host "Fertiges Modpaket: $destination"
Write-Host 'Vor dem Veroeffentlichen den Lobby-Test aus docs/TESTPLAN.md durchfuehren.'
