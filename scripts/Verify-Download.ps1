param(
 [Parameter(Mandatory=$true)][string]$File,
 [Parameter(Mandatory=$true)][ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$ExpectedSha256,
 [switch]$RequireSignature,
 [string]$ExpectedSignerThumbprint
)
$ErrorActionPreference='Stop'
$item=Get-Item -LiteralPath $File
if($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Only ordinary files are accepted.'}
if((Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash -ne $ExpectedSha256){throw 'Download checksum mismatch. Do not run the file.'}
if($RequireSignature) {
 if($ExpectedSignerThumbprint -notmatch '^[A-Fa-f0-9]{40}$'){throw 'An independently trusted signer certificate thumbprint is required.'}
 $signature=Get-AuthenticodeSignature -LiteralPath $item.FullName
 if($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Thumbprint -ne $ExpectedSignerThumbprint){throw 'Invalid or unexpected publisher signature. Do not run the file.'}
}
Write-Output 'Download verification passed. No file was executed.'
