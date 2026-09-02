$OutputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName System.Drawing
$f = 'g:/modmake/TKF_weapon/Framework/Assets/guns/m4/kac ris.png'
$bmp = New-Object System.Drawing.Bitmap($f)
$w=$bmp.Width; $h=$bmp.Height
Write-Host "=== kac ris ${w}x${h} ==="
for ($x=0; $x -lt $w; $x+=4) {
  $minY=-1; $maxY=-1
  for ($y=0; $y -lt $h; $y++) { if ($bmp.GetPixel($x,$y).A -gt 30) { if($minY -lt 0){$minY=$y}; $maxY=$y } }
  if ($minY -ge 0) { Write-Host ("x={0}: y {1}..{2} (h={3})" -f $x,$minY,$maxY,($maxY-$minY+1)) }
}
$bmp.Dispose()
