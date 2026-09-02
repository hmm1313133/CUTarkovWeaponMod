Add-Type -AssemblyName System.Drawing
$files = @(
    'g:/modmake/TKF_weapon/Framework/Assets/guns/sks/Tapco intrafuse.png',
    'g:/modmake/TKF_weapon/Framework/Assets/guns/sks/uas sks.png',
    'g:/modmake/TKF_weapon/Framework/Assets/guns/sks/sks_10.png'
)
foreach ($f in $files) {
    $bmp = New-Object System.Drawing.Bitmap($f)
    $w = $bmp.Width; $h = $bmp.Height
    $minx = $w; $miny = $h; $maxx = -1; $maxy = -1
    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            $c = $bmp.GetPixel($x, $y)
            if ($c.A -gt 20) {
                if ($x -lt $minx) { $minx = $x }
                if ($x -gt $maxx) { $maxx = $x }
                if ($y -lt $miny) { $miny = $y }
                if ($y -gt $maxy) { $maxy = $y }
            }
        }
    }
    Write-Output ("{0}: {1}x{2}, bbox=({3},{4})-({5},{6}), size={7}x{8}" -f (Split-Path $f -Leaf), $w, $h, $minx, $miny, $maxx, $maxy, ($maxx-$minx+1), ($maxy-$miny+1))
    $bmp.Dispose()
}
