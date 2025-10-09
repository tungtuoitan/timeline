# Code Review Checklist

## Overview

This checklist ensures code quality, security, and adherence to SuperApp backend standards before merging pull requests.

---

## 🔍 Pre-Review

### Pull Request Information
- [ ] **Clear title** - Describes the change concisely
- [ ] **Description provided** - Explains what, why, and how
- [ ] **Linked issue/ticket** - References related work items
- [ ] **Target branch correct** - Usually `develop` for features
- [ ] **No merge conflicts** - Branch is up to date

---

## 📁 Architecture & Structure

### Layer Boundaries
- [ ] **Dependencies flow correctly** - API → Application → Domain
- [ ] **Infrastructure isolated** - Only accessed through interfaces
- [ ] **Domain has no dependencies** - Pure business logic layer
- [ ] **Correct layer placement** - Code in appropriate project/folder

### Folder Organization
- [ ] **Feature-based structure** - Commands/Queries grouped by feature
- [ ] **One class per file** - File name matches class name
- [ ] **Consistent naming** - Follows established patterns
- [ ] **Proper namespaces** - Match folder structure

**Reference:** [Architecture Guide](ARCHITECTURE.md)

---

## 💻 Coding Standards

### Naming Conventions
- [ ] **Classes: PascalCase** - `NoteRepository`, `CreateNoteCommand`
- [ ] **Interfaces: I + PascalCase** - `INoteRepository`, `IConnectionFactory`
- [ ] **Methods: PascalCase** - `GetNoteByIdAsync`, `CreateNoteAsync`
- [ ] **Private fields: \_camelCase** - `_logger`, `_connectionFactory`
- [ ] **Parameters: camelCase** - `userId`, `searchText`
- [ ] **Constants: PascalCase** - `MaxRetryAttempts`, `DefaultTimeout`
- [ ] **Async suffix** - All async methods end with `Async`

### Code Quality
- [ ] **No magic numbers** - Use named constants
- [ ] **No hardcoded strings** - Use constants or configuration
- [ ] **Descriptive variable names** - Avoid abbreviations
- [ ] **Single responsibility** - Classes/methods do one thing
- [ ] **DRY principle** - No duplicate code
- [ ] **SOLID principles** - Applied where appropriate

### Comments & Documentation
- [ ] **XML documentation** - All public APIs documented
- [ ] **Complex logic explained** - Why, not what
- [ ] **No obvious comments** - Code should be self-documenting
- [ ] **TODO comments tracked** - Linked to issues if present

**Reference:** [Coding Standards](CODING_STANDARDS.md)

---

## 🗄️ Database Access

### Repository Implementation
- [ ] **Inherits BaseRepository** - All repositories extend base class
- [ ] **Uses IConnectionFactory** - No direct connection creation
- [ ] **Stored procedure constants** - No hardcoded SP names
- [ ] **Async/await properly** - All DB calls are async
- [ ] **Proper timeout configured** - Uses timeout constants

### SQL & Parameters
- [ ] **Parameterized queries** - No SQL concatenation (SQL injection)
- [ ] **Nullable parameters handled** - Uses `AddParameterIfNotNull`
- [ ] **Proper parameter types** - SqlParameter with correct SqlDbType
- [ ] **Connection disposal** - Using statements or `await using`
- [ ] **Command disposal** - Properly disposed in all paths

### Data Mapping
- [ ] **Uses MapToList/MapToSingle** - Leverages BaseRepository helpers
- [ ] **No manual mapping** - Uses AutoMapper where appropriate
- [ ] **Handles null results** - Checks for null before accessing

**Reference:** [Database Access](DATABASE_ACCESS.md)

---

## 🌐 API Design

### Controllers
- [ ] **RESTful conventions** - Proper HTTP methods and routes
- [ ] **No business logic** - Controllers are thin
- [ ] **Uses DTOs** - Never exposes domain entities
- [ ] **Proper HTTP status codes** - 200, 201, 204, 400, 401, 404, 500
- [ ] **Async actions** - All actions return Task
- [ ] **Action results typed** - `ActionResult<T>` used

### Endpoints
- [ ] **Consistent naming** - Follows API conventions
- [ ] **Proper route attributes** - `[Route]`, `[HttpGet]`, etc.
- [ ] **Authorization applied** - `[Authorize]` or `[AllowAnonymous]`
- [ ] **Model binding correct** - `[FromBody]`, `[FromRoute]`, `[FromQuery]`
- [ ] **Returns created location** - POST returns `CreatedAtAction`

### DTOs
- [ ] **Request/Response separation** - Separate DTO classes
- [ ] **No domain entities exposed** - DTOs used in all APIs
- [ ] **Appropriate validation** - Data annotations if needed
- [ ] **Mapping configured** - AutoMapper profiles updated

**Reference:** [API Design](API_DESIGN.md)

---

## ⚠️ Error Handling

### Exception Usage
- [ ] **Custom exceptions used** - `NotFoundException`, `ValidationException`, etc.
- [ ] **Appropriate exception types** - Matches HTTP status code intent
- [ ] **Meaningful error messages** - Clear, actionable messages
- [ ] **No empty catch blocks** - Never catch without handling
- [ ] **No generic exceptions** - Don't throw `Exception`

### Logging
- [ ] **Structured logging** - Uses template parameters
- [ ] **Appropriate log levels** - Debug, Info, Warning, Error, Critical
- [ ] **No sensitive data logged** - Passwords, tokens excluded
- [ ] **Exception details logged** - Full stack trace in errors
- [ ] **Contextual information** - UserId, RequestId, etc.

### Middleware
- [ ] **No controller try-catch** - Let middleware handle errors
- [ ] **Global exception handling** - Middleware catches all errors
- [ ] **Consistent error responses** - Same format across API

**Reference:** [Error Handling](ERROR_HANDLING.md)

---

## 🔐 Security

### Secrets & Configuration
- [ ] **No hardcoded secrets** - Uses User Secrets or environment variables
- [ ] **No connection strings in code** - From configuration only
- [ ] **No API keys committed** - gitignore and secrets.json used
- [ ] **Configuration validation** - Required settings validated at startup

### Authentication & Authorization
- [ ] **JWT properly configured** - Token validation settings correct
- [ ] **Endpoints protected** - `[Authorize]` applied appropriately
- [ ] **Passwords hashed** - Uses `PasswordHelper.HashPassword`
- [ ] **Password verification correct** - Uses `PasswordHelper.VerifyPassword`
- [ ] **No password comparison** - Never compares hashes directly

### Input Validation
- [ ] **All inputs validated** - FluentValidation rules applied
- [ ] **SQL injection prevented** - Parameterized queries only
- [ ] **XSS prevention** - Output encoding applied
- [ ] **Mass assignment protected** - DTOs prevent over-posting
- [ ] **File upload validation** - Size, type, content validated

### Data Protection
- [ ] **Sensitive data encrypted** - At rest and in transit
- [ ] **CORS configured correctly** - Restricted origins in production
- [ ] **No sensitive data in logs** - PII, passwords excluded
- [ ] **No sensitive data in URLs** - Query params safe

**Reference:** [Security Best Practices](SECURITY.md)

---

## ✅ Validation

### FluentValidation
- [ ] **Validators created** - All commands/queries validated
- [ ] **Registered in DI** - Added to service collection
- [ ] **Validation pipeline** - `ValidationBehavior` configured
- [ ] **Meaningful messages** - Clear error messages
- [ ] **Appropriate rules** - NotEmpty, MaxLength, Email, etc.

### Business Rules
- [ ] **Domain rules enforced** - Business logic validated
- [ ] **Edge cases handled** - Null, empty, boundary values
- [ ] **Error messages clear** - User-friendly messages

**Reference:** [Validation](VALIDATION.md)

---

## 🧪 Testing

### Test Coverage
- [ ] **Unit tests added** - For new handlers/services
- [ ] **Integration tests added** - For repository methods
- [ ] **Test naming convention** - `MethodName_Scenario_ExpectedResult`
- [ ] **All tests pass** - No failing tests
- [ ] **Code coverage acceptable** - Minimum 70% for new code

### Test Quality
- [ ] **Arrange-Act-Assert** - Clear test structure
- [ ] **One assertion per test** - Focused test cases
- [ ] **Mock dependencies** - Uses Moq for unit tests
- [ ] **Test data realistic** - Representative scenarios
- [ ] **Edge cases tested** - Null, empty, boundary values

### Test Categories
- [ ] **Happy path tested** - Normal scenarios work
- [ ] **Error scenarios tested** - Exceptions thrown correctly
- [ ] **Validation tested** - Invalid inputs rejected
- [ ] **Authorization tested** - Security rules enforced

**Reference:** [Testing Guidelines](TESTING.md)

---

## 🔄 CQRS & MediatR

### Commands
- [ ] **Command class created** - Immutable command object
- [ ] **Handler implemented** - `IRequestHandler<TCommand, TResponse>`
- [ ] **Validator created** - FluentValidation validator
- [ ] **Side effects documented** - Modifies state clearly

### Queries
- [ ] **Query class created** - Immutable query object
- [ ] **Handler implemented** - `IRequestHandler<TQuery, TResponse>`
- [ ] **Read-only operations** - No state modification
- [ ] **Caching considered** - If appropriate

### MediatR Usage
- [ ] **Registered in DI** - MediatR services added
- [ ] **Pipeline behaviors** - Validation, logging applied
- [ ] **No direct handler calls** - Always through `_mediator.Send()`

---

## 📦 Dependencies

### NuGet Packages
- [ ] **Necessary packages only** - No unused dependencies
- [ ] **Latest stable versions** - Security patches applied
- [ ] **License compatible** - Package licenses reviewed
- [ ] **Package vulnerabilities** - No known security issues

### Dependency Injection
- [ ] **Interfaces registered** - Proper DI configuration
- [ ] **Lifecycle correct** - Singleton, Scoped, Transient appropriate
- [ ] **Constructor injection** - No service locator pattern
- [ ] **No circular dependencies** - Clean dependency graph

---

## 📝 Documentation

### Code Documentation
- [ ] **XML comments present** - Public APIs documented
- [ ] **README updated** - If feature affects setup
- [ ] **CHANGELOG updated** - Breaking changes noted
- [ ] **API docs updated** - If endpoints changed

### Inline Documentation
- [ ] **Complex logic explained** - Why decisions made
- [ ] **Assumptions documented** - Known limitations noted
- [ ] **TODOs tracked** - Future work identified

---

## 🚀 Performance

### Efficiency
- [ ] **No N+1 queries** - Batch operations used
- [ ] **Async/await properly** - No `.Result` or `.Wait()`
- [ ] **Lazy loading avoided** - Explicit loading used
- [ ] **Large datasets handled** - Pagination implemented
- [ ] **Connection pooling** - No connection leaks

### Caching
- [ ] **Caching considered** - For expensive operations
- [ ] **Cache invalidation** - Proper cache strategy
- [ ] **Memory usage reasonable** - No excessive memory allocation

---

## 🔧 Configuration

### Settings
- [ ] **appsettings.json updated** - New settings added
- [ ] **Environment variables** - Production config separate
- [ ] **Validation on startup** - Required settings checked
- [ ] **Defaults provided** - Sensible fallback values

---

## 🎨 Code Style

### Formatting
- [ ] **Consistent indentation** - 4 spaces (or configured)
- [ ] **Line length reasonable** - Under 120 characters
- [ ] **No trailing whitespace** - Clean formatting
- [ ] **File ends with newline** - Proper file endings

### Organization
- [ ] **Using statements organized** - Sorted and grouped
- [ ] **Regions avoided** - Proper class organization instead
- [ ] **Methods ordered logically** - Public before private

---

## 🔀 Git Practices

### Commits
- [ ] **Atomic commits** - One logical change per commit
- [ ] **Clear commit messages** - Descriptive messages
- [ ] **No commented code** - Removed or documented
- [ ] **No debug code** - Console.WriteLine removed

### Branch
- [ ] **Feature branch** - Not committed to main/develop
- [ ] **Up to date** - Merged latest from target branch
- [ ] **No merge commits** - Rebase if needed

---

## 📋 Final Checks

### Pre-Merge
- [ ] **All checklist items reviewed** - Complete review done
- [ ] **CI/CD pipeline passes** - All automated checks pass
- [ ] **No build warnings** - Clean build
- [ ] **Manual testing done** - Feature works as expected
- [ ] **Approved by reviewer** - Required approvals obtained

### Post-Merge
- [ ] **Feature branch deleted** - Clean up branches
- [ ] **Deploy to staging** - If applicable
- [ ] **Monitor logs** - Check for errors after deploy

---

## 🎯 Priority Checklist (Critical Items)

If time is limited, ensure these **critical items** are checked:

1. ⚠️ **No secrets committed** - Security risk
2. ⚠️ **SQL injection prevented** - Parameterized queries only
3. ⚠️ **Input validation present** - All inputs validated
4. ⚠️ **Error handling correct** - No silent failures
5. ⚠️ **Tests passing** - No broken tests
6. ⚠️ **Authentication working** - Security not bypassed
7. ⚠️ **No hardcoded connections** - Uses configuration
8. ⚠️ **DTOs used in API** - Domain entities not exposed

---

## 📚 Related Documentation

- [Coding Standards](CODING_STANDARDS.md)
- [Architecture Guide](ARCHITECTURE.md)
- [Database Access](DATABASE_ACCESS.md)
- [API Design](API_DESIGN.md)
- [Error Handling](ERROR_HANDLING.md)
- [Authentication](AUTHENTICATION.md)
- [Validation](VALIDATION.md)
- [Testing Guidelines](TESTING.md)
- [Security Best Practices](SECURITY.md)

---

## 📝 Notes for Reviewers

### Review Tips
1. **Start with architecture** - Ensure code is in the right place
2. **Check security first** - Security issues are highest priority
3. **Verify tests** - Tests should demonstrate functionality
4. **Look for patterns** - Ensure consistency with existing code
5. **Be constructive** - Suggest improvements, not just criticisms

### Common Issues to Watch For
- ❌ Connection strings in code
- ❌ SQL concatenation (injection risk)
- ❌ Empty try-catch blocks
- ❌ Missing input validation
- ❌ Domain entities in API responses
- ❌ Passwords in logs
- ❌ Missing async/await
- ❌ No error handling

---

**Document Version:** 1.0  
**Last Updated:** October 2025  
**Review Cycle:** With each pull request