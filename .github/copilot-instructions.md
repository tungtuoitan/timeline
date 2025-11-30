# SuperApp Backend Documentation

Welcome to the SuperApp backend documentation. This guide will help you understand the architecture, coding standards, and best practices for developing and maintaining this .NET 8 Web API application.

## 📚 Documentation Index

### Getting Started
- **[Project Overview](../docs/PROJECT_OVERVIEW.md)** - Technology stack, architecture pattern, and layer responsibilities

### Development Guidelines
- **[Coding Standards](../docs/CODING_STANDARDS.md)** - Naming conventions, file organization, and code style
- **[Architecture Guide](../docs/ARCHITECTURE.md)** - Clean architecture principles, folder structure, and dependencies
- **[Database Access](../docs/DATABASE_ACCESS.md)** - **Hybrid approach: EF Core + Stored Procedures**
- **[EF Core Guide](../docs/EF_CORE_GUIDE.md)** - **Entity Framework Core setup, DbContext, and LINQ queries**
- **[API Design](../docs/API_DESIGN.md)** - RESTful conventions, DTOs, request/response patterns

### Core Concepts
- **[Error Handling](../docs/ERROR_HANDLING.md)** - Exception types, logging, and global error middleware
- **[Authentication & Authorization](../docs/AUTHENTICATION.md)** - JWT, OAuth, password security, and endpoint protection
- **[Validation](../docs/VALIDATION.md)** - FluentValidation setup, validation patterns, and error responses

---

## 🚀 Quick Start

### For New Developers
1. Review **[Project Overview](../docs/PROJECT_OVERVIEW.md)** to understand the architecture
2. Study **[Coding Standards](../docs/CODING_STANDARDS.md)** before writing code

### For Feature Development
1. Check **[Architecture Guide](../docs/ARCHITECTURE.md)** to understand where code belongs
2. Follow **[Database Access](../docs/DATABASE_ACCESS.md)** for hybrid data access strategy (EF Core 80% + SP 20%)
3. Use **[EF Core Guide](../docs/EF_CORE_GUIDE.md)** for ORM operations (CRUD, simple queries)
4. Apply **[API Design](../docs/API_DESIGN.md)** principles for endpoints
5. Implement **[Validation](../docs/VALIDATION.md)** for all inputs

---

## 🛠️ Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|------|
| Framework | .NET | 8.0 |
| Language | C# | 12.0 |
| API Pattern | REST API | - |
| Architecture | Clean Architecture + CQRS | - |
| Database Access | **EF Core (80%) + Stored Procedures (20%)** | **Hybrid** |
| ORM | Entity Framework Core | Latest |
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
│   ├── DATABASE-CURRENT/          # 📊 Currently deployed database (MVP 1.0) - Production schema
│   ├── DATABASE-FOR-REFERENCES/   # � Complete database design with future features
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


### Managing Secrets

```bash
# Set connection string
dotnet user-secrets set "ConnectionStrings:SuperAppConnection" "your-connection-string" --project src/SuperApp.API

# List all secrets
dotnet user-secrets list --project src/SuperApp.API

# Remove a secret
dotnet user-secrets remove "ConnectionStrings:SuperAppConnection" --project src/SuperApp.API
```

## 📋 Code Review Checklist

Before submitting a pull request, ensure:

- [ ] Code follows **[Coding Standards](../docs/CODING_STANDARDS.md)**
- [ ] XML documentation used only for complex methods and classes, not simple properties
- [ ] DTOs used instead of domain entities in controllers
- [ ] Input validation implemented (see **[Validation](../docs/VALIDATION.md)**)
- [ ] Appropriate error handling (see **[Error Handling](../docs/ERROR_HANDLING.md)**)
- [ ] Repository inherits from `BaseRepository`
- [ ] No SQL concatenation (SQL injection risk)
- [ ] Async methods properly implemented
- [ ] EF Core for simple CRUD, Stored Procedures for complex queries
