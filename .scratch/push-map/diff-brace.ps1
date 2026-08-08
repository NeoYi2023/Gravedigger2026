$lines = Get-Content '.scratch\push-map\stage.diff'
$addOpen = 0; $addClose = 0; $delOpen = 0; $delClose = 0
foreach ($l in $lines) {
  if ($l.StartsWith('+++') -or $l.StartsWith('---')) { continue }
  if ($l.StartsWith('+')) {
    $body = $l.Substring(1)
    $addOpen += ([regex]::Matches($body, '\{')).Count
    $addClose += ([regex]::Matches($body, '\}')).Count
  } elseif ($l.StartsWith('-')) {
    $body = $l.Substring(1)
    $delOpen += ([regex]::Matches($body, '\{')).Count
    $delClose += ([regex]::Matches($body, '\}')).Count
  }
}
Write-Output ("added:   open=" + $addOpen + " close=" + $addClose)
Write-Output ("removed: open=" + $delOpen + " close=" + $delClose)
Write-Output ("net bias change (close-open): " + (($addClose - $addOpen) - ($delClose - $delOpen)))
