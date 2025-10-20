# Database Connection Information

## Development Database

**Server:** TUNGHOMEPC\MSSQLSERVER03
**Database:** SuperApp-dev
**User:** sa
**Password:** Tung76721119@

**Connection String:**
```
Server=TUNGHOMEPC\MSSQLSERVER03;Database=SuperApp-dev;User Id=sa;Password=Tung76721119@;TrustServerCertificate=True;Encrypt=False;Connection Timeout=30;
```

## Usage

This is the development database for the SuperApp project.

### Common Commands

**Connect via sqlcmd:**
```bash
sqlcmd -S "TUNGHOMEPC\MSSQLSERVER03" -U sa -P "Tung76721119@" -d SuperApp-dev
```

**Execute queries:**
```bash
sqlcmd -S "TUNGHOMEPC\MSSQLSERVER03" -U sa -P "Tung76721119@" -d SuperApp-dev -Q "YOUR_QUERY_HERE"
```
