# SuperApp Backend Documentation

Welcome to the SuperApp backend documentation. This guide will help you understand the architecture, coding standards, and best practices for developing and maintaining this .NET 8 Web API application.

## 📚 Documentation Index


### Getting Started
- **[Setup Guide](../docs/SETUP.md)** - Environment setup, prerequisites, and first-time configuration
- **[Project Overview](../docs/PROJECT_OVERVIEW.md)** - Technology stack, architecture pattern, and layer responsibilities

### Development Guidelines
- **[Coding Standards](../docs/CODING_STANDARDS.md)** - Naming conventions, file organization, and code style
- **[Architecture Guide](../docs/ARCHITECTURE.md)** - Clean architecture principles, folder structure, and dependencies
- **[Database Access](../docs/DATABASE_ACCESS.md)** - Repository patterns, stored procedures, and connection management
- **[API Design](../docs/API_DESIGN.md)** - RESTful conventions, DTOs, request/response patterns, and versioning

### Core Concepts
- **[Error Handling](../docs/ERROR_HANDLING.md)** - Exception types, logging, and global error middleware
- **[Authentication & Authorization](../docs/AUTHENTICATION.md)** - JWT, OAuth, password security, and endpoint protection
- **[Validation](../docs/VALIDATION.md)** - FluentValidation setup, validation patterns, and error responses

### Quality Assurance
- **[Testing Guidelines](../docs/TESTING.md)** - Unit tests, integration tests, and testing patterns
- **[Security Best Practices](../docs/SECURITY.md)** - Secrets management, SQL injection prevention, and CORS configuration


---

## 🚀 Quick Start

### For New Developers

1. Read **[Setup Guide](../docs/SETUP.md)** to configure your environment
2. Review **[Project Overview](../docs/PROJECT_OVERVIEW.md)** to understand the architecture
3. Study **[Coding Standards](../docs/CODING_STANDARDS.md)** before writing code

### For Feature Development

1. Check **[Architecture Guide](../docs/ARCHITECTURE.md)** to understand where code belongs
2. Follow **[Database Access](../docs/DATABASE_ACCESS.md)** for repository implementation
3. Apply **[API Design](../docs/API_DESIGN.md)** principles for endpoints
4. Implement **[Validation](../docs/VALIDATION.md)** for all inputs
5. Add **[Tests](../docs/TESTING.md)** for new features

### For Code Reviews

Use the **[Code Review Checklist](../docs/CODE_REVIEW_CHECKLIST.md)** to ensure quality and consistency.

---

## 🛠️ Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Framework | .NET | 9.0 |
| Language | C# | 12.0 |
| API Pattern | REST API | - |
| Architecture | Clean Architecture + CQRS | - |
| Database Access | ADO.NET + Stored Procedures | - |
| Authentication | JWT + Google OAuth | - |
| Logging | Serilog | Latest |
| Validation | FluentValidation | Latest |
| Mapping | AutoMapper | Latest |
| Mediator | MediatR | Latest |

---

## 📁 Project Structure

```
SuperApp/
├── .github/ copilot-instructions.md        # 👈 You are here
├── docs/                           # 📚 All documentation files
│   ├── SETUP.md
│   ├── PROJECT_OVERVIEW.md
│   ├── CODING_STANDARDS.md
│   ├── ARCHITECTURE.md
│   ├── DATABASE_ACCESS.md
│   ├── API_DESIGN.md
│   ├── ERROR_HANDLING.md
│   ├── AUTHENTICATION.md
│   ├── VALIDATION.md
│   ├── TESTING.md
│   ├── SECURITY.md
│   ├── DEPLOYMENT.md
│   ├── TROUBLESHOOTING.md
│   ├── CODE_EXAMPLES.md
│   ├── MIGRATION_GUIDE.md
│   ├── API_REFERENCE.md
│   ├── CODE_REVIEW_CHECKLIST.md
│   └── CHANGELOG.md
│
├── src/
│   ├── SuperApp.API/               # 🌐 Presentation Layer
│   ├── SuperApp.Application/       # 💼 Business Logic Layer
│   ├── SuperApp.Domain/            # 🏛️ Domain Layer
│   ├── SuperApp.Infrastructure/    # 🗄️ Data Access Layer
│   └── SuperApp.Shared/            # 🔧 Shared Utilities
│
├── tests/
│   ├── SuperApp.Tests.Unit/        # 🧪 Unit Tests
│   └── SuperApp.Tests.Integration/ # 🔗 Integration Tests
│
├
└── .gitignore
```

---

## 🎯 Common Tasks

### Creating a New Feature

```bash
# 1. Create feature branch
git checkout -b feature/note-tags

# 2. Follow the feature creation guide
# See: docs/CODE_EXAMPLES.md > "Creating a New Feature"

# 3. Run tests
dotnet test

# 4. Create pull request
```

### Running the Application

```bash
# Development
dotnet run --project src/SuperApp.API

# Watch mode (auto-reload)
dotnet watch --project src/SuperApp.API

# Production build
dotnet build -c Release
```

### Managing Secrets

```bash
# Set connection string
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "your-connection-string" --project src/SuperApp.API

# List all secrets
dotnet user-secrets list --project src/SuperApp.API

# Remove a secret
dotnet user-secrets remove "ConnectionStrings:SuperAppConnection" --project src/SuperApp.API
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/SuperApp.Tests.Unit

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 📋 Code Review Checklist

Before submitting a pull request, ensure:

- [ ] Code follows **[Coding Standards](../docs/CODING_STANDARDS.md)**
- [ ] No secrets or connection strings in code (see **[Security](../docs/SECURITY.md)**)
- [ ] XML documentation used only for complex methods and classes, not simple properties
- [ ] DTOs used instead of domain entities in controllers
- [ ] Input validation implemented (see **[Validation](../docs/VALIDATION.md)**)
- [ ] Appropriate error handling (see **[Error Handling](../docs/ERROR_HANDLING.md)**)
- [ ] Repository inherits from `BaseRepository`
- [ ] Tests written and passing (see **[Testing](../docs/TESTING.md)**)
- [ ] No SQL concatenation (SQL injection risk)
- [ ] Async methods properly implemented

Full checklist: **[Code Review Checklist](../docs/CODE_REVIEW_CHECKLIST.md)**

---

## 📝 Documentation Standards

### XML Documentation Guidelines

- **DO use `<summary>` tags for:**
  - Complex classes with business logic
  - Public API methods with non-obvious behavior
  - Methods with multiple parameters or complex return types
  - Controllers and their action methods

- **DON'T use `<summary>` tags for:**
  - Simple properties (getters/setters)
  - DTOs with self-explanatory property names
  - Basic CRUD operations with obvious functionality
  - Private methods with clear names

### Example - Good Documentation:
```csharp
/// <summary>
/// Processes payment with fraud detection and external gateway integration
/// </summary>
/// <param name="request">Payment details including amount and method</param>
/// <returns>Payment result with transaction ID and status</returns>
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)

// Simple property - no documentation needed
public string Email { get; set; }

// Simple DTO - no class or property documentation needed
public class CreateNoteRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
}
```

---

## 🐛 Troubleshooting

### Common Issues

| Issue | Solution | Details |
|-------|----------|---------|
| Connection string error | Check user secrets | [Security Guide](../docs/SECURITY.md) |
| 401 Unauthorized | Check JWT configuration | [Authentication Guide](../docs/AUTHENTICATION.md) |
| Validation not working | Register FluentValidation | [Validation Guide](../docs/VALIDATION.md) |
| Tests failing | Check database connection | [Testing Guide](../docs/TESTING.md) |

---

## 🤝 Contributing

### For Team Members

1. Create a feature branch from `develop`
2. Follow the guidelines in this documentation
3. Write tests for new features
4. Submit pull request with clear description
5. Address code review feedback

### Code Style

This project follows:
- Microsoft C# coding conventions
- Clean Architecture principles
- SOLID principles
- DRY (Don't Repeat Yourself)
- Minimal XML documentation for simple code elements

See **[Coding Standards](../docs/CODING_STANDARDS.md)** for detailed guidelines.

---

## 📞 Getting Help

### External Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Clean Architecture Guide](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)

---

## 📝 Document Maintenance

- **Owner:** Development Team
- **Last Updated:** October 2025
- **Version:** 1.0
- **Review Cycle:** Quarterly

To suggest documentation improvements, create an issue or submit a pull request.

---

## 🔒 Security Notice

This documentation may reference security-sensitive topics. Always:
- Keep secrets in User Secrets or environment variables
- Never commit sensitive data to version control
- Follow **[Security Best Practices](../docs/SECURITY.md)**
- Report security vulnerabilities privately to the team lead

---

## 📈 Project Status

- **Current Version:** 1.0
- **Status:** Active Development
- **Next Release:** Q1 2026
- **Known Issues:** See [Issues](https://github.com/tungtuoitan/SuperApp-backend/issues)

For version history and changes, see **[Changelog](../docs/CHANGELOG.md)**