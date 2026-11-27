$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$repo=Join-Path $root ('artifacts/tag-tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $repo -Force | Out-Null
function Git-Test {
 & git -C $repo -c user.name=Flux-test -c user.email=offline-test -c commit.gpgsign=false -c tag.gpgsign=false @args
 if($LASTEXITCODE -ne 0){throw 'Git fixture command failed.'}
}
function Check-Tag([string]$Tag,[bool]$Expected) {
 $accepted=$true
 try {& (Join-Path $PSScriptRoot 'Verify-ReleaseTag.ps1') -Tag $Tag -Repository $repo -MainRef refs/heads/main | Out-Null}catch{$accepted=$false}
 if($accepted -ne $Expected){throw "Release tag regression: $Tag"}
 $script:checks++
}
Git-Test init --quiet
Git-Test symbolic-ref HEAD refs/heads/main
[IO.File]::WriteAllText((Join-Path $repo 'file.txt'),'test')
Git-Test add file.txt
Git-Test commit --quiet -m fixture
Git-Test tag -a v2.1.0 -m release
Git-Test tag v2.1.1
$checks=0
Check-Tag 'v2.1.0' $true
Check-Tag 'v2.1.1' $true
Check-Tag 'v9.9.9' $false
Check-Tag '--invalid' $false
$blob=& git -C $repo rev-parse HEAD:file.txt
if($LASTEXITCODE -ne 0){throw 'Blob fixture lookup failed.'}
Git-Test update-ref refs/tags/v2.1.2 $blob
Check-Tag 'v2.1.2' $false
Git-Test checkout --quiet --orphan isolated
Git-Test commit --quiet -m isolated
Git-Test tag v2.1.3
Check-Tag 'v2.1.3' $false
Git-Test checkout --quiet main
Check-Tag 'v2.1.3' $false
Write-Output "Release tag tests passed: $checks checks."
