param([string]$Directory=$PSScriptRoot)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Directory).Path
if((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Links are not accepted.'}
$manifest=Join-Path $root 'SHA256SUMS.txt'
if((Get-Item -LiteralPath $manifest).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked manifest rejected.'}
$seen=@{}
foreach($line in [IO.File]::ReadAllLines($manifest)) {
 if($line -notmatch '^([a-fA-F0-9]{64})  ([A-Za-z0-9][A-Za-z0-9._-]*)$'){throw 'Invalid checksum entry.'}
 $expected=$Matches[1];$name=$Matches[2]
 if($name -eq 'SHA256SUMS.txt' -or $seen.ContainsKey($name)){throw 'Duplicate or self-referencing checksum entry.'}
 $seen[$name]=$true
 $file=Get-Item -LiteralPath (Join-Path $root $name)
 if($file.PSIsContainer -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Only ordinary files are accepted.'}
 if((Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne $expected){throw "Checksum mismatch: $name"}
}
if($seen.Count -eq 0){throw 'Empty checksum manifest.'}
foreach($item in Get-ChildItem -LiteralPath $root -Force) {
 if($item.Name -ne 'SHA256SUMS.txt' -and -not $seen.ContainsKey($item.Name)){throw "Unexpected package entry: $($item.Name)"}
}
Write-Output "SHA-256 verification passed: $($seen.Count) files."
