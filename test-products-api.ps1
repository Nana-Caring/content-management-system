# Test script to check if products API works
Write-Host "Testing NANA Caring Products API..." -ForegroundColor Green

$baseUrl = "https://nanacaring-backend.onrender.com"
$endpoints = @(
    "/api/admin/products",
    "/admin/products",
    "/api/products",
    "/products"
)

foreach ($endpoint in $endpoints) {
    Write-Host "`nTesting: $baseUrl$endpoint" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl$endpoint" -Method GET -ErrorAction Stop
        Write-Host "✅ Success! Status: $($response.StatusCode)" -ForegroundColor Green
        $content = $response.Content
        if ($content.Length -gt 500) {
            $preview = $content.Substring(0, 500) + "..."
        } else {
            $preview = $content
        }
        Write-Host "Response preview: $preview" -ForegroundColor Cyan
        break  # Exit loop if we find a working endpoint
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "❌ Failed with status: $statusCode ($($_.Exception.Message))" -ForegroundColor Red
    }
}

# Test if we need authentication
Write-Host "`n--- Testing with High Court credentials ---" -ForegroundColor Magenta

try {
    # First get auth token
    $loginBody = @{
        email = "highcourt@nanacaring.com"
        password = "highcourt2025"
    } | ConvertTo-Json

    Write-Host "Attempting login..." -ForegroundColor Yellow
    $loginResponse = Invoke-WebRequest -Uri "$baseUrl/api/auth/admin-login" -Method POST -Body $loginBody -ContentType "application/json" -ErrorAction Stop
    
    Write-Host "✅ Login successful!" -ForegroundColor Green
    $loginData = $loginResponse.Content | ConvertFrom-Json
    $token = $loginData.token
    
    # Now try products with auth
    Write-Host "Testing products with authentication..." -ForegroundColor Yellow
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $productsResponse = Invoke-WebRequest -Uri "$baseUrl/api/admin/products" -Method GET -Headers $headers -ErrorAction Stop
    Write-Host "✅ Products API successful! Status: $($productsResponse.StatusCode)" -ForegroundColor Green
    
    # Show the full response
    $productsData = $productsResponse.Content | ConvertFrom-Json
    Write-Host "Response structure: $($productsData | ConvertTo-Json -Depth 2)" -ForegroundColor Cyan
    
    $productsData = $productsResponse.Content | ConvertFrom-Json
    Write-Host "Products found: $($productsData.data.Count)" -ForegroundColor Green
    Write-Host "First product: $($productsData.data[0].name) - Price: $($productsData.data[0].price)" -ForegroundColor Cyan
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "❌ Authentication test failed: $statusCode ($($_.Exception.Message))" -ForegroundColor Red
}

Write-Host "`nAPI testing complete!" -ForegroundColor Green