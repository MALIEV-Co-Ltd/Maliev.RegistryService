# AGENTS.md

This file contains instructions for AI agents (and human developers) working on the Maliev.RegistryService repository.

## Project Overview
- **Framework**: .NET 10.0 (C#)
- **Type**: ASP.NET Core Web API Microservice
- **Architecture**: Layered (Api, Data, Tests)
- **Infrastructure**: .NET Aspire Service Defaults, Entity Framework Core (PostgreSQL), MassTransit (RabbitMQ), Redis

## Build, Run, and Test Commands

### Build
```bash
dotnet build
```
Note: `<TreatWarningsAsErrors>` is enabled. Fix all warnings.

### Run
```bash
dotnet run --project Maliev.RegistryService.Api
```

### Test
Run all tests:
```bash
dotnet test
```

Run a single test (Example):
```bash
dotnet test --filter "FullyQualifiedName~Maliev.RegistryService.Tests.Unit.ThaiRegistryServiceTests.GetByIdAsync_WhenExists_ReturnsLocation"
```
Or by display name (simpler):
```bash
dotnet test --filter "DisplayName~GetByIdAsync"
```

### Database Operations
The application attempts to migrate on startup (`app.MigrateDatabaseAsync`).
To run migrations manually (requires `dotnet-ef` tool):
```bash
dotnet ef database update --project Maliev.RegistryService.Infrastructure --startup-project Maliev.RegistryService.Infrastructure
```

## Code Style & Conventions

### General
- **Indentation**: 4 spaces.
- **Namespaces**: Use file-scoped namespaces (e.g., `namespace Maliev.RegistryService.Api.Controllers;`).
- **Var**: Use `var` when the type is obvious from the right-hand side.
- **Async**: Use `async`/`await` for all I/O operations. Avoid `GetAwaiter().GetResult()`.

### Naming
- **Classes/Methods**: PascalCase (e.g., `CompaniesController`, `Search`).
- **Interfaces**: I-prefix PascalCase (e.g., `IDbdProxyService`).
- **Private Fields**: `_camelCase` (e.g., `private readonly ILogger _logger;`).
- **Parameters/Locals**: camelCase.

### Architecture Patterns
- **Dependency Injection**: Heavy use of Constructor Injection. Register services in `Program.cs`.
- **Controllers**: Thin controllers. Delegate logic to Services.
- **Response Wrapper**: Use `ApiResponse<T>` for consistent API responses.
  - Success: `return Ok(ApiResponse<T>.CreateSuccess(data));`
  - Error: `return BadRequest(ApiResponse<T>.CreateError("Message"));`
- **Configuration**: Use `appsettings.json` and Options pattern. Secrets loaded via `AddGoogleSecretManagerVolume` or environment variables.

### Error Handling
- Use global exception handling where possible, but catch specific exceptions in Controllers for meaningful 4xx/5xx responses.
- Log exceptions using `ILogger`.

### Testing Guidelines
- **Framework**: xUnit.
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior`.
- **Database**: Use **Testcontainers** (PostgreSQL) for integration tests. No InMemoryDatabase (banned by constitution).
- **Assertions**: Use `Assert` class (e.g., `Assert.NotNull`, `Assert.Equal`).

## Key Dependencies
- `Microsoft.EntityFrameworkCore` & `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Maliev.Aspire.ServiceDefaults` (Shared infrastructure)
- `MassTransit`
- `xunit`

## Checklist for Changes
1. [ ] Code compiles with no warnings.
2. [ ] New logic is covered by unit tests.
3. [ ] Existing tests pass.
4. [ ] Public APIs are documented with XML comments (`///`).


## Git & Version Control — Mandatory Rules

### 🚨 CRITICAL: Always Commit Code Changes (Non-Negotiable)
- **You MUST commit your changes to the local repository after completing any meaningful unit of work.**
- **Never accumulate uncommitted changes.** Do not wait until end of session or until something breaks.
- **Commit early and often** — if a change is meaningful (even a small fix or refactor), commit it.
- **You do NOT need to push to remote** — local commits are sufficient to protect against accidental loss.
- **If you are unsure whether to commit, commit anyway.** Extra commits are harmless; lost work is irreversible.
- This rule applies even if you are just "testing" or "exploring" — use git branches to isolate experimental work and commit those changes too.

### 🚨 CRITICAL: Never Use `git checkout` to Restore Broken Files
- **NEVER use `git checkout` to restore or recover files.** This operation discards uncommitted changes permanently and will result in data loss.
- **To undo/recover from broken files: first commit your current changes, then use `git revert` or `git reset --soft` to safely undo.**

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- ❌ `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- ✅ It belongs ONLY in the Infrastructure (or Data) project where migrations live
- Migration commands must target Infrastructure as both project and startup-project (since EF Core Design package is in Infrastructure):
  ```
  dotnet ef migrations add <Name> --project Maliev.<Domain>Service.Infrastructure --startup-project Maliev.<Domain>Service.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- ❌ Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- ❌ Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- ❌ Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
