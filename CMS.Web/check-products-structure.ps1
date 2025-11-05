# Simple test to see the exact structure of /api/products
Write-Host "Getting /api/products response structure..." -ForegroundColor Green

try {
    $response = Invoke-WebRequest -Uri "https://nanacaring-backend.onrender.com/api/products" -Method GET -ErrorAction Stop
    Write-Host "✅ Success! Status: $($response.StatusCode)" -ForegroundColor Green
    
    $data = $response.Content | ConvertFrom-Json
    Write-Host "`nFull Response:" -ForegroundColor Yellow
    Write-Host ($data | ConvertTo-Json -Depth 4) -ForegroundColor Cyan
    
    if ($data.data -and $data.data.Count -gt 0) {
        Write-Host "`nFirst Product Details:" -ForegroundColor Yellow
        Write-Host ($data.data[0] | ConvertTo-Json -Depth 3) -ForegroundColor Cyan
        
        Write-Host "`nKey observations:" -ForegroundColor Green
        Write-Host "- Total products: $($data.data.Count)" -ForegroundColor White
        Write-Host "- Price type: $($data.data[0].price.GetType().Name) = '$($data.data[0].price)'" -ForegroundColor White
        Write-Host "- Has pagination: $($null -ne $data.pagination)" -ForegroundColor White
        Write-Host "- Success field: $($data.success)" -ForegroundColor White
    }
}
catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}