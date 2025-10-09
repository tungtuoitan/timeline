# Setup Guide

## Prerequisites

### Required Software

| Software | Minimum Version | Download Link |
|----------|----------------|---------------|
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download |
| Visual Studio 2022 | 17.8+ | https://visualstudio.microsoft.com/ |
| SQL Server | 2019+ | https://www.microsoft.com/sql-server |
| Git | 2.30+ | https://git-scm.com/ |

### Alternative IDEs

- **Visual Studio Code** with C# extension
- **JetBrains Rider** (recommended for advanced developers)

### Optional Tools

- **Postman** or **Insomnia** - API testing
- **SQL Server Management Studio (SSMS)** - Database management
- **Azure Data Studio** - Cross-platform database tool
- **Docker Desktop** - Containerization (optional)

---

## Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/yourorg/superapp.git
cd superapp
```

### 2. Restore Dependencies

```bash
# Restore NuGet packages
dotnet restore

# Verify installation
dotnet --version
```

### 3. Configure Database

#### Create Database

```sql
-- Run in SQL Server Management Studio or Azure Data Studio
CREATE DATABASE SuperAppDb;
GO

USE SuperAppDb;
GO
```

#### Run Migrations

```bash
# Apply database schema
# (Scripts location: database/migrations/)
sqlcmd -S localhost -d SuperAppDb -i database/migrations/001_InitialSchema.sql
```

### 4. Configure User Secrets

**Never store sensitive data in `appsettings.json`!**

```bash
# Navigate to API project
cd src/SuperApp.API

# Initialize user secrets
dotnet user-secrets init

# Set connection string
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=localhost;Database=SuperAppDb;Integrated Security=true;TrustServerCertificate=true;"

# Set JWT settings
dotnet user-secrets set "JwtSettings:SecretKey" "your-super-secret-key-min-32-characters-long"
dotnet user-secrets set "JwtSettings:Issuer" "SuperApp"
dotnet user-secrets set "JwtSettings:Audience" "SuperApp"

# Set Google OAuth (optional)
dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
```

#### View All Secrets

```bash
dotnet user-secrets list
```

#### Remove a Secret

```bash
dotnet user-secrets remove "ConnectionStrings:SuperAppConnection"
```

### 5. Update Configuration Files

#### appsettings.json

Keep only non-sensitive configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "JwtSettings": {
    "ExpirationMinutes": 60
  }
}
```

#### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Serilog": {
    "MinimumLevel": "Debug"
  }
}
```

---

## Verify Installation

### 1. Build Solution

```bash
# From solution root
dotnet build

# Expected output: Build succeeded. 0 Warning(s), 0 Error(s)
```

### 2. Run Tests

```bash
# Run all tests
dotnet test

# Expected: All tests pass
```

### 3. Start Application

```bash
# Navigate to API project
cd src/SuperApp.API

# Run application
dotnet run

# Or use watch mode (auto-reload on changes)
dotnet watch run
```

### 4. Test API

Open browser and navigate to:
- **Swagger UI:** http://localhost:5000/swagger
- **Health Check:** http://localhost:5000/health

Test with curl:

```bash
# Health check
curl http://localhost:5000/health

# Expected: {"status": "healthy"}
```

---

## IDE Setup

### Visual Studio 2022

#### Recommended Extensions

1. **ReSharper** (optional, paid) - Code analysis
2. **CodeMaid** - Code cleanup
3. **GitFlow** - Git workflow
4. **Markdown Editor** - Documentation editing

#### Configure Code Style

1. Open **Tools > Options > Text Editor > C# > Code Style**
2. Import: `SuperApp.CodeStyle.editorconfig`

#### Configure Debugging

1. Right-click **SuperApp.API** project > **Properties**
2. **Debug** tab:
   - Launch: Project
   - App URL: http://localhost:5000
   - Enable SSL: ☑

### Visual Studio Code

#### Required Extensions

```bash
# Install extensions
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit
code --install-extension kreativ-software.csharpextensions
code --install-extension jongrant.csharpsortusings
code --install-extension patcx.vscode-nuget-gallery
```

#### Settings (`.vscode/settings.json`)

```json
{
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "editor.formatOnSave": true,
  "csharp.format.enable": true
}
```

#### Launch Configuration (`.vscode/launch.json`)

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/SuperApp.API/bin/Debug/net8.0/SuperApp.API.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/SuperApp.API",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### JetBrains Rider

#### Import Settings

1. **File > Manage IDE Settings > Import Settings**
2. Select: `SuperApp.Rider.settings.zip`

#### Configure Code Cleanup

1. **Settings > Editor > Code Cleanup**
2. Enable: **Run code cleanup on save**
3. Profile: **Full Cleanup**

---

## Database Setup

### Local SQL Server

#### Windows Authentication

```bash
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=localhost;Database=SuperAppDb;Integrated Security=true;TrustServerCertificate=true;"
```

#### SQL Server Authentication

```bash
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=localhost;Database=SuperAppDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
```

### Azure SQL Database

```bash
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=tcp:yourserver.database.windows.net,1433;Database=SuperAppDb;User ID=admin;Password=YourPassword123!;Encrypt=True;"
```

### Docker SQL Server

#### Start Container

```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
  -p 1433:1433 --name superapp-sql \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

#### Connection String

```bash
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=localhost,1433;Database=SuperAppDb;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
```

---

## Troubleshooting Setup Issues

### "Unable to connect to database"

**Solution:**
1. Verify SQL Server is running
2. Check connection string in user secrets
3. Test connection with SSMS or Azure Data Studio
4. Ensure firewall allows connections

### "Missing user secrets"

**Solution:**
```bash
# Re-initialize user secrets
cd src/SuperApp.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "your-connection-string"
```

### "Port already in use"

**Solution:**
```bash
# Change port in launchSettings.json
# Or kill process using port 5000
# Windows:
netstat -ano | findstr :5000
taskkill /PID <process-id> /F

# Linux/Mac:
lsof -ti:5000 | xargs kill -9
```

### "NuGet restore failed"

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore --force
```

---

## Next Steps

1. ✅ Complete setup steps above
2. 📖 Read [Project Overview](PROJECT_OVERVIEW.md)
3. 📏 Review [Coding Standards](CODING_STANDARDS.md)
4. 🏗️ Study [Architecture Guide](ARCHITECTURE.md)
5. 💻 Check [Code Examples](CODE_EXAMPLES.md)

---

## Getting Help

- **Setup Issues:** Check [Troubleshooting](TROUBLESHOOTING.md)
- **Code Questions:** See [Code Examples](CODE_EXAMPLES.md)
- **Team Support:** Contact team lead or create issue

---

**Last Updated:** October 2025  
**Maintainer:** Development Team