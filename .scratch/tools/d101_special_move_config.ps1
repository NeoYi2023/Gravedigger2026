# D-101 slice 01: add ClassConfig.SpecialMove and bake CSV (SPEC_04 14.7).
# ASCII-only source so Windows PowerShell 5 parses it without a UTF-8 BOM.
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
            if ($v -eq "ClassId") { return $r }
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

$zhSpecialMove = U 0x7279,0x6B8A,0x79FB,0x52A8
$zhNote = "0=no 1=yes; SpecialMove soldiers ignore SupportsSpecialMove AirWall (D-101)"

function Patch-Class($xlsxPath, [int]$rogueValue) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        Ensure-Column $sheet $headerRow "SpecialMove" $zhSpecialMove $zhNote
        $names = Get-HeaderNames $sheet $headerRow
        $idCol = Col-Index $names "ClassId"
        $flagCol = Col-Index $names "SpecialMove"
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        for ($r = $headerRow + 1; $r -le $lastRow; $r++) {
            $id = ([string]$sheet.Cells.Item($r, $idCol).Text).Trim()
            if ($id -eq "") { continue }
            $value = 0
            if ($id -eq "Class_BaseRogue") { $value = $rogueValue }
            $sheet.Cells.Item($r, $flagCol) = $value
        }
        Write-Host ("OK class rogue={0} {1}" -f $rogueValue, $xlsxPath)
    } $true
}

$mode1Excel = Join-Path $root "Excel"
$mode1Csv = Join-Path $root "Csv"
$mode2Excel = Join-Path $root "Mode2\Excel"
$mode2Csv = Join-Path $root "Mode2\Csv"

Patch-Class (Resolve-Xlsx $mode1Excel "Manufacture_ClassConfig") 0
Patch-Class (Resolve-Xlsx $mode2Excel "Manufacture_ClassConfig") 1
Bake-Sheet (Resolve-Xlsx $mode1Excel "Manufacture_ClassConfig") (Join-Path $mode1Csv "Manufacture_ClassConfig.csv")
Bake-Sheet (Resolve-Xlsx $mode2Excel "Manufacture_ClassConfig") (Join-Path $mode2Csv "Manufacture_ClassConfig.csv")
