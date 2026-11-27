param([Parameter(Mandatory=$true)][string]$SourceDirectory,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$root=(Resolve-Path -LiteralPath $SourceDirectory).Path
$target=[IO.Path]::GetFullPath($Destination)
if($target.StartsWith($root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'ZIP destination must be outside its input folder.'}
$names=[Collections.Generic.List[string]]::new()
$pending=[Collections.Generic.Queue[string]]::new();$pending.Enqueue($root)
while($pending.Count -gt 0) {
 $dir=$pending.Dequeue()
 if((Get-Item -LiteralPath $dir).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked ZIP input rejected.'}
 foreach($item in Get-ChildItem -LiteralPath $dir -Force) {
  if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked ZIP input rejected.'}
  if($item.PSIsContainer){$pending.Enqueue($item.FullName)}else{$names.Add($item.FullName.Substring($root.Length+1).Replace('\','/'))}
 }
}
$names.Sort([StringComparer]::Ordinal)
$stream=[IO.File]::Create($target)
try {
 $zip=[IO.Compression.ZipArchive]::new($stream,[IO.Compression.ZipArchiveMode]::Create,$true)
 try {
  foreach($name in $names) {
   # Stored entries avoid differences between compression-library versions.
   $entry=$zip.CreateEntry($name,[IO.Compression.CompressionLevel]::NoCompression)
   $entry.LastWriteTime=[DateTimeOffset]::new(2000,1,1,0,0,0,[TimeSpan]::Zero)
   $entry.ExternalAttributes=0
   $input=[IO.File]::OpenRead((Join-Path $root $name));$output=$entry.Open()
   try {$input.CopyTo($output)}finally{$output.Dispose();$input.Dispose()}
  }
 }finally{$zip.Dispose()}
}finally{$stream.Dispose()}
