$p = 'f:\CursorGame_Git\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps\Coc_Lv2_01.prefab'
$t = [IO.File]::ReadAllText($p)
$visualCount = ([regex]::Matches($t, 'm_Name: Visual')).Count
Write-Host "Visual count=$visualCount"
1..8 | ForEach-Object {
    $n = "CocDestructible_0$_"
    if ($t -notmatch [regex]::Escape("m_Name: $n")) { Write-Host "MISSING $n"; return }
    # root should not list 3 components including sprite after name nearby - soft check: each has Visual before it
    Write-Host "found $n"
}
# ensure sprite fileIDs still exist once each
$sprites = @(749072030188738574,2965411588718094782,3517537344635416759,6439629476068453610,2588308873611078390,6801601004515823984,7521911306155227885,8530981548946836609)
foreach ($s in $sprites) {
    $c = ([regex]::Matches($t, "--- !u!212 &$s")).Count
    if ($c -ne 1) { Write-Host "Sprite $s count=$c" } else { Write-Host "Sprite $s OK" }
}
