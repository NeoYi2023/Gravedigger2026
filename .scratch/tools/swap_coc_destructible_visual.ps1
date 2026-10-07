$ErrorActionPreference = 'Stop'
$path = 'f:\CursorGame_Git\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps\Coc_Lv2_01.prefab'
$text = [System.IO.File]::ReadAllText($path)

function NL([string]$s) { return ($s -replace "`r`n", "`n") }

# Inverse of Rx(45): Rx(-45)
$rotNeg45 = '{x: -0.38268343, y: 0, z: 0, w: 0.92387956}'
$rotPos45 = '{x: 0.38268343, y: 0, z: 0, w: 0.92387956}'
$rotId = '{x: 0, y: 0, z: 0, w: 1}'

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

foreach ($item in $items) {
    $name = $item.Name
    $rootGo = $item.RootGo
    $rootTr = $item.RootTr
    $mono = $item.Mono
    $sprite = $item.Sprite
    $visualGo = $item.VisualGo
    $visualTr = $item.VisualTr

    # --- Root GO components: Transform + SpriteRenderer (drop Mono)
    $rootGoOld = NL @"
  m_Component:
  - component: {fileID: $rootTr}
  - component: {fileID: $mono}
  m_Layer: 0
  m_Name: $name
"@
    $rootGoNew = NL @"
  m_Component:
  - component: {fileID: $rootTr}
  - component: {fileID: $sprite}
  m_Layer: 0
  m_Name: $name
"@
    if ($text.IndexOf($rootGoOld) -lt 0) { throw "Root GO components not found: $name" }
    $text = $text.Replace($rootGoOld, $rootGoNew)

    # --- Visual GO components: Transform + Mono (drop Sprite)
    $visGoOld = NL @"
  m_Component:
  - component: {fileID: $visualTr}
  - component: {fileID: $sprite}
  m_Layer: 0
  m_Name: Visual
"@
    $visGoNew = NL @"
  m_Component:
  - component: {fileID: $visualTr}
  - component: {fileID: $mono}
  m_Layer: 0
  m_Name: Visual
"@
    if ($text.IndexOf($visGoOld) -lt 0) { throw "Visual GO components not found: $name" }
    $text = $text.Replace($visGoOld, $visGoNew)

    # --- Root Transform: rot 0 -> 45 (keep children)
    $rootTrStart = "--- !u!4 &$rootTr`nTransform:"
    $rootTrIdx = $text.IndexOf($rootTrStart)
    if ($rootTrIdx -lt 0) { throw "Root transform missing: $name" }
    $rootTrEnd = $text.IndexOf("`n--- !", $rootTrIdx + 10)
    $rootTrBlock = $text.Substring($rootTrIdx, $rootTrEnd - $rootTrIdx)
    if ($rootTrBlock -notmatch 'm_LocalEulerAnglesHint: \{x: 0, y: 0, z: 0\}') {
        throw "Root expected rot X=0 before swap: $name"
    }
    $rootTrBlock2 = $rootTrBlock.Replace("m_LocalRotation: $rotId", "m_LocalRotation: $rotPos45")
    $rootTrBlock2 = $rootTrBlock2.Replace(
        'm_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}',
        'm_LocalEulerAnglesHint: {x: 45, y: 0, z: 0}')
    $text = $text.Substring(0, $rootTrIdx) + $rootTrBlock2 + $text.Substring($rootTrEnd)

    # --- Visual Transform: rot 45 -> -45 (world identity under tilted parent)
    $visTrStart = "--- !u!4 &$visualTr`nTransform:"
    $visTrIdx = $text.IndexOf($visTrStart)
    if ($visTrIdx -lt 0) { throw "Visual transform missing: $name" }
    $visTrEnd = $text.IndexOf("`n--- !", $visTrIdx + 10)
    $visTrBlock = $text.Substring($visTrIdx, $visTrEnd - $visTrIdx)
    if ($visTrBlock -notmatch 'm_LocalEulerAnglesHint: \{x: 45, y: 0, z: 0\}') {
        throw "Visual expected rot X=45 before swap: $name"
    }
    $visTrBlock2 = $visTrBlock.Replace("m_LocalRotation: $rotPos45", "m_LocalRotation: $rotNeg45")
    $visTrBlock2 = $visTrBlock2.Replace(
        'm_LocalEulerAnglesHint: {x: 45, y: 0, z: 0}',
        'm_LocalEulerAnglesHint: {x: -45, y: 0, z: 0}')
    $text = $text.Substring(0, $visTrIdx) + $visTrBlock2 + $text.Substring($visTrEnd)

    # --- MonoBehaviour: retarget to Visual + sink root
    $monoStart = "--- !u!114 &$mono`nMonoBehaviour:"
    $monoIdx = $text.IndexOf($monoStart)
    if ($monoIdx -lt 0) { throw "Mono missing: $name" }
    $monoEnd = $text.IndexOf("`n--- !", $monoIdx + 10)
    $monoBlock = $text.Substring($monoIdx, $monoEnd - $monoIdx)
    $goLineOld = "  m_GameObject: {fileID: $rootGo}"
    if ($monoBlock.IndexOf($goLineOld) -lt 0) { throw "Mono GO line missing: $name" }
    $monoBlock2 = $monoBlock.Replace($goLineOld, "  m_GameObject: {fileID: $visualGo}")
    if ($monoBlock2 -notmatch '_targetValue: 1') { throw "Mono fields missing: $name" }
    if ($monoBlock2 -notmatch '_sinkRoot:') {
        $monoBlock2 = $monoBlock2.Replace(
            "  _maxHp: 5`n  _targetValue: 1",
            "  _maxHp: 5`n  _targetValue: 1`n  _sinkRoot: {fileID: $rootTr}")
    }
    $text = $text.Substring(0, $monoIdx) + $monoBlock2 + $text.Substring($monoEnd)

    # --- SpriteRenderer: retarget to Root
    $sprStart = "--- !u!212 &$sprite`nSpriteRenderer:"
    $sprIdx = $text.IndexOf($sprStart)
    if ($sprIdx -lt 0) { throw "Sprite missing: $name" }
    $sprEnd = $text.IndexOf("`n--- !", $sprIdx + 10)
    $sprBlock = $text.Substring($sprIdx, $sprEnd - $sprIdx)
    $sprGoOld = "  m_GameObject: {fileID: $visualGo}"
    if ($sprBlock.IndexOf($sprGoOld) -lt 0) { throw "Sprite GO line missing: $name" }
    $sprBlock2 = $sprBlock.Replace($sprGoOld, "  m_GameObject: {fileID: $rootGo}")
    $text = $text.Substring(0, $sprIdx) + $sprBlock2 + $text.Substring($sprEnd)

    Write-Host "OK $name"
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
Write-Host "done"
