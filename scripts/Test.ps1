$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Test-Dependencies.ps1')
& (Join-Path $PSScriptRoot 'Test-WorkflowSecurity.ps1')
$output=Join-Path $root 'artifacts/tests'
New-Item -ItemType Directory $output -Force | Out-Null
$fx=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
if(-not(Test-Path "$fx/csc.exe")){$fx=Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319'}
$refs=@('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xml.dll','/r:System.Core.dll')
$refs+=Get-ChildItem (Join-Path $env:WINDIR 'System32/WinMetadata') -Filter '*.winmd' | ForEach-Object {'/r:'+$_.FullName}
foreach($name in @('System.Runtime','System.ObjectModel','System.Runtime.InteropServices.WindowsRuntime')) {
 $dll=Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET/assembly/GAC_MSIL/$name") -Filter '*.dll' -Recurse | Select-Object -First 1
 if($dll){$refs+=('/r:'+$dll.FullName)}
}
$assets=& (Join-Path $root 'Assets/Resources.ps1')
$source=Get-ChildItem (Join-Path $root 'Kaynak') -Filter '*.cs' | ForEach-Object {$_.FullName}
foreach($name in @('SecurityTests','BrandingTests','NavigationCacheTests','LocalizationTests')) {
 $exe=Join-Path $output "$name.exe"
 & "$fx/csc.exe" /nologo /target:exe /optimize+ /debug- "/main:Akis.$name" "/out:$exe" $assets $refs $source (Join-Path $root "tests/$name.cs")
 if($LASTEXITCODE -ne 0){throw "Test compilation failed: $name"}
 $folder=Join-Path $output $name
 $p=Start-Process -FilePath $exe -ArgumentList ('"'+$folder+'"') -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){Get-Content (Join-Path $folder 'results.txt');throw "Test failed: $name"}
 $checks=@(Get-Content (Join-Path $folder 'results.txt') | Where-Object {$_ -like 'PASS*'}).Count
 Write-Output "$name passed: $checks checks."
}
& (Join-Path $PSScriptRoot 'Test-Package.ps1')
& (Join-Path $PSScriptRoot 'Test-ReleaseSecurity.ps1')
& (Join-Path $PSScriptRoot 'Test-ReleaseTag.ps1')
