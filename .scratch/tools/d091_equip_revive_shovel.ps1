$ErrorActionPreference = "Stop"
$excelDir = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\Gravedigger2026\Assets\ConfigTables\Mode2\Excel"))
$csv = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\Gravedigger2026\Assets\ConfigTables\Mode2\Csv\Protagonist_ProtagonistEquipmentConfig.csv"))
$xlsx = (Get-ChildItem -LiteralPath $excelDir -Filter "*Protagonist_ProtagonistEquipmentConfig.xlsx" | Select-Object -First 1).FullName
if (-not $xlsx) { throw "xlsx not found in $excelDir" }

function U([int[]]$codes) {
    -join ($codes | ForEach-Object { [char]$_ })
}

$displayName = U @(0x590D, 0x6D3B, 0x94F2)
function MakeDesc([int]$pct) {
    return (U @(0x6316, 0x5F00, 0x575F, 0x5893, 0x65F6, 0x6709)) + "$pct" + (U @(0x0025, 0x6982, 0x7387, 0x76F4, 0x63A5, 0x4EA7, 0x51FA, 0x58EB, 0x5175))
}

$rows = @(
    ,@("Equip_ReviveShovel", 1, $displayName, "Icon_IronShovel", 1, 1, "Dig", "DigOnGraveClear_0.2|DigLightningPreviewSec_2", (MakeDesc 20))
    ,@("Equip_ReviveShovel", 2, $displayName, "Icon_IronShovel", 1, 2, "Dig", "DigOnGraveClear_0.4|DigLightningPreviewSec_2", (MakeDesc 40))
    ,@("Equip_ReviveShovel", 3, $displayName, "Icon_IronShovel", 1, 3, "Dig", "DigOnGraveClear_0.6|DigLightningPreviewSec_2", (MakeDesc 60))
    ,@("Equip_ReviveShovel", 4, $displayName, "Icon_IronShovel", 1, 4, "Dig", "DigOnGraveClear_0.8|DigLightningPreviewSec_2", (MakeDesc 80))
    ,@("Equip_ReviveShovel", 5, $displayName, "Icon_IronShovel", $null, 5, "Dig", "DigOnGraveClear_1|DigLightningPreviewSec_2", (MakeDesc 100))
)

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$wb = $null
$ws = $null
try {
    $wb = $excel.Workbooks.Open($xlsx)
    $ws = $wb.Worksheets.Item(1)
    $used = $ws.UsedRange
    $headerRow = 0
    $idCol = 0
    $maxCol = [int]$used.Columns.Count
    $maxRow = [int]$used.Rows.Count
    for ($r = 1; $r -le 3; $r++) {
        for ($c = 1; $c -le $maxCol; $c++) {
            $v = [string]$ws.Cells.Item($r, $c).Text
            if ($v -eq "EquipId") {
                $headerRow = $r
                $idCol = $c
                break
            }
        }
        if ($headerRow -gt 0) { break }
    }
    if ($headerRow -eq 0) { throw "EquipId header not found" }

    $existing = @{}
    for ($r = $headerRow + 1; $r -le $maxRow; $r++) {
        $eid = [string]$ws.Cells.Item($r, $idCol).Text
        if ($eid -eq "Equip_ReviveShovel") {
            $lvl = [int]$ws.Cells.Item($r, $idCol + 1).Text
            $existing[$lvl] = $r
        }
    }

    foreach ($row in $rows) {
        $lvl = [int]$row[1]
        if ($existing.ContainsKey($lvl)) {
            $target = [int]$existing[$lvl]
        } else {
            $maxRow = $maxRow + 1
            $target = $maxRow
        }
        for ($c = 0; $c -lt $row.Count; $c++) {
            $cellVal = $row[$c]
            if ($null -eq $cellVal -or $cellVal -eq "") {
                $ws.Cells.Item($target, $c + 1).Clear()
            } else {
                $ws.Cells.Item($target, $c + 1).NumberFormat = "@"
                $ws.Cells.Item($target, $c + 1).Value2 = [string]$cellVal
            }
        }
        Write-Output ("WRITE L{0} row {1}" -f $lvl, $target)
    }

    $wb.Save()
    Write-Output "SAVED xlsx"

    $used = $ws.UsedRange
    $maxCol = [int]$used.Columns.Count
    $maxRow = [int]$used.Rows.Count
    $lines = New-Object System.Collections.Generic.List[string]
    for ($r = $headerRow; $r -le $maxRow; $r++) {
        $cells = New-Object System.Collections.Generic.List[string]
        for ($c = 1; $c -le $maxCol; $c++) {
            $raw = $ws.Cells.Item($r, $c).Text
            if ($null -eq $raw) { $raw = "" }
            $text = [string]$raw
            if ($text -match '[,\"\r\n]') {
                $text = '"' + $text.Replace('"', '""') + '"'
            }
            $cells.Add($text)
        }
        if ($r -gt $headerRow -and [string]::IsNullOrWhiteSpace(($cells -join "").Trim())) {
            continue
        }
        $lines.Add(($cells -join ","))
    }
    [System.IO.File]::WriteAllText($csv, (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding $false))
    Write-Output ("BAKE csv lines={0}" -f $lines.Count)
}
finally {
    if ($wb) { $wb.Close($false) }
    $excel.Quit()
    if ($ws) { [System.Runtime.Interopservices.Marshal]::ReleaseComObject($ws) | Out-Null }
    if ($wb) { [System.Runtime.Interopservices.Marshal]::ReleaseComObject($wb) | Out-Null }
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
