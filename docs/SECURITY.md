# Security Best Practices

[← Back to Main Documentation](../.copilot-instructions.md.md)

---

## Table of Contents
1. [Secrets Management](#secrets-management)
2. [SQL Injection Prevention](#sql-injection-prevention)
3. [Authentication Security](#authentication-security)
4. [Password Security](#password-security)
5. [CORS Configuration](#cors-configuration)
6. [HTTPS Enforcement](#https-enforcement)
7. [Input Validation](#input-validation)
8. [Sensitive Data Handling](#sensitive-data-handling)

---

## Secrets Management

### ❌ NEVER Commit Secrets to Git

```csharp
// ❌ BAD - Hardcoded in source code
public static string ConnectionString = "Server=...;Password=MyPassword123;";
public const string ApiKey = "abc123-secret-key";
public const string JwtSecret = "my-super-secret-key";
```

### ✅ Use User Secrets (Development)

```bash
# Navigate to API project
cd SuperApp.API

# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "Server=...;Database=...;"
dotnet user-secrets set "Jwt:Key" "your-secret-jwt-key-here"
dotnet user-secrets set "OAuth:ClientSecret" "your-oauth-secret"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "Jwt:Key"

# Clear all secrets
dotnet user-secrets clear
```

### ✅ Use Environment Variables (Production)

```csharp
// Program.cs - Access environment variables
var builder = WebApplication.CreateBuilder(args);

// Environment variables override appsettings.json
var connectionString = builder.Configuration.GetConnectionString("SuperAppConnection");
var jwtKey = builder.Configuration["Jwt:Key"];
```

```bash
# Linux/Mac
export ConnectionStrings__SuperAppConnection="Server=...;Database=...;"
export Jwt__Key="production-jwt-key"

# Windows PowerShell
$env:ConnectionStrings__SuperAppConnection="Server=...;Database=...;"
$env:Jwt__Key="production-jwt-key"

# Azure App Service - Set in Configuration settings
# AWS - Use Parameter Store or Secrets Manager
# Docker - Use secrets or environment variables
```

### ✅ Use Azure Key Vault (Production)

```csharp
// Install: Azure.Extensions.AspNetCore.Configuration.Secrets
// Install: Azure.Identity

// Program.cs
var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = new Uri(builder.Configuration["KeyVault:Url"]!);
    builder.Configuration.AddAzureKeyVault(
        keyVaultUrl,
        new DefaultAzureCredential());
}
```

### Configuration Hierarchy

```
Priority (highest to lowest):
1. Command line arguments
2. Environment variables
3. User secrets (Development only)
4. appsettings.{Environment}.json
5. appsettings.json
```

### What Goes Where

```jsonc
// appsettings.json - Safe to commit
{
  "Logging": {
    "LogLevel": { "Default": "Information" }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Issuer": "SuperApp",
    "Audience": "SuperAppClient",
    "ExpirationMinutes": 60
    // ❌ DO NOT put "Key" here!
  },
  "OAuth": {
    "ClientId": "your-client-id.apps.googleusercontent.com",
    "RedirectUri": "https://your-app.com/callback"
    // ❌ DO NOT put "ClientSecret" here!
  }
}

// secrets.json - Never committed (Development)
{
  "ConnectionStrings": {
    "SuperAppConnection": "Server=...;Password=...;",
    "UserProfileConnection": "Server=...;Password=...;"
  },
  "Jwt": {
    "Key": "development-jwt-secret-key-min-32-chars"
  },
  "OAuth": {
    "ClientSecret": "GOCSPX-your-oauth-client-secret"
  }
}

// Environment Variables - Production
ConnectionStrings__SuperAppConnection
Jwt__Key
OAuth__ClientSecret
```

---

## SQL Injection Prevention

### ✅ Always Use Parameterized Queries

```csharp
// ✅ GOOD - Parameterized query
public async Task<List<Note>> SearchNotesAsync(string searchText)
{
    return await ExecuteStoredProcedure(
        StoredProcedures.SearchNotes,
        addParameters: (cmd) =>
        {
            cmd.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
            return Task.CompletedTask;
        },
        mapResult: MapToList<Note>
    );
}

// ✅ GOOD - Parameterized with null handling
AddParameterIfNotNull(cmd, "@iv_SearchText", searchText);
```

### ❌ NEVER Concatenate SQL Strings

```csharp
// ❌ DANGEROUS - SQL Injection vulnerability!
command.CommandText = $"SELECT * FROM Notes WHERE Name = '{searchText}'";

// ❌ DANGEROUS - Still vulnerable!
command.CommandText = "SELECT * FROM Notes WHERE Name = '" + searchText + "'";

// Attacker could input: ' OR '1'='1' --
// Resulting query: SELECT * FROM Notes WHERE Name = '' OR '1'='1' --'
// Returns all notes!

// Attacker could input: '; DROP TABLE Notes; --
// Resulting query: SELECT * FROM Notes WHERE Name = ''; DROP TABLE Notes; --'
// Deletes the table!
```

### Stored Procedures Reduce Risk

```csharp
// ✅ GOOD - Stored procedures with parameters
command.CommandText = "[dbo].[usp_s_Notes]";
command.CommandType = CommandType.StoredProcedure;
command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));

// Database validates parameter types
// SQL Server treats input as data, not code
```

### Input Validation Layer

```csharp
// Add validation BEFORE database call
public class SearchNotesValidator : AbstractValidator<SearchNotesQuery>
{
    public SearchNotesValidator()
    {
        RuleFor(x => x.SearchText)
            .MaximumLength(200)
            .Matches(@"^[a-zA-Z0-9\s\-_]+$") // Alphanumeric, spaces, hyphens, underscores only
            .When(x => !string.IsNullOrEmpty(x.SearchText));
    }
}
```

---

## Authentication Security

### JWT Configuration

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // No tolerance for expired tokens
            
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Log authentication failures
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication challenge: {Error}", context.Error);
                return Task.CompletedTask;
            }
        };
    });
```

### Token Generation Best Practices

```csharp
// SuperApp.Shared/Helpers/JwtHelper.cs
public static class JwtHelper
{
    public static string GenerateToken(
        string userId,
        string email,
        IConfiguration configuration)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256); // Use strong algorithm

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
            new Claim(JwtRegisteredClaimNames.Iat, 
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64), // Issued at
            // Add custom claims
            new Claim("user_id", userId),
            new Claim("email", email)
        };

        var expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"]!);
        
        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ClaimsPrincipal? ValidateToken(string token, IConfiguration configuration)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
```

### Protect Endpoints

```csharp
// Require authentication for entire controller
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    // All endpoints require authentication
}

// Allow anonymous access to specific endpoint
[AllowAnonymous]
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    // Public endpoint
}

// Require specific role
[Authorize(Roles = "Admin")]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteNote(int id)
{
    // Only admins can delete
}

// Require custom policy
[Authorize(Policy = "RequireNoteOwnership")]
[HttpPut("{id}")]
public async Task<IActionResult> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
{
    // Custom policy checks if user owns the note
}
```

### Token Refresh Strategy

```csharp
// Implement refresh tokens to avoid long-lived access tokens
public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public class RefreshTokenService
{
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
    {
        // Validate refresh token
        // Generate new access token
        // Optionally rotate refresh token
    }
}
```

---

## Password Security

### ❌ Current Bug in SuperApp

```csharp
// ❌ CRITICAL BUG - This will ALWAYS fail!
if(Helpers.HashPassword(model.Password ?? "") == existUser.Password)
{
    // This comparison never succeeds because HashPassword generates
    // a new random salt each time, producing different hashes
}
```

### ✅ Correct Implementation

```csharp
// SuperApp.Shared/Helpers/PasswordHelper.cs
public static class PasswordHelper
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    private const int Iterations = 100000; // OWASP recommendation

    /// <summary>
    /// Hash a password using PBKDF2 with SHA256
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        // Generate random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        
        // Generate hash
        var hash = new Rfc2898DeriveBytes(
            password, 
            salt, 
            Iterations, 
            HashAlgorithmName.SHA256);
        byte[] hashBytes = hash.GetBytes(HashSize);
        
        // Combine salt and hash
        byte[] combined = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, combined, 0, SaltSize);
        Array.Copy(hashBytes, 0, combined, SaltSize, HashSize);
        
        return Convert.ToBase64String(combined);
    }

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        if (string.IsNullOrEmpty(hashedPassword))
            return false;

        try
        {
            // Decode stored hash
            byte[] combined = Convert.FromBase64String(hashedPassword);
            
            if (combined.Length != SaltSize + HashSize)
                return false;

            // Extract salt
            byte[] salt = new byte[SaltSize];
            Array.Copy(combined, 0, salt, 0, SaltSize);
            
            // Extract stored hash
            byte[] storedHash = new byte[HashSize];
            Array.Copy(combined, SaltSize, storedHash, 0, HashSize);
            
            // Compute hash of provided password with same salt
            var hash = new Rfc2898DeriveBytes(
                password, 
                salt, 
                Iterations, 
                HashAlgorithmName.SHA256);
            byte[] computedHash = hash.GetBytes(HashSize);
            
            // Compare hashes (use constant-time comparison to prevent timing attacks)
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }
        catch
        {
            return false;
        }
    }
}

// Usage in login:
var existingUser = await _repository.GetUserByEmailAsync(model.Email);
if (existingUser == null)
    throw new UnauthorizedException("Invalid credentials");

// ✅ CORRECT - Use VerifyPassword
if (!PasswordHelper.VerifyPassword(model.Password, existingUser.Password))
    throw new UnauthorizedException("Invalid credentials");

// Generate JWT token...
```

### Password Requirements

```csharp
public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d").WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");
    }
}
```

### Rate Limiting Failed Login Attempts

```csharp
public class LoginRateLimitService
{
    private readonly IMemoryCache _cache;
    private const int MaxAttempts = 5;
    private const int LockoutMinutes = 15;

    public async Task<bool> IsLockedOutAsync(string email)
    {
        var key = $"login_attempts_{email}";
        if (_cache.TryGetValue(key, out int attempts))
        {
            return attempts >= MaxAttempts;
        }
        return false;
    }

    public async Task RecordFailedAttemptAsync(string email)
    {
        var key = $"login_attempts_{email}";
        var attempts = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(LockoutMinutes);
            return 0;
        });

        _cache.Set(key, attempts + 1, TimeSpan.FromMinutes(LockoutMinutes));
    }

    public async Task ResetAttemptsAsync(string email)
    {
        var key = $"login_attempts_{email}";
        _cache.Remove(key);
    }
}
```

---

## CORS Configuration

### ❌ Dangerous CORS Configuration

```csharp
// ❌ DANGEROUS - Allows any origin
services.AddCors(options =>
{
    options.AddPolicy("DangerousPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
```

### ✅ Secure CORS Configuration

```csharp
// Program.cs or Startup.cs
services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", builder =>
    {
        builder.WithOrigins(
                "https://app.yourdomain.com",
                "https://admin.yourdomain.com")
               .WithMethods("GET", "POST", "PUT", "DELETE")
               .WithHeaders("Content-Type", "Authorization")
               .AllowCredentials()
               .SetIsOriginAllowedToAllowWildcardSubdomains(); // *.yourdomain.com
    });

    options.AddPolicy("DevelopmentPolicy", builder =>
    {
        builder.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://localhost:3003")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Apply policy based on environment
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseCors("ProductionPolicy");
}
```

### Dynamic CORS Based on Configuration

```csharp
// appsettings.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://app.yourdomain.com",
      "https://admin.yourdomain.com"
    ]
  }
}

// Program.cs
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

services.AddCors(options =>
{
    options.AddPolicy("ConfiguredPolicy", builder =>
    {
        builder.WithOrigins(allowedOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});
```

---

## HTTPS Enforcement

### Redirect HTTP to HTTPS

```csharp
// Program.cs
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
    app.UseHsts(); // HTTP Strict Transport Security
}
```

### HSTS Configuration

```csharp
// Program.cs
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

### Require HTTPS

```csharp
// Force HTTPS in production
builder.Services.Configure<MvcOptions>(options =>
{
    if (!builder.Environment.IsDevelopment())
    {
        options.Filters.Add(new RequireHttpsAttribute());
    }
});
```

---

## Input Validation

### Always Validate User Input

```csharp
// Use FluentValidation for all requests
public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
{
    public CreateNoteValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_.,!?]+$").WithMessage("Name contains invalid characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");

        RuleFor(x => x.Tags)
            .MaximumLength(500).WithMessage("Tags cannot exceed 500 characters")
            .Must(BeValidTagFormat).WithMessage("Invalid tag format")
            .When(x => !string.IsNullOrEmpty(x.Tags));
    }

    private bool BeValidTagFormat(string? tags)
    {
        if (string.IsNullOrEmpty(tags))
            return true;

        // Tags should be comma-separated, alphanumeric
        var tagArray = tags.Split(',');
        return tagArray.All(tag => 
            !string.IsNullOrWhiteSpace(tag) && 
            tag.Trim().Length <= 50 &&
            Regex.IsMatch(tag.Trim(), @"^[a-zA-Z0-9\-_]+$"));
    }
}
```

### Sanitize HTML Input

```csharp
// Install: HtmlSanitizer
public static class HtmlSanitizer
{
    private static readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();

    static HtmlSanitizer()
    {
        // Configure allowed tags and attributes
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedTags.Add("p");
        _sanitizer.AllowedTags.Add("br");
        _sanitizer.AllowedTags.Add("strong");
        _sanitizer.AllowedTags.Add("em");
        _sanitizer.AllowedTags.Add("ul");
        _sanitizer.AllowedTags.Add("ol");
        _sanitizer.AllowedTags.Add("li");
    }

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        return _sanitizer.Sanitize(html);
    }
}

// Usage
public async Task<NoteDto> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
{
    var note = new Note
    {
        Name = request.Name,
        Description = HtmlSanitizer.Sanitize(request.Description) // Sanitize HTML
    };
    // ...
}
```

### File Upload Validation

```csharp
public class FileUploadValidator : AbstractValidator<FileUploadRequest>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public FileUploadValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(HaveAllowedExtension).WithMessage("File type not allowed")
            .Must(HaveAllowedSize).WithMessage($"File size cannot exceed {MaxFileSize / 1024 / 1024} MB");
    }

    private bool HaveAllowedExtension(IFormFile file)
    {
        if (file == null)
            return false;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension);
    }

    private bool HaveAllowedSize(IFormFile file)
    {
        return file?.Length <= MaxFileSize;
    }
}
```

---

## Sensitive Data Handling

### ❌ Never Log Sensitive Information

```csharp
// ❌ DANGEROUS - Logs sensitive data
_logger.LogInformation("User logged in with password: {Password}", password);
_logger.LogDebug("JWT Token: {Token}", jwtToken);
_logger.LogError("Payment failed for card {CardNumber}", cardNumber);
_logger.LogInformation("User SSN: {SSN}", ssn);
```

### ✅ Log Safely

```csharp
// ✅ SAFE - Only log non-sensitive identifiers
_logger.LogInformation("User {UserId} logged in successfully", userId);
_logger.LogDebug("JWT token generated for user {UserId}", userId);
_logger.LogError("Payment failed for user {UserId}", userId);
_logger.LogInformation("User profile updated for user {UserId}", userId);
```

### Mask Sensitive Data in Responses

```csharp
public record UserDto
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    
    // ❌ Don't expose password hash
    // public string Password { get; init; }
    
    // ✅ Mask sensitive data
    public string Phone { get; init; } = string.Empty;
    
    public string MaskedPhone => 
        string.IsNullOrEmpty(Phone) ? "" : $"***-***-{Phone[^4..]}";
    
    // ❌ Don't expose full credit card
    // public string CreditCard { get; init; }
    
    // ✅ Only show last 4 digits
    public string CreditCardLast4 { get; init; } = string.Empty;
}
```

### Secure Data Storage

```csharp
// Encrypt sensitive data at rest
public class EncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IConfiguration configuration)
    {
        _key = Convert.FromBase64String(configuration["Encryption:Key"]!);
        _iv = Convert.FromBase64String(configuration["Encryption:IV"]!);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
```

---

## Security Headers

### Add Security Headers

```csharp
// Middleware to add security headers
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent clickjacking
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        
        // Prevent MIME type sniffing
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        
        // Enable XSS protection
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        
        // Content Security Policy
        context.Response.Headers.Add("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self';");
        
        // Referrer policy
        context.Response.Headers.Add("Referrer-Policy", "no-referrer");
        
        // Permissions policy
        context.Response.Headers.Add("Permissions-Policy", 
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

        await _next(context);
    }
}

// Register middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
```

---

## Security Checklist

### Development

- [ ] User secrets configured for local development
- [ ] No secrets in appsettings.json
- [ ] .gitignore includes secrets.json and appsettings.Development.json
- [ ] All database queries use parameters
- [ ] Password hashing uses PBKDF2 with 100,000+ iterations
- [ ] JWT tokens expire within 60 minutes
- [ ] Input validation on all endpoints
- [ ] CORS restricted to known origins
- [ ] Sensitive data not logged

### Production

- [ ] Environment variables configured
- [ ] Azure Key Vault or equivalent configured
- [ ] HTTPS enforced
- [ ] HSTS enabled
- [ ] Security headers added
- [ ] Rate limiting implemented
- [ ] Failed login attempts tracked
- [ ] Token refresh mechanism implemented
- [ ] Database connection uses least privilege
- [ ] Error messages don't expose implementation details

### Code Review

- [ ] No hardcoded secrets
- [ ] No SQL string concatenation
- [ ] All inputs validated
- [ ] Passwords hashed correctly
- [ ] Authentication required on protected endpoints
- [ ] Authorization checks in place
- [ ] Sensitive data masked in logs
- [ ] CORS policy restrictive

---

[← Back to Main Documentation](../.copilot-instructions.md.md) | [Next: Testing →](TESTING.md)