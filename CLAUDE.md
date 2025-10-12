# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## 📖 Documentation

**All development guidelines, architecture patterns, coding standards, and best practices are documented in:**

**`.github/copilot-instructions.md`**

Please refer to that file for:
- Clean Architecture + CQRS patterns
- Project structure and layer responsibilities
- Database access with stored procedures
- API design and RESTful conventions
- Authentication & Authorization (JWT + OAuth)
- Error handling and logging
- Validation with FluentValidation
- Testing guidelines
- Security best practices
- Code review checklist

---

## Quick Reference

- **Run dev server:** `dotnet run --project SuperAppAPI`
- **Watch mode:** `dotnet watch --project SuperAppAPI`
- **Build:** `dotnet build`
- **Tests:** `dotnet test`
- **Manage secrets:** `dotnet user-secrets set "key" "value" --project SuperAppAPI`

---

**For complete documentation, see:** `.github/copilot-instructions.md`
