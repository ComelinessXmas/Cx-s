$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
function RoundPath([float]$x,[float]$y,[float]$w,[float]$h,[float]$r) {
    $p=[Drawing.Drawing2D.GraphicsPath]::new(); $d=$r*2
    $p.AddArc($x,$y,$d,$d,180,90); $p.AddArc(($x+$w-$d),$y,$d,$d,270,90)
    $p.AddArc(($x+$w-$d),($y+$h-$d),$d,$d,0,90); $p.AddArc($x,($y+$h-$d),$d,$d,90,90)
    $p.CloseFigure(); return ,$p
}
$frames=@(); $sizes=@(16,24,32,48,64,128,256)
foreach($size in $sizes) {
    $bmp=[Drawing.Bitmap]::new($size,$size)
    $g=[Drawing.Graphics]::FromImage($bmp); $g.SmoothingMode='AntiAlias'
    $g.ScaleTransform(($size/256.0),($size/256.0))
    $shape=RoundPath 8 8 240 240 55
    $gradient=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(128,8),[Drawing.Point]::new(128,248),[Drawing.Color]::White,[Drawing.Color]::White)
    $g.FillPath($gradient,$shape)
    $edge=[Drawing.Pen]::new([Drawing.Color]::FromArgb(220,222,226),2)
    $g.DrawPath($edge,$shape)
    $back=RoundPath 88 62 112 87 13
    $line=[Drawing.Pen]::new([Drawing.Color]::FromArgb(125,130,140),10)
    $g.DrawPath($line,$back)
    $front=RoundPath 54 106 112 87 13
    $glass=[Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Point]::new(110,106),[Drawing.Point]::new(110,193),[Drawing.Color]::White,[Drawing.Color]::White)
    $g.FillPath($glass,$front)
    $white=[Drawing.Pen]::new([Drawing.Color]::FromArgb(45,48,55),10); $g.DrawPath($white,$front)
    $ms=[IO.MemoryStream]::new(); $bmp.Save($ms,[Drawing.Imaging.ImageFormat]::Png); $frames+=,$ms.ToArray()
    if($size -eq 256){$bmp.Save((Join-Path $PSScriptRoot 'AirLink-icon.png'),[Drawing.Imaging.ImageFormat]::Png)}
    $ms.Dispose(); $g.Dispose(); $bmp.Dispose(); $shape.Dispose(); $gradient.Dispose(); $edge.Dispose(); $back.Dispose(); $line.Dispose(); $front.Dispose(); $glass.Dispose(); $white.Dispose()
}
$writer=[IO.BinaryWriter]::new([IO.File]::Create((Join-Path $PSScriptRoot 'AirLink.ico')))
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
$offset=6+16*$sizes.Count
for($i=0;$i -lt $sizes.Count;$i++) {
    $writer.Write([byte]($sizes[$i]%256)); $writer.Write([byte]($sizes[$i]%256)); $writer.Write([uint16]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$frames[$i].Length); $writer.Write([uint32]$offset)
    $offset+=$frames[$i].Length
}
foreach($frame in $frames){$writer.Write([byte[]]$frame)}
$writer.Dispose()

