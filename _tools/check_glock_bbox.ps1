Add-Type -AssemblyName System.Drawing
$files = @("glock.png", "glock_magout.png", "glock_bigstick.png", "glock_g50.png")
foreach ($f in $files) {
    $path = "g:/modmake/TKF_weapon/Framework/Assets/guns/glock/$f"
    if (-not (Test-Path $path)) { Write-Host "$f : NOT FOUND"; continue }
    $bmp = [System.Drawing.Bitmap]::FromFile($path)
    $minY = 9999; $maxY = -1; $minX = 9999; $maxX = -1
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            $a = $bmp.GetPixel($x, $y).A
            if ($a -gt 10) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    Write-Host ("{0} size={1}x{2} bbox=({3},{4})-({5},{6})" -f $f, $bmp.Width, $bmp.Height, $minX, $minY, $maxX, $maxY)
    $bmp.Dispose()
}
