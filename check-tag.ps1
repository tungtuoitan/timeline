# Quick check for tag 137
$parentTagId = 137

Write-Host "Checking tag $parentTagId..." -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/api/tags/$parentTagId" -Method Get
    Write-Host "Tag exists: $($response.name)" -ForegroundColor Green
    Write-Host "User: $($response.userId)" -ForegroundColor Green
    Write-Host "Deleted: $($response.deletedAt)" -ForegroundColor Green
}
catch {
    Write-Host "Tag NOT found: $($_.Exception.Message)" -ForegroundColor Red
}
