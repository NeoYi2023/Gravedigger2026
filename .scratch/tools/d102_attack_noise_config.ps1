# D-102 slice 01: attack-noise / berserk columns and bake CSV (SPEC_04 14.7).
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
            if ($v -eq "ClassId" -or $v -eq "MonsterId" -or $v -eq "SkillEffectId") { return $r }
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

function Fill-IfEmpty($sheet, $row, $col, $value) {
    $cur = ([string]$sheet.Cells.Item($row, $col).Text).Trim()
    if ($cur -eq "") { $sheet.Cells.Item($row, $col) = $value }
}

function Patch-NumericNoise($xlsxPath, $pk, $cols) {
    Invoke-Excel $xlsxPath {
        param($sheet)
        $headerRow = Get-HeaderRowIndex $sheet
        foreach ($col in $cols) {
            Ensure-Column $sheet $headerRow $col.En $col.Zh $col.Note
        }
        $names = Get-HeaderNames $sheet $headerRow
        $idCol = Col-Index $names $pk
        $lastRow = $sheet.UsedRange.Row + $sheet.UsedRange.Rows.Count - 1
        for ($r = $headerRow + 1; $r -le $lastRow; $r++) {
            $id = ([string]$sheet.Cells.Item($r, $idCol).Text).Trim()
            if ($id -eq "") { continue }
            foreach ($col in $cols) {
                $c = Col-Index $names $col.En
                if ($col.EmptyText) {
                    Fill-IfEmpty $sheet $r $c ""
                } else {
                    Fill-IfEmpty $sheet $r $c 0
                }
            }
        }
    } $true
}

$zhAttackNoise = U 25915,20987,22122,22768,20540
$zhNoiseRadius = U 22122,22768,21322,24452
$zhBerserkReq = U 29378,26292,35201,27714,20540
$zhBerserkMode = U 29378,26292,21518,20167,24680,27169,24335
$noteClassNoise = "D-102 0=none; >0 AA HitConfirm noise"
$noteRadius = "D-102 world XZ radius"
$noteThresh = "D-102 0=never berserk"
$noteMode = "D-102 empty=no switch; PassiveChase/StationaryPassive provoke"
$noteSkillNoise = "D-102 0=none; >0 on matching TriggerHook"

$classCols = @(
    @{ En = "AttackNoiseValue"; Zh = $zhAttackNoise; Note = $noteClassNoise; EmptyText = $false },
    @{ En = "NoiseRadius"; Zh = $zhNoiseRadius; Note = $noteRadius; EmptyText = $false }
)
$monsterCols = @(
    @{ En = "BerserkNoiseThreshold"; Zh = $zhBerserkReq; Note = $noteThresh; EmptyText = $false },
    @{ En = "BerserkAggroMode"; Zh = $zhBerserkMode; Note = $noteMode; EmptyText = $true }
)
$skillCols = @(
    @{ En = "AttackNoiseValue"; Zh = $zhAttackNoise; Note = $noteSkillNoise; EmptyText = $false },
    @{ En = "NoiseRadius"; Zh = $zhNoiseRadius; Note = $noteRadius; EmptyText = $false }
)

$jobs = @(
    @{ Dir = "Excel"; Csv = "Csv" },
    @{ Dir = "Mode2\Excel"; Csv = "Mode2\Csv" }
)

foreach ($job in $jobs) {
    $excelDir = Join-Path $root $job.Dir
    $csvDir = Join-Path $root $job.Csv
    $classXlsx = Resolve-Xlsx $excelDir "Manufacture_ClassConfig"
    $monsterXlsx = Resolve-Xlsx $excelDir "Defend_MonsterConfig"
    $skillXlsx = Resolve-Xlsx $excelDir "Combat_SkillEffectConfig"
    Patch-NumericNoise $classXlsx "ClassId" $classCols
    Write-Host ("OK class {0}" -f $classXlsx)
    Patch-NumericNoise $monsterXlsx "MonsterId" $monsterCols
    Write-Host ("OK monster {0}" -f $monsterXlsx)
    Patch-NumericNoise $skillXlsx "SkillEffectId" $skillCols
    Write-Host ("OK skill {0}" -f $skillXlsx)
    Bake-Sheet $classXlsx (Join-Path $csvDir "Manufacture_ClassConfig.csv")
    Bake-Sheet $monsterXlsx (Join-Path $csvDir "Defend_MonsterConfig.csv")
    Bake-Sheet $skillXlsx (Join-Path $csvDir "Combat_SkillEffectConfig.csv")
}
