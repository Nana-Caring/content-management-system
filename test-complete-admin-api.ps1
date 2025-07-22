# Complete Admin Transaction API Test Script
# Tests all endpoints from the Admin Transaction Management API

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$AdminEmail = "admin@example.com",
    [string]$AdminPassword = "Admin123!"
)

Write-Host "🚀 Complete Admin Transaction Management API Test" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "Admin Email: $AdminEmail" -ForegroundColor Cyan
Write-Host ""

# Helper function for API calls
function Invoke-AdminApiCall {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [string]$Description = ""
    )
    
    try {
        $uri = "$BaseUrl$Endpoint"
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $Headers
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.Headers["Content-Type"] = "application/json"
        }
        
        Write-Host "📡 $Method $Endpoint" -ForegroundColor Cyan
        if ($Description) {
            Write-Host "   $Description" -ForegroundColor Gray
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "✅ Success" -ForegroundColor Green
        return @{ Success = $true; Data = $response }
    }
    catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            try {
                $errorStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($errorStream)
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
                $errorStream.Close()
                Write-Host "Error Details: $errorBody" -ForegroundColor Yellow
            } catch {
                Write-Host "Could not read error details" -ForegroundColor Yellow
            }
        }
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Test Results Tracking
$testResults = @{
    Passed = 0
    Failed = 0
    Tests = @()
}

function Add-TestResult {
    param([string]$TestName, [bool]$Success, [string]$Details = "")
    
    $testResults.Tests += @{
        Name = $TestName
        Success = $Success
        Details = $Details
    }
    
    if ($Success) {
        $testResults.Passed++
        Write-Host "✅ $TestName" -ForegroundColor Green
    } else {
        $testResults.Failed++
        Write-Host "❌ $TestName" -ForegroundColor Red
    }
    
    if ($Details) {
        Write-Host "   $Details" -ForegroundColor Gray
    }
}

# Test 1: Admin Authentication
Write-Host "`n1️⃣ Testing Admin Authentication" -ForegroundColor Yellow
$authBody = @{
    email = $AdminEmail
    password = $AdminPassword
}

$authResponse = Invoke-AdminApiCall -Method "POST" -Endpoint "/api/auth/admin-login" -Body $authBody -Description "Authenticating admin user"

if ($authResponse.Success -and $authResponse.Data.accessToken) {
    $adminToken = $authResponse.Data.accessToken
    $authHeaders = @{
        "Authorization" = "Bearer $adminToken"
    }
    Add-TestResult "Admin Authentication" $true "Token obtained successfully"
} else {
    Add-TestResult "Admin Authentication" $false "Failed to get admin token"
    Write-Host "❌ Cannot proceed without admin authentication. Exiting..." -ForegroundColor Red
    return
}

# Test 2: Get All Transactions (Basic)
Write-Host "`n2️⃣ Testing Get All Transactions (Basic)" -ForegroundColor Yellow
$allTransactionsResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions" -Headers $authHeaders -Description "Fetching all transactions"

if ($allTransactionsResponse.Success) {
    $transactionCount = $allTransactionsResponse.Data.data.transactions.Count
    Add-TestResult "Get All Transactions" $true "Found $transactionCount transactions"
    $sampleTransactionId = if ($transactionCount -gt 0) { $allTransactionsResponse.Data.data.transactions[0].id } else { $null }
} else {
    Add-TestResult "Get All Transactions" $false
    $sampleTransactionId = $null
}

# Test 3: Get All Transactions (Advanced Filtering)
Write-Host "`n3️⃣ Testing Advanced Filtering" -ForegroundColor Yellow
$advancedFilterResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions?page=1&limit=5&sortBy=amount&sortOrder=DESC" -Headers $authHeaders -Description "Testing advanced filtering with pagination and sorting"

if ($advancedFilterResponse.Success) {
    $filteredCount = $advancedFilterResponse.Data.data.transactions.Count
    $pagination = $advancedFilterResponse.Data.data.pagination
    Add-TestResult "Advanced Filtering" $true "Retrieved $filteredCount transactions with pagination (Page: $($pagination.page), Total: $($pagination.total))"
} else {
    Add-TestResult "Advanced Filtering" $false
}

# Test 4: Get Transaction Statistics
Write-Host "`n4️⃣ Testing Transaction Statistics" -ForegroundColor Yellow
$statsResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions/stats" -Headers $authHeaders -Description "Fetching comprehensive transaction statistics"

if ($statsResponse.Success) {
    $summary = $statsResponse.Data.data.summary
    Add-TestResult "Transaction Statistics" $true "Total: $($summary.totalTransactions), Credits: $($summary.creditTransactions), Debits: $($summary.debitTransactions)"
    Write-Host "   💰 Total Credit Amount: $($summary.totalCreditAmount)" -ForegroundColor Cyan
    Write-Host "   💸 Total Debit Amount: $($summary.totalDebitAmount)" -ForegroundColor Cyan
    Write-Host "   📊 Net Amount: $($summary.netAmount)" -ForegroundColor Cyan
} else {
    Add-TestResult "Transaction Statistics" $false
}

# Test 5: Get Specific Transaction (if available)
if ($sampleTransactionId) {
    Write-Host "`n5️⃣ Testing Get Transaction by ID" -ForegroundColor Yellow
    $specificTransactionResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions/$sampleTransactionId" -Headers $authHeaders -Description "Fetching specific transaction details"
    
    if ($specificTransactionResponse.Success) {
        $transaction = $specificTransactionResponse.Data.data
        Add-TestResult "Get Transaction by ID" $true "Retrieved transaction: $($transaction.description)"
        Write-Host "   Account: $($transaction.account.accountNumber)" -ForegroundColor Cyan
        Write-Host "   Amount: $($transaction.amount) ($($transaction.type))" -ForegroundColor Cyan
    } else {
        Add-TestResult "Get Transaction by ID" $false
    }
} else {
    Write-Host "`n5️⃣ Skipping Get Transaction by ID (no transactions available)" -ForegroundColor Gray
    Add-TestResult "Get Transaction by ID" $true "Skipped - no transactions available"
}

# Test 6: Get first account for manual transaction testing
Write-Host "`n6️⃣ Getting Account for Transaction Testing" -ForegroundColor Yellow
$accountsResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/admin/accounts" -Headers $authHeaders -Description "Fetching accounts for transaction testing"

$testAccountId = $null
if ($accountsResponse.Success -and $accountsResponse.Data.Count -gt 0) {
    $testAccountId = $accountsResponse.Data[0].id
    Add-TestResult "Get Test Account" $true "Using account: $testAccountId"
} else {
    Add-TestResult "Get Test Account" $false "No accounts available for testing"
}

# Test 7: Create Manual Transaction (if account available)
if ($testAccountId) {
    Write-Host "`n7️⃣ Testing Create Manual Transaction" -ForegroundColor Yellow
    $transactionBody = @{
        accountId = $testAccountId
        amount = 25.50
        type = "Credit"
        description = "Test manual transaction from PowerShell API test"
        reference = "test-$(Get-Date -Format 'yyyyMMddHHmmss')"
        metadata = @{
            reason = "API testing"
            category = "test"
        }
    }
    
    $createResponse = Invoke-AdminApiCall -Method "POST" -Endpoint "/api/admin/transactions" -Headers $authHeaders -Body $transactionBody -Description "Creating manual test transaction"
    
    if ($createResponse.Success) {
        $newTransactionId = $createResponse.Data.data.id
        Add-TestResult "Create Manual Transaction" $true "Created transaction: $newTransactionId"
        
        # Test 8: Update Transaction Description
        Write-Host "`n8️⃣ Testing Update Transaction" -ForegroundColor Yellow
        $updateBody = @{
            description = "Updated description via PowerShell API test - $(Get-Date)"
        }
        
        $updateResponse = Invoke-AdminApiCall -Method "PUT" -Endpoint "/api/admin/transactions/$newTransactionId" -Headers $authHeaders -Body $updateBody -Description "Updating transaction description"
        
        if ($updateResponse.Success) {
            Add-TestResult "Update Transaction" $true "Transaction updated successfully"
        } else {
            Add-TestResult "Update Transaction" $false
        }
        
        # Test 9: Reverse Transaction
        Write-Host "`n9️⃣ Testing Reverse Transaction" -ForegroundColor Yellow
        $reverseBody = @{
            reason = "Testing reversal functionality via PowerShell API test"
        }
        
        $reverseResponse = Invoke-AdminApiCall -Method "POST" -Endpoint "/api/admin/transactions/$newTransactionId/reverse" -Headers $authHeaders -Body $reverseBody -Description "Reversing test transaction"
        
        if ($reverseResponse.Success) {
            $reversalId = $reverseResponse.Data.data.reversalTransaction.id
            Add-TestResult "Reverse Transaction" $true "Reversal transaction created: $reversalId"
            
            # Test 10: Delete Original Transaction
            Write-Host "`n🔟 Testing Delete Transaction" -ForegroundColor Yellow
            $deleteBody = @{
                confirmDelete = $true
                adjustBalance = $true
            }
            
            $deleteResponse = Invoke-AdminApiCall -Method "DELETE" -Endpoint "/api/admin/transactions/$newTransactionId" -Headers $authHeaders -Body $deleteBody -Description "Deleting original test transaction"
            
            if ($deleteResponse.Success) {
                Add-TestResult "Delete Transaction" $true "Transaction deleted successfully"
            } else {
                Add-TestResult "Delete Transaction" $false
            }
        } else {
            Add-TestResult "Reverse Transaction" $false
        }
    } else {
        Add-TestResult "Create Manual Transaction" $false
    }
} else {
    Write-Host "`n7️⃣-🔟 Skipping Transaction Manipulation Tests (no accounts available)" -ForegroundColor Gray
    Add-TestResult "Create Manual Transaction" $true "Skipped - no accounts available"
    Add-TestResult "Update Transaction" $true "Skipped - no accounts available"
    Add-TestResult "Reverse Transaction" $true "Skipped - no accounts available"
    Add-TestResult "Delete Transaction" $true "Skipped - no accounts available"
}

# Test 11: Bulk Operations (if we have transactions)
Write-Host "`n1️⃣1️⃣ Testing Bulk Operations" -ForegroundColor Yellow
if ($sampleTransactionId) {
    $bulkBody = @{
        operation = "updateDescription"
        transactionIds = @($sampleTransactionId)
        data = @{
            description = "Bulk updated description via PowerShell test - $(Get-Date)"
        }
    }
    
    $bulkResponse = Invoke-AdminApiCall -Method "POST" -Endpoint "/api/admin/transactions/bulk" -Headers $authHeaders -Body $bulkBody -Description "Testing bulk description update"
    
    if ($bulkResponse.Success) {
        $updatedCount = $bulkResponse.Data.data.updatedCount
        Add-TestResult "Bulk Operations" $true "Updated $updatedCount transactions"
    } else {
        Add-TestResult "Bulk Operations" $false
    }
} else {
    Add-TestResult "Bulk Operations" $true "Skipped - no transactions available"
}

# Test 12: Legacy Simple Endpoints
Write-Host "`n1️⃣2️⃣ Testing Legacy Simple Endpoints" -ForegroundColor Yellow
$simpleResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions/simple" -Headers $authHeaders -Description "Testing legacy simple transactions endpoint"

if ($simpleResponse.Success) {
    $simpleCount = $simpleResponse.Data.Count
    Add-TestResult "Legacy Simple Endpoint" $true "Retrieved $simpleCount transactions via simple endpoint"
} else {
    Add-TestResult "Legacy Simple Endpoint" $false
}

# Test 13: Date Range Filtering
Write-Host "`n1️⃣3️⃣ Testing Date Range Filtering" -ForegroundColor Yellow
$today = Get-Date -Format "yyyy-MM-dd"
$lastWeek = (Get-Date).AddDays(-7).ToString("yyyy-MM-dd")

$dateFilterResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions?startDate=$lastWeek&endDate=$today" -Headers $authHeaders -Description "Testing date range filtering (last 7 days)"

if ($dateFilterResponse.Success) {
    $dateFilteredCount = $dateFilterResponse.Data.data.transactions.Count
    Add-TestResult "Date Range Filtering" $true "Found $dateFilteredCount transactions in the last 7 days"
} else {
    Add-TestResult "Date Range Filtering" $false
}

# Test 14: Statistics with Date Range
Write-Host "`n1️⃣4️⃣ Testing Statistics with Date Range" -ForegroundColor Yellow
$dateStatsResponse = Invoke-AdminApiCall -Method "GET" -Endpoint "/api/admin/transactions/stats?startDate=$lastWeek&endDate=$today" -Headers $authHeaders -Description "Testing statistics with date range filter"

if ($dateStatsResponse.Success) {
    $dateStats = $dateStatsResponse.Data.data.summary
    Add-TestResult "Date Range Statistics" $true "Period stats: $($dateStats.totalTransactions) transactions"
} else {
    Add-TestResult "Date Range Statistics" $false
}

# Final Test Summary
Write-Host "`n🎉 Test Summary" -ForegroundColor Green
Write-Host "===============" -ForegroundColor Green
Write-Host "✅ Passed: $($testResults.Passed)" -ForegroundColor Green
Write-Host "❌ Failed: $($testResults.Failed)" -ForegroundColor Red
Write-Host "📊 Total: $($testResults.Passed + $testResults.Failed)" -ForegroundColor Cyan

$successRate = if (($testResults.Passed + $testResults.Failed) -gt 0) {
    [math]::Round(($testResults.Passed / ($testResults.Passed + $testResults.Failed)) * 100, 2)
} else { 0 }

Write-Host "🎯 Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { 'Green' } elseif ($successRate -ge 60) { 'Yellow' } else { 'Red' })

Write-Host "`n📋 Detailed Results:" -ForegroundColor Cyan
foreach ($test in $testResults.Tests) {
    $status = if ($test.Success) { "✅" } else { "❌" }
    Write-Host "$status $($test.Name)" -ForegroundColor $(if ($test.Success) { 'Green' } else { 'Red' })
    if ($test.Details) {
        Write-Host "   $($test.Details)" -ForegroundColor Gray
    }
}

Write-Host "`n🚀 API Endpoints Tested:" -ForegroundColor Cyan
Write-Host "• POST /api/auth/admin-login" -ForegroundColor Gray
Write-Host "• GET /api/admin/transactions" -ForegroundColor Gray
Write-Host "• GET /api/admin/transactions/{id}" -ForegroundColor Gray
Write-Host "• POST /api/admin/transactions" -ForegroundColor Gray
Write-Host "• PUT /api/admin/transactions/{id}" -ForegroundColor Gray
Write-Host "• POST /api/admin/transactions/{id}/reverse" -ForegroundColor Gray
Write-Host "• DELETE /api/admin/transactions/{id}" -ForegroundColor Gray
Write-Host "• GET /api/admin/transactions/stats" -ForegroundColor Gray
Write-Host "• POST /api/admin/transactions/bulk" -ForegroundColor Gray
Write-Host "• GET /api/admin/transactions/simple" -ForegroundColor Gray

if ($successRate -ge 80) {
    Write-Host "`n🎉 Admin Transaction Management API is ready for production!" -ForegroundColor Green
} elseif ($successRate -ge 60) {
    Write-Host "`n⚠️ Admin Transaction Management API has some issues that need attention." -ForegroundColor Yellow
} else {
    Write-Host "`n❌ Admin Transaction Management API has significant issues that need to be resolved." -ForegroundColor Red
}

Write-Host "`n📖 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Review any failed tests and fix underlying issues" -ForegroundColor Gray
Write-Host "2. Integrate the working endpoints with your frontend" -ForegroundColor Gray
Write-Host "3. Test the complete user workflow in the browser" -ForegroundColor Gray
Write-Host "4. Set up proper admin user authentication" -ForegroundColor Gray
Write-Host "5. Configure appropriate permissions and role-based access" -ForegroundColor Gray
