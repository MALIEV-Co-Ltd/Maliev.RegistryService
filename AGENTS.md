# AGENTS.md

This file contains instructions for AI agents (and human developers) working on the Maliev.RegistryService repository.

## Project Overview
- **Framework**: .NET 10.0 (C#)
- **Type**: ASP.NET Core Web API Microservice
- **Architecture**: Layered (Api, Application, Domain, Infrastructure, Tests)
- **Infrastructure**: .NET Aspire Service Defaults, Entity Framework Core (PostgreSQL), MassTransit (RabbitMQ), Redis

## Build, Run, and Test Commands

All commands run from within this service directory (`B:\maliev\Maliev.RegistryService`).

### Build
```powershell
dotnet build Maliev.RegistryService.slnx
```
Note: `<TreatWarningsAsErrors>` is enabled. Fix all warnings.

### Run
```powershell
dotnet run --project Maliev.RegistryService.Api
```

### Test
Run all tests:
```powershell
dotnet test Maliev.RegistryService.slnx --verbosity normal
```

Run a single test method:
```powershell
dotnet test --filter "FullyQualifiedName~Maliev.RegistryService.Tests.Unit.ThaiRegistryServiceTests.GetByIdAsync_WhenExists_ReturnsLocation"
```

Run all tests in a class:
```powershell
dotnet test --filter "FullyQualifiedName~ThaiRegistryServiceTests"
```

Run with code coverage:
```powershell
dotnet test Maliev.RegistryService.slnx --collect:"XPlat Code Coverage"
```

### Format Check
```powershell
dotnet format Maliev.RegistryService.slnx
```

### Database Operations
The application attempts to migrate on startup (`app.MigrateDatabaseAsync`).
To run migrations manually (requires `dotnet-ef` tool):
```powershell
dotnet ef migrations add <Name> --project Maliev.RegistryService.Infrastructure --startup-project Maliev.RegistryService.Infrastructure
dotnet ef database update --project Maliev.RegistryService.Infrastructure --startup-project Maliev.RegistryService.Infrastructure
```

## Code Style & Conventions

### C# Naming & Formatting
- **Namespaces**: File-scoped (`namespace Maliev.RegistryService.Api.Controllers;`)
- **Classes/Methods/Properties**: `PascalCase`
- **Private fields**: `_camelCase` (underscore prefix)
- **Parameters/locals**: `camelCase`
- **Async methods**: Suffix with `Async` (e.g., `GetLocationAsync`)
- **Interfaces**: Prefix with `I` (e.g., `IThaiRegistryService`)
- **Permissions**: GCP-style `{domain}.{plural-resource}.{action}` as `public const string` in a `Permissions` static class
  - Valid: `registry.companies.create`, `registry.locations.read`
  - Invalid: `registry.company.create` (singular), `registry.create` (missing resource)
- **XML docs**: Required on ALL public methods and properties
- **Nullable**: Enabled (`<Nullable>enable</Nullable>`). Use `?` explicitly
- **Imports**: System first, then third-party, then local. Alphabetize within groups. Remove unused `using`
- **Braces**: Allman style (new line) for methods and control structures. Expression-bodied for properties/accessors
- **Indentation**: 4 spaces, LF line endings, UTF-8, trim trailing whitespace

### C# Patterns
- **DI**: Constructor injection with `private readonly` fields
- **Controllers**: `[ApiController]`, `[ApiVersion("1")]`, `[Route("registry/v{version:apiVersion}")]`
- **Logging**: `ILogger<T>` with structured placeholders (never interpolate): `_logger.LogInformation("Processing {CompanyId}", companyId)`
- **Error handling**: Global exception middleware. Return `ProblemDetails` / `ErrorResponse` DTOs. Never expose stack traces
- **Response Wrapper**: Use `ApiResponse<T>` for consistent API responses
  - Success: `return Ok(ApiResponse<T>.CreateSuccess(data));`
  - Error: `return BadRequest(ApiResponse<T>.CreateError("Message"));`
- **Configuration**: Use `appsettings.json` and Options pattern. Secrets loaded via `AddGoogleSecretManagerVolume` or environment variables
- **Manual mapping**: Static extension methods (`ToDto()`, `ToEntity()`). AutoMapper is banned
- **Validation**: `System.ComponentModel.DataAnnotations` on DTOs. FluentValidation is banned

## Banned Libraries (Build Will Fail)

| Banned | Use Instead |
|--------|-------------|
| AutoMapper | Manual mapping extensions |
| FluentValidation | DataAnnotations or manual validation |
| FluentAssertions | Standard xUnit `Assert.*` |
| Swashbuckle/Swagger | Scalar (at `/registry/scalar`) |
| InMemoryDatabase (EF Core) | Testcontainers with real PostgreSQL |

## Testing Rules

- **Framework**: xUnit with standard `Assert` (`Assert.Equal`, `Assert.NotNull`, etc.)
- **Naming**: `MethodName_StateUnderTest_ExpectedBehavior` or `HTTP_METHOD_Path_Scenario_ExpectedStatus`
- **Coverage**: Minimum 80% per service
- **Integration tests**: `BaseIntegrationTestFactory<TProgram, TDbContext>` with Testcontainers (PostgreSQL, Redis, RabbitMQ). Never InMemoryDatabase
- **System tests** (Tier 3): `AspireTestFixture` with `[Collection("AspireDomainTests")]` — shared AppHost, never one per class
- **Eventual consistency**: Use `TestHelpers.WaitForAsync`. Never `Task.Delay`
- **MassTransit consumers**: Must have consumer tests using `AddMassTransitTestHarness()`

### Testing Strategy (4-Tier Pyramid Context)

This service's tests cover **Tier 1 (Unit)** and **Tier 2 (Service Integration)** of the Maliev testing pyramid:

| Tier | What to Test | Infrastructure |
|------|-------------|---------------|
| **Unit** | Business logic, domain models, service methods with mocked dependencies | None (mocks only) |
| **Service Integration** | API endpoints, database persistence, permission enforcement, input validation | `BaseIntegrationTestFactory` + Testcontainers (Postgres/Redis/RabbitMQ) |

**Tier 3 (System Integration)** — cross-service workflows and event chains — is tested in `Maliev.Aspire.Tests/`.

#### Key Rules
- Use `BaseIntegrationTestFactory<TProgram, TDbContext>` for integration tests (real Testcontainers, never InMemoryDatabase)
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Minimum 80% code coverage
- Use `[Fact]` for single cases, `[Theory]` for parameterized tests

> Full ecosystem test strategy: `Maliev.Aspire.Tests/TEST_PLAN.md`

## Mandatory Rules

- **`TreatWarningsAsErrors = true`**: Zero warnings allowed. No suppression
- **`[RequirePermission("domain.resources.action")]`**: On all endpoints, not plain `[Authorize]`
- **API versioning**: All routes versioned (`v1/`)
- **Service prefix**: Routes prefixed with service domain (`/registry`)
- **Scalar docs**: Configured at `/registry/scalar`
- **Secrets**: Never hardcoded. Use GCP Secret Manager or environment variables
- **Async/await**: All the way down. Pass `CancellationToken`
- **EF Core Design package**: Only in Infrastructure project, never in Api
- **PostgreSQL xmin**: Shadow property only — `entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion()`. Never add entity property
- **Temporary files**: Generate in `/temp` folder, clean up afterwards

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

## Git Rules

- Each `Maliev.*` folder is an independent git repo. `cd` into it before git commands
- **Commit early and often** after every meaningful unit of work. Do not accumulate changes
- **Never use `git checkout` to restore files** — commit first, then `git revert` or `git reset --soft`
- Feature branches merged to `develop` via PR. Do not push without being asked

## Database & EF Core — Mandatory Rules

### EF Core Design Package
- `Microsoft.EntityFrameworkCore.Design` MUST NOT be in Api projects
- It belongs ONLY in the Infrastructure project where migrations live
- Migration commands must target Infrastructure as both project and startup-project:
  ```powershell
  dotnet ef migrations add <Name> --project Maliev.RegistryService.Infrastructure --startup-project Maliev.RegistryService.Infrastructure
  ```

### PostgreSQL xmin Concurrency — Mandatory Pattern
Use shadow property ONLY. Never add a Xmin/xmin property to domain entities.
```csharp
entity.Property<uint>("xmin").HasColumnType("xid").IsRowVersion();
```
- Never use `UseXminAsConcurrencyToken()` (removed in Npgsql EF v7)
- Never use entity property `public uint Xmin { get; set; }` or `public uint xmin { get; set; }`
- Never use `.Ignore(e => e.Xmin)` — remove the entity property instead
