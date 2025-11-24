# Authentication & Authorization Guide

## Table of Contents
- [Authentication \& Authorization Guide](#authentication--authorization-guide)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [JWT Token Authentication](#jwt-token-authentication)
    - [Token Structure](#token-structure)
    - [Generating Tokens](#generating-tokens)
    - [Login Flow](#login-flow)
  - [Google OAuth Integration](#google-oauth-integration)
  - [Password Security](#password-security)
  - [Protecting Endpoints](#protecting-endpoints)
  - [Authorization Patterns](#authorization-patterns)
  - [Token Management](#token-management)
  - [Security Best Practices](#security-best-practices)
  - [Troubleshooting](#troubleshooting)
  - [Quick Reference](#quick-reference)
  - [Related Documentation](#related-documentation)
  - [Document Information](#document-information)
  - [Appendix](#appendix)
    - [A. Complete Login Example](#a-complete-login-example)
    - [B. Token Flow](#b-token-flow)
    - [C. Stored Procedures](#c-stored-procedures)
    - [D. Env Config](#d-env-config)
    - [E. Testing Checklist](#e-testing-checklist)

## Overview

Dual auth: JWT for email/password, Google OAuth.

Tech: ASP.NET Identity (JWT), Google OAuth Client, BCrypt hashing, Serilog logging.

## JWT Token Authentication

### Token Structure

Claims: sub (user ID), jti (unique ID), exp/iat (timestamps), iss/aud.

Config: appsettings.json (Issuer, Audience, ExpiryMinutes=60); secrets (Key).

### Generating Tokens

IJwtTokenService: GenerateToken(userId) using SymmetricSecurityKey, HmacSha256.

ValidateToken: TokenValidationParameters (key, iss, aud, lifetime).

### Login Flow

Handler: Get user, verify password, generate token.

Middleware: AddAuthentication(JwtBearer), events for failed/challenge.

## Google OAuth Integration

Config: appsettings (ClientId, RedirectUri, Scope); secrets (ClientSecret).

IGoogleOAuthService: VerifyGoogleTokenAsync(idToken) via tokeninfo endpoint.

Handler: Verify token, get/create user, generate JWT.

Endpoint: POST /auth/google with IdToken.

## Password Security

BCrypt (work factor 12).

HashPassword(password), VerifyPassword(password, hash).

Policy: Min 8 chars, upper/lower/number/special; confirm match.

Never store plain, log, or compare hashes directly.

## Protecting Endpoints

[Authorize] on controller/method; [AllowAnonymous] for public.

Access user: User.FindFirst(ClaimTypes.NameIdentifier)?.Value.

ICurrentUserService: UserId, UserEmail, IsAuthenticated via HttpContextAccessor.

## Authorization Patterns

Roles: [Authorize(Roles="Admin")].

Policies: AddPolicy in services (RequireRole, RequireClaim).

Resource-based: AuthorizationHandler for ops like Update/Delete.

## Token Management

Refresh: GenerateRefreshToken (random64), RefreshTokenAsync (validate, new access/refresh).

Revocation: Cache revoked jti until exp; check IsTokenRevokedAsync.

## Security Best Practices

- Exclude sensitive in DTOs.
- Validate inputs (FluentValidation).
- Rate limiting on auth endpoints.
- Secure cookies (HttpOnly, Secure, SameSite=Strict).
- Log events (structured, no passwords).
- HTTPS/HSTS in prod.

## Troubleshooting

- 401: Check key, exp, header, middleware order.
- Password fail: Use VerifyPassword.
- Google invalid: Check ClientId, URI, consent.
- Claims missing: Add HttpContextAccessor.
- CORS: AllowCredentials, order before auth.

## Quick Reference

Middleware: Https > Routing > Cors > Auth > Authorization > Controllers.

Testing: curl for token/login.

Decode: jwt.io or JwtSecurityTokenHandler.

## Related Documentation

- API Design, Error Handling, Security, Code Examples, Testing.

## Document Information

- Version: 1.0
- Last Updated: October 2025
- Owner: Dev Team
- Review: Quarterly

## Appendix

### A. Complete Login Example

Controller: POST login/register/google, GET profile.

DTOs: LoginRequest (email/pass), RegisterRequest (email/pass/confirm/name), GoogleLoginRequest (idToken).

Responses: Token, ExpiresIn, Type, Email/Name.

Command/Validator/Handler: Validate, get user, verify, token, update login.

Repo: GetUserByEmail, CreateUser, UpdateLastLogin (SPs).

### B. Token Flow

Client > Controller > Handler > Repo/Verify > Token > Response.

### C. Stored Procedures

usp_s_UserByEmail, usp_i_User, usp_u_UserLastLogin.

### D. Env Config

Dev: Debug logging, longer expiry.

Prod: Info logging, 60 min expiry.

Secrets: Jwt:Key, OAuth:Google:ClientSecret.

### E. Testing Checklist

- Units, hashing, token claims/valid, expired/invalid reject.
- Protected/anon endpoints, claims access.
- Google, rate limit, logging, no secrets commit, HTTPS.