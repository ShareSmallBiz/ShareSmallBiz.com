<#
.SYNOPSIS
    End-to-end acceptance check of a locally running API over real HTTP.

.DESCRIPTION
    Walks the full account lifecycle (register, confirm, login, me, refresh, replay
    detection, logout, password reset) plus health, contract and CORS, reading
    confirmation and reset links from the Development-only /dev/mailbox API.
    Prints PASS/FAIL per check and exits 1 if anything fails.

    Refuses to run against anything but localhost, so it can never touch the live site.

.EXAMPLE
    ./scripts/api-local.ps1 -Reset     # terminal 1
    ./scripts/api-smoke.ps1            # terminal 2
#>
param(
    [string]$BaseUrl = 'http://localhost:5280'
)

$ErrorActionPreference = 'Stop'
if (([Uri]$BaseUrl).Host -notin @('localhost', '127.0.0.1')) {
    throw "api-smoke only runs against a local API (got $BaseUrl). It must never touch the live site."
}

$script:results = [System.Collections.Generic.List[object]]::new()

function Invoke-Api([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null, [hashtable]$Headers = @{}) {
    $request = @{ Method = $Method; Uri = "$BaseUrl$Path"; SkipHttpErrorCheck = $true; Headers = $Headers.Clone() }
    if ($Token) { $request.Headers['Authorization'] = "Bearer $Token" }
    if ($null -ne $Body) { $request.Body = ($Body | ConvertTo-Json -Compress); $request.ContentType = 'application/json' }
    $response = Invoke-WebRequest @request
    # PowerShell returns non-text media types such as application/problem+json as bytes
    $text = if ($response.Content -is [byte[]]) { [Text.Encoding]::UTF8.GetString($response.Content) } else { $response.Content }
    $json = $null
    if ($text -and "$($response.Headers['Content-Type'])" -match 'json') { $json = $text | ConvertFrom-Json }
    [pscustomobject]@{ Status = [int]$response.StatusCode; Json = $json; Headers = $response.Headers; Content = $text }
}

function Check([string]$Name, [bool]$Passed, [string]$Detail = '') {
    $script:results.Add([pscustomobject]@{ Name = $Name; Passed = $Passed })
    $mark = if ($Passed) { 'PASS' } else { 'FAIL' }
    $color = if ($Passed) { 'Green' } else { 'Red' }
    Write-Host ("  [{0}] {1}{2}" -f $mark, $Name, $(if (-not $Passed -and $Detail) { " -- $Detail" } else { '' })) -ForegroundColor $color
}

# Reads the newest matching email from the Development-only mailbox API; returns its first link's query values
function Get-LastLinkQuery([string]$Email, [string]$Subject) {
    $mail = Invoke-Api GET "/dev/mailbox/latest?to=$([Uri]::EscapeDataString($Email))&subject=$Subject"
    if ($mail.Status -ne 200) { throw "No '$Subject' email for $Email in /dev/mailbox (status $($mail.Status))" }
    return $mail.Json.links[0].query
}

$id = [guid]::NewGuid().ToString('N').Substring(0, 12)
$email = "smoke-$id@example.test"
$userName = "smoke-$id"
$password = 'smoke test password'
$newPassword = 'a brand new passphrase'

Write-Host "Smoke testing $BaseUrl as $email`n"

try {
    $health = Invoke-Api GET '/health'
    Check 'Health is Healthy' ($health.Status -eq 200 -and $health.Content -eq 'Healthy') "status $($health.Status)"
}
catch {
    Write-Host "  Cannot reach $BaseUrl. Start it first: ./scripts/api-local.ps1" -ForegroundColor Red
    exit 1
}

$harness = Invoke-Api GET '/api-test-spark/config'
Check 'API Test Spark harness is available locally' ($harness.Status -eq 200 -and $harness.Content -match '/openapi/v1.json') "status $($harness.Status)"

$contract = Invoke-Api GET '/openapi/v1.json'
Check 'OpenAPI contract is served' ($contract.Status -eq 200 -and $null -ne $contract.Json.paths.'/api/v1/auth/login')
Check 'Dev mailbox is listed in the local contract' ($null -ne $contract.Json.paths.'/dev/mailbox/latest')

$register = Invoke-Api POST '/api/v1/auth/register' @{ email = $email; password = $password; userName = $userName; displayName = 'Smoke Test' }
Check 'Register returns 202' ($register.Status -eq 202) "status $($register.Status)"

$bad = Invoke-Api POST '/api/v1/auth/register' @{ email = 'not-an-email'; password = 'short'; userName = 'Bad Name'; displayName = '' }
Check 'Invalid registration returns 400 validation problem' ($bad.Status -eq 400 -and $null -ne $bad.Json.errors) "status $($bad.Status)"

$early = Invoke-Api POST '/api/v1/auth/login' @{ email = $email; password = $password }
Check 'Login before confirming returns 403' ($early.Status -eq 403 -and $early.Json.code -eq 'email_not_confirmed') "status $($early.Status)"

$confirmQuery = Get-LastLinkQuery $email 'Confirm'
$confirm = Invoke-Api POST '/api/v1/auth/confirm-email' @{ userId = $confirmQuery.userId; token = $confirmQuery.token }
Check 'Confirmation link confirms the email (204)' ($confirm.Status -eq 204) "status $($confirm.Status)"

$login = Invoke-Api POST '/api/v1/auth/login' @{ email = $email; password = $password }
Check 'Login returns tokens' ($login.Status -eq 200 -and $login.Json.accessToken -and $login.Json.refreshToken) "status $($login.Status)"
$tokens = $login.Json

$me = Invoke-Api GET '/api/v1/me' -Token $tokens.accessToken
Check '/me returns own account' ($me.Status -eq 200 -and $me.Json.email -eq $email -and $me.Json.userName -eq $userName) "status $($me.Status)"

$anon = Invoke-Api GET '/api/v1/me'
Check '/me without a token returns 401' ($anon.Status -eq 401) "status $($anon.Status)"

$wrong = Invoke-Api POST '/api/v1/auth/login' @{ email = $email; password = 'wrong password!!' }
Check 'Wrong password returns 401' ($wrong.Status -eq 401) "status $($wrong.Status)"

$refresh = Invoke-Api POST '/api/v1/auth/refresh' @{ refreshToken = $tokens.refreshToken }
Check 'Refresh rotates the token' ($refresh.Status -eq 200 -and $refresh.Json.refreshToken -ne $tokens.refreshToken) "status $($refresh.Status)"
$replay = Invoke-Api POST '/api/v1/auth/refresh' @{ refreshToken = $tokens.refreshToken }
Check 'Replaying a spent refresh token returns 401' ($replay.Status -eq 401) "status $($replay.Status)"
$afterReplay = Invoke-Api POST '/api/v1/auth/refresh' @{ refreshToken = $refresh.Json.refreshToken }
Check 'Replay revokes the whole session' ($afterReplay.Status -eq 401) "status $($afterReplay.Status)"

$session = (Invoke-Api POST '/api/v1/auth/login' @{ email = $email; password = $password }).Json
$logout = Invoke-Api POST '/api/v1/auth/logout' @{ refreshToken = $session.refreshToken }
$afterLogout = Invoke-Api POST '/api/v1/auth/refresh' @{ refreshToken = $session.refreshToken }
Check 'Logout ends the session' ($logout.Status -eq 204 -and $afterLogout.Status -eq 401) "logout $($logout.Status), refresh $($afterLogout.Status)"

$forgot = Invoke-Api POST '/api/v1/auth/forgot-password' @{ email = $email }
Check 'Forgot password returns 202' ($forgot.Status -eq 202) "status $($forgot.Status)"
$resetQuery = Get-LastLinkQuery $email 'Reset'
$reset = Invoke-Api POST '/api/v1/auth/reset-password' @{ email = $resetQuery.email; token = $resetQuery.token; newPassword = $newPassword }
Check 'Reset link sets a new password (204)' ($reset.Status -eq 204) "status $($reset.Status)"
$oldLogin = Invoke-Api POST '/api/v1/auth/login' @{ email = $email; password = $password }
$newLogin = Invoke-Api POST '/api/v1/auth/login' @{ email = $email; password = $newPassword }
Check 'Old password stops working, new one works' ($oldLogin.Status -eq 401 -and $newLogin.Status -eq 200) "old $($oldLogin.Status), new $($newLogin.Status)"

$token = $newLogin.Json.accessToken
$update = Invoke-Api PUT '/api/v1/me/profile' @{ displayName = 'Smoke Bakery'; firstName = 'Private'; bio = 'Fresh bread.'; websiteUrl = 'https://smoke.example' } -Token $token
Check 'Profile update returns the updated account' ($update.Status -eq 200 -and $update.Json.displayName -eq 'Smoke Bakery') "status $($update.Status)"
$unsafe = Invoke-Api PUT '/api/v1/me/profile' @{ displayName = 'x'; websiteUrl = 'javascript:alert(1)' } -Token $token
Check 'Unsafe website URL is rejected (400)' ($unsafe.Status -eq 400) "status $($unsafe.Status)"

$profile = Invoke-Api GET "/api/v1/profiles/$userName"
$profileFields = @($profile.Json.PSObject.Properties.Name) | Sort-Object
Check 'Public profile shows only public fields' (
    $profile.Status -eq 200 -and ($profileFields -join ',') -eq 'bio,displayName,joinedAt,userName,websiteUrl') "fields: $($profileFields -join ',')"

$directory = Invoke-Api GET '/api/v1/profiles?search=Smoke%20Bakery&pageSize=5'
Check 'Directory search finds the member' ($directory.Status -eq 200 -and ($directory.Json.items.userName -contains $userName)) "status $($directory.Status)"
$malformed = Invoke-Api GET '/api/v1/profiles?page=abc'
Check 'Malformed query returns 400, not 500' ($malformed.Status -eq 400) "status $($malformed.Status)"

$null = Invoke-Api PUT '/api/v1/me/profile' @{ displayName = 'Smoke Bakery'; profileVisibility = 'Private' } -Token $token
$hiddenProfile = Invoke-Api GET "/api/v1/profiles/$userName"
$ownView = Invoke-Api GET "/api/v1/profiles/$userName" -Token $token
Check 'Private profile is hidden from others but visible to its owner' ($hiddenProfile.Status -eq 404 -and $ownView.Status -eq 200) "anon $($hiddenProfile.Status), owner $($ownView.Status)"

$corsOk = Invoke-Api OPTIONS '/api/v1/auth/login' -Headers @{ Origin = 'http://localhost:5173'; 'Access-Control-Request-Method' = 'POST' }
$corsBad = Invoke-Api OPTIONS '/api/v1/auth/login' -Headers @{ Origin = 'https://evil.example'; 'Access-Control-Request-Method' = 'POST' }
Check 'CORS allows the local client and rejects others' (
    "$($corsOk.Headers['Access-Control-Allow-Origin'])" -eq 'http://localhost:5173' -and -not $corsBad.Headers['Access-Control-Allow-Origin'])

$failed = @($script:results | Where-Object { -not $_.Passed }).Count
Write-Host ''
if ($failed -eq 0) {
    Write-Host "All $($script:results.Count) checks passed." -ForegroundColor Green
    exit 0
}
Write-Host "$failed of $($script:results.Count) checks failed." -ForegroundColor Red
exit 1
