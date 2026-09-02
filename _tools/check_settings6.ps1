$path = 'C:/Users/Administrator/AppData/LocalLow/Orsoniks/CasualtiesUnknown/settings.json'
$item = Get-Item $path
Write-Output ("LastWriteTime: " + $item.LastWriteTime)
$content = Get-Content $path -Raw
$json = $content | ConvertFrom-Json
foreach ($i in $json) {
    if ($i.name -match 'cutarkovweapon|nightmare') {
        Write-Output ("{0} = {1}" -f $i.name, $i.value)
    }
}
