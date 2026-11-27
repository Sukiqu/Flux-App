param(
 [Parameter(Mandatory=$true)][ValidatePattern('^v[0-9]+\.[0-9]+\.[0-9]+$')][string]$Tag,
 [Parameter(Mandatory=$true)][string]$AllowedSignersFile,
 [string]$Repository=(Split-Path $PSScriptRoot -Parent),
 [string]$MainRef='refs/remotes/origin/main'
)
$ErrorActionPreference='Stop'
$signers=(Resolve-Path -LiteralPath $AllowedSignersFile).Path
if((Get-Item -LiteralPath $signers).Length -eq 0){throw 'Trusted SSH signing keys have not been configured.'}
$keygen=(Get-Command ssh-keygen -ErrorAction Stop).Source
$ref='refs/tags/'+$Tag
$type=& git -C $Repository cat-file -t $ref 2>$null
if($LASTEXITCODE -ne 0 -or $type -ne 'tag'){throw 'A signed annotated tag is required; lightweight tags are rejected.'}
$object=& git -C $Repository cat-file tag $ref
if($LASTEXITCODE -ne 0 -or ($object -join "`n") -notmatch '(?m)^-----BEGIN SSH SIGNATURE-----$'){throw 'An SSH-signed tag is required.'}
$commit=& git -C $Repository rev-parse --verify ($ref+'^{commit}')
if($LASTEXITCODE -ne 0){throw 'Invalid release tag target.'}
$head=& git -C $Repository rev-parse HEAD
if($LASTEXITCODE -ne 0 -or $head -ne $commit){throw 'Checkout does not match the release tag.'}
# Trust is supplied out-of-band, never by a key committed alongside an untrusted tag.
& git -C $Repository -c gpg.format=ssh -c "gpg.ssh.program=$keygen" -c "gpg.ssh.allowedSignersFile=$signers" verify-tag $ref
if($LASTEXITCODE -ne 0){throw 'Tag signature does not match an allowed release signing key.'}
& git -C $Repository merge-base --is-ancestor $commit $MainRef
if($LASTEXITCODE -ne 0){throw 'Release commit is not in the protected main history.'}
Write-Output 'Signed release tag and main ancestry verified.'
