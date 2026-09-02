Add-Type -AssemblyName System.Drawing

function Get-Pixels($bmp) {
    $w = $bmp.Width; $h = $bmp.Height
    $px = New-Object 'System.Drawing.Color[]' ($w * $h)
    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            $px[$y * $w + $x] = $bmp.GetPixel($x, $y)
        }
    }
    return ,$px
}

$base = New-Object System.Drawing.Bitmap('g:/modmake/TKF_weapon/Framework/Assets/guns/sks/sks_sksa5.png')
$uas = New-Object System.Drawing.Bitmap('g:/modmake/TKF_weapon/Framework/Assets/guns/sks/uas sks.png')
$bw = $base.Width; $bh = $base.Height
$uw = $uas.Width; $uh = $uas.Height
$bp = Get-Pixels $base
$up = Get-Pixels $uas

$sb = New-Object System.Text.StringBuilder
foreach ($cy in @(20.5, 23, 24, 25)) {
    $minY = [Math]::Min(0, [int][Math]::Floor($cy - $uh / 2))
    $maxY = [Math]::Max($bh, [int][Math]::Ceiling($cy + $uh / 2))
    $newH = $maxY - $minY
    $offY = -$minY
    $newW = $bw
    $out = New-Object 'System.Drawing.Color[]' ($newW * $newH)
    for ($i = 0; $i -lt $out.Length; $i++) { $out[$i] = [System.Drawing.Color]::FromArgb(0,0,0,0) }
    for ($y = 0; $y -lt $bh; $y++) { for ($x = 0; $x -lt $bw; $x++) { $out[($y + $offY) * $newW + $x] = $bp[$y * $bw + $x] } }
    for ($y = 0; $y -lt $bh; $y++) { for ($x = 0; $x -lt 45; $x++) { $out[($y + $offY) * $newW + $x] = [System.Drawing.Color]::FromArgb(0,0,0,0) } }
    $startX = 0
    $startY = [int]($cy) - [int]($uh / 2) + $offY
    for ($y = 0; $y -lt $uh; $y++) {
        $dy = $startY + $y
        if ($dy -lt 0 -or $dy -ge $newH) { continue }
        for ($x = 0; $x -lt $uw; $x++) {
            $dx = $startX + $x
            if ($dx -lt 0 -or $dx -ge $newW) { continue }
            $fg = $up[$y * $uw + $x]
            if ($fg.A -le 5) { continue }
            $out[$dy * $newW + $dx] = $fg
        }
    }
    [void]$sb.AppendLine("=== centerY=$cy (canvas ${newW}x${newH}) ===")
    for ($y = $newH - 1; $y -ge 0; $y--) {
        $line = ''
        for ($x = 0; $x -lt $newW; $x++) {
            $c = $out[$y * $newW + $x]
            if ($c.A -lt 20) { $line += '.' }
            elseif ($c.A -lt 128) { $line += ':' }
            else { $line += '#' }
        }
        [void]$sb.AppendLine($line)
    }
}
$base.Dispose(); $uas.Dispose()
[System.IO.File]::WriteAllText('g:/modmake/TKF_weapon/_tools/sim_out.txt', $sb.ToString())
Write-Output "done"
