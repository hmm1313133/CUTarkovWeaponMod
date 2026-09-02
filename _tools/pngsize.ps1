function Get-PngSize($p) {
    $b = [System.IO.File]::ReadAllBytes($p)
    $wBytes = $b[16..19]
    $hBytes = $b[20..23]
    [Array]::Reverse($wBytes)
    [Array]::Reverse($hBytes)
    $w = [System.BitConverter]::ToUInt32($wBytes, 0)
    $h = [System.BitConverter]::ToUInt32($hBytes, 0)
    return "$w x $h"
}
'sks_10: ' + (Get-PngSize 'Framework/Assets/guns/sks/sks_10.png')
'uas: ' + (Get-PngSize 'Framework/Assets/guns/sks/uas sks.png')
