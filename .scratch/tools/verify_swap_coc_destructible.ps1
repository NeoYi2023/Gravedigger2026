$p = 'f:\CursorGame_Git\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps\Coc_Lv2_01.prefab'
$t = [IO.File]::ReadAllText($p)
1..8 | ForEach-Object {
    $n = "CocDestructible_0$_"
    if ($t -notmatch [regex]::Escape("m_Name: $n")) { throw "missing $n" }
}
$sink = ([regex]::Matches($t, '_sinkRoot: \{fileID:')).Count
Write-Host "sinkRoot fields=$sink"
# Sample check: Visual should have Mono script guid for obstacle
$c = ([regex]::Matches($t, 'm_Name: Visual')).Count
Write-Host "Visual count=$c"
Write-Host 'OK verify'
