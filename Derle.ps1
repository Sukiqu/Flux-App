$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& (Join-Path $root 'scripts/Test-Manifest.ps1')
$fx = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if(-not (Test-Path "$fx\csc.exe")) { $fx = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
$refs = @('/r:System.Windows.Forms.dll','/r:System.Drawing.dll','/r:System.Xml.dll','/r:System.Core.dll')
$refs += Get-ChildItem (Join-Path $env:WINDIR 'System32\WinMetadata') -Filter '*.winmd' | ForEach-Object { '/r:'+$_.FullName }
foreach($name in @('System.Runtime','System.ObjectModel','System.Runtime.InteropServices.WindowsRuntime')) {
    $dll=Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\$name") -Filter '*.dll' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if($dll) { $refs+=('/r:'+$dll.FullName) }
}
$source = Get-ChildItem (Join-Path $root 'Kaynak') -Filter '*.cs' | ForEach-Object { $_.FullName }
$assets = & (Join-Path $root 'Assets\Resources.ps1')
& "$fx\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ /debug- "/out:$root\Flux.exe" "/win32icon:$root\Assets\flux.ico" "/win32manifest:$root\Assets\app.manifest" $assets $refs $source
if($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }
# Preserve existing shortcuts/startup commands with the same updated binary.
Copy-Item -LiteralPath "$root\Flux.exe" -Destination "$root\AkisFlyout.exe" -Force
Write-Output 'Derleme tamamlandı.'
