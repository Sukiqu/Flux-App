$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$lock=Get-Content -LiteralPath (Join-Path $root 'dependencies.lock.json') -Raw | ConvertFrom-Json
if($lock.schemaVersion -ne 1){throw 'Unknown dependency lock schema.'}
foreach($file in Get-ChildItem (Join-Path $root '.github/workflows') -Filter '*.yml') {
 $yaml=[IO.File]::ReadAllText($file.FullName)
 foreach($match in [Regex]::Matches($yaml,'(?m)^\s*(?:-\s*)?uses:\s*([^\s#]+)')) {
  if($match.Groups[1].Value -notmatch '^([A-Za-z0-9_-]+/[A-Za-z0-9_-]+)@([a-f0-9]{40})$'){throw 'Floating or unrecognized action dependency.'}
  $name=$Matches[1];$sha=$Matches[2]
  $expected=$lock.actions.PSObject.Properties[$name]
  if($null -eq $expected -or $expected.Value -ne $sha){throw "Action differs from dependency lock: $name"}
 }
}
Write-Output 'Dependency lock verified: no floating action references; no NuGet dependencies.'
