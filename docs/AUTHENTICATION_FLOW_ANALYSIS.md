# Authentication Flow Analysis & Security Assessment

> **Document version:** 2026-03-18
> **Scope:** Portal Frontend (React/MSAL) + URM Service Backend (.NET)
> **Author:** Claude Code Review

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [FRONTEND – Portal (MSAL / Azure AD)](#2-frontend--portal-msal--azure-ad)
3. [BACKEND – URM Service API](#3-backend--urm-service-api)
4. [End-to-End Authentication Flow](#4-end-to-end-authentication-flow)
5. [Security Assessment](#5-security-assessment)
6. [Recommendations](#6-recommendations)

---

## 1. System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PORTAL (React SPA)                                   │
│                                                                              │
│  ┌──────────┐   ┌───────────────┐   ┌──────────────────┐                    │
│  │authConfig │──▶│TopNavigation2 │──▶│acquireTokenSilent │                   │
│  │   (.ts)   │   │ (main auth)   │   │  (token refresh)  │                  │
│  └──────────┘   └──────┬────────┘   └────────┬──────────┘                   │
│                        │                      │                              │
│          ┌─────────────▼──────────────────────▼──────────┐                  │
│          │           AuthContext (React Context)          │                  │
│          │  userToken | userEmail | admin | roleAccess    │                  │
│          └───────────────────────┬────────────────────────┘                  │
│                                  │                                           │
│                     Azure AD ID Token (Bearer)                               │
│                                  │                                           │
└──────────────────────────────────┼───────────────────────────────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                      URM SERVICE API (.NET)                                  │
│                                                                              │
│  Request ──▶ CORS ──▶ ExceptionHandler ──▶ Routing                          │
│          ──▶ UseAuthentication ──▶ UseAuthorization                          │
│          ──▶ UseSession                                                      │
│          ──▶ TokenValidationMiddleware (custom)                              │
│              ├── ReadJwtToken (NO signature verification!)                   │
│              ├── Extract: email, oid, name                                   │
│              ├── Validate: audience + issuer (string compare)                │
│              └── Store in Session                                            │
│          ──▶ ExceptionHandler #2 (detailed logging)                         │
│          ──▶ Controllers                                                     │
│              ├── urm/UserProfile/authorized      (check VT group)           │
│              ├── urm/UrmUserRole/rights           (get role access)          │
│              ├── urm/UrmRole/*                    (role management)          │
│              ├── kaiser/UserProfile/iskaiseruser  (external supplier)        │
│              └── ta/UserProfile/isTAUser          (external supplier)        │
│                                                                              │
│  Backend-to-Backend:                                                         │
│  MicrosoftGraphClient (Client Credentials) ──▶ MS Graph API                 │
│              ├── GetUserGroups() ──▶ Check VT Everyone group                │
│              └── GetTimeZoneInfo() ──▶ Mailbox settings                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. FRONTEND – Portal (MSAL / Azure AD)

### 2.1 MSAL Configuration

Có **2 file cấu hình** MSAL:

#### `msalConfig.js` (legacy – có thể dead code)
```
Client ID:    615b1736-361b-480f-950c-b3d6ff7b952d  (= Tenant ID??)
Authority:    default (common endpoint – multi-tenant)
Redirect URI: https://{hostname}/
Cache:        storeAuthStateInCookie (IE/Edge/Firefox)
```

#### `authConfig.ts` (file đang dùng – imported bởi TopNavigation2)
```
Client ID:    13bc147a-c407-48c0-ba28-c5c73b9fb135  (App Registration)
Authority:    https://login.microsoftonline.com/615b1736-361b-480f-950c-b3d6ff7b952d
                                                     ^^^^ Tenant ID (single-tenant)
Redirect URI: {protocol}//{host}/
Cache:        localStorage
Scopes:       ["User.Read"]
```

### 2.2 Login Flow

```
┌──────────┐     ┌──────────────┐     ┌────────────────┐     ┌──────────┐
│  User    │     │  TopNav2     │     │  MSAL Library  │     │ Azure AD │
└────┬─────┘     └──────┬───────┘     └───────┬────────┘     └────┬─────┘
     │                  │                      │                   │
     │  1. Click Login  │                      │                   │
     │─────────────────▶│                      │                   │
     │                  │  2. loginRedirect()  │                   │
     │                  │   scopes: User.Read  │                   │
     │                  │─────────────────────▶│                   │
     │                  │                      │  3. Redirect to   │
     │                  │                      │  Azure AD login   │
     │◀────────────────────────────────────────────────────────── │
     │                  │                      │                   │
     │  4. User enters credentials (Azure AD)  │                   │
     │─────────────────────────────────────────────────────────▶ │
     │                  │                      │                   │
     │  5. Redirect back with auth code        │                   │
     │◀────────────────────────────────────────────────────────── │
     │                  │                      │                   │
     │                  │  6. MSAL exchanges   │                   │
     │                  │  code for tokens     │                   │
     │                  │  (ID + Access +      │                   │
     │                  │   Refresh)           │                   │
     │                  │◀─────────────────────│                   │
     │                  │                      │                   │
     │                  │  7. useEffect([account]) triggers        │
     │                  │  checkIfUserIsAuthorized()               │
     │                  │                      │                   │
     │                  │  8. acquireTokenSilent() → ID Token      │
     │                  │                      │                   │
     │                  │  9. isAuthorized({token, email})         │
     │                  │  ──────────────────────▶ URM Service     │
     │                  │                      │                   │
     │                  │  10. getUserAccessDataURM()              │
     │                  │  11. getUserAccessOT()                   │
     │                  │  12. Set AuthContext (admin, roles, etc.)│
```

### 2.3 Token Acquisition

```typescript
// TopNavigation2.tsx (line 317-323) - Điểm quan trọng
instance.acquireTokenSilent({
    redirectUri: `https://${window.location.hostname}/`,
    ...loginRequest,       // scopes: ["User.Read"]
    account: account       // MSAL account object
}).then((response) => {
    profile.token = response.idToken;  // ⚠️ Dùng idToken, KHÔNG phải accessToken
});
```

**ID Token** của Azure AD chứa các claims:
- `preferred_username` → email
- `oid` → Object ID trong Azure AD
- `name` → display name
- `aud` → Client ID (13bc147a-c407-48c0-ba28-c5c73b9fb135)
- `iss` → `https://login.microsoftonline.com/{tenantId}/v2.0`

### 2.4 Token Refresh Mechanisms

```
┌─────────────────────────────────────────────────────────────────┐
│                    TOKEN REFRESH MECHANISMS                       │
│                                                                  │
│  1. MSAL INTERNAL (automatic):                                   │
│     acquireTokenSilent() tự refresh qua hidden iframe            │
│     nếu ID token hết hạn (default ~1 hour)                      │
│                                                                  │
│  2. PERIODIC CHECK (mỗi 10 phút):                               │
│     setInterval(checkTokenExpiration, 10 * 60 * 1000)            │
│     ├── acquireTokenSilent() → get current token                 │
│     ├── if (expiresOn - now < 10 min)                            │
│     │   └── checkIfUserIsAuthorized(refreshToken: true)          │
│     │       = Full re-authorization flow                         │
│     └── Cập nhật token mới vào AuthContext                       │
│                                                                  │
│  3. TOKEN REFRESH STORE (TokenRefresh.tsx):                      │
│     React Context cho phép child components gọi tokenRefresh()   │
│     └── acquireTokenSilent() → update AuthContext                │
│                                                                  │
│  4. DAILY LOGIN CHECK:                                           │
│     localStorage['last-login-date'] !== today?                   │
│     └── YES → clear storage + reload (force new login)           │
│                                                                  │
│  5. CROSS-TAB LOGOUT:                                            │
│     window.addEventListener('storage', handleStorage)            │
│     if key='is-logout-all-tab' && value='true'                   │
│     └── clear all storage + reload                               │
└─────────────────────────────────────────────────────────────────┘
```

### 2.5 Teams Authentication Path

```
Teams SDK ──▶ Provides token directly (no MSAL redirect needed)
              │
              ├── authState.isTeamsUser = true
              ├── Token comes from Teams context
              ├── Same authorization flow (isAuthorized → getUserAccessDataURM)
              └── Login/Logout buttons hidden
```

### 2.6 Authorization Flow (sau khi có token)

```
Token acquired (idToken from MSAL or Teams)
    │
    ▼
isAuthorized({ token, email })
──▶ URM Service: GET /urm/UserProfile/authorized?email={email}
    │    Header: Authorization: Bearer {idToken}
    │
    ├── FAIL (res.reference === "VTUserGroupValidation")
    │   └── Show error + auto-logout after 5s
    │
    └── SUCCESS
         │
         ├── setAuth({ admin, urmAdmin, position, ... })
         │
         ├── getUserAccessDataURM(token, email, ...)
         │   └── GET /urm/UrmUserRole/rights?email={email}
         │       ├── checkIsManager(token) → getEmployee + getEmpManagerByReportToId
         │       ├── checkIsAssistant(token, email) → getUserRightAccessOT
         │       └── getHREmployeesByCostCenter(token, allHRCostCenters)
         │
         └── getUserAccessOT(token, email)
             └── getUserRightAccessOT(token, email) → Set overtime access
```

---

## 3. BACKEND – URM Service API

### 3.1 Middleware Pipeline (Startup.cs)

```csharp
// Startup.cs – Configure method – ACTUAL order:

app.UseCors("WebAPIPolicy");              // 1. CORS

if (env.IsDevelopment()) {
    app.UseDeveloperExceptionPage();       // 2. Dev exception page
    app.UseSwagger();                      // 3. Swagger (dev only)
}

if (env.IsProduction()) {
    app.UseHttpsRedirection();             // 4. HTTPS redirect (prod only)
}

app.UseExceptionHandler(JsonExceptionMiddleware);  // 5. JSON error handler

app.UseRouting();                          // 6. Routing

app.UseAuthentication();                   // 7. Auth (⚠️ NO JWT Bearer configured!)
app.UseAuthorization();                    // 8. Authorization

app.UseSession();                          // 9. Session

app.UseTokenValidation();                  // 10. ⚡ CUSTOM TOKEN VALIDATION
                                           //     (THE MAIN AUTH MECHANISM)

app.UseExceptionHandler(/* detailed */);   // 11. Second exception handler (logging)

app.UseEndpoints(...);                     // 12. Endpoints
```

### 3.2 TokenValidationMiddleware (Core Authentication)

**Đây là cơ chế authentication chính của URM Service:**

```csharp
// File: TokenValidationMiddleware.cs
public async Task InvokeAsync(HttpContext context)
{
    // 1. BYPASS: Swagger requests (via Referer header)
    //    ⚠️ Check bằng Referer header – spoofable!
    if (refererHeader.Contains("swagger")) → skip validation

    // 2. BYPASS: Download endpoints
    if (path.Contains("download")) → skip validation

    // 3. Extract Bearer token
    string auth = Headers["Authorization"];
    string token = auth.Split("Bearer ")[1];

    // 4. READ token (⚠️ NO SIGNATURE VERIFICATION!)
    JwtSecurityToken jwtSecToken = tokenHandler.ReadJwtToken(token);

    // 5. Extract claims and store in Session
    string email = claims["preferred_username"];    // Azure AD claim
    string oid   = claims["oid"];                   // Azure AD Object ID
    string name  = claims["name"];                  // Display name

    context.Session.SetString("oid", oid);
    context.Session.SetString("email", email);
    context.Session.SetString("name", name);

    // 6. Validate Audience (string comparison)
    if (audience != "13bc147a-c407-48c0-ba28-c5c73b9fb135")
        throw SecurityTokenException("Invalid audience.");

    // 7. Validate Issuer (string comparison)
    if (issuer != "https://login.microsoftonline.com/615b1736-361b-480f-950c-b3d6ff7b952d/v2.0")
        throw SecurityTokenException("Invalid issuer");
}
```

```
⚠️⚠️⚠️ CRITICAL FINDING ⚠️⚠️⚠️

tokenHandler.ReadJwtToken(token)  ←── CHỈ ĐỌC token, KHÔNG VERIFY SIGNATURE!

vs.

tokenHandler.ValidateToken(token, params, out _)  ←── Đây mới là verify đúng cách

Hậu quả: Bất kỳ ai cũng có thể TẠO một JWT giả với:
  - aud = "13bc147a-c407-48c0-ba28-c5c73b9fb135"
  - iss = "https://login.microsoftonline.com/615b1736-.../v2.0"
  - preferred_username = "admin@vanthiel.com"
  - oid = bất kỳ
→ Middleware sẽ CHẤP NHẬN token này và set session!
```

### 3.3 Token Flow Diagram (Complete)

```
┌──────────┐                    ┌──────────────────┐                ┌──────────┐
│  Portal  │                    │  URM Service API  │               │ MS Graph │
│  (React) │                    │     (.NET)        │               │   API    │
└────┬─────┘                    └────────┬─────────┘                └────┬─────┘
     │                                   │                               │
     │  GET /urm/UserProfile/authorized  │                               │
     │  Authorization: Bearer {idToken}  │                               │
     │  ?email=user@vanthiel.com         │                               │
     │──────────────────────────────────▶│                               │
     │                                   │                               │
     │                      TokenValidationMiddleware                    │
     │                      ├── ReadJwtToken(token)                     │
     │                      ├── Extract email, oid, name                │
     │                      ├── Check audience == ClientID ✓            │
     │                      ├── Check issuer == AzureAD ✓              │
     │                      └── Session.Set(email, oid, name)           │
     │                                   │                               │
     │                      UserProfileController.IsAuthorized()        │
     │                      ├── GetUrmUser(email) → DB lookup           │
     │                      │                                            │
     │                      ├── Check Position == 3 (special)?          │
     │                      │   └── YES → return authorized             │
     │                      │                                            │
     │                      ├── IsVTUserGroup()                         │
     │                      │   ├── Check Referer header                 │
     │                      │   │   (portal/dev-portal/uat-portal/      │
     │                      │   │    localhost:3000)                     │
     │                      │   ├── If unknown referer → return true!   │
     │                      │   │                                        │
     │                      │   └── GetUserGroups()                     │
     │                      │       │                                    │
     │                      │       │ Using Client Credentials:         │
     │                      │       │ ClientSecretCredential(            │
     │                      │       │   tenantId, clientId, secret)     │
     │                      │       │──────────────────────────────────▶│
     │                      │       │ POST /users/{oid}/getMemberGroups │
     │                      │       │◀──────────────────────────────────│
     │                      │       │ groups = [...]                    │
     │                      │       │                                    │
     │                      │       └── groups.Any(x => x == "d3ca638a..")
     │                      │           (Everyone@vanthiel.com group)    │
     │                      │                                            │
     │                      └── Return ResultOptions {                  │
     │                            Success: true/false,                   │
     │                            Reference: isAdmin,                    │
     │                            Reference2: position                   │
     │                          }                                        │
     │                                   │                               │
     │  Response: { Success, Reference } │                               │
     │◀──────────────────────────────────│                               │
```

### 3.4 External Supplier Token Enhancement (Kaiser/TA)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Kaiser/TA TokenService.UpdateClaims()                               │
│                                                                      │
│  1. Read original Azure AD token (ReadJwtToken)                      │
│  2. Copy all existing claims                                         │
│  3. Add custom claims:                                               │
│     ├── "vtExternalPortalSecretKey" = "54860aac-48b0-441e-..."      │
│     ├── "supplierGroupId" = Azure AD group ID                        │
│     └── "supplierGroupType" = "Kaiser" | "TheodoreAlexander"        │
│  4. RE-SIGN token with AzureADVTPortalAPI:ClientSecret              │
│     ├── Algorithm: HS256                                             │
│     ├── Issuer: original token issuer (Azure AD)                     │
│     ├── Audience: original token audience                            │
│     └── Expires: DateTime.Now.AddMinutes(65)                         │
│  5. Return new JWT string                                            │
│                                                                      │
│  ⚠️ ISSUE: Token được re-sign bằng client secret khác               │
│     → Backend nào nhận token này cần biết dùng key nào để verify     │
│     → Nhưng TokenValidationMiddleware KHÔNG verify signature!        │
└──────────────────────────────────────────────────────────────────────┘
```

### 3.5 CORS Configuration

```csharp
// Startup.cs
builder.WithOrigins(
    "http://localhost:3000",
    "http://localhost:3001",
    "http://localhost:3003",
    "*.vanthiel.com"                    // Wildcard subdomain
)
.AllowAnyMethod()
.AllowAnyHeader()
.SetIsOriginAllowed(_ => true)          // ⚠️ Allows ALL origins!
.AllowCredentials();
```

### 3.6 Session Management

```
Session Configuration:
├── IdleTimeout: 30 minutes
├── Cookie.HttpOnly: true ✅
├── Cookie.IsEssential: true
└── Storage: Distributed Memory Cache (in-process)

Session Data Stored:
├── "oid"           → Azure AD Object ID
├── "email"         → User email (from JWT claim)
├── "name"          → Display name
├── "locale"        → Accept-Language header
└── "baseTimeZone"  → Server timezone config
```

---

## 4. End-to-End Authentication Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                     COMPLETE AUTH LIFECYCLE                               │
│                                                                          │
│  ┌─── LOGIN ─────────────────────────────────────────────────────────┐  │
│  │ 1. User clicks Login                                               │  │
│  │ 2. MSAL redirects to Azure AD (single-tenant)                     │  │
│  │ 3. User authenticates with Microsoft credentials                   │  │
│  │ 4. Azure AD returns auth code                                      │  │
│  │ 5. MSAL exchanges code for tokens (ID + Access + Refresh)         │  │
│  │ 6. Portal stores ID Token in AuthContext                           │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                   │                                      │
│  ┌─── AUTHORIZATION ─────────────────────────────────────────────────┐  │
│  │ 7. Portal calls URM Service: /urm/UserProfile/authorized          │  │
│  │    Authorization: Bearer {idToken}                                 │  │
│  │ 8. TokenValidationMiddleware reads JWT (no signature verify)       │  │
│  │ 9. Checks audience + issuer strings                                │  │
│  │ 10. Stores email/oid/name in server Session                       │  │
│  │ 11. Controller checks:                                             │  │
│  │     a. User exists in URM database?                                │  │
│  │     b. User in VT Everyone group? (via MS Graph with app creds)   │  │
│  │ 12. Returns: admin status, position, success/fail                 │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                   │                                      │
│  ┌─── ROLE ACCESS ────────────────────────────────────────────────────┐  │
│  │ 13. Portal calls: /urm/UrmUserRole/rights?email={email}           │  │
│  │ 14. Returns: List<UrmRoleAccessDto> (component + action rights)   │  │
│  │ 15. Portal stores in AuthContext (urmRoleAccess)                   │  │
│  │ 16. Components check hasAccessRights() for UI rendering           │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                   │                                      │
│  ┌─── TOKEN REFRESH (every 10 min) ──────────────────────────────────┐  │
│  │ 17. checkTokenExpiration() → acquireTokenSilent()                  │  │
│  │ 18. If token expires in < 10 min → re-authorize                   │  │
│  │ 19. MSAL handles refresh internally (hidden iframe/refresh token) │  │
│  │ 20. New ID Token → update AuthContext → re-call URM Service       │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                                   │                                      │
│  ┌─── LOGOUT ────────────────────────────────────────────────────────┐  │
│  │ 21. User clicks Sign Out                                          │  │
│  │ 22. Set localStorage['is-logout-all-tab'] = 'true'                │  │
│  │ 23. Remove localStorage['last-login-date']                        │  │
│  │ 24. MSAL instance.logout() → redirect to Azure AD logout          │  │
│  │ 25. Other tabs detect storage event → clear + reload              │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Security Assessment

### 5.1 Điểm Mạnh ✅

#### Portal (Frontend)
| # | Feature | Rating | Detail |
|---|---------|--------|--------|
| 1 | MSAL library | ⭐⭐⭐⭐⭐ | Microsoft-maintained, battle-tested |
| 2 | Azure AD single tenant | ⭐⭐⭐⭐ | Specific authority URL, prevents multi-tenant abuse |
| 3 | Token expiration check | ⭐⭐⭐⭐ | 10-minute interval, proactive refresh |
| 4 | Cross-tab logout | ⭐⭐⭐⭐ | StorageEvent listener, all tabs cleared |
| 5 | Daily login check | ⭐⭐⭐ | Forces re-auth on new day |
| 6 | PII logging disabled | ⭐⭐⭐⭐ | `piiLoggingEnabled: false` |
| 7 | Auto-logout unauthorized | ⭐⭐⭐ | 5-second delay, shows error |
| 8 | Teams integration | ⭐⭐⭐⭐ | Separate auth path, shared authorization |

#### URM Service (Backend)
| # | Feature | Rating | Detail |
|---|---------|--------|--------|
| 1 | Session HttpOnly cookie | ⭐⭐⭐⭐ | `Cookie.HttpOnly = true` |
| 2 | HTTPS in production | ⭐⭐⭐⭐ | `UseHttpsRedirection()` |
| 3 | Audience validation | ⭐⭐⭐ | Checks client ID match |
| 4 | Issuer validation | ⭐⭐⭐ | Checks Azure AD issuer URL |
| 5 | VT Group membership check | ⭐⭐⭐⭐ | MS Graph API with app credentials |
| 6 | Session timeout | ⭐⭐⭐ | 30 minutes idle timeout |
| 7 | Structured error logging | ⭐⭐⭐ | Logs to blob storage |

---

### 5.2 Vấn Đề Bảo Mật & Rủi Ro ⚠️

#### 🔴 CRITICAL

| # | Issue | File | Detail |
|---|-------|------|--------|
| **C1** | **JWT Signature KHÔNG được verify!** | `TokenValidationMiddleware.cs:41` | `ReadJwtToken()` chỉ **decode** Base64 JWT, KHÔNG verify chữ ký. Bất kỳ ai có thể tạo JWT giả với đúng audience/issuer → bypass hoàn toàn authentication. **Đây là lỗ hổng nghiêm trọng nhất.** |
| **C2** | **CORS cho phép ALL origins** | `Startup.cs:55` | `SetIsOriginAllowed(_ => true)` → override tất cả origin restrictions. Combined với `AllowCredentials()`, attacker có thể gọi API từ bất kỳ domain nào với session cookies. |
| **C3** | **Swagger bypass bằng Referer header** | `TokenValidationMiddleware.cs:25` | Check `Referer.Contains("swagger")` → attacker có thể set `Referer: swagger` để bypass toàn bộ auth. Referer header hoàn toàn do client control. |
| **C4** | **Download endpoint bypass** | `TokenValidationMiddleware.cs:27` | `path.Contains("download")` → bất kỳ URL nào chứa "download" đều bypass auth. Ví dụ: `/urm/UserProfile/authorized?download=1` hoặc `/download/../urm/...` |
| **C5** | **UseAuthentication() không có JWT scheme** | `Program.cs` + `Startup.cs` | `UseAuthentication()` được gọi nhưng **không có authentication scheme nào được register** (không có `AddAuthentication().AddJwtBearer()`). Middleware này không làm gì. |

#### 🟠 HIGH

| # | Issue | File | Detail |
|---|-------|------|--------|
| **H1** | **ID Token dùng như Access Token** | `TopNavigation2.tsx:323` | `response.idToken` gửi tới backend thay vì `response.accessToken`. ID Token dành cho client identity, không dành cho API authorization. Audience của ID Token = client app ID, không phải API. |
| **H2** | **Secret key hardcoded trong Constants** | `Constants.cs:31` | `VTExpernalPortalSecretKey = "54860aac-48b0-441e-..."` hardcoded. Ai có source code đều biết secret này. |
| **H3** | **Token re-signing with client secret** | `Kaiser/TokenService.cs:61-69` | Original Azure AD token được re-sign bằng `ClientSecret` với HS256. Nhưng downstream middleware KHÔNG verify signature → re-signing vô nghĩa. Nếu tương lai enable verify → key mismatch. |
| **H4** | **NullReferenceException potential** | `TokenValidationMiddleware.cs:43-45` | `jwtSecToken.Claims.FirstOrDefault(x => x.Type == "preferred_username").Value` → NullReferenceException nếu claim không tồn tại. Crash middleware, có thể leak info. |
| **H5** | **Referer-based security bypass** | `URMGraphService.cs:102-107` | `IsVTUserGroup()` returns `true` nếu Referer không phải portal URLs → bất kỳ non-portal request nào đều bypass group check. |
| **H6** | **Error response leaks exception details** | Multiple controllers | `return StatusCode(500, ex.Message)` và `Reference = ex.ToString()` → leaks stack trace, internal paths, DB info tới client. |
| **H7** | **Duplicate acquireTokenSilent retry loop** | `TopNavigation2.tsx:399-483` | Catch → retry → catch → `localStorage.clear()` + `reload()` + `href='/'`. Nếu Azure AD down → **infinite reload loop**. |
| **H8** | **localStorage cho MSAL tokens** | `authConfig.ts:13` | `cacheLocation: "localStorage"` → Tokens persist qua sessions. XSS attack = full token theft. |

#### 🟡 MEDIUM

| # | Issue | File | Detail |
|---|-------|------|--------|
| **M1** | **Unused ServiceCollection in middleware** | `TokenValidationMiddleware.cs:9` | `new ServiceCollection()` tạo rồi không dùng. Memory leak nếu middleware instantiate nhiều lần. |
| **M2** | **No rate limiting** | All controllers | Không có rate limiting cho bất kỳ endpoint nào → brute force, enumeration attacks. |
| **M3** | **Hardcoded audience/issuer** | `TokenValidationMiddleware.cs:58,64` | Client ID và issuer URL hardcoded → khó maintain khi thay đổi app registration. |
| **M4** | **Console.log token response** | `TokenRefresh.tsx:31` | `console.log('refresh token auth', response)` → log token ra browser console. |
| **M5** | **No HTTPS in development** | `Startup.cs:85-88` | `UseHttpsRedirection()` chỉ bật ở production. Dev traffic is plaintext → token theft trên shared networks. |
| **M6** | **Session not validated against token** | Middleware | Session email/oid set từ token nhưng không validate lại khi session reuse. Session fixation risk. |
| **M7** | **Hardcoded machine names** | `Startup.cs:40` | `string[] localMachines = { "VTHKNB01", ... }` → internal machine info in source code. |
| **M8** | **Two ExceptionHandlers** | `Startup.cs:90,104` | Hai `UseExceptionHandler()` calls. Second one overrides first → `JsonExceptionMiddleware` có thể bị bypass. |
| **M9** | **ConfigurationBuilder in constructor** | `Kaiser/TokenService.cs:27-30` | Mỗi lần DI resolve → đọc lại appsettings.json từ disk. Performance issue. |

#### 🟢 LOW

| # | Issue | File | Detail |
|---|-------|------|--------|
| **L1** | **msalConfig.js có thể dead code** | `msalConfig.js` | Khác Client ID với `authConfig.ts`. Nếu không import ở đâu → nên remove. |
| **L2** | **Large commented code blocks** | `TopNavigation2.tsx` | ~60 lines commented code. Nên remove. |
| **L3** | **Redundant reload + redirect** | `TopNavigation2.tsx:481-482` | `window.location.reload()` rồi `window.location.href = '/'`. Chỉ cần 1. |
| **L4** | **Namespace mismatch** | `TokenValidationMiddleware.cs:5` | Namespace `EmailServiceAPI.Middleware` nhưng file thuộc `URMServiceAPI`. Copy-paste artifact. |

---

### 5.3 Security Score

```
┌──────────────────────────────────────────────────────────────────────┐
│                      OVERALL SECURITY SCORE                          │
│                                                                      │
│  Portal (Frontend/MSAL):           7.0 / 10                         │
│  ├── Authentication mechanism:     9/10  (MSAL + Azure AD)          │
│  ├── Token management:             6/10  (idToken misuse, localStorage)│
│  ├── Error handling:               6/10  (retry loops, clear/reload) │
│  └── Code quality:                 6/10  (duplication, dead code)    │
│                                                                      │
│  URM Service (Backend):            3.5 / 10  ⚠️                     │
│  ├── Token validation:             1/10  (NO signature verification!)│
│  ├── Auth bypass protections:      2/10  (Referer, "download", CORS)│
│  ├── Session management:           5/10  (HttpOnly but no validation)│
│  ├── Error handling:               4/10  (leaks stack traces)        │
│  ├── MS Graph integration:         7/10  (Client Credentials, groups)│
│  └── VT Group authorization:       4/10  (Referer bypass)           │
│                                                                      │
│  ╔══════════════════════════════════════════════════════════════════╗│
│  ║  COMBINED SCORE:                5.0 / 10                        ║│
│  ║                                                                  ║│
│  ║  ⚠️ Backend score thấp do JWT không verify signature.           ║│
│  ║  Toàn bộ auth chain phụ thuộc vào sự tin tưởng rằng             ║│
│  ║  chỉ có Azure AD mới issue token, nhưng KHÔNG VERIFY điều này. ║│
│  ╚══════════════════════════════════════════════════════════════════╝│
└──────────────────────────────────────────────────────────────────────┘
```

---

## 6. Recommendations

### 🔴 Priority 1 – NGAY LẬP TỨC (1-3 ngày)

#### Fix C1: Verify JWT Signature (CRITICAL)

```csharp
// TokenValidationMiddleware.cs – BEFORE (INSECURE):
JwtSecurityToken jwtSecToken = tokenHandler.ReadJwtToken(token);

// AFTER – Option A: Validate Azure AD token properly
// Fetch Azure AD public keys and validate signature
var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
    "https://login.microsoftonline.com/615b1736-361b-480f-950c-b3d6ff7b952d/v2.0/.well-known/openid-configuration",
    new OpenIdConnectConfigurationRetriever());

var openIdConfig = await configManager.GetConfigurationAsync();

var validationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKeys = openIdConfig.SigningKeys,   // Azure AD public keys
    ValidateIssuer = true,
    ValidIssuer = "https://login.microsoftonline.com/615b1736-361b-480f-950c-b3d6ff7b952d/v2.0",
    ValidateAudience = true,
    ValidAudience = "13bc147a-c407-48c0-ba28-c5c73b9fb135",
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(5)
};

ClaimsPrincipal principal = tokenHandler.ValidateToken(token, validationParameters, out _);
// Extract claims from validated principal
string email = principal.FindFirst("preferred_username")?.Value;
string oid = principal.FindFirst("oid")?.Value;
string name = principal.FindFirst("name")?.Value;
```

#### Fix C2: CORS – Remove wildcard allow

```csharp
// BEFORE (INSECURE):
.SetIsOriginAllowed(_ => true)

// AFTER:
// Remove SetIsOriginAllowed entirely, let WithOrigins handle it
builder.WithOrigins(
    "http://localhost:3000",
    "https://portal.vanthiel.com",
    "https://dev-portal.vanthiel.com",
    "https://uat-portal.vanthiel.com"
)
.AllowAnyMethod()
.AllowAnyHeader()
.AllowCredentials();
```

#### Fix C3 & C4: Remove insecure bypasses

```csharp
// BEFORE (INSECURE):
if (refererHeader.Contains("swagger") || path.Contains("download"))
    await _next(context);

// AFTER: Use specific anonymous endpoints
var anonymousEndpoints = new[]
{
    "/swagger",
    "/health",
    "/version"
};

if (anonymousEndpoints.Any(ep => pathValue.StartsWith(ep, StringComparison.OrdinalIgnoreCase)))
{
    await _next(context);
    return;
}
// Download endpoints should still require authentication
```

#### Fix H4: Null-safe claim extraction

```csharp
// BEFORE (crashes if claim missing):
string email = jwtSecToken.Claims.FirstOrDefault(x => x.Type == "preferred_username").Value;

// AFTER:
string email = jwtSecToken.Claims.FirstOrDefault(x => x.Type == "preferred_username")?.Value
    ?? throw new SecurityTokenException("Missing required claim: preferred_username");
```

#### Fix H6: Stop leaking exception details

```csharp
// BEFORE:
return StatusCode(500, ex.Message);
// Reference = ex.ToString()  ← LEAKS STACK TRACE!

// AFTER:
_loggerService.Error(/* log full details server-side */);
return StatusCode(500, new { message = "An internal error occurred." });
// Reference = "internal_error"  ← Generic for client
```

### 🟠 Priority 2 – Short Term (1-2 tuần)

1. **Register proper JWT Bearer authentication in Program.cs**
   ```csharp
   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options => {
           options.Authority = "https://login.microsoftonline.com/615b1736-.../v2.0";
           options.Audience = "13bc147a-c407-48c0-ba28-c5c73b9fb135";
           options.TokenValidationParameters = new TokenValidationParameters {
               ValidateIssuerSigningKey = true,
               ValidateLifetime = true,
               ClockSkew = TimeSpan.FromMinutes(5)
           };
       });
   ```

2. **Add Rate Limiting**
   ```csharp
   builder.Services.AddRateLimiter(options => {
       options.AddFixedWindowLimiter("api", opt => {
           opt.PermitLimit = 100;
           opt.Window = TimeSpan.FromMinutes(1);
       });
   });
   ```

3. **Fix IsVTUserGroup Referer bypass**
   ```csharp
   // Remove Referer-based bypass entirely
   // Always check group membership regardless of Referer
   ```

4. **Remove console.log token** in `TokenRefresh.tsx`

5. **Fix TopNavigation2 retry loop** – add max retry count, avoid infinite reload

6. **Move hardcoded values to appsettings**
   - Audience, Issuer → `appsettings.json`
   - Machine names → environment config
   - Secret keys → Azure Key Vault or user-secrets

### 🟡 Priority 3 – Medium Term (1 tháng)

1. **Consider switching Portal to use Access Token** thay vì ID Token
2. **Move MSAL cache to sessionStorage**
3. **Add [Authorize] attributes** trên controllers thay vì chỉ rely on middleware
4. **Remove dead code**: `msalConfig.js`, commented blocks, namespace mismatch
5. **Consolidate ExceptionHandlers** → chỉ dùng 1
6. **Add Security Headers middleware** (X-Frame-Options, CSP, etc.)
7. **Implement session validation** – verify session email matches token on each request

---

## Appendix: File Reference

| File | System | Purpose |
|------|--------|---------|
| `portal/src/auth/msalConfig.js` | Portal | MSAL config (legacy?) |
| `portal/src/auth/authConfig.ts` | Portal | MSAL config (active) |
| `portal/src/components/toolbars/TopNavigation2.tsx` | Portal | Main auth orchestrator |
| `portal/src/store/TokenRefresh.tsx` | Portal | Token refresh context |
| `urm-service/URMServiceAPI/Program.cs` | URM | App bootstrap, DI |
| `urm-service/URMServiceAPI/Startup.cs` | URM | Middleware pipeline, CORS |
| `urm-service/URMServiceAPI/Controllers/Middleware/TokenValidationMiddleware.cs` | URM | **Core auth middleware** |
| `urm-service/URMServiceAPI/Controllers/Middleware/JsonExceptionMiddleware.cs` | URM | Exception handler |
| `urm-service/URMServiceAPI/Controllers/UserProfileController.cs` | URM | Authorization endpoints |
| `urm-service/URMServiceAPI/Controllers/UrmRoleController.cs` | URM | Role management |
| `urm-service/URMServiceAPI/Controllers/UrmUserRoleController.cs` | URM | User role access |
| `urm-service/URMServiceAPI/Controllers/Kaiser/UserProfileController.cs` | URM | Kaiser external supplier |
| `urm-service/URMServiceAPI/Controllers/TA/UserProfileController.cs` | URM | TA external supplier |
| `urm-service/MicrosoftGraphService/URMGraphService.cs` | URM | MS Graph API, group checks |
| `urm-service/MicrosoftGraphService/Helpers/MicrosoftGraphClient.cs` | URM | Graph client (Client Credentials) |
| `urm-service/MicrosoftGraphService/Kaiser/TokenService.cs` | URM | Kaiser token enhancement |
| `urm-service/MicrosoftGraphService/TA/TokenService.cs` | URM | TA token enhancement |
| `urm-service/MicrosoftGraphService/HttpContextService.cs` | URM | Session/context helpers |
| `urm-service/MicrosoftGraphService/Utilities/Constants.cs` | URM | Constants, group IDs |
| `urm-service/CommonLibrary/AuthenticationConfig.cs` | Common | MSAL public client config |
| `urm-service/CommonLibrary/SecurityTools/SecurityExtensions.cs` | Common | SecureString helper |
