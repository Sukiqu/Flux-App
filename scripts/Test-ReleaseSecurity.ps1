$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root ('artifacts/release-security-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $scratch -Force | Out-Null
$checks=0
$original=[IO.File]::ReadAllText((Join-Path $root 'Assets/app.manifest'))
foreach($mode in @('valid','malformed','elevated','ui-access','version','identity','dtd','oversize','no-privileges')) {
 $text=$original
 switch($mode) {
  'malformed' {$text='<assembly>'}
  'elevated' {$text=$text.Replace('asInvoker','requireAdministrator')}
  'ui-access' {$text=$text.Replace('uiAccess="false"','uiAccess="true"')}
  'version' {$text=$text.Replace('2.1.0.0','9.0.0.0')}
  'identity' {$text=$text.Replace('Flux.Desktop','Other.App')}
  'dtd' {$text=$text.Replace('?>','?><!DOCTYPE assembly [<!ENTITY x SYSTEM "file:///untrusted">]>')}
  'oversize' {$text=' '*65537}
  'no-privileges' {$text=$text.Replace('requestedExecutionLevel','otherElement')}
 }
 $path=Join-Path $scratch "$mode.manifest";[IO.File]::WriteAllText($path,$text)
 $accepted=$true
 try {& (Join-Path $PSScriptRoot 'Test-Manifest.ps1') -Path $path | Out-Null}catch{$accepted=$false}
 if($accepted -ne ($mode -eq 'valid')){throw "Manifest regression: $mode"};$checks++
}
$inputDir=Join-Path $scratch 'zip-input';New-Item -ItemType Directory $inputDir | Out-Null
[IO.File]::WriteAllText((Join-Path $inputDir 'z.txt'),'last')
[IO.File]::WriteAllText((Join-Path $inputDir '.hidden'),'included')
[IO.File]::WriteAllText((Join-Path $inputDir 'a.txt'),'first')
$zip1=Join-Path $scratch 'one.zip';$zip2=Join-Path $scratch 'two.zip'
& (Join-Path $PSScriptRoot 'New-ReproducibleZip.ps1') -SourceDirectory $inputDir -Destination $zip1
Get-ChildItem $inputDir -Force | ForEach-Object {$_.LastWriteTime=[DateTime]::Now.AddDays(-40)}
& (Join-Path $PSScriptRoot 'New-ReproducibleZip.ps1') -SourceDirectory $inputDir -Destination $zip2
if((Get-FileHash $zip1).Hash -ne (Get-FileHash $zip2).Hash){throw 'ZIP is not reproducible.'};$checks++
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::OpenRead($zip1)
try {if(($zip.Entries.FullName -join ',') -ne '.hidden,a.txt,z.txt'){throw 'ZIP order or hidden files incorrect.'}}finally{$zip.Dispose()};$checks++
$payload=Join-Path $scratch 'installer.exe';[IO.File]::WriteAllText($payload,'not executable - hash verification fixture')
$hash=(Get-FileHash $payload).Hash
& (Join-Path $PSScriptRoot 'Verify-Download.ps1') -File $payload -ExpectedSha256 $hash | Out-Null;$checks++
foreach($mode in @('tampered','unsigned')) {
 $accepted=$true
 try {
  if($mode -eq 'tampered'){& (Join-Path $PSScriptRoot 'Verify-Download.ps1') -File $payload -ExpectedSha256 ('0'*64) | Out-Null}
  else {& (Join-Path $PSScriptRoot 'Verify-Download.ps1') -File $payload -ExpectedSha256 $hash -RequireSignature -ExpectedSignerThumbprint ('0'*40) | Out-Null}
 }catch{$accepted=$false}
 if($accepted){throw "Download verification accepted $mode input."};$checks++
}
Write-Output "Release security tests passed: $checks checks."
