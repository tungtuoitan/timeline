# Entity Framework Core Guide

## Table of Contents

- [Entity Framework Core Guide](#entity-framework-core-guide)
  - [Table of Contents](#table-of-contents)
  - [Overview](#overview)
  - [Setup \& Configuration](#setup--configuration)
  - [DbContext](#dbcontext)
  - [Entity Configurations](#entity-configurations)
  - [Migrations](#migrations)
  - [CRUD Operations](#crud-operations)
  - [Querying Patterns](#querying-patterns)
  - [Relationships \& Navigation Properties](#relationships--navigation-properties)
  - [Performance Optimization](#performance-optimization)
  - [Best Practices](#best-practices)
  - [Common Patterns](#common-patterns)

## Overview

EF Core: Object-database mapper for .NET, supports LINQ, change tracking, migrations.

Benefits for SuperApp: Type safety, productivity, LINQ queries.

Database: 10 tables (users, tags, entity_types, workspaces, workspace_members, workspace_relationship_types, workspace_items, notes, note_members, note_versions).

## Setup & Configuration

Install packages: Microsoft.EntityFrameworkCore, .SqlServer, .Tools, .Design.

Connection strings in appsettings.json / user secrets.

Register DbContext in Program.cs:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions => { /* retries, timeout */ });
    if (Development) { options.EnableSensitiveDataLogging(); }
});
```

## DbContext

ApplicationDbContext: DbSets for all entities.

OnModelCreating: Apply configurations from assembly.

Override SaveChangesAsync: Update timestamps for ITimestampEntity.

## Entity Configurations

Use Fluent API in separate classes.

Example: UserConfiguration - Table, PK, properties, indexes, query filter (soft delete), relationships.

Example: NoteConfiguration - Similar, with navigation to User, Members, Versions.

Example: WorkspaceItemConfiguration - Unified table for tag/note relationships, unique constraints.

## Migrations

Add: dotnet ef migrations add Name --project Infrastructure --startup-project API

Update: dotnet ef database update

Script: dotnet ef migrations script

Remove: dotnet ef migrations remove

Scaffold: dotnet ef dbcontext scaffold "conn" Microsoft.EntityFrameworkCore.SqlServer ...

## CRUD Operations

Create: _context.Notes.Add(note); await SaveChangesAsync();

With related: Add members in loop.

Bulk: AddRange.

Read: FindAsync(id); Where().ToListAsync(); Include() for eager loading; AsNoTracking(); Select(DTO); Pagination with Skip/Take.

Update: Update(note); or modify properties; ExecuteUpdate for bulk (EF7+).

Delete: Remove(note); Soft: set DeletedAt; ExecuteDelete for bulk.

## Querying Patterns

Dynamic: Build query with if conditions for filters/sorting.

GroupBy: Select aggregates.

Any/Exists: AnyAsync().

Raw SQL: FromSqlRaw().

## Relationships & Navigation Properties

One-to-Many: User.Notes, Note.User.

Many-to-Many: Note.Members, with junction NoteMember.

Self-Referencing: Tag.Parent/Children.

Query: Include/ThenInclude.

## Performance Optimization

AsNoTracking for read-only.

Project to DTOs.

Pagination.

Avoid N+1: Use Include.

Compiled Queries: EF.CompileAsyncQuery.

SplitQuery for multiple includes.

## Best Practices

Use async methods.

DI for DbContext.

Transactions for multi-ops.

Soft deletes with query filters.

Value converters for enums.

## Common Patterns

Repository: Implements EF ops + SPs.

Unit of Work: Wraps repositories, handles transactions.