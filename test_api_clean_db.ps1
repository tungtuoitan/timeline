# Test API with clean DATABASE_CURRENT schema
$baseUrl = "http://localhost:5000"

Write-Host "============================================"
Write-Host "Testing SuperApp API with Clean Database"
Write-Host "Database Schema: DATABASE_CURRENT (MVP 1.0)"
Write-Host "============================================"

# Test 1: Health Check
Write-Host "`n1. Testing Health Check..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/health" -Method GET -UseBasicParsing
    Write-Host "✅ Health Check: $($response.StatusCode) - $($response.Content)"
} catch {
    Write-Host "❌ Health Check Failed: $($_.Exception.Message)"
}

# Test 2: Get Notes (should be empty or minimal data)
Write-Host "`n2. Testing Get Notes..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/notes" -Method GET -UseBasicParsing
    Write-Host "✅ Get Notes: $($response.StatusCode)"
    Write-Host "Response: $($response.Content)"
} catch {
    Write-Host "❌ Get Notes Failed: $($_.Exception.Message)"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}

# Test 3: Get Standard Registry (should work with entity_types data)
Write-Host "`n3. Testing Standard Registry..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/standardregistry" -Method GET -UseBasicParsing
    Write-Host "✅ Standard Registry: $($response.StatusCode)"
    Write-Host "Response: $($response.Content)"
} catch {
    Write-Host "❌ Standard Registry Failed: $($_.Exception.Message)"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}

# Test 4: Try creating a note (might need auth)
Write-Host "`n4. Testing Create Note (POST)..."
$noteData = @{
    Name = "Test Note from Clean DB"
    Description = "Testing the clean DATABASE_CURRENT schema"
    Tags = "test"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/notes" -Method POST -Body $noteData -ContentType "application/json" -UseBasicParsing
    Write-Host "✅ Create Note: $($response.StatusCode)"
    Write-Host "Response: $($response.Content)"
} catch {
    Write-Host "⚠️ Create Note Expected Failure (likely needs auth): $($_.Exception.Message)"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}

# Test 5: Test Tags endpoint
Write-Host "`n5. Testing Tags..."
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/tags" -Method GET -UseBasicParsing
    Write-Host "✅ Get Tags: $($response.StatusCode)"
    Write-Host "Response: $($response.Content)"
} catch {
    Write-Host "❌ Get Tags Failed: $($_.Exception.Message)"
    Write-Host "Status Code: $($_.Exception.Response.StatusCode)"
}

Write-Host "`n============================================"
Write-Host "Test completed. Check results above."
Write-Host "============================================"