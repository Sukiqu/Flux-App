$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
# Format conversion only. The transparent master was extracted with imagegen.
# Keep this viewport in sync with BrandAssets.Paint.
$sourceRect=[System.Drawing.RectangleF]::new(90,330,1080,688)
$source=[System.Drawing.Bitmap]::FromFile((Join-Path $PSScriptRoot 'flux-symbol.png'))
$sizes=@(16,20,24,32,40,48,64,128,256)
$frames=@()
try {
 foreach($size in $sizes) {
  $bitmap=[System.Drawing.Bitmap]::new($size,$size,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g=[System.Drawing.Graphics]::FromImage($bitmap)
  try {
   $g.Clear([System.Drawing.Color]::Transparent)
   $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
   $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
   $width=$size*0.94
   $height=$width*$sourceRect.Height/$sourceRect.Width
   $target=[System.Drawing.RectangleF]::new(($size-$width)/2,($size-$height)/2,$width,$height)
   $g.DrawImage($source,$target,$sourceRect,[System.Drawing.GraphicsUnit]::Pixel)
  } finally {$g.Dispose()}
  $data=[System.IO.MemoryStream]::new()
  try {
   if($size -eq 256) {$bitmap.Save($data,[System.Drawing.Imaging.ImageFormat]::Png)}
   else {
    $writer=[System.IO.BinaryWriter]::new($data)
    $stride=[int]([Math]::Ceiling($size/32.0)*4)
    $writer.Write([int]40); $writer.Write([int]$size); $writer.Write([int]($size*2))
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([int]0)
    $writer.Write([int]($size*$size*4+$stride*$size))
    1..4 | ForEach-Object {$writer.Write([int]0)}
    for($y=$size-1;$y -ge 0;$y--) {for($x=0;$x -lt $size;$x++) {
     $pixel=$bitmap.GetPixel($x,$y)
     $writer.Write([byte]$pixel.B); $writer.Write([byte]$pixel.G)
     $writer.Write([byte]$pixel.R); $writer.Write([byte]$pixel.A)
    }}
    for($y=$size-1;$y -ge 0;$y--) {
     $mask=[byte[]]::new($stride)
     for($x=0;$x -lt $size;$x++) {
      if($bitmap.GetPixel($x,$y).A -eq 0) {
       $index=[int][Math]::Floor($x/8)
       $mask[$index]=$mask[$index] -bor (128 -shr ($x%8))
      }
     }
     $writer.Write($mask)
    }
    $writer.Flush()
   }
   $frames+=,@{Size=$size;Data=$data.ToArray()}
  } finally {$data.Dispose();$bitmap.Dispose()}
 }
 $stream=[System.IO.File]::Create((Join-Path $PSScriptRoot 'flux.ico'))
 $output=[System.IO.BinaryWriter]::new($stream)
 try {
  $output.Write([uint16]0);$output.Write([uint16]1);$output.Write([uint16]$frames.Count)
  $offset=6+16*$frames.Count
  foreach($frame in $frames) {
   $dimension=if($frame.Size -eq 256){0}else{$frame.Size}
   $output.Write([byte]$dimension);$output.Write([byte]$dimension)
   $output.Write([byte]0);$output.Write([byte]0)
   $output.Write([uint16]1);$output.Write([uint16]32)
   $output.Write([int]$frame.Data.Length);$output.Write([int]$offset)
   $offset+=$frame.Data.Length
  }
  foreach($frame in $frames){$output.Write([byte[]]$frame.Data)}
 } finally {$output.Dispose();$stream.Dispose()}
} finally {$source.Dispose()}
Write-Output 'Flux icon: 16, 20, 24, 32, 40, 48, 64, 128, 256 px.'
