# Admin API Test Script
# PowerShell script to test all admin endpoints

Write-Host "=== CMS Admin API Test Script ===" -ForegroundColor Green
Write-Host ""

$baseUrl = "http://localhost:5000"
$adminEmail = "admin@example.com"
$adminPassword = "password123"

# Function to make API calls with error handling
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Url,
        [hashtable]$Headers = @{},
        [object]$Body = $null
    )
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
        }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response }
    }
    catch {
        return @{ Success = $false; Error = $_.Exception.Message; Response = $_.Exception.Response }
    }
}

# Test 1: Admin Login
Write-Host "1. Testing Admin Login..." -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
}

$loginResult = Invoke-ApiCall -Method "POST" -Url "$baseUrl/api/auth/admin-login" -Body $loginBody

if ($loginResult.Success) {
    $token = $loginResult.Data.accessToken
    Write-Host "✓ Login successful" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0, 20))..." -ForegroundColor Cyan
} else {
    Write-Host "✗ Login failed: $($loginResult.Error)" -ForegroundColor Red
    Write-Host "Continuing with dummy token for other tests..."
    $token = "dummy-token-for-testing"
}

Write-Host ""

# Common headers for authenticated requests
$authHeaders = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 2: Get System Statistics
Write-Host "2. Testing Get System Statistics..." -ForegroundColor Yellow
$statsResult = Invoke-ApiCall -Method "GET" -Url "$baseUrl/admin/stats" -Headers $authHeaders

if ($statsResult.Success) {
    Write-Host "✓ Stats retrieved successfully" -ForegroundColor Green
    Write-Host "Users: $($statsResult.Data.users)" -ForegroundColor Cyan
    Write-Host "Accounts: $($statsResult.Data.accounts)" -ForegroundColor Cyan
    Write-Host "Transactions: $($statsResult.Data.transactions)" -ForegroundColor Cyan
} else {
    Write-Host "✗ Stats failed: $($statsResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Get All Users
Write-Host "3. Testing Get All Users..." -ForegroundColor Yellow
$usersResult = Invoke-ApiCall -Method "GET" -Url "$baseUrl/admin/users" -Headers $authHeaders

if ($usersResult.Success) {
    $userCount = $usersResult.Data.Count
    Write-Host "✓ Users retrieved successfully ($userCount users)" -ForegroundColor Green
    if ($userCount -gt 0) {
        $firstUser = $usersResult.Data[0]
        Write-Host "First user: $($firstUser.firstName) $($firstUser.surname) ($($firstUser.email))" -ForegroundColor Cyan
    }
} else {
    Write-Host "✗ Users failed: $($usersResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Get All Accounts
Write-Host "4. Testing Get All Accounts..." -ForegroundColor Yellow
$accountsResult = Invoke-ApiCall -Method "GET" -Url "$baseUrl/admin/accounts" -Headers $authHeaders

if ($accountsResult.Success) {
    $accountCount = $accountsResult.Data.Count
    Write-Host "✓ Accounts retrieved successfully ($accountCount accounts)" -ForegroundColor Green
    if ($accountCount -gt 0) {
        $firstAccount = $accountsResult.Data[0]
        Write-Host "First account: $($firstAccount.accountNumber) - $($firstAccount.accountType)" -ForegroundColor Cyan
    }
} else {
    Write-Host "✗ Accounts failed: $($accountsResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Get All Transactions
Write-Host "5. Testing Get All Transactions..." -ForegroundColor Yellow
$transactionsResult = Invoke-ApiCall -Method "GET" -Url "$baseUrl/admin/transactions" -Headers $authHeaders

if ($transactionsResult.Success) {
    $transactionCount = $transactionsResult.Data.Count
    Write-Host "✓ Transactions retrieved successfully ($transactionCount transactions)" -ForegroundColor Green
    if ($transactionCount -gt 0) {
        $firstTransaction = $transactionsResult.Data[0]
        Write-Host "First transaction: $($firstTransaction.amount) - $($firstTransaction.description)" -ForegroundColor Cyan
    }
} else {
    Write-Host "✗ Transactions failed: $($transactionsResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Test 6: Get Blocked Users
Write-Host "6. Testing Get Blocked Users..." -ForegroundColor Yellow
$blockedUsersResult = Invoke-ApiCall -Method "GET" -Url "$baseUrl/admin/blocked-users" -Headers $authHeaders

if ($blockedUsersResult.Success) {
    $blockedCount = $blockedUsersResult.Data.Count
    Write-Host "✓ Blocked users retrieved successfully ($blockedCount blocked users)" -ForegroundColor Green
    if ($blockedCount -gt 0) {
        $firstBlocked = $blockedUsersResult.Data[0]
        Write-Host "First blocked user: $($firstBlocked.firstName) $($firstBlocked.surname)" -ForegroundColor Cyan
    }
} else {
    Write-Host "✗ Blocked users failed: $($blockedUsersResult.Error)" -ForegroundColor Red
}

Write-Host ""

# Test 7: Block User (if users exist)
if ($usersResult.Success -and $usersResult.Data.Count -gt 0) {
    $testUser = $usersResult.Data | Where-Object { $_.role -ne "admin" } | Select-Object -First 1
    
    if ($testUser) {
        Write-Host "7. Testing Block User (ID: $($testUser.id))..." -ForegroundColor Yellow
        $blockBody = @{
            reason = "Test blocking via API"
        }
        
        $blockResult = Invoke-ApiCall -Method "PUT" -Url "$baseUrl/admin/users/$($testUser.id)/block" -Headers $authHeaders -Body $blockBody
        
        if ($blockResult.Success) {
            Write-Host "✓ User blocked successfully" -ForegroundColor Green
            Write-Host "Blocked user: $($blockResult.Data.user.email)" -ForegroundColor Cyan
            
            # Test 8: Unblock User
            Write-Host ""
            Write-Host "8. Testing Unblock User..." -ForegroundColor Yellow
            $unblockResult = Invoke-ApiCall -Method "PUT" -Url "$baseUrl/admin/users/$($testUser.id)/unblock" -Headers $authHeaders
            
            if ($unblockResult.Success) {
                Write-Host "✓ User unblocked successfully" -ForegroundColor Green
            } else {
                Write-Host "✗ Unblock failed: $($unblockResult.Error)" -ForegroundColor Red
            }
        } else {
            Write-Host "✗ Block user failed: $($blockResult.Error)" -ForegroundColor Red
        }
    } else {
        Write-Host "7-8. Skipping Block/Unblock tests (no non-admin users found)" -ForegroundColor Gray
    }
} else {
    Write-Host "7-8. Skipping Block/Unblock tests (no users available)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "=== Test Completed ===" -ForegroundColor Green
Write-Host ""
Write-Host "Usage Examples:" -ForegroundColor Yellow
Write-Host ""
Write-Host "# Get all users" -ForegroundColor Cyan
Write-Host 'Invoke-RestMethod -Uri "http://localhost:5000/admin/users" -Headers @{"Authorization"="Bearer YOUR_TOKEN"}'
Write-Host ""
Write-Host "# Block a user" -ForegroundColor Cyan
Write-Host '$body = @{reason="Violation of terms"} | ConvertTo-Json'
Write-Host 'Invoke-RestMethod -Uri "http://localhost:5000/admin/users/1/block" -Method Put -Headers @{"Authorization"="Bearer YOUR_TOKEN";"Content-Type"="application/json"} -Body $body'
Write-Host ""
Write-Host "# Get system stats" -ForegroundColor Cyan
Write-Host 'Invoke-RestMethod -Uri "http://localhost:5000/admin/stats" -Headers @{"Authorization"="Bearer YOUR_TOKEN"}'
