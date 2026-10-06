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

- **Run dev server:** `powershell -ExecutionPolicy Bypass -File scripts/run-dev.ps1` (secret qua tung-vault + SSH tunnel DB; `dotnet run` trực tiếp sẽ dừng vì `.env` chỉ có `vault://`)
- **Watch mode:** `... scripts/run-dev.ps1 -Watch` · đổi DB: `-Database SuperApp-test`
- **Build:** `dotnet build`
- **Tests:** `dotnet test`
- **Manage secrets:** `dotnet user-secrets set "key" "value" --project SuperAppAPI`

---

## Time (dates & instants) — task #1450

- **Instants** (`CreatedAt`, `UpdatedAt`, `OccurredAt`, `SrsNextReviewAt`…): `DateTime` in **UTC**
  (`DateTime.UtcNow`; never `DateTime.Now`). EF reads them back as `Kind=Utc`; JSON writes ISO 8601
  with the **user's offset** (e.g. `+07:00`); JSON input without `Z`/offset is rejected (400).
- **Calendar dates** (task/project `StartDate`/`EndDate`, `DailyLog.LogDate`, `DateOfBirth`):
  `DateOnly` / SQL `DATE`, `"YYYY-MM-DD"`.
- "Today" / day boundaries of the user: `UserClock` (`SuperAppModels/Time/UserClock.cs`), never
  `UtcNow.Date`. Parsing strings: `TimeParsing` (query "to" filters: `ParseInstantLenientEnd`).
- SQL defaults: `SYSUTCDATETIME()`, never `GETDATE()`/`SYSDATETIME()` (the server runs at +07).

---

**For complete documentation, see:** `.github/copilot-instructions.md`
