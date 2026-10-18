<#
.SYNOPSIS
    Runs the new ShareSmallBiz API locally in Development on http://localhost:5280.

.DESCRIPTION
    Local only. Uses src/ShareSmallBiz.Api/sharesmallbiz-dev.db and src/ShareSmallBiz.Api/dev-mail,
    never the live site's data or keys. On first run, creates a random JWT signing key in user secrets.

.PARAMETER Reset
    Deletes the local database and dev mail before starting (a fresh, empty API).

.EXAMPLE
    ./scripts/api-local.ps1 -Reset
#>
param(
    [switch]$Reset,
    [int]$Port = 5280
)

$ErrorActionPreference = 'Stop'
$project = Join-Path (Split-Path -Parent $PSScriptRoot) 'src/ShareSmallBiz.Api'

$secrets = dotnet user-secrets list --project $project 2>$null
if (-not ($secrets -match '^Jwt:SigningKey = ')) {
    $bytes = [byte[]]::new(48)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
    dotnet user-secrets set 'Jwt:SigningKey' ([Convert]::ToBase64String($bytes)) --project $project | Out-Null
    Write-Host 'Created a local JWT signing key in user secrets.'
}

if ($Reset) {
    Remove-Item (Join-Path $project 'sharesmallbiz-dev.db*'), (Join-Path $project 'dev-mail') -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Reset the local database and dev mail.'
}

Write-Host ''
Write-Host "  API   http://localhost:$Port"
Write-Host "  Test  http://localhost:$Port/api-test-spark/   (interactive harness)"
Write-Host "  Docs  http://localhost:$Port/scalar/v1"
Write-Host "  Mail  $(Join-Path $project 'dev-mail')"
Write-Host "  Check ./scripts/api-smoke.ps1   (in a second terminal)"
Write-Host ''

$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $project
try {
    dotnet run --no-launch-profile --urls "http://localhost:$Port"
}
finally {
    Pop-Location
}
