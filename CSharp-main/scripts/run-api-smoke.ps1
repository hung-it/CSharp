param(
    [string]$BaseUrl = 'http://localhost:5140/api/v1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$runId = Get-Date -Format 'yyyyMMddHHmmss'
$testUserRef = "SHOP_MANAGER_SMOKE_$runId"
$testPoiCode = "POI_SMOKE_$runId"
$testAnonymousRef = "ANON_SMOKE_$runId"

$normalizedBaseUrl = $BaseUrl.TrimEnd('/')
$apiBaseUrl = if ($normalizedBaseUrl -match '/api/v\d+$') {
    $normalizedBaseUrl
}
else {
    "$normalizedBaseUrl/api/v1"
}

$serviceRootUrl = if ($normalizedBaseUrl -match '/api/v\d+$') {
    ($normalizedBaseUrl -replace '/api/v\d+$', '')
}
else {
    $normalizedBaseUrl
}

$testUserId = $null
$poiManagerUserId = $null
$testPoiId = $null
$testSubscriptionId = $null

function Step([string]$message) {
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Assert-True([bool]$condition, [string]$message) {
    if (-not $condition) {
        throw "Assertion failed: $message"
    }
}

function New-QueryString([hashtable]$Query) {
    if ($null -eq $Query -or $Query.Count -eq 0) {
        return ''
    }

    $pairs = @(foreach ($key in $Query.Keys) {
        $value = $Query[$key]
        if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) {
            continue
        }

        "{0}={1}" -f [Uri]::EscapeDataString([string]$key), [Uri]::EscapeDataString([string]$value)
    })

    if ($pairs.Count -eq 0) {
        return ''
    }

    return "?{0}" -f ($pairs -join '&')
}

function Invoke-Api {
    param(
        [ValidateSet('GET', 'POST', 'PATCH', 'PUT', 'DELETE')]
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [hashtable]$Query = @{},
        [hashtable]$Headers = @{}
    )

    $uri = "$apiBaseUrl$Path$(New-QueryString -Query $Query)"

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 8
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers -ContentType 'application/json' -Body $json
    }

    return Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers
}

try {
    Step "Health check"
    $health = $null
    $healthUris = @(
        "$apiBaseUrl/health",
        "$serviceRootUrl/health"
    )

    foreach ($healthUri in $healthUris) {
        try {
            $health = Invoke-RestMethod -Method 'GET' -Uri $healthUri
            break
        }
        catch {
            continue
        }
    }

    if ($null -eq $health) {
        throw "Health check failed for all known endpoints: $($healthUris -join ', ')"
    }

    Assert-True ($health.status -eq 'ok') 'Health endpoint must return status=ok.'

    Step "Seed demo users"
    $demoSeed = Invoke-Api -Method 'POST' -Path '/users/demo-seed'
    Assert-True ($null -ne $demoSeed.createdCount) 'demo-seed response must include createdCount.'
    Assert-True ($demoSeed.users.Count -ge 1) 'demo-seed response must include demo users.'

    Step "Resolve POI manager identity"
    $shopManagerUsers = Invoke-Api -Method 'GET' -Path '/users' -Query @{ search = 'SHOP_MANAGER_01'; limit = 10 }
    $poiManager = $shopManagerUsers | Where-Object { $_.externalRef -eq 'SHOP_MANAGER_01' } | Select-Object -First 1
    if ($null -eq $poiManager) {
        $adminUsers = Invoke-Api -Method 'GET' -Path '/users' -Query @{ search = 'ADMIN_USER'; limit = 10 }
        $poiManager = $adminUsers | Where-Object { $_.externalRef -eq 'ADMIN_USER' } | Select-Object -First 1
    }

    Assert-True ($null -ne $poiManager) 'Expected a seeded SHOP_MANAGER_01 or ADMIN_USER for POI management tests.'
    $poiManagerUserId = [string]$poiManager.id

    Step "Resolve user for subscription flow"
    $resolvedUser = Invoke-Api -Method 'POST' -Path '/users/resolve' -Body @{
        externalRef = $testUserRef
        preferredLanguage = 'vi'
    }

    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$resolvedUser.id)) 'Resolved user must include id.'
    Assert-True ($resolvedUser.externalRef -eq $testUserRef) 'Resolved user externalRef mismatch.'
    $testUserId = [string]$resolvedUser.id

    Step "Get user details by id"
    $userDetails = Invoke-Api -Method 'GET' -Path "/users/$testUserId"
    Assert-True ($userDetails.id -eq $testUserId) 'GET /users/{id} returned unexpected user id.'

    Step "Search users by externalRef"
    $users = Invoke-Api -Method 'GET' -Path '/users' -Query @{ search = $testUserRef; limit = 10 }
    $found = $users | Where-Object { $_.externalRef -eq $testUserRef }
    Assert-True ($null -ne $found) 'GET /users with search must include the resolved user.'

    Step "Create POI"
    $createdPoi = Invoke-Api -Method 'POST' -Path '/pois' -Body @{
        code = $testPoiCode
        name = "POI Smoke Test $runId"
        latitude = 16.047079
        longitude = 108.206230
        triggerRadiusMeters = 60
        district = 'Hai Chau'
        priority = 5
        imageUrl = "https://example.com/$testPoiCode.jpg"
        mapLink = "https://maps.google.com/?q=16.047079,108.206230"
        description = 'Smoke test POI for web-backend integration.'
    } -Headers @{ 'X-User-Id' = $poiManagerUserId }

    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$createdPoi.id)) 'Created POI must include id.'
    Assert-True ($createdPoi.code -eq $testPoiCode) 'Created POI code mismatch.'
    $testPoiId = [string]$createdPoi.id

    Step "Update POI"
    $updatedPoi = Invoke-Api -Method 'PATCH' -Path "/pois/$testPoiId" -Body @{
        name = "POI Smoke Updated $runId"
        priority = 7
        triggerRadiusMeters = 80
        description = 'Updated from smoke test.'
    } -Headers @{ 'X-User-Id' = $poiManagerUserId }

    Assert-True ($updatedPoi.priority -eq 7) 'Updated POI priority must be 7.'
    Assert-True ($updatedPoi.triggerRadiusMeters -eq 80) 'Updated POI trigger radius must be 80.'

    Step "Assign and read POI audio"
    $null = Invoke-Api -Method 'POST' -Path "/pois/$testPoiId/audios" -Body @{
        languageCode = 'vi'
        filePath = "/media/audio/smoke-$runId.mp3"
        durationSeconds = 45
        isTextToSpeech = $false
    } -Headers @{ 'X-User-Id' = $poiManagerUserId }

    $audios = Invoke-Api -Method 'GET' -Path "/pois/$testPoiId/audios"
    Assert-True ($audios.Count -ge 1) 'POI audios must contain at least one item after assign.'

    Step "Create active premium subscription"
    $subscription = Invoke-Api -Method 'POST' -Path '/subscriptions' -Body @{
        userId = $testUserId
        planTier = 'PremiumSegmented'
        amountUsd = 9.99
        isActive = $true
    }

    Assert-True (-not [string]::IsNullOrWhiteSpace([string]$subscription.id)) 'Created subscription must include id.'
    Assert-True ($subscription.planTier -eq 'PremiumSegmented') 'Subscription planTier mismatch.'
    Assert-True ($subscription.isActive -eq $true) 'Subscription must be active.'
    $testSubscriptionId = [string]$subscription.id

    Step "Check active subscription and premium access"
    $activeSubscription = Invoke-Api -Method 'GET' -Path "/subscriptions/users/$testUserId/active"
    Assert-True ($activeSubscription.hasActive -eq $true) 'User must have active subscription.'

    $segments = Invoke-Api -Method 'GET' -Path '/feature-segments'
    $premiumSegment = $segments | Where-Object { $_.code -like 'premium.*' } | Select-Object -First 1
    if ($null -ne $premiumSegment) {
        $access = Invoke-Api -Method 'GET' -Path "/subscriptions/users/$testUserId/access/$($premiumSegment.code)"
        Assert-True ($access.hasAccess -eq $true) 'Premium user must have access to premium segment.'
    }

    Step "Analytics usage endpoint"
    $usage = Invoke-Api -Method 'GET' -Path '/analytics/usage' -Query @{ days = 7 }
    Assert-True ($usage.days -eq 7) 'Analytics usage days must echo the request.'
    Assert-True ($null -ne $usage.totalListens) 'Analytics usage must include totalListens.'
    Assert-True ($null -ne $usage.activeCells) 'Analytics usage must include activeCells.'

    Step "Anonymous route logging"
    $routePoint = Invoke-Api -Method 'POST' -Path "/routes/anonymous/$testAnonymousRef/points" -Body @{
        latitude = 16.047079
        longitude = 108.206230
        source = 'gps'
    }
    Assert-True ($null -ne $routePoint.id) 'Logged route point must include id.'

    $routeList = Invoke-Api -Method 'GET' -Path "/routes/anonymous/$testAnonymousRef"
    Assert-True ($routeList.Count -ge 1) 'Anonymous route list must include logged point.'

    Step "Negative test for invalid subscription plan"
    $invalidPlanRejected = $false
    try {
        $null = Invoke-Api -Method 'POST' -Path '/subscriptions' -Body @{
            userId = $testUserId
            planTier = 'InvalidPlanTier'
            amountUsd = 1.0
            isActive = $false
        }
    }
    catch {
        $response = $_.Exception.Response
        if ($null -ne $response -and [int]$response.StatusCode -eq 400) {
            $invalidPlanRejected = $true
        }
    }

    Assert-True $invalidPlanRejected 'Invalid plan tier must be rejected with HTTP 400.'

    Write-Host ''
    Write-Host 'Smoke test PASSED: web-backend API flow is healthy for Shop Manager/Admin critical paths.' -ForegroundColor Green
}
finally {
    if ($null -ne $testSubscriptionId) {
        try {
            Step "Cleanup: delete test subscription"
            $null = Invoke-Api -Method 'DELETE' -Path "/subscriptions/$testSubscriptionId"
        }
        catch {
            Write-Warning "Cleanup failed (subscription): $($_.Exception.Message)"
        }
    }

    if ($null -ne $testPoiId) {
        try {
            Step "Cleanup: delete test POI"
            $null = Invoke-Api -Method 'DELETE' -Path "/pois/$testPoiId" -Headers @{ 'X-User-Id' = $poiManagerUserId }
        }
        catch {
            Write-Warning "Cleanup failed (poi): $($_.Exception.Message)"
        }
    }

    if ($null -ne $testUserId) {
        try {
            Step "Cleanup: delete test user"
            $null = Invoke-Api -Method 'DELETE' -Path "/users/$testUserId"
        }
        catch {
            Write-Warning "Cleanup failed (user): $($_.Exception.Message)"
        }
    }
}
