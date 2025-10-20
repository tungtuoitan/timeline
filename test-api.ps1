# Test the GetNotes API endpoint

Write-Host "Testing GetNotes endpoint..."

# Test 1: Get all notes without filtering
Write-Host "`n1. Testing without parameters:"
try {
    $response1 = Invoke-RestMethod -Uri "http://localhost:5000/api/Notes" -Method Get -ErrorAction Stop
    Write-Host "Success: $($response1.Count) notes returned"
    $response1 | Format-Table -AutoSize
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
}

# Test 2: Get notes with tag filtering (the previously failing case)
Write-Host "`n2. Testing with tag filtering (tagIds=1,2):"
try {
    $response2 = Invoke-RestMethod -Uri "http://localhost:5000/api/Notes?tagIds=1&tagIds=2" -Method Get -ErrorAction Stop
    Write-Host "Success: $($response2.Count) notes returned with tag filtering"
    $response2 | Format-Table -AutoSize
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
}

# Test 3: Get notes with search text
Write-Host "`n3. Testing with search text:"
try {
    $response3 = Invoke-RestMethod -Uri "http://localhost:5000/api/Notes?searchText=test" -Method Get -ErrorAction Stop
    Write-Host "Success: $($response3.Count) notes returned with search filter"
    $response3 | Format-Table -AutoSize
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host "Response: $($_.Exception.Response)"
}

Write-Host "`nAPI testing complete!"