# PowerShell script to add note_id to notes INSERT statements
$filePath = "comprehensive_test_data.sql"
$content = Get-Content $filePath -Raw

# Find the notes section and add note_id
$noteId = 4  # Start from 4 since we already fixed 1-3

# Pattern to match note INSERT lines (starts with two spaces and parenthesis, followed by user_id number)
$pattern = '(\s+)\((\d+),\s+''([^'']+)'','
$replacement = {
    $indent = $matches[1]
    $userId = $matches[2] 
    $name = $matches[3]
    $script:noteId++
    return "${indent}($($script:noteId - 1), ${userId}, '${name}',"
}

# Replace all matches
$newContent = [regex]::Replace($content, $pattern, $replacement)

# Write back to file
$newContent | Set-Content $filePath -NoNewline

Write-Host "Fixed note IDs in $filePath"
Write-Host "Last note_id used: $($script:noteId - 1)"