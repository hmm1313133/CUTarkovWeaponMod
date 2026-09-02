Add-Type -AssemblyName System.Drawing
param([string]$Path = 'g:/modmake/TKF_weapon/Framework/Assets/guns/sks/Tapco intrafuse.png')
$bmp = New-Object System.Drawing.Bitmap($Path)
$w = $bmp.Width; $h = $bmp.Height
Write-Output ("=== {0} ({1}x{2}) ===" -f (Split-Path $Path -Leaf), $w, $h)
for ($y = $h - 1; $y -ge 0; $y--) {
    $line = ''
    for ($x = 0; $x -lt $w; $x++) {
        $c = $bmp.GetPixel($x, $y)
        if ($c.A -lt 20) { $line += '.' }
        elseif ($c.A -lt 128) { $line += ':' }
        else { $line += '#' }
    }
    Write-Output $line
}
$bmp.Dispose()
