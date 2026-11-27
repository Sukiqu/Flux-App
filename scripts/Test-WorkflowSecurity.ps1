$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$workflows=Get-ChildItem (Join-Path $root '.github/workflows') -Filter '*.yml' -File
if($workflows.Count -eq 0){throw 'No workflows found.'}
$checks=0
foreach($workflow in $workflows) {
 $yaml=[IO.File]::ReadAllText($workflow.FullName)
 if($yaml -match '(?m)^\s*pull_request_target\s*:'){throw "Unsafe pull_request_target trigger: $($workflow.Name)"};$checks++
 if($yaml -match '(?im)^\s*permissions\s*:\s*(?:write-all|\{[^}]*write)'){throw "Broad workflow permissions: $($workflow.Name)"};$checks++
 if($yaml -match '(?im)^\s*secrets\s*:\s*inherit\s*$'){throw "Inherited secrets are not allowed: $($workflow.Name)"};$checks++
 if($yaml -notmatch '(?im)^\s*permissions\s*:'){throw "Workflow permissions are not declared: $($workflow.Name)"};$checks++
 if($yaml -notmatch '(?im)^\s*contents\s*:\s*read\s*$'){throw "Read-only contents permission is missing: $($workflow.Name)"};$checks++
}
$release=[IO.File]::ReadAllText((Join-Path $root '.github/workflows/release.yml'))
if($release -match 'RELEASE_ALLOWED_SIGNERS|WINDOWS_SIGNER_THUMBPRINT|RequireSignature'){throw 'Release publishing must not require a signing certificate or signing key.'};$checks++
if($release -notmatch 'Verify-ReleaseTag\.ps1'){throw 'Release tag validation is missing.'};$checks++
if($release -notmatch 'Verify-Checksums\.ps1'){throw 'Release checksum validation is missing.'};$checks++
if($release -notmatch 'gh release create[^\r\n]*--draft'){throw 'Release publishing must create a draft for review.'};$checks++
Write-Output "Workflow security tests passed: $checks checks across $($workflows.Count) workflows."
