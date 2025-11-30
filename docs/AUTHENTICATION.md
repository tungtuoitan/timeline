# Authentication & Authorization

## Overview

SuperApp hỗ trợ 2 phương thức authentication:
1. **Local**: Email/Phone + Password
2. **OAuth**: Google

## JWT Token Authentication

### Token Structure

**Claims:**
- `sub`: User ID
- `jti`: Unique token ID
- `exp`: Expiration time
- `iat`: Issued at
- `iss`: Issuer
- `aud`: Audience

**Configuration** (appsettings.json):
```json
{
  "Jwt": {
    "Issuer": "SuperApp",
    "Audience": "SuperAppUsers",
    "ExpiryMinutes": 60
  }
}
```

**User Secrets:**
```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-chars"
  }
}
```

### Token Generation

```csharp
public string GenerateToken(int userId)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: _issuer,
        audience: _audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(60),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Middleware Setup (Program.cs)

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Middleware order
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
```

## Password Security

**BCrypt Hashing:**
```csharp
// Hash password
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

// Verify password
bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
```

**Password Policy:**
- Min 8 characters
- At least 1 uppercase
- At least 1 lowercase
- At least 1 number
- At least 1 special character

**Best Practices:**
- Never store plain passwords
- Never log passwords
- Don't compare hashes directly (use BCrypt.Verify)

## Google OAuth Integration

**Configuration:**
```json
{
  "OAuth": {
    "Google": {
      "ClientId": "your-client-id.apps.googleusercontent.com",
      "RedirectUri": "http://localhost:5000/auth/google/callback"
    }
  }
}
```

**User Secrets:**
```json
{
  "OAuth": {
    "Google": {
      "ClientSecret": "your-client-secret"
    }
  }
}
```

**Verify Google Token:**
```csharp
public async Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken)
{
    var tokenInfoUrl = $"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}";
    var response = await _httpClient.GetAsync(tokenInfoUrl);

    if (!response.IsSuccessStatusCode)
        throw new UnauthorizedException("Invalid Google token");

    var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>();
    return userInfo;
}
```

**Login Flow:**
1. Client sends Google ID token
2. Backend verifies token với Google
3. Get/Create user in database
4. Generate JWT token
5. Return JWT to client

## Protecting Endpoints

**Controller Level:**
```csharp
[Authorize]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    // All endpoints require authentication
}
```

**Action Level:**
```csharp
[HttpGet("public")]
[AllowAnonymous]
public IActionResult GetPublicData()
{
    // Public endpoint
}

[HttpGet("private")]
[Authorize]
public IActionResult GetPrivateData()
{
    // Requires authentication
}
```

**Get Current User:**
```csharp
// In controller
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

// Using extension
var userId = User.GetUserId();
```

## Authorization Patterns

### Role-Based
```csharp
[Authorize(Roles = "Admin")]
public IActionResult AdminOnly()
{
    // Only Admin role
}
```

### Policy-Based
```csharp
// Register policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));
});

// Use policy
[Authorize(Policy = "RequireAdmin")]
public IActionResult AdminEndpoint()
{
}
```

### Resource-Based
```csharp
// Check ownership in service
public async Task<Note> GetNoteAsync(int noteId, string userEmail)
{
    var note = await _repository.GetByIdAsync(noteId);

    if (note.UserEmail != userEmail)
        throw new ForbiddenException("Not authorized to access this note");

    return note;
}
```

## Security Best Practices

### ✅ DO
- Use HTTPS in production
- Hash passwords với BCrypt (work factor ≥ 12)
- Validate JWT lifetime
- Use secure secret keys (min 32 chars)
- Implement rate limiting on auth endpoints
- Log authentication events
- Use HttpOnly cookies for refresh tokens
- Implement token revocation
- Validate all inputs

### ❌ DON'T
- Store passwords in plain text
- Log passwords or tokens
- Expose sensitive data in DTOs
- Use weak secret keys
- Skip HTTPS
- Trust client-provided data
- Return detailed auth errors (info leak)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Check token validity, expiration, secret key |
| Token expired | Implement refresh token flow |
| Invalid signature | Verify Jwt:Key matches between token gen & validation |
| Claims missing | Ensure HttpContextAccessor is registered |
| CORS errors | Check CORS policy, order middleware correctly |

## Middleware Order (Critical)

```
1. UseHttpsRedirection
2. UseRouting
3. UseCors
4. UseAuthentication    ← Before Authorization
5. UseAuthorization     ← Before Controllers
6. MapControllers
```

---

**Version:** 2.0
**Last Updated:** November 2025
**See Also:** ERROR_HANDLING.md, API_DESIGN.md
