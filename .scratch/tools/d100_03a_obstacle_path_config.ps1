# D-100 slice 03a: MonsterConfig.ObstaclePathMode + COC spawn sample (SPEC_04 14.7).
# ASCII-only source so Windows PowerShell 5 parses it without a UTF-8 BOM.
$ErrorActionPreference = "Stop"

function U([int[]]$codes) {
    -join ($codes | ForEach-Object { [char]$_ })
}

$root = Join-Path $PSScriptRoot "..\..\Gravedigger2026\Assets\ConfigTables"
$root = [System.IO.Path]::GetFullPath($root)

$zhMode = (U 0x969C,0x788D) + (U 0x5BFB,0x8DEF) + (U 0x6A21,0x5F0F)
$around = (U 0x89C6,0x4E3A) + (U 0x969C,0x788D)
$asTarget = (U 0x89C6,0x4E3A) + (U 0x76EE,0x6807)
$ignore = (U 0x65E0,0x89C6) + (U 0x969C,0x788D)
$note = "COC only: around|as-target|ignore; empty=around; illegal fail"

function Resolve-Xlsx($dir, $enSuffix) {
    $hit = Get-ChildItem -LiteralPath $dir -Filter "*_$enSuffix.xlsx" | Select-Object -First 1
    if (-not $hit) { throw "missing *$enSuffix.xlsx in $dir" }
    return $hit.FullName
}

function Get-HeaderRowIndex($sheet) {
    for ($r = 1; $r -le 3; $r++) {
        $used = $sheet.UsedRange.Columns.Count
        for ($c = 1; $c -le $used; $c++) {
            $v = ([string]$sheet.Cells.Item($r, $c).Text).Trim()
            if ($v -eq "MonsterId" -or $v -eq "GameplayConfigId") { return $r }
        }
    }
    throw "no EN header in first 3 rows"
}

function Get-HeaderNames($sheet, $headerRow) {
    $names = New-Object System.Collections.Generic.List[string]
    $used = $sheet.UsedRange.Columns.Count
    for ($c = 1; $c -le $used; $c++) {
        $names.Add((([string]$sheet.Cells.Item($headerRow, $c).Text).Trim()))
    }
    while ($names.Count -gt 0 -and $names[$names.Count - 1] -eq "") {
        $names.RemoveAt($names.Count - 1)
    }
    return $names
}

function Ensure-Column($sheet, $headerRow, $en, $zh, $noteText) {
    $names = Get-HeaderNames $sheet $headerRow
    if ($names.Contains($en)) { return }
    $insertAt = $names.Count + 1
    if ($headerRow -ge 3) {
        $sheet.Cells.Item(1, $insertAt) = $zh
        $sheet.Cells.Item(2, $insertAt) = $noteText
    }
    $sheet.Cells.Item($headerRow, $insertAt) = $en
}

function Col-Index($names, $en) {
    $i = $names.IndexOf($en)
    if ($i -lt 0) { throw "missing column $en" }
    return $i + 1
}

function Format-CsvNumber([double]$n) {
    if ([double]::IsNaN($n) -or [double]::IsInfinity($n)) { return "$n" }
    $roundedInt = [Math]::Round($n, 0, [MidpointRounding]::AwayFromZero)
    if ([Math]::Abs($n - $roundedInt) -lt 1e-9 -and [Math]::Abs($n) -lt 1e15) {
        return ([int]$roundedInt).ToString()
    }
    $text = ("{0:0.##########}" -f $n).TrimEnd("0").TrimEnd(".")
    if ($text -eq "" -or $text -eq "-") { return "0" }
    return $text
}

function Escape-CsvField([string]$value) {
    if ($null -eq $value) { return "" }
    if ($value -match '[,"\r\n]') {
        return '"' + ($value.Replace('"', '""')) + '"'
    }
    return $value
}

function Cell-CsvText($cell) {
    $v = $cell.Value2
    if ($null -eq $v) { return "" }
    if ($v -is [double] -or $v -is [int] -or $v -is [decimal] -or $v -is [long]) {
        return Format-CsvNumber ([double]$v)
    }
    return ([string]$v).Trim()
}

function Invoke-Excel($xlsxPath, [scriptblock]$body, [bool]$save) {
    $excel = $null
    $wb = $null
    try {
        $excel = New-Object -ComObject Excel.Application
        $excel.Visible = $false
        $excel.DisplayAlerts = $false
        $wb = $excel.Workbooks.Open($xlsxPath)
        $sheet = $wb.Worksheets.Item(1)
        & $body $sheet
        if ($save) { $wb.Save() | Out-Null }
    }
    finally {
        if ($wb) { $wb.Close($save) | Out-Null }
        if ($excel) { $excel.Quit() | Out-Null }
        if ($wb) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($wb) | Out-Null }
        if ($excel) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
        [GC]::Collect()
        [GC]::WaitForPendingFinalizers()
    }
}

function Bake-Sheet($xlsxPath, $csvPath) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        $names = Get-HeaderNames $sheet $headerRow
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        $lines = New-Object System.Collections.Generic.List[string]
        for ($r = $headerRow; $r -le $lastRow; $r++) {
            $vals = @()
            $any = $false
            for ($c = 1; $c -le $names.Count; $c++) {
                $t = Cell-CsvText $sheet.Cells.Item($r, $c)
                if ($t -ne "") { $any = $true }
                $vals += (Escape-CsvField $t)
            }
            if ($r -gt $headerRow -and -not $any) { continue }
            $lines.Add(($vals -join ","))
        }
        $text = ($lines -join "`n")
        if ($text.Length -gt 0) { $text += "`n" }
        [System.IO.File]::WriteAllText($csvPath, $text, (New-Object System.Text.UTF8Encoding $false))
        Write-Host ("BAKE {0} -> {1} header={2}" -f ([IO.Path]::GetFileName($xlsxPath)), ([IO.Path]::GetFileName($csvPath)), $lines[0])
    } $false
}

function Patch-Monster($xlsxPath, [bool]$fillDemo) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        Ensure-Column $sheet $headerRow "ObstaclePathMode" $zhMode $note
        $names = Get-HeaderNames $sheet $headerRow
        $idCol = Col-Index $names "MonsterId"
        $modeCol = Col-Index $names "ObstaclePathMode"
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        for ($r = $headerRow + 1; $r -le $lastRow; $r++) {
            $id = ([string]$sheet.Cells.Item($r, $idCol).Text).Trim()
            if ($id -eq "") { continue }
            if (-not $fillDemo) {
                $sheet.Cells.Item($r, $modeCol) = ""
                continue
            }
            if ($id -eq "Monster_02") {
                $sheet.Cells.Item($r, $modeCol) = $ignore
            } elseif ($id -eq "Monster_03") {
                $sheet.Cells.Item($r, $modeCol) = $asTarget
            } else {
                $sheet.Cells.Item($r, $modeCol) = $around
            }
        }
        Write-Host ("OK monster fillDemo={0} {1}" -f $fillDemo, $xlsxPath)
    } $true
}

function Patch-Spawn($xlsxPath) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        $names = Get-HeaderNames $sheet $headerRow
        $cGameplay = Col-Index $names "GameplayConfigId"
        $cPoint = Col-Index $names "SpawnPointId"
        $cMonster = Col-Index $names "MonsterId"
        $cCount = Col-Index $names "SpawnCount"
        $cRole = Col-Index $names "SpawnRole"
        $cOrder = Col-Index $names "SpawnOrder"
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        if ($lastRow -gt $headerRow) {
            $sheet.Rows.Item(($headerRow + 1).ToString() + ":" + $lastRow.ToString()).Delete() | Out-Null
        }
        $rows = New-Object System.Collections.Generic.List[object]
        foreach ($gameplay in @("Coc_Normal", "Coc_Hard", "Coc_Hell")) {
            $rows.Add(@($gameplay, "SP_01", "Monster_01", 1, "Normal", 1))
            $rows.Add(@($gameplay, "SP_01", "Monster_02", 1, "Normal", 2))
            $rows.Add(@($gameplay, "SP_01", "Monster_03", 1, "Normal", 3))
            $rows.Add(@($gameplay, "SP_Mini", "Monster_01", 1, "MiniBoss", 1))
            $rows.Add(@($gameplay, "SP_Final", "Monster_01", 1, "FinalBoss", 1))
        }
        for ($i = 0; $i -lt $rows.Count; $i++) {
            $r = $headerRow + 1 + $i
            $row = $rows[$i]
            $sheet.Cells.Item($r, $cGameplay) = $row[0]
            $sheet.Cells.Item($r, $cPoint) = $row[1]
            $sheet.Cells.Item($r, $cMonster) = $row[2]
            $sheet.Cells.Item($r, $cCount) = $row[3]
            $sheet.Cells.Item($r, $cRole) = $row[4]
            $sheet.Cells.Item($r, $cOrder) = $row[5]
        }
        Write-Host ("OK spawn {0} rows={1}" -f $xlsxPath, $rows.Count)
    } $true
}

$mode1Excel = Join-Path $root "Excel"
$mode1Csv = Join-Path $root "Csv"
$mode2Excel = Join-Path $root "Mode2\Excel"
$mode2Csv = Join-Path $root "Mode2\Csv"

Patch-Monster (Resolve-Xlsx $mode1Excel "Defend_MonsterConfig") $false
Patch-Monster (Resolve-Xlsx $mode2Excel "Defend_MonsterConfig") $true
Patch-Spawn (Resolve-Xlsx $mode2Excel "Coc_CocSpawnConfig")

Bake-Sheet (Resolve-Xlsx $mode1Excel "Defend_MonsterConfig") (Join-Path $mode1Csv "Defend_MonsterConfig.csv")
Bake-Sheet (Resolve-Xlsx $mode2Excel "Defend_MonsterConfig") (Join-Path $mode2Csv "Defend_MonsterConfig.csv")
Bake-Sheet (Resolve-Xlsx $mode2Excel "Coc_CocSpawnConfig") (Join-Path $mode2Csv "Coc_CocSpawnConfig.csv")
