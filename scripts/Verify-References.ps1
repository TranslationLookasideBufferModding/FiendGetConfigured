param([Parameter(Mandatory=$true)][string]$ProfileDirectory)
$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $ProfileDirectory 'BepInEx\core\Mono.Cecil.dll')
$fiend=Join-Path $ProfileDirectory 'BepInEx\plugins\AuroraBytes-The_Fiend\TheFiend.dll'
if(-not(Test-Path -LiteralPath $fiend)){throw 'AuroraBytes-The_Fiend/TheFiend.dll fehlt im Profil.'}
$assembly=[Mono.Cecil.AssemblyDefinition]::ReadAssembly($fiend)
try {
 $plugin=$assembly.MainModule.GetType('TheFiend.TheFiendPlugin')
 $metadata=$plugin.CustomAttributes | Where-Object {$_.AttributeType.Name -eq 'BepInPlugin'} | Select-Object -First 1
 if($metadata.ConstructorArguments[0].Value -ne 'com.TheFiend' -or $metadata.ConstructorArguments[2].Value -ne '1.1.6'){throw 'Diese Ausgabe ist fuer AuroraBytes The Fiend 1.1.6 vorgesehen.'}
 $type=$assembly.MainModule.GetType('TheFiendAI')
 foreach($name in @('enableLegacy','StateOfMind','Seeking','RageMode','Funky','LungApparatusWillRage','TargetLook','breakerBox')) {
  if(-not($type.Fields | Where-Object Name -eq $name)){throw "Erwartetes Feld fehlt: $name"}
 }
 $ai=$type.Methods | Where-Object Name -eq 'DoAIInterval'
 $expected=@{TargetClosestPlayer=5;set_speed=9;SetDestinationToPosition=6}
 foreach($key in $expected.Keys){
  $count=@($ai.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq $key}).Count
  if($count -ne $expected[$key]){throw "Unbekanntes Fiend-Layout: $key $count"}
 }
 $state=$type.NestedTypes | Where-Object {$_.Name.StartsWith('<Grabbing>')} | Select-Object -First 1
 $next=$state.Methods | Where-Object Name -eq 'MoveNext'
 $damage=@($next.Body.Instructions | Where-Object {$_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.Name -eq 'DamagePlayer'}).Count
 if($damage -ne 1){throw 'Der erwartete Schadenspfad fehlt.'}
 foreach($name in @('Awake','OnNetworkSpawn','OnTriggerStay','GrabServerRpc','GrabClientRpc','TeleportServerRpc','SetLungLightServerRpc','BreakDoorServerRpc','BreakerBoxBreakServerRpc','FearedServerRpc')){
  if(-not($type.Methods | Where-Object Name -eq $name)){throw "Erwartete Methode fehlt: $name"}
 }
 Write-Host 'INSTALLED_REFERENCES_OK AuroraBytes-The_Fiend 1.1.6 (real installed DLL)'
} finally {$assembly.Dispose()}
