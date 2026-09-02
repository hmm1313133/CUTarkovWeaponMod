# 删除所有配件介绍中的 "<color=#ffcc4d>...</color>" 安装提示段落
# 以及其前面的 "\n\n"（保留效果数值段落）
param([string]$Path)

$raw = Get-Content $Path -Encoding UTF8 -Raw

# 匹配 "<color=#ffcc4d>...</color>" 及其前面的 "\n\n"
# 使用非贪婪匹配，跨行（JSON 里 \n 是字面字符，不是真实换行）
$pattern = '\\n\\n<color=#ffcc4d>.*?</color>'
$new = [regex]::Replace($raw, $pattern, '')

# 处理结尾可能残留的 "\\n"（如果安装提示在末尾且前面只有单个 \n）
$pattern2 = '\\n<color=#ffcc4d>.*?</color>'
$new = [regex]::Replace($new, $pattern2, '')

if ($new -ne $raw) {
    [System.IO.File]::WriteAllText($Path, $new, (New-Object System.Text.UTF8Encoding($false)))
    Write-Output "Updated: $Path"
} else {
    Write-Output "No change: $Path"
}
