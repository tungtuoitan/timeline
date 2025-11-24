# SuperApp Backend - Coding Standards

## Table of Contents

- [SuperApp Backend - Coding Standards](#superapp-backend---coding-standards)
  - [Table of Contents](#table-of-contents)
  - [General Principles](#general-principles)
    - [Clean Code](#clean-code)
    - [Code Style](#code-style)
    - [Language Features](#language-features)
  - [Naming Conventions](#naming-conventions)
  - [Code Organization](#code-organization)
    - [File Structure](#file-structure)
    - [Class Organization](#class-organization)
  - [Error Handling](#error-handling)
  - [Documentation Standards](#documentation-standards)
  - [Models vs DTOs](#models-vs-dtos)
  - [Database Access](#database-access)
  - [Performance Guidelines](#performance-guidelines)
  - [Quick Reference](#quick-reference)
    - [Naming](#naming)

## General Principles

### Clean Code

- SOLID, DRY, KISS, YAGNI.

### Code Style

- Indent: 4 spaces.
- PascalCase public, camelCase private/params, SCREAMING_SNAKE_CASE constants.
- Line max: 120 chars.
- Always braces.

### Language Features

- Nullable types (`string?`).
- Explicit types over var if unclear.
- Expression-bodied for simple.
- async/await over .Result.
- using for IDisposable.

## Naming Conventions

- Classes/Interfaces: PascalCase, I prefix (NoteRepository, INoteRepository).
- Methods: PascalCase, Async suffix (GetNoteByIdAsync).
- Properties: PascalCase (Name).
- Fields: _camelCase (_logger).
- Constants: PascalCase (MAX_RETRY_ATTEMPTS).
- Enums: PascalCase (NoteStatus.Draft).
- Booleans: is/has/can/was (IsActive).

## Code Organization

### File Structure

```
SuperApp.Application/
├── Features/Notes/Commands/CreateNote/ (Command.cs, Handler.cs, Validator.cs)
└── Common/ (Mappings, Exceptions, Interfaces)
```

### Class Organization

1. Constants
2. Statics
3. Privates
4. Ctor
5. Public props
6. Public methods
7. Private methods

## Error Handling

- Specific exceptions with messages (ArgumentException, NotFoundException).
- Structured logging (_logger.LogError(ex, "Failed {Op}", op)).

## Documentation Standards

- <summary> for complex classes/methods, APIs, controllers.
- Skip for simple props/DTOs, basic CRUD, privates.

Examples:
- Complex: /// <summary>Processes payment...</summary>
- Simple: public string Email { get; set; } (no doc)

## Models vs DTOs

- Models: SuperAppModels\Models\, logic, DB mapping, relationships, internal.
  Example: Tag with Update/ToggleActive.

- DTOs: SuperAppModels\DTOs\Requests/Responses\, data only, flat, validation, API safe.
  Examples: CreateTagRequest ([Required]), TagResponse (no sensitive).

| Aspect | Model | DTO |
|--------|-------|-----|
| Purpose | Business | Transfer |
| Logic | Yes | No |
| Security | Sensitive | Filtered |

- Use Models: Repos/services, DB, logic.
- Use DTOs: Controllers, layers, hide data.
- Map with AutoMapper in handlers.
- Best: No model exposure in APIs, specific DTOs per op, flat, validation on requests.

## Database Access

- Hybrid: EF Core 80% (simple CRUD), SPs 20% (complex agg/recursive/report/bulk).
- Repos: Inherit BaseRepository (EF + SP).
- EF: Include eager, AsNoTracking read-only, project DTOs, pagination.
- SP: ExecuteStoredProcedure for complex.
- Config: Fluent API separate (keys, props, rels, indexes, filters).
- Safety: Parameterized only.
- Performance: Avoid N+1, SplitQuery.

| Use | Tech | Ex |
|-----|------|----|
| CRUD | EF | FindAsync |
| Complex | SP | usp_get_stats |


## Performance Guidelines

- Async: await, no .Result.
- Memory: using streams, StringBuilder concat.

## Quick Reference

### Naming

| Element | Conv | Ex |
|---------|------|----|
| Class | Pascal | NoteRepository |
| Method | PascalAsync | GetNotesAsync |
| Prop | Pascal | NoteId |
| Field | _camel | _logger |
| Const | Pascal | MaxRetryAttempts |
| Bool | is/has | IsActive |

- Class: <500 lines, Method: <50, Params: <4.