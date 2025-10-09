# Authentication & Authorization Guide

## Table of Contents
1. [Overview](#overview)
2. [JWT Token Authentication](#jwt-token-authentication)
3. [Google OAuth Integration](#google-oauth-integration)
4. [Password Security](#password-security)
5. [Protecting Endpoints](#protecting-endpoints)
6. [Authorization Patterns](#authorization-patterns)
7. [Token Management](#token-management)
8. [Security Best Practices](#security-best-practices)
9. [Troubleshooting](#troubleshooting)

---

## Overview

SuperApp uses a dual authentication system:
- **JWT (JSON Web Tokens)** for standard username/password authentication
- **Google OAuth 2.0** for third-party authentication

**Key Technologies:**
- ASP.NET Core Identity (JWT Bearer)
- Google OAuth 2.0 Client Library
- BCrypt for password hashing
- Serilog for security event logging

---

## JWT Token Authentication

### Token Structure

JWT tokens in SuperApp contain the following claims:

```json
{
  "sub": "user@example.com",     // Subject: User identifier (email or phone)
  "jti": "550e8400-e29b-41d4-a716-446655440000",  // JWT ID: Unique token ID
  "exp": 1704067200,             // Expiration: Unix timestamp
  "iat": 1704063600,             // Issued At: Unix timestamp
  "iss": "SuperApp",             // Issuer
  "aud": "SuperApp-API"          // Audience
}
```

### Token Configuration

Configure JWT settings in `appsettings.json`:

```json
{
  "Jwt": {
    "Issuer": "SuperApp",
    "Audience": "SuperApp-API",
    "ExpiryMinutes": 60
  }
}
```

**Security Configuration (User Secrets):**

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long"
  }
}
```

⚠️ **CRITICAL:** Never commit the JWT Key to source control. Always use User Secrets or environment variables.

```bash
# Set JWT key using user secrets
dotnet user-secrets set "Jwt:Key" "your-secret-key-here" --project src/SuperApp.API
```

### Generating JWT Tokens

**Service Implementation:**

```csharp
public interface IJwtTokenService
{
    string GenerateToken(string userIdentifier);
    ClaimsPrincipal? ValidateToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateToken(string userIdentifier)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var securityKey = new SymmetricSecurityKey(key);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userIdentifier),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:ExpiryMinutes"]!)),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        
        _logger.LogInformation("JWT token generated for user {UserIdentifier}", userIdentifier);
        
        return tokenString;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
}
```

### Login Flow Example

**Command Handler:**

```csharp
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Retrieve user from database
        var user = await _authRepository.GetUserByEmailAsync(request.Email);
        
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            throw new UnauthorizedException("Invalid credentials");
        }

        // 2. Verify password
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            throw new UnauthorizedException("Invalid credentials");
        }

        // 3. Generate JWT token
        var token = _jwtTokenService.GenerateToken(user.Email);

        _logger.LogInformation("User {Email} logged in successfully", request.Email);

        return new LoginResponse
        {
            Token = token,
            ExpiresIn = 3600, // 60 minutes in seconds
            TokenType = "Bearer",
            UserEmail = user.Email
        };
    }
}
```

### Middleware Configuration

**Program.cs:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add Authentication
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
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    JsonSerializer.Serialize(new { error = "Unauthorized" }));
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// IMPORTANT: Order matters!
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

---

## Google OAuth Integration

### Configuration

**appsettings.json (public config):**

```json
{
  "OAuth": {
    "Google": {
      "ClientId": "887853390661-xxxxxxxxxxxxx.apps.googleusercontent.com",
      "RedirectUri": "https://yourapp.com/auth/google/callback",
      "Scope": "openid profile email"
    }
  }
}
```

**User Secrets (sensitive config):**

```json
{
  "OAuth": {
    "Google": {
      "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxx"
    }
  }
}
```

```bash
# Set OAuth client secret
dotnet user-secrets set "OAuth:Google:ClientSecret" "your-client-secret" --project src/SuperApp.API
```

### Google OAuth Service

```csharp
public interface IGoogleOAuthService
{
    Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken);
}

public class GoogleOAuthService : IGoogleOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleOAuthService> _logger;
    private readonly HttpClient _httpClient;

    public GoogleOAuthService(
        IConfiguration configuration, 
        ILogger<GoogleOAuthService> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var clientId = _configuration["OAuth:Google:ClientId"];
            var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenInfo = JsonSerializer.Deserialize<GoogleTokenInfo>(content);

            if (tokenInfo?.Aud != clientId)
            {
                _logger.LogWarning("Google token audience mismatch");
                throw new UnauthorizedException("Invalid Google token");
            }

            return new GoogleUserInfo
            {
                Email = tokenInfo.Email,
                Name = tokenInfo.Name,
                Picture = tokenInfo.Picture,
                EmailVerified = tokenInfo.EmailVerified
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to verify Google token");
            throw new UnauthorizedException("Failed to verify Google authentication");
        }
    }
}
```

### Google Login Command Handler

```csharp
public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, LoginResponse>
{
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public async Task<LoginResponse> Handle(
        GoogleLoginCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Verify Google token
        var googleUser = await _googleOAuthService.VerifyGoogleTokenAsync(request.IdToken);

        if (!googleUser.EmailVerified)
        {
            throw new UnauthorizedException("Email not verified with Google");
        }

        // 2. Get or create user
        var user = await _authRepository.GetUserByEmailAsync(googleUser.Email);
        
        if (user == null)
        {
            // Create new user with Google OAuth
            user = await _authRepository.CreateOAuthUserAsync(
                email: googleUser.Email,
                name: googleUser.Name,
                authType: AuthType.Google,
                externalId: googleUser.Email
            );

            _logger.LogInformation("New user created via Google OAuth: {Email}", googleUser.Email);
        }
        else if (user.AuthType != AuthType.Google)
        {
            throw new UnauthorizedException(
                "This email is already registered with a different authentication method");
        }

        // 3. Generate JWT token
        var token = _jwtTokenService.GenerateToken(user.Email);

        _logger.LogInformation("User {Email} logged in via Google OAuth", user.Email);

        return new LoginResponse
        {
            Token = token,
            ExpiresIn = 3600,
            TokenType = "Bearer",
            UserEmail = user.Email,
            UserName = user.Name
        };
    }
}
```

### Controller Endpoint

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<ActionResult<LoginResponse>> GoogleLogin(
        [FromBody] GoogleLoginRequest request)
    {
        var command = new GoogleLoginCommand(request.IdToken);
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}
```

---

## Password Security

### Hashing Algorithm

SuperApp uses **BCrypt** with automatic salt generation for password security.

⚠️ **CRITICAL RULES:**
1. **NEVER** store passwords in plain text
2. **NEVER** compare password hashes directly
3. **ALWAYS** use `PasswordHelper` methods
4. **NEVER** log passwords (even hashed)

### Password Helper Implementation

```csharp
public static class PasswordHelper
{
    private const int WorkFactor = 12; // BCrypt work factor (2^12 iterations)

    /// <summary>
    /// Hashes a password using BCrypt with automatic salt generation
    /// </summary>
    /// <param name="password">Plain text password</param>
    /// <returns>Hashed password with embedded salt</returns>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies a password against a stored hash
    /// </summary>
    /// <param name="password">Plain text password to verify</param>
    /// <param name="hashedPassword">Stored hash from database</param>
    /// <returns>True if password matches, false otherwise</returns>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception)
        {
            // Invalid hash format or other BCrypt error
            return false;
        }
    }
}
```

### Usage Examples

**Registration:**

```csharp
public async Task<User> RegisterUserAsync(RegisterRequest request)
{
    // ✅ Correct: Hash password before storing
    var hashedPassword = PasswordHelper.HashPassword(request.Password);
    
    var user = new User
    {
        Email = request.Email,
        PasswordHash = hashedPassword,
        AuthType = AuthType.Local
    };

    await _repository.CreateUserAsync(user);
    
    _logger.LogInformation("User registered: {Email}", request.Email);
    return user;
}
```

**Login:**

```csharp
public async Task<bool> AuthenticateUserAsync(string email, string password)
{
    var user = await _repository.GetUserByEmailAsync(email);
    
    if (user == null)
        return false;

    // ✅ Correct: Verify using helper method
    return PasswordHelper.VerifyPassword(password, user.PasswordHash);
}
```

**❌ Common Mistakes:**

```csharp
// ❌ WRONG: Comparing hashes directly (will ALWAYS fail)
if (PasswordHelper.HashPassword(inputPassword) == storedHash)
    return true;

// ❌ WRONG: Storing plain text
user.PasswordHash = request.Password;

// ❌ WRONG: Logging passwords
_logger.LogDebug("User password: {Password}", password);

// ❌ WRONG: Returning password in response
return new UserDto { Email = user.Email, Password = user.PasswordHash };
```

### Password Policy

Implement validation rules:

```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match");
    }
}
```

---

## Protecting Endpoints

### Controller-Level Protection

```csharp
// Protect entire controller (all endpoints require authentication)
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    // All methods require valid JWT token
}
```

### Method-Level Protection

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Public endpoint - no authentication required
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Anyone can access
    }

    // Protected endpoint - authentication required
    [Authorize]
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfile>> GetProfile()
    {
        // Requires valid JWT token
    }
}
```

### Accessing Current User

```csharp
[Authorize]
[HttpGet("my-notes")]
public async Task<ActionResult<List<NoteDto>>> GetMyNotes()
{
    // Get user identifier from JWT claims
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userEmail))
        return Unauthorized();

    var query = new GetUserNotesQuery(userEmail);
    var notes = await _mediator.Send(query);
    
    return Ok(notes);
}
```

### Custom User Context Service

```csharp
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? UserEmail => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => 
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

// Register in Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

---

## Authorization Patterns

### Role-Based Authorization (Future Enhancement)

```csharp
// Define roles
public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Manager = "Manager";
}

// Protect endpoint with role
[Authorize(Roles = Roles.Admin)]
[HttpDelete("notes/{id}")]
public async Task<IActionResult> DeleteAnyNote(int id)
{
    // Only admins can delete any note
}

// Multiple roles (OR logic)
[Authorize(Roles = "Admin,Manager")]
[HttpGet("reports")]
public async Task<IActionResult> GetReports()
{
    // Admins OR Managers can access
}
```

### Policy-Based Authorization

**Define policies in Program.cs:**

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("RequireEmailVerified", policy =>
        policy.RequireClaim("email_verified", "true"));

    options.AddPolicy("MinimumAge", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));
});
```

**Use in controllers:**

```csharp
[Authorize(Policy = "RequireAdminRole")]
[HttpPost("admin/settings")]
public async Task<IActionResult> UpdateSettings([FromBody] SettingsDto settings)
{
    // Only accessible to users with Admin role
}
```

### Resource-Based Authorization

```csharp
public class NoteAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Note>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Note resource)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
            return Task.CompletedTask;

        // User can only modify their own notes
        if (requirement.Name == "Update" || requirement.Name == "Delete")
        {
            if (resource.UserId == userId)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// Usage in handler
public async Task Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
{
    var note = await _repository.GetNoteByIdAsync(request.Id);
    
    var authResult = await _authorizationService.AuthorizeAsync(
        _currentUser.User, 
        note, 
        "Update");

    if (!authResult.Succeeded)
        throw new ForbiddenException("You can only update your own notes");

    // Proceed with update
}
```

---

## Token Management

### Token Refresh Strategy

SuperApp currently uses **fixed expiration** (60 minutes). For refresh tokens (future enhancement):

```csharp
public class RefreshTokenService
{
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
    {
        // 1. Validate refresh token from database
        var storedToken = await _repository.GetRefreshTokenAsync(refreshToken);
        
        if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token");

        // 2. Generate new access token
        var newAccessToken = _jwtService.GenerateToken(storedToken.UserId);

        // 3. Optionally rotate refresh token
        var newRefreshToken = GenerateRefreshToken();
        await _repository.UpdateRefreshTokenAsync(storedToken.UserId, newRefreshToken);

        return new LoginResponse
        {
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 3600
        };
    }
}
```

### Token Revocation

```csharp
// Store active tokens in database or cache
public class TokenRevocationService
{
    private readonly IDistributedCache _cache;

    public async Task RevokeTokenAsync(string token)
    {
        var jti = GetJtiFromToken(token);
        var expiration = GetExpirationFromToken(token);

        // Store revoked token until it expires
        await _cache.SetStringAsync(
            $"revoked_token:{jti}",
            "true",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiration
            });
    }

    public async Task<bool> IsTokenRevokedAsync(string token)
    {
        var jti = GetJtiFromToken(token);
        var value = await _cache.GetStringAsync($"revoked_token:{jti}");
        return value != null;
    }
}
```

---

## Security Best Practices

### 1. Never Expose Sensitive Data

```csharp
// ✅ Good: Exclude password hash
public class UserDto
{
    public string Email { get; set; }
    public string Name { get; set; }
    // No PasswordHash property
}

// ❌ Bad: Exposing internal data
public class UserDto
{
    public string Email { get; set; }
    public string PasswordHash { get; set; } // NEVER do this!
}
```

### 2. Validate All Auth Inputs

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(100); // Prevent DOS attacks
    }
}
```

### 3. Rate Limiting (Future Enhancement)

```csharp
// Prevent brute force attacks
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

[EnableRateLimiting("auth")]
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
{
    // Limited to 5 attempts per minute
}
```

### 4. Secure Cookie Configuration (if using cookies)

```csharp
services.AddAuthentication()
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });
```

### 5. Logging Security Events

```csharp
// Log successful authentication
_logger.LogInformation(
    "User {Email} logged in successfully from {IpAddress}", 
    email, 
    httpContext.Connection.RemoteIpAddress);

// Log failed authentication
_logger.LogWarning(
    "Failed login attempt for {Email} from {IpAddress}", 
    email, 
    httpContext.Connection.RemoteIpAddress);

// Log token generation
_logger.LogInformation(
    "JWT token generated for user {UserId} with expiry {Expiry}", 
    userId, 
    expiryTime);
```

### 6. HTTPS Enforcement

```csharp
// Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

---

## Troubleshooting

### Common Issues

#### 1. 401 Unauthorized on Protected Endpoints

**Symptoms:**
- All requests return 401
- Token is being sent

**Checklist:**
- [ ] Verify JWT key matches between token generation and validation
- [ ] Check token hasn't expired (look at `exp` claim)
- [ ] Ensure `Authorization: Bearer <token>` header format is correct
- [ ] Verify middleware order: `UseAuthentication()` before `UseAuthorization()`
- [ ] Check JWT configuration in appsettings.json

**Debug:**
```csharp
// Add to JWT Bearer options
options.Events = new JwtBearerEvents
{
    OnAuthenticationFailed = context =>
    {
        Console.WriteLine($"Auth failed: {context.Exception.Message}");
        return Task.CompletedTask;
    }
};
```

#### 2. Password Verification Always Fails

**Symptoms:**
- Correct password returns false
- Login always fails

**Common Causes:**
```csharp
// ❌ Wrong: Comparing hashes directly
if (PasswordHelper.HashPassword(input) == storedHash)
    return true; // This will NEVER work!

// ✅ Correct: Use VerifyPassword
return PasswordHelper.VerifyPassword(input, storedHash);
```

#### 3. Google OAuth Returns Invalid Token

**Checklist:**
- [ ] Verify ClientId in configuration matches Google Console
- [ ] Check redirect URI matches exactly (including protocol and trailing slash)
- [ ] Ensure OAuth consent screen is configured in Google Console
- [ ] Verify token hasn't expired (Google tokens expire in 1 hour)

**Test Google token manually:**
```bash
curl "https://oauth2.googleapis.com/tokeninfo?id_token=YOUR_TOKEN"
```

#### 4. User Claims Not Available

**Solution:**
```csharp
// Ensure HttpContextAccessor is registered
builder.Services.AddHttpContextAccessor();

// Access claims correctly
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
var email = User.FindFirst(ClaimTypes.Email)?.Value;
```

#### 5. CORS Issues with Authentication

**Symptoms:**
- Authentication works in Postman but not in browser
- CORS errors in console

**Solution:**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.WithOrigins("https://yourfrontend.com")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Required for cookies
    });
});

// Order matters!
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
```

---

## Quick Reference

### Middleware Order

```csharp
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();  // Must be before Authorization
app.UseAuthorization();   // Must be before MapControllers
app.MapControllers();
```

### Testing Authentication

**Get Token:**
```bash
curl -X POST https://localhost:7001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}'
```

**Use Token:**
```bash
curl -X GET https://localhost:7001/api/notes \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### Decode JWT Token

Use [jwt.io](https://jwt.io) or:

```csharp
var handler = new JwtSecurityTokenHandler();
var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
var claims = jsonToken?.Claims;
```

---

## Related Documentation

- **[API Design](API_DESIGN.md)** - Request/response patterns
- **[Error Handling](ERROR_HANDLING.md)** - Exception handling for auth failures
- **[Security Best Practices](SECURITY.md)** - Secrets management and security
- **[Code Examples](CODE_EXAMPLES.md)** - Complete authentication examples
- **[Testing Guidelines](TESTING.md)** - Testing authentication flows

---

## Appendix

### A. Complete Login Example

**Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates user with email and password
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await _mediator.Send(command);
        return Ok(response);
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.Name);
        var response = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProfile), new { }, response);
    }

    /// <summary>
    /// Gets current user's profile
    /// </summary>
    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var query = new GetCurrentUserProfileQuery();
        var profile = await _mediator.Send(query);
        return Ok(profile);
    }

    /// <summary>
    /// Authenticates user with Google OAuth
    /// </summary>
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        var command = new GoogleLoginCommand(request.IdToken);
        var response = await _mediator.Send(command);
        return Ok(response);
    }
}
```

**Request DTOs:**
```csharp
public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password
);

public record RegisterRequest(
    [Required][EmailAddress] string Email,
    [Required][MinLength(8)] string Password,
    [Required] string ConfirmPassword,
    [Required][MaxLength(100)] string Name
);

public record GoogleLoginRequest(
    [Required] string IdToken
);
```

**Response DTOs:**
```csharp
public record LoginResponse(
    string Token,
    int ExpiresIn,
    string TokenType,
    string UserEmail,
    string? UserName = null
);

public record RegisterResponse(
    string UserId,
    string Email,
    string Name,
    string Token
);
```

**Command:**
```csharp
public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
```

**Validator:**
```csharp
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email too long");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password too long");
    }
}
```

**Handler:**
```csharp
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _authRepository.GetUserByEmailAsync(request.Email);
        
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Verify password
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive account: {Email}", request.Email);
            throw new UnauthorizedException("Account is disabled");
        }

        // Generate token
        var token = _jwtTokenService.GenerateToken(user.Email);

        // Update last login
        await _authRepository.UpdateLastLoginAsync(user.Id);

        _logger.LogInformation("User {Email} logged in successfully", user.Email);

        return new LoginResponse(
            Token: token,
            ExpiresIn: 3600,
            TokenType: "Bearer",
            UserEmail: user.Email,
            UserName: user.Name
        );
    }
}
```

**Repository:**
```csharp
public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(User user);
    Task UpdateLastLoginAsync(int userId);
}

public class AuthRepository : BaseRepository, IAuthRepository
{
    public AuthRepository(
        IConnectionFactory connectionFactory,
        ILogger<AuthRepository> logger)
        : base(connectionFactory, logger)
    {
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var users = await ExecuteStoredProcedure(
            StoredProcedures.GetUserByEmail,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_Email", email));
            },
            mapResult: MapToList<User>
        );

        return users.FirstOrDefault();
    }

    public async Task<User> CreateUserAsync(User user)
    {
        var result = await ExecuteStoredProcedure(
            StoredProcedures.InsertUser,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_Email", user.Email));
                cmd.Parameters.Add(new SqlParameter("@iv_PasswordHash", user.PasswordHash));
                cmd.Parameters.Add(new SqlParameter("@iv_Name", user.Name));
                cmd.Parameters.Add(new SqlParameter("@iv_AuthType", (int)user.AuthType));
            },
            mapResult: MapToSingle<User>
        );

        return result;
    }

    public async Task UpdateLastLoginAsync(int userId)
    {
        await ExecuteStoredProcedure(
            StoredProcedures.UpdateLastLogin,
            addParameters: (cmd) =>
            {
                cmd.Parameters.Add(new SqlParameter("@iv_UserId", userId));
                cmd.Parameters.Add(new SqlParameter("@iv_LastLoginDate", DateTime.UtcNow));
            },
            mapResult: (reader) => Task.FromResult(0)
        );
    }
}
```

### B. Token Generation Complete Flow

```
┌──────────┐          ┌────────────┐          ┌──────────┐
│  Client  │          │ Controller │          │ Handler  │
└────┬─────┘          └─────┬──────┘          └────┬─────┘
     │                      │                      │
     │ POST /api/auth/login │                      │
     │─────────────────────>│                      │
     │                      │                      │
     │                      │ LoginCommand         │
     │                      │─────────────────────>│
     │                      │                      │
     │                      │                      │ Get User
     │                      │                      │────────┐
     │                      │                      │        │
     │                      │                      │<───────┘
     │                      │                      │
     │                      │                      │ Verify Password
     │                      │                      │────────┐
     │                      │                      │        │
     │                      │                      │<───────┘
     │                      │                      │
     │                      │                      │ Generate JWT
     │                      │                      │────────┐
     │                      │                      │        │
     │                      │                      │<───────┘
     │                      │                      │
     │                      │ LoginResponse        │
     │                      │<─────────────────────│
     │                      │                      │
     │ 200 OK + Token       │                      │
     │<─────────────────────│                      │
     │                      │                      │
     │ Subsequent requests  │                      │
     │ with Bearer token    │                      │
     │─────────────────────>│                      │
     │                      │                      │
```

### C. Stored Procedures Reference

**Get User by Email:**
```sql
CREATE PROCEDURE [dbo].[usp_s_UserByEmail]
    @iv_Email NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        Id,
        Email,
        PasswordHash,
        Name,
        AuthType,
        IsActive,
        CreatedAt,
        LastLoginAt
    FROM Users
    WHERE Email = @iv_Email
        AND IsDeleted = 0;
END
```

**Insert User:**
```sql
CREATE PROCEDURE [dbo].[usp_i_User]
    @iv_Email NVARCHAR(255),
    @iv_PasswordHash NVARCHAR(MAX),
    @iv_Name NVARCHAR(100),
    @iv_AuthType INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Users (Email, PasswordHash, Name, AuthType, IsActive, CreatedAt)
    VALUES (@iv_Email, @iv_PasswordHash, @iv_Name, @iv_AuthType, 1, GETUTCDATE());

    SELECT 
        Id,
        Email,
        Name,
        AuthType,
        IsActive,
        CreatedAt
    FROM Users
    WHERE Id = SCOPE_IDENTITY();
END
```

**Update Last Login:**
```sql
CREATE PROCEDURE [dbo].[usp_u_UserLastLogin]
    @iv_UserId INT,
    @iv_LastLoginDate DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Users
    SET LastLoginAt = @iv_LastLoginDate
    WHERE Id = @iv_UserId;
END
```

### D. Environment Configuration

**Development (appsettings.Development.json):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.Authentication": "Debug"
    }
  },
  "Jwt": {
    "Issuer": "SuperApp-Dev",
    "Audience": "SuperApp-API-Dev",
    "ExpiryMinutes": 1440
  }
}
```

**Production (appsettings.Production.json):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Jwt": {
    "Issuer": "SuperApp",
    "Audience": "SuperApp-API",
    "ExpiryMinutes": 60
  }
}
```

**User Secrets (all environments):**
```bash
# Development
dotnet user-secrets set "Jwt:Key" "dev-secret-key-at-least-32-chars-long" --project src/SuperApp.API
dotnet user-secrets set "OAuth:Google:ClientSecret" "GOCSPX-dev-secret" --project src/SuperApp.API

# Production (use environment variables or Azure Key Vault)
export JWT__KEY="prod-secret-key-at-least-32-chars-long"
export OAUTH__GOOGLE__CLIENTSECRET="GOCSPX-prod-secret"
```

### E. Testing Checklist

Before deploying authentication changes:

- [ ] Unit tests pass for all handlers
- [ ] Password hashing works correctly
- [ ] Token generation includes correct claims
- [ ] Token validation works with generated tokens
- [ ] Expired tokens are rejected
- [ ] Invalid tokens return 401
- [ ] Protected endpoints require authentication
- [ ] Anonymous endpoints work without token
- [ ] User claims are accessible in controllers
- [ ] Google OAuth integration tested (if enabled)
- [ ] Rate limiting prevents brute force (if enabled)
- [ ] Logging captures auth events correctly
- [ ] Secrets not committed to source control
- [ ] HTTPS enforced in production
- [ ] CORS configured correctly for frontend

---

## Document Information

- **Version:** 1.0
- **Last Updated:** October 2025
- **Owner:** Development Team
- **Review Cycle:** Quarterly

---

## Feedback

For questions or improvements to this documentation:
1. Create an issue in the repository
2. Submit a pull request with suggested changes
3. Contact the development team lead

**Security Issues:** Report security vulnerabilities privately to the security team, not in public issues.