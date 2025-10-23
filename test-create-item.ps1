# Test creating workspace item and capture detailed error
$payload = @{
    parentTagId = 137
    childType = "tag"
    tagName = "test1"
    color = "#1976D2"
    sortOrder = 0
} | ConvertTo-Json

Write-Host "Sending request..." -ForegroundColor Cyan
Write-Host $payload -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/workspace/1/items" `
        -Method Post `
        -Body $payload `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Host "SUCCESS!" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3)
}
catch {
    Write-Host "ERROR!" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red

    if ($_.ErrorDetails.Message) {
        Write-Host "Error Details:" -ForegroundColor Yellow
        Write-Host $_.ErrorDetails.Message
    }

    Write-Host ""
    Write-Host "Full Exception:" -ForegroundColor Yellow
    Write-Host $_.Exception.Message
}
