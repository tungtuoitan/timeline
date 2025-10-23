# Diagnostic script for workspace item creation issue
# Checks if parent tag exists and tests the endpoint

$baseUrl = "http://localhost:5000"
$workspaceId = 1
$parentTagId = 137

Write-Host "=== Workspace Item Creation Diagnostics ===" -ForegroundColor Cyan
Write-Host ""

# Check if parent tag exists via Tags endpoint
Write-Host "1. Checking if tag $parentTagId exists..." -ForegroundColor Yellow
try {
    $tagResponse = Invoke-RestMethod -Uri "$baseUrl/api/tags/$parentTagId" -Method Get -ErrorAction Stop
    Write-Host "   ✓ Tag $parentTagId exists:" -ForegroundColor Green
    Write-Host "     Name: $($tagResponse.name)"
    Write-Host "     User ID: $($tagResponse.userId)"
    Write-Host "     Deleted: $($tagResponse.deletedAt -ne $null)"
} catch {
    Write-Host "   ✗ Tag $parentTagId not found or error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   This is likely causing the 500 error." -ForegroundColor Red
}

Write-Host ""

# Check workspace tree structure
Write-Host "2. Checking workspace $workspaceId tree..." -ForegroundColor Yellow
try {
    $treeResponse = Invoke-RestMethod -Uri "$baseUrl/api/workspace/$workspaceId/tree" -Method Get -ErrorAction Stop
    Write-Host "   ✓ Workspace tree retrieved successfully" -ForegroundColor Green
    Write-Host "     Total items: $($treeResponse.items.Count)"

    # Check if parent tag 137 is in the tree
    $parentInTree = $treeResponse.items | Where-Object { $_.itemType -eq "tag" -and $_.itemId -eq $parentTagId }
    if ($parentInTree) {
        Write-Host "     ✓ Tag $parentTagId is in workspace tree" -ForegroundColor Green
    } else {
        Write-Host "     ✗ Tag $parentTagId is NOT in workspace tree" -ForegroundColor Red
        Write-Host "     You can only add items under tags that are already in the workspace." -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ✗ Error retrieving workspace tree: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test creating a new tag
Write-Host "3. Testing workspace item creation..." -ForegroundColor Yellow
$testPayload = @{
    parentTagId = $parentTagId
    childType = "tag"
    tagName = "diagnostic-test-$(Get-Date -Format 'HHmmss')"
    color = "#FF5733"
    sortOrder = 0
} | ConvertTo-Json

Write-Host "   Payload:" -ForegroundColor Gray
Write-Host "   $testPayload" -ForegroundColor Gray

try {
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/workspace/$workspaceId/items" `
        -Method Post `
        -Body $testPayload `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Host "   ✓ Item created successfully!" -ForegroundColor Green
    Write-Host "     Item ID: $($createResponse.itemId)"
    Write-Host "     Child ID: $($createResponse.childId)"
} catch {
    Write-Host "   ✗ Error creating item:" -ForegroundColor Red
    $errorDetails = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($errorDetails) {
        Write-Host "     Error: $($errorDetails.error)" -ForegroundColor Red
    } else {
        Write-Host "     Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
        Write-Host "     Message: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Diagnostics Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Common Solutions:" -ForegroundColor Yellow
Write-Host "1. If tag $parentTagId doesn't exist, create it first or use a different parent"
Write-Host "2. If tag exists but not in workspace tree, add it to workspace first with parentTagId=null"
Write-Host "3. Check application logs for detailed error messages"
