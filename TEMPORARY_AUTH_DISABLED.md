# TEMPORARY - AUTHORIZATION DISABLED

**?? WARNING: AUTHORIZATION IS TEMPORARILY DISABLED FOR DEVELOPMENT PURPOSES**

This file documents the changes made to temporarily disable authorization. **IMPORTANT: Re-enable authorization before production deployment!**

## Changes Made

### 1. Program.cs
- **Lines ~85-95**: Commented out JWT authentication configuration
- **Lines ~97**: Commented out `services.AddAuthorization()`

### 2. Startup.cs
- **Lines ~118-120**: Commented out `app.UseAuthentication()` and `app.UseAuthorization()`

### 3. Controllers
All controller `[Authorize]` attributes have been commented out:

#### AuthenController.cs
- **Line ~235**: Commented out `[Authorize]` for `/me` endpoint
- **Line ~244**: Updated `GetCurrentUser()` to return mock data when no claims exist

#### HealthController.cs
- **Line ~102**: Commented out `[Authorize]` for `/secure` endpoint
- **Line ~109**: Updated `GetSecureHealth()` to use mock data and added `authDisabled` flag

#### NotesController.cs
- **Line ~18**: Commented out class-level `[Authorize]` attribute
- **Multiple locations**: Replaced `User.GetUserEmail()` validation with fallback to `"hoanhtungle@gmail.com"`

#### TagsController.cs
- **Line ~17**: Commented out class-level `[Authorize]` attribute
- **Multiple locations**: Replaced `User.GetUserEmail()` validation with fallback to `"hoanhtungle@gmail.com"`

#### UserProfileController.cs
- **Line ~17**: Commented out class-level `[Authorize]` attribute
- **Multiple locations**: Replaced `User.GetUserEmail()` validation with fallback to `"hoanhtungle@gmail.com"`
- **Line ~102**: Commented out profile ownership validation

#### StandardRegistryController.cs
- **Line ~17**: Commented out class-level `[Authorize]` attribute
- **Multiple locations**: Replaced `User.GetUserEmail()` validation with fallback to `"hoanhtungle@gmail.com"`

## Mock Data Used

While authorization is disabled, the following mock data is used:
- **Email**: `hoanhtungle@gmail.com`
- **User ID**: `1`
- **First Name**: `Hoang` 
- **Last Name**: `Tung`

## To Re-enable Authorization

1. **Uncomment JWT configuration in Program.cs**:
   ```csharp
   services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options => { ... });
   services.AddAuthorization();
   ```

2. **Uncomment middleware in Startup.cs**:
   ```csharp
   app.UseAuthentication();
   app.UseAuthorization();
   ```

3. **Restore [Authorize] attributes** in all controllers:
   - Remove `//` comments from `[Authorize]` attributes
   - Change comments like `//[Authorize] // TEMPORARY: Authorization disabled for development` back to `[Authorize]`

4. **Restore authentication validation** in controller methods:
   - Remove fallback logic like `?? "hoanhtungle@gmail.com"`
   - Uncomment validation blocks that check for empty userEmail
   - Remove mock data assignments in AuthenController and HealthController

5. **Remove this documentation file**: `TEMPORARY_AUTH_DISABLED.md`

## Security Reminder

**?? NEVER deploy to production with authorization disabled!**

This configuration allows unrestricted access to all endpoints and should only be used in local development environments.

## Created

- **Date**: December 19, 2024
- **Purpose**: Temporary development convenience
- **Status**: TEMPORARY - MUST BE REVERTED BEFORE PRODUCTION