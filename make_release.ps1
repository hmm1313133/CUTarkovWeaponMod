Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$pluginDir = "F:\SteamLibrary\steamapps\common\Casualties Unknown Demo\BepInEx\plugins\CUTarkovWeaponMod"
$releaseDir = "G:\modmake\TKF_medical\Release"
$zipPath = "$releaseDir\CUTarkovWeaponMod_v2.0.0.0.zip"

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
if (!(Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null }

$excludeExts = @('.pdb', '.xml')
$fileCount = 0

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, "$pluginDir\CUTarkovWeaponMod.dll", "CUTarkovWeaponMod.dll", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    $fileCount++

    $assetsDir = "$pluginDir\Framework\Assets"
    if (Test-Path $assetsDir) {
        Get-ChildItem $assetsDir -Recurse -File | ForEach-Object {
            if ($excludeExts -notcontains $_.Extension.ToLower()) {
                $relPath = $_.FullName.Substring($pluginDir.Length + 1).Replace('\', '/')
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $relPath, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                $fileCount++
            }
        }
    }

    $langDir = "$pluginDir\Lang"
    if (Test-Path $langDir) {
        Get-ChildItem $langDir -File | ForEach-Object {
            if ($excludeExts -notcontains $_.Extension.ToLower()) {
                [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, "Lang/$($_.Name)", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
                $fileCount++
            }
        }
    }
} finally {
    $zip.Dispose()
}

Copy-Item "G:\modmake\TKF_weapon\CHANGELOG.md" "$releaseDir\CUTarkovWeaponMod_CHANGELOG.md" -Force

$size = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Output "Created: $zipPath"
Write-Output "Files: $fileCount"
Write-Output "Size: $size MB"
