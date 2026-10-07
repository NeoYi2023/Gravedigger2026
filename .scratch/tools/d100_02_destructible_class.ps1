# D-100 slice 02: Mode2 Class_BaseWarrior TargetTypeScores adds destructible obstacle.
# ASCII-only source so Windows PowerShell 5 parses it without a UTF-8 BOM.
$ErrorActionPreference = "Stop"

function U([int[]]$codes) {
    -join ($codes | ForEach-Object { [char]$_ })
}

$root = Join-Path $PSScriptRoot "..\..\Gravedigger2026\Assets\ConfigTables"
$root = [System.IO.Path]::GetFullPath($root)
$mode2Excel = Join-Path $root "Mode2\Excel"
$mode2Csv = Join-Path $root "Mode2\Csv"
$xlsxPath = (Get-ChildItem -LiteralPath $mode2Excel -Filter "*_Manufacture_ClassConfig.xlsx" | Select-Object -First 1).FullName
$csvPath = Join-Path $mode2Csv "Manufacture_ClassConfig.csv"
if (-not $xlsxPath) { throw "missing Manufacture_ClassConfig.xlsx" }

$enemy = U 0x654C,0x65B9,0x5355,0x4F4D
$wall = U 0x53EF,0x7834,0x574F,0x969C,0x788D,0x7269
$newScores = $enemy + ";1|" + $wall + ";1"

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

$excel = $null
$wb = $null
try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false
    $wb = $excel.Workbooks.Open($xlsxPath)
    $sheet = $wb.Worksheets.Item(1)
    $headerRow = Get-HeaderRowIndex $sheet
    $names = Get-HeaderNames $sheet $headerRow
    $idCol = Col-Index $names "ClassId"
    $typeCol = Col-Index $names "TargetTypeScores"
    $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
    $patched = $false
    for ($r = $headerRow + 1; $r -le $lastRow; $r++) {
        $id = ([string]$sheet.Cells.Item($r, $idCol).Text).Trim()
        if ($id -ne "Class_BaseWarrior") { continue }
        $sheet.Cells.Item($r, $typeCol) = $newScores
        $patched = $true
        break
    }
    if (-not $patched) { throw "Class_BaseWarrior not found" }
    $wb.Save() | Out-Null
    Write-Host ("OK patched Class_BaseWarrior TargetTypeScores={0}" -f $newScores)

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
    [System.IO.File]::WriteAllText($csvPath, $text, (New-Object System.Text.UTF8Encoding $true))
    Write-Host ("BAKE {0}" -f $csvPath)
}
finally {
    if ($wb) { $wb.Close($true) | Out-Null }
    if ($excel) { $excel.Quit() | Out-Null }
    if ($wb) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($wb) | Out-Null }
    if ($excel) { [System.Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null }
    [GC]::Collect()
    [GC]::WaitForPendingFinalizers()
}
