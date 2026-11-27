param(
 [Parameter(Mandatory=$true)][ValidatePattern('^v[0-9]+\.[0-9]+\.[0-9]+$')][string]$Tag,
 [string]$Repository=(Split-Path $PSScriptRoot -Parent),
 [string]$MainRef='refs/remotes/origin/main'
)
$ErrorActionPreference='Stop'
$ref='refs/tags/'+$Tag
$commit=& git -C $Repository rev-parse --verify ($ref+'^{commit}') 2>$null
if($LASTEXITCODE -ne 0){throw 'Invalid release tag target.'}
$head=& git -C $Repository rev-parse HEAD
if($LASTEXITCODE -ne 0 -or $head -ne $commit){throw 'Checkout does not match the release tag.'}
& git -C $Repository merge-base --is-ancestor $commit $MainRef
if($LASTEXITCODE -ne 0){throw 'Release commit is not in the protected main history.'}
Write-Output 'Release tag, checkout and main ancestry verified.'
