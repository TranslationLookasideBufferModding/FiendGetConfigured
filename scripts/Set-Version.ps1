param([Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$path=Join-Path $root 'src\VersionInfo.cs'
$text=Get-Content -LiteralPath $path -Raw -Encoding UTF8
$text=[regex]::Replace($text,'Version = "\d+\.\d+\.\d+"','Version = "'+$Version+'"')
[IO.File]::WriteAllText($path,$text,[Text.UTF8Encoding]::new($false))
$manifestPath=Join-Path $root 'package\manifest.json'
$manifest=Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$manifest.version_number=$Version
[IO.File]::WriteAllText($manifestPath,($manifest | ConvertTo-Json -Depth 4)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
Write-Host "Version gesetzt: $Version. Jetzt CHANGELOG.md aktualisieren und Release.cmd ausfuehren."
