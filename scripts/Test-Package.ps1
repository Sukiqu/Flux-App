$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$scratch=Join-Path $root ('artifacts/package-tests-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $scratch -Force | Out-Null
$checks=0
foreach($mode in @('valid','tampered','extra','traversal','duplicate','empty','absolute','bad-hash','missing-file','missing-entry','case-duplicate','self-reference','alternate-stream')) {
 $folder=Join-Path $scratch $mode
 New-Item -ItemType Directory $folder -Force | Out-Null
 [IO.File]::WriteAllText((Join-Path $folder 'payload.txt'),'test payload')
 $hash=(Get-FileHash (Join-Path $folder 'payload.txt') -Algorithm SHA256).Hash
 $line="$hash  payload.txt"
 switch($mode) {
  'tampered' {[IO.File]::WriteAllText((Join-Path $folder 'payload.txt'),'modified')}
  'extra' {[IO.File]::WriteAllText((Join-Path $folder 'extra.txt'),'unexpected')}
  'traversal' {$line="$hash  ../payload.txt"}
  'duplicate' {$line+="`n$line"}
  'empty' {$line=''}
  'absolute' {$line="$hash  C:\payload.txt"}
  'bad-hash' {$line=('0'*64)+'  payload.txt'}
  'missing-file' {$line="$hash  missing.txt"}
  'missing-entry' {$line="$hash  payload.txt";[IO.File]::WriteAllText((Join-Path $folder 'unlisted.txt'),'unlisted')}
  'case-duplicate' {$line+="`n$hash  PAYLOAD.TXT"}
  'self-reference' {$line="$hash  SHA256SUMS.txt"}
  'alternate-stream' {$line="$hash  payload.txt:hidden"}
 }
 [IO.File]::WriteAllText((Join-Path $folder 'SHA256SUMS.txt'),$line)
 $accepted=$true
 try {& (Join-Path $PSScriptRoot 'Verify-Checksums.ps1') -Directory $folder | Out-Null}catch{$accepted=$false}
 if($accepted -ne ($mode -eq 'valid')){throw "Package verification regression: $mode"}
 $checks++
}
foreach($mode in @('clean','personal-path','credential','private-file','checklist','publishing-notes','private-note','private-folder')) {
 $folder=Join-Path $scratch $mode
 New-Item -ItemType Directory $folder -Force | Out-Null
 $name='data.txt';$data='public data'
 if($mode -eq 'personal-path'){$data=('C:','Users','example-person','Desktop' -join '\')}
 if($mode -eq 'credential'){$data='ghp_'+('a'*36)}
 if($mode -eq 'private-file'){$name='settings.xml'}
 if($mode -eq 'checklist'){$name='TODO.md'}
 if($mode -eq 'publishing-notes'){$name='PUBLISHING.md'}
 if($mode -eq 'private-note'){$name='review.private.md'}
 if($mode -eq 'private-folder'){New-Item -ItemType Directory (Join-Path $folder 'private') | Out-Null;$name='private/notes.md'}
 [IO.File]::WriteAllText((Join-Path $folder $name),$data)
 $accepted=$true
 try {& (Join-Path $PSScriptRoot 'Test-Privacy.ps1') -Path $folder | Out-Null}catch{$accepted=$false}
 if($accepted -ne ($mode -eq 'clean')){throw "Privacy scanner regression: $mode"}
 $checks++
}
Write-Output "Package/scanner tests passed: $checks checks."
