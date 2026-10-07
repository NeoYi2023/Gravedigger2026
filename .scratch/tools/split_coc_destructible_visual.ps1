$ErrorActionPreference = 'Stop'
$path = 'f:\CursorGame_Git\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps\Coc_Lv2_01.prefab'
$text = [System.IO.File]::ReadAllText($path)

function NL([string]$s) {
    return ($s -replace "`r`n", "`n")
}

$items = @(
    @{ Name='CocDestructible_01'; RootGo=8800000000000000401; RootTr=8800000000000000402; Mono=8800000000000000403; Sprite=749072030188738574; VisualGo=8800000000000000411; VisualTr=8800000000000000412 },
    @{ Name='CocDestructible_02'; RootGo=1437464043381446414; RootTr=2670880671327819954; Mono=6317084314843151083; Sprite=2965411588718094782; VisualGo=8800000000000000421; VisualTr=8800000000000000422 },
    @{ Name='CocDestructible_03'; RootGo=9037810961488421548; RootTr=5726680356874706591; Mono=7406043960309149832; Sprite=3517537344635416759; VisualGo=8800000000000000431; VisualTr=8800000000000000432 },
    @{ Name='CocDestructible_04'; RootGo=6762972401806835602; RootTr=7349210509176246149; Mono=4136556375063622475; Sprite=6439629476068453610; VisualGo=8800000000000000441; VisualTr=8800000000000000442 },
    @{ Name='CocDestructible_05'; RootGo=6821384725674047833; RootTr=3431459092294589197; Mono=3192298098382864277; Sprite=2588308873611078390; VisualGo=8800000000000000451; VisualTr=8800000000000000452 },
    @{ Name='CocDestructible_06'; RootGo=4759817321543214113; RootTr=6478533563377278434; Mono=5663045381746392218; Sprite=6801601004515823984; VisualGo=8800000000000000461; VisualTr=8800000000000000462 },
    @{ Name='CocDestructible_07'; RootGo=3825233883471533031; RootTr=5233232982993155422; Mono=5191234773320179336; Sprite=7521911306155227885; VisualGo=8800000000000000471; VisualTr=8800000000000000472 },
    @{ Name='CocDestructible_08'; RootGo=8408635932491590164; RootTr=1746892786774414147; Mono=1108214546299772720; Sprite=8530981548946836609; VisualGo=8800000000000000481; VisualTr=8800000000000000482 }
)

function Get-VisualBlock($item) {
    return (NL @"
--- !u!1 &$($item.VisualGo)
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: $($item.VisualTr)}
  - component: {fileID: $($item.Sprite)}
  m_Layer: 0
  m_Name: Visual
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &$($item.VisualTr)
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($item.VisualGo)}
  m_LocalRotation: {x: 0.38268343, y: 0, z: 0, w: 0.92387956}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: $($item.RootTr)}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 45, y: 0, z: 0}

"@)
}

foreach ($item in $items) {
    $name = $item.Name
    $rootGo = $item.RootGo
    $rootTr = $item.RootTr
    $mono = $item.Mono
    $sprite = $item.Sprite
    $visualGo = $item.VisualGo
    $visualTr = $item.VisualTr

    $goOld = NL @"
  m_Component:
  - component: {fileID: $rootTr}
  - component: {fileID: $mono}
  - component: {fileID: $sprite}
  m_Layer: 0
  m_Name: $name
"@
    $goNew = NL @"
  m_Component:
  - component: {fileID: $rootTr}
  - component: {fileID: $mono}
  m_Layer: 0
  m_Name: $name
"@
    if ($text.IndexOf($goOld) -lt 0) { throw "GO block not found: $name" }
    $text = $text.Replace($goOld, $goNew)

    $trNeedleStart = "--- !u!4 &$rootTr`nTransform:"
    $trIdx = $text.IndexOf($trNeedleStart)
    if ($trIdx -lt 0) { throw "Transform not found: $name" }
    $trEndRel = $text.IndexOf("`n--- !", $trIdx + 10)
    if ($trEndRel -lt 0) { throw "Transform end not found: $name" }
    $trBlock = $text.Substring($trIdx, $trEndRel - $trIdx)

    if ($trBlock -notmatch 'm_LocalEulerAnglesHint: \{x: 45, y: 0, z: 0\}') {
        throw "Unexpected root rotation for $name"
    }

    $trBlock2 = $trBlock.Replace(
        'm_LocalRotation: {x: 0.38268343, y: 0, z: 0, w: 0.92387956}',
        'm_LocalRotation: {x: 0, y: 0, z: 0, w: 1}')
    $trBlock2 = $trBlock2.Replace(
        (NL "m_Children: []`n  m_Father: {fileID: 2092522387770036821}"),
        (NL "m_Children:`n  - {fileID: $visualTr}`n  m_Father: {fileID: 2092522387770036821}"))
    $trBlock2 = $trBlock2.Replace(
        'm_LocalEulerAnglesHint: {x: 45, y: 0, z: 0}',
        'm_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}')
    $text = $text.Substring(0, $trIdx) + $trBlock2 + $text.Substring($trEndRel)

    $sprOld = "--- !u!212 &$sprite`nSpriteRenderer:"
    $sprIdx = $text.IndexOf($sprOld)
    if ($sprIdx -lt 0) { throw "SpriteRenderer not found: $name" }
    $sprEndRel = $text.IndexOf("`n--- !", $sprIdx + 10)
    $sprBlock = $text.Substring($sprIdx, $sprEndRel - $sprIdx)
    $goLineOld = "  m_GameObject: {fileID: $rootGo}"
    if ($sprBlock.IndexOf($goLineOld) -lt 0) { throw "Sprite GO line not found: $name" }
    $sprBlock2 = $sprBlock.Replace($goLineOld, "  m_GameObject: {fileID: $visualGo}")
    $text = $text.Substring(0, $sprIdx) + $sprBlock2 + $text.Substring($sprEndRel)

    $marker = "--- !u!1 &$rootGo`n"
    $mIdx = $text.IndexOf($marker)
    if ($mIdx -lt 0) { throw "Root GO marker not found: $name" }
    $visual = Get-VisualBlock $item
    $text = $text.Substring(0, $mIdx) + $visual + $text.Substring($mIdx)

    Write-Host "OK $name"
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
Write-Host "done"
