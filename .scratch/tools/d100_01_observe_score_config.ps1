# D-100 slice 01: add ClassConfig/MonsterConfig columns and bake CSV (SPEC_04 14.7).
# ASCII-only source so Windows PowerShell 5 parses it without a UTF-8 BOM.
param([switch]$BakeOnly)
$ErrorActionPreference = "Stop"

function U([int[]]$codes) {
    -join ($codes | ForEach-Object { [char]$_ })
}

$root = Join-Path $PSScriptRoot "..\..\Gravedigger2026\Assets\ConfigTables"
$root = [System.IO.Path]::GetFullPath($root)

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
            if ($v -eq "ClassId" -or $v -eq "MonsterId") { return $r }
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

function Ensure-Column($sheet, $headerRow, $en, $zh, $note) {
    $names = Get-HeaderNames $sheet $headerRow
    if ($names.Contains($en)) { return }
    $insertAt = $names.Count + 1
    if ($headerRow -ge 3) {
        $sheet.Cells.Item(1, $insertAt) = $zh
        $sheet.Cells.Item(2, $insertAt) = $note
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
    $s = ([string]$v).Trim()
    return $s
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

$zhObserve = U 0x89C2,0x5BDF,0x8303,0x56F4
$zhDistPri = (U 0x8DDD,0x79BB) + (U 0x4F18,0x5148,0x7EA7)
$zhValMode = (U 0x76EE,0x6807,0x4EF7,0x503C) + (U 0x6A21,0x5F0F)
$zhTypeScore = (U 0x76EE,0x6807,0x7C7B,0x578B) + (U 0x5206,0x6570)
$zhTargetVal = U 0x76EE,0x6807,0x4EF7,0x503C
$nearest = U 0x4F18,0x5148,0x6700,0x8FD1
$highToLow = U 0x7531,0x5927,0x5230,0x5C0F
$enemyUnit = (U 0x654C,0x65B9,0x5355,0x4F4D) + ";1"

function Patch-Class($xlsxPath, [bool]$fillDemo) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        Ensure-Column $sheet $headerRow "ObserveRange" $zhObserve "COC observe+fog radius; empty/<=0 -> 2"
        Ensure-Column $sheet $headerRow "DistancePriority" $zhDistPri "prefer nearest|farthest; empty=nearest"
        Ensure-Column $sheet $headerRow "TargetValueMode" $zhValMode "high-to-low|ignore; empty=high-to-low"
        Ensure-Column $sheet $headerRow "TargetTypeScores" $zhTypeScore "type;score|... ; empty=no types"
        $names = Get-HeaderNames $sheet $headerRow
        $idCol = Col-Index $names "ClassId"
        $obsCol = Col-Index $names "ObserveRange"
        $distCol = Col-Index $names "DistancePriority"
        $valCol = Col-Index $names "TargetValueMode"
        $typeCol = Col-Index $names "TargetTypeScores"
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        for ($r = $headerRow + 1; $r -le $lastRow; $r++) {
            $id = ([string]$sheet.Cells.Item($r, $idCol).Text).Trim()
            if ($id -eq "") { continue }
            if ($fillDemo) {
                $sheet.Cells.Item($r, $obsCol) = 2
                $sheet.Cells.Item($r, $distCol) = $nearest
                $sheet.Cells.Item($r, $valCol) = $highToLow
                $sheet.Cells.Item($r, $typeCol) = $enemyUnit
            } else {
                $sheet.Cells.Item($r, $obsCol) = ""
                $sheet.Cells.Item($r, $distCol) = ""
                $sheet.Cells.Item($r, $valCol) = ""
                $sheet.Cells.Item($r, $typeCol) = ""
            }
        }
        Write-Host ("OK class fillDemo={0} {1}" -f $fillDemo, $xlsxPath)
    } $true
}

function Patch-Monster($xlsxPath, [bool]$fillDemo) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        Ensure-Column $sheet $headerRow "TargetValue" $zhTargetVal "COC target value; empty->1; <0 fail"
        $names = Get-HeaderNames $sheet $headerRow
        $idCol = Col-Index $names "MonsterId"
        $valueCol = Col-Index $names "TargetValue"
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        for ($r = $headerRow + 1; $r -le $lastRow; $r++) {
            $id = ([string]$sheet.Cells.Item($r, $idCol).Text).Trim()
            if ($id -eq "") { continue }
            if ($fillDemo) {
                $sheet.Cells.Item($r, $valueCol) = 1
            } else {
                $sheet.Cells.Item($r, $valueCol) = ""
            }
        }
        Write-Host ("OK monster fillDemo={0} {1}" -f $fillDemo, $xlsxPath)
    } $true
}

$mode1Excel = Join-Path $root "Excel"
$mode1Csv = Join-Path $root "Csv"
$mode2Excel = Join-Path $root "Mode2\Excel"
$mode2Csv = Join-Path $root "Mode2\Csv"

if (-not $BakeOnly) {
    Patch-Class (Resolve-Xlsx $mode1Excel "Manufacture_ClassConfig") $false
    Patch-Monster (Resolve-Xlsx $mode1Excel "Defend_MonsterConfig") $false
    Patch-Class (Resolve-Xlsx $mode2Excel "Manufacture_ClassConfig") $true
    Patch-Monster (Resolve-Xlsx $mode2Excel "Defend_MonsterConfig") $true
}

foreach ($pair in @(
        @{ Excel = $mode1Excel; Csv = $mode1Csv },
        @{ Excel = $mode2Excel; Csv = $mode2Csv }
    )) {
    Bake-Sheet (Resolve-Xlsx $pair.Excel "Manufacture_ClassConfig") (Join-Path $pair.Csv "Manufacture_ClassConfig.csv")
    Bake-Sheet (Resolve-Xlsx $pair.Excel "Defend_MonsterConfig") (Join-Path $pair.Csv "Defend_MonsterConfig.csv")
}
