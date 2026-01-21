# Implementation Plan: Maliev.RegistryService

**Branch**: `001-create-registry-service` | **Date**: 2026-01-21 | **Spec**: [specs/001-create-registry-service/spec.md](spec.md)
**Input**: Create a step-by-step implementation plan for `Maliev.RegistryService`.

## Summary

High-performance registry service for Thai addresses and DBD company lookups. The solution uses .NET 10 with a 3-layer architecture, utilizing PostgreSQL `pg_trgm` for trigram-based search across 80k+ records and Redis for caching external company data.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: NPOI (Excel), Npgsql.EntityFrameworkCore.PostgreSQL (Trigrams), StackExchange.Redis, Asp.Versioning, Maliev.Aspire.ServiceDefaults
**Storage**: PostgreSQL 18, Redis 7
**Testing**: xUnit, Testcontainers (PostgreSQL)
**Target Platform**: Docker / Linux
**Project Type**: Microservice (Api, Data, Tests)
**Performance Goals**: <100ms P95 for autocomplete
**Constraints**: High-performance batch seeding from legacy XLS
**Scale/Scope**: ~80,000 address records, thousands of company lookups

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- [x] **Library-First**: Service logic encapsulated in Data/Service layers.
- [x] **Test-First**: Implementation includes xUnit with Testcontainers for integration testing.
- [x] **Observability**: Uses Maliev.Aspire.ServiceDefaults for OpenTelemetry/Logging.

## Project Structure

### Documentation (this feature)

```text
specs/001-create-registry-service/
├── plan.md              # This file
├── research.md          # Technology decisions (NPOI, Trigrams)
├── data-model.md        # Entity definitions and indices
├── quickstart.md        # Setup instructions
├── contracts/           # OpenAPI schema
└── tasks.md             # Pending tasks
```

### Source Code (repository root)

```text
Maliev.RegistryService.slnx
src/
├── Maliev.RegistryService.Api/
│   ├── Controllers/
│   └── Program.cs
├── Maliev.RegistryService.Data/
│   ├── Entities/
│   ├── Context/
│   ├── Services/
│   └── Seeding/
tests/
├── Maliev.RegistryService.Tests/
│   ├── Integration/
│   └── Unit/
```

**Structure Decision**: Standard platform 3-layer microservice structure.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| pg_trgm extension | High-performance partial search | Standard LIKE is too slow for 80k rows without index |
| NPOI | Legacy XLS binary support | ExcelDataReader has limited cell type control for this specific binary format |
| No CLI Interface | Service is a pure high-performance registry proxy | CLI is not required for the initial scope as data management is handled via automated seeding and manual DB maintenance. |

