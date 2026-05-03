param()

$stdin = [Console]::In.ReadToEnd()
$logFile = Join-Path $PSScriptRoot '..\agent_communications.log'
$timestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')

$entry = @"
=== $timestamp ===
$stdin
"@

$entry | Out-File -FilePath $logFile -Append -Encoding UTF8
Write-Output '{"continue": true}'
