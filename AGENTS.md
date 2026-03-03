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
dotnet ef database update --project Maliev.RegistryService.Data --startup-project Maliev.RegistryService.Api
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
