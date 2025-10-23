# Test Update Workspace Item API
# Date: 2025-10-22

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Update Workspace Item API" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5000/api/workspace"
$workspaceId = 1
$itemId = 1

# Test 1: Update Label Only
Write-Host "Test 1: Update Label Only" -ForegroundColor Yellow
$body = @{
    label = "Updated Label - Test"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$itemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Item ID: $($response.itemId)" -ForegroundColor White
    Write-Host "Label: $($response.label)" -ForegroundColor White
    Write-Host "Message: $($response.message)" -ForegroundColor White
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Update Color and Icon
Write-Host "Test 2: Update Color and Icon" -ForegroundColor Yellow
$body = @{
    color = "#FF5733"
    icon = "🎯"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$itemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Item ID: $($response.itemId)" -ForegroundColor White
    Write-Host "Color: $($response.color)" -ForegroundColor White
    Write-Host "Icon: $($response.icon)" -ForegroundColor White
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Update All Fields
Write-Host "Test 3: Update All Fields" -ForegroundColor Yellow
$body = @{
    label = "Complete Update Test"
    notes = "This is a comprehensive test of all fields"
    color = "#4CAF50"
    icon = "📌"
    sortOrder = 10
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$itemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Item ID: $($response.itemId)" -ForegroundColor White
    Write-Host "Label: $($response.label)" -ForegroundColor White
    Write-Host "Notes: $($response.notes)" -ForegroundColor White
    Write-Host "Color: $($response.color)" -ForegroundColor White
    Write-Host "Icon: $($response.icon)" -ForegroundColor White
    Write-Host "Sort Order: $($response.sortOrder)" -ForegroundColor White
    Write-Host ""
    Write-Host "Full Response:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 3 | Write-Host
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Invalid Color Format (Should Fail)
Write-Host "Test 4: Invalid Color Format (Expected to Fail)" -ForegroundColor Yellow
$body = @{
    color = "red"  # Invalid format
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$itemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "⚠️ Unexpected success - validation might be missing!" -ForegroundColor Red
} catch {
    Write-Host "✅ Correctly rejected invalid color format" -ForegroundColor Green
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}
Write-Host ""

# Test 5: Update Notes Only
Write-Host "Test 5: Update Notes Only" -ForegroundColor Yellow
$body = @{
    notes = "Adding some important notes about this item"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$itemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Notes: $($response.notes)" -ForegroundColor White
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 6: Update Sort Order
Write-Host "Test 6: Update Sort Order" -ForegroundColor Yellow
$body = @{
    sortOrder = 5
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$itemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "✅ Success!" -ForegroundColor Green
    Write-Host "Sort Order: $($response.sortOrder)" -ForegroundColor White
} catch {
    Write-Host "❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 7: Non-existent Item (Should Fail)
Write-Host "Test 7: Non-existent Item (Expected to Fail)" -ForegroundColor Yellow
$nonExistentItemId = 99999
$body = @{
    label = "This should fail"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/$workspaceId/items/$nonExistentItemId" `
        -Method Put `
        -Body $body `
        -ContentType "application/json"
    
    Write-Host "⚠️ Unexpected success - item shouldn't exist!" -ForegroundColor Red
} catch {
    Write-Host "✅ Correctly rejected non-existent item" -ForegroundColor Green
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Testing Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
