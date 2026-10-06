# Chạy BE local với secret từ tung-vault (#1502).
#
#   powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1            # dotnet run
#   powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1 -Watch     # dotnet watch
#   powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1 -Database SuperApp-test
#
# .env dev chỉ chứa tham chiếu vault:// — `secret run` giải mã và bơm vào env của
# process con, Program.cs nạp .env với NoClobber nên không ghi đè lại. Connection
# string trong .env không có Password: script gắn DB_PASSWORD vào trong process,
# không in ra đâu cả.
#
# DB đi qua SSH tunnel 127.0.0.1:14330 -> VPS:1433 (cổng 1433 public đã chặn);
# script tự mở tunnel nếu chưa có.
param(
    [switch]$Watch,
    [string]$Database,          # ghi đè Database= trong connection string (vd SuperApp-test)
    [switch]$Inner              # nội bộ: đang chạy bên trong `secret run`
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

if (-not $Inner) {
    $vault = if ($env:TUNG_VAULT) { $env:TUNG_VAULT } else { Join-Path $HOME 'source\tung-vault' }
    if (-not (Test-Path "$vault\secret.ps1")) { throw "Không thấy tung-vault ở $vault (đặt `$env:TUNG_VAULT)" }

    $tunnelUp = { (New-Object Net.Sockets.TcpClient).ConnectAsync('127.0.0.1', 14330).Wait(1000) }
    if (-not (& $tunnelUp)) {
        Write-Host 'Mở SSH tunnel 127.0.0.1:14330 -> vps-superapp:1433 ...'
        # ssh của Git (ssh Windows hay báo "Bad permissions" thư mục .ssh)
        $ssh = 'C:\Program Files\Git\usr\bin\ssh.exe'
        if (-not (Test-Path $ssh)) { $ssh = 'ssh' }
        Start-Process $ssh -ArgumentList '-N', '-o', 'ExitOnForwardFailure=yes', '-o', 'ServerAliveInterval=60',
            '-L', '14330:127.0.0.1:1433', 'vps-superapp' -WindowStyle Hidden
        for ($i = 0; $i -lt 15 -and -not (& $tunnelUp); $i++) { Start-Sleep 1 }
        if (-not (& $tunnelUp)) { throw 'Tunnel không lên — thử tay: ssh -N -L 14330:127.0.0.1:1433 vps-superapp' }
    }

    $childArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-Inner')
    if ($Watch) { $childArgs += '-Watch' }
    if ($Database) { $childArgs += @('-Database', $Database) }
    & "$vault\secret.ps1" run --env-file "$root\.env" -- (Join-Path $PSHOME 'powershell.exe') @childArgs
    exit $LASTEXITCODE
}

# --- Bên trong secret run: DB_PASSWORD đã có trong env ---
foreach ($name in 'ConnectionStrings__SuperAppConnection', 'ConnectionStrings__UserProfileConnection') {
    $cs = [Environment]::GetEnvironmentVariable($name)
    if (-not $cs) { continue }
    $b = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $cs
    if (-not $b.Password) { $b.Password = $env:DB_PASSWORD }
    if ($Database) { $b.InitialCatalog = $Database }
    [Environment]::SetEnvironmentVariable($name, $b.ConnectionString)
}

Set-Location $root
$mode = if ($Watch) { 'watch' } else { 'run' }
dotnet $mode --project SuperAppAPI
exit $LASTEXITCODE
