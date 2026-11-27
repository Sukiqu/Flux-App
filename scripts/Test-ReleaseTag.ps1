$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root ('artifacts/tag-tests-'+[Guid]::NewGuid().ToString('N'))
$repo=Join-Path $scratch 'repo';New-Item -ItemType Directory $repo -Force | Out-Null
$keygen=(Get-Command ssh-keygen -ErrorAction Stop).Source
foreach($name in @('trusted','untrusted')) {
 $key=Join-Path $scratch $name
 $p=Start-Process -FilePath $keygen -ArgumentList @('-q','-t','ed25519','-N','""','-C','flux-test','-f',('"'+$key+'"')) -WindowStyle Hidden -Wait -PassThru
 if($p.ExitCode -ne 0){throw 'Ephemeral test key generation failed.'}
}
$allowed=Join-Path $scratch 'allowed-signers'
[IO.File]::WriteAllText($allowed,'flux-test '+[IO.File]::ReadAllText((Join-Path $scratch 'trusted.pub')))
function Git-Test {
 & git -C $repo -c user.name=Flux-test -c user.email=offline-test -c commit.gpgsign=false -c tag.gpgsign=false @args
 if($LASTEXITCODE -ne 0){throw 'Git fixture command failed.'}
}
Git-Test init --quiet
[IO.File]::WriteAllText((Join-Path $repo 'file.txt'),'test')
Git-Test add file.txt
Git-Test commit --quiet -m fixture
Git-Test branch main
Git-Test tag -a v2.1.1 -m unsigned
Git-Test tag v2.1.2
foreach($case in @(@('trusted','v2.1.0'),@('untrusted','v2.1.3'))) {
 Git-Test -c gpg.format=ssh -c "gpg.ssh.program=$keygen" -c ('user.signingKey='+(Join-Path $scratch $case[0])) tag -s $case[1] -m signed
}
$checks=0
$object=(& git -C $repo cat-file tag refs/tags/v2.1.0) -join "`n"
$tampered=Join-Path $scratch 'tampered-tag'
[IO.File]::WriteAllText($tampered,([Regex]::Replace($object,'(?m)^signed$','changed')+"`n"),[Text.UTF8Encoding]::new($false))
$objectId=& git -C $repo hash-object -t tag -w $tampered
if($LASTEXITCODE -ne 0){throw 'Failed to create a tampered tag fixture.'}
Git-Test update-ref refs/tags/v2.1.5 $objectId
foreach($tag in @('v2.1.0','v2.1.1','v2.1.2','v2.1.3','v2.1.5')) {
 $accepted=$true
 try {& (Join-Path $PSScriptRoot 'Verify-ReleaseTag.ps1') -Tag $tag -AllowedSignersFile $allowed -Repository $repo -MainRef refs/heads/main | Out-Null}catch{$accepted=$false}
 if($accepted -ne ($tag -eq 'v2.1.0')){throw "Signature regression: $tag"};$checks++
}
Git-Test checkout --quiet --orphan isolated
Git-Test commit --quiet -m isolated
Git-Test -c gpg.format=ssh -c "gpg.ssh.program=$keygen" -c ('user.signingKey='+(Join-Path $scratch 'trusted')) tag -s v2.1.4 -m signed
$accepted=$true
try {& (Join-Path $PSScriptRoot 'Verify-ReleaseTag.ps1') -Tag v2.1.4 -AllowedSignersFile $allowed -Repository $repo -MainRef refs/heads/main | Out-Null}catch{$accepted=$false}
if($accepted){throw 'Release outside main was accepted.'};$checks++
Git-Test checkout --quiet main
$accepted=$true
try {& (Join-Path $PSScriptRoot 'Verify-ReleaseTag.ps1') -Tag v2.1.4 -AllowedSignersFile $allowed -Repository $repo -MainRef refs/heads/main | Out-Null}catch{$accepted=$false}
if($accepted){throw 'Mismatched tag checkout was accepted.'};$checks++
Write-Output "Signed tag tests passed: $checks checks. Keys are disposable test fixtures, not release credentials."
