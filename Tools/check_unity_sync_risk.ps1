param(
    [string]$Since = "10.days",
    [int]$Top = 12
)

$ErrorActionPreference = "Stop"

function Test-RiskAsset {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $false
    }

    $prefixes = @(
        "Gravedigger2026/Assets/Scenes/",
        "Gravedigger2026/Assets/Prefabs/",
        "Gravedigger2026/Assets/Settings/"
    )
    $suffixes = @(".unity", ".prefab", ".asset")

    $prefixHit = $false
    foreach ($prefix in $prefixes) {
        if ($Path.StartsWith($prefix)) {
            $prefixHit = $true
            break
        }
    }
    if (-not $prefixHit) {
        return $false
    }

    foreach ($suffix in $suffixes) {
        if ($Path.EndsWith($suffix)) {
            return $true
        }
    }
    return $false
}

function Get-GitOutput {
    param([string[]]$GitArgs)

    $output = & git @GitArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git command failed: git $($GitArgs -join ' ')"
    }
    return $output
}

$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $workspaceRoot

$repoCheck = Get-GitOutput @("rev-parse", "--show-toplevel")
if (-not $repoCheck) {
    throw "Current workspace is not a git repository."
}

$historyLines = Get-GitOutput @("log", "--since=$Since", "--name-only", "--pretty=format:", "--")
$counts = @{}
foreach ($line in $historyLines) {
    $path = $line.Trim()
    if (-not (Test-RiskAsset $path)) {
        continue
    }
    if (-not $counts.ContainsKey($path)) {
        $counts[$path] = 0
    }
    $counts[$path]++
}

$statusLines = Get-GitOutput @("status", "--porcelain")
$dirty = New-Object System.Collections.Generic.HashSet[string]
foreach ($line in $statusLines) {
    if ($line.Length -lt 4) {
        continue
    }
    $path = $line.Substring(3).Trim().Trim('"')
    if (Test-RiskAsset $path) {
        [void]$dirty.Add($path)
    }
}

Write-Host "Unity sync risk report"
Write-Host "repo:  $workspaceRoot"
Write-Host "since: $Since"
Write-Host ""

$ranked = $counts.GetEnumerator() | Sort-Object @{Expression="Value";Descending=$true}, @{Expression="Key";Descending=$false}
$topCount = [Math]::Min($Top, @($ranked).Count)
Write-Host "Top $topCount frequently changed Unity assets"
Write-Host ("-" * 39)
if ($topCount -eq 0) {
    Write-Host "No recent Unity YAML asset changes found in the selected range."
} else {
    $ranked | Select-Object -First $Top | ForEach-Object {
        $marker = if ($dirty.Contains($_.Key)) { "DIRTY" } else { "clean" }
        "{0,3}  {1,5}  {2}" -f $_.Value, $marker, $_.Key
    }
}

Write-Host ""
Write-Host "Currently dirty high-risk Unity assets"
Write-Host ("-" * 37)
if ($dirty.Count -eq 0) {
    Write-Host "No dirty high-risk Unity assets."
} else {
    $dirty | Sort-Object | ForEach-Object {
        $recentHits = if ($counts.ContainsKey($_)) { $counts[$_] } else { 0 }
        if ($recentHits -gt 0) {
            "- $_ recentCommits=$recentHits"
        } else {
            "- $_"
        }
    }
}

Write-Host ""
Write-Host "Suggested workflow"
Write-Host "------------------"
Write-Host "1. Pull before opening Unity when any listed asset is shared across machines."
Write-Host "2. Avoid editing the same prefab/scene on both machines before a commit lands."
Write-Host "3. If a shared asset is already DIRTY, inspect it before pulling or reopening Unity."
