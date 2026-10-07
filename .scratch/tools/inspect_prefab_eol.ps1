$p = 'f:\CursorGame_Git\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps\Coc_Lv2_01.prefab'
$bytes = [IO.File]::ReadAllBytes($p)
$crlf = 0
$lf = 0
$n = [Math]::Min(20000, $bytes.Length)
for ($i = 0; $i -lt $n; $i++) {
    if ($bytes[$i] -eq 10) {
        if ($i -gt 0 -and $bytes[$i - 1] -eq 13) { $crlf++ } else { $lf++ }
    }
}
Write-Host "sample crlf=$crlf lf=$lf"
$t = [IO.File]::ReadAllText($p)
$i = $t.IndexOf('m_Name: CocDestructible_01')
$snippet = $t.Substring($i - 200, 260)
$snippet = $snippet.Replace([char]13, [char]0x25A1).Replace("`n", "<LF>`n")
Write-Host $snippet
