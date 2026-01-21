# Tasks: Maliev.RegistryService

**Input**: Design documents from `/specs/001-create-registry-service/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Integration tests for search ranking logic and unit tests are requested in the specification and plan.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Paths follow the 3-layer microservice structure defined in plan.md:
  - `src/Maliev.RegistryService.Api/`
  - `src/Maliev.RegistryService.Data/`
  - `tests/Maliev.RegistryService.Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create project structure and Maliev.RegistryService.slnx per implementation plan
- [X] T002 Initialize src/Maliev.RegistryService.Data/ with NPOI and Npgsql.EntityFrameworkCore.PostgreSQL dependencies
- [X] T003 Initialize src/Maliev.RegistryService.Api/ with Asp.Versioning and Maliev.Aspire.ServiceDefaults
- [X] T004 Initialize tests/Maliev.RegistryService.Tests/ with xUnit and Testcontainers dependencies
- [X] T005 [P] Configure linting and formatting tools for the solution

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Setup RegistryDbContext with pg_trgm extension in src/Maliev.RegistryService.Data/Context/RegistryDbContext.cs
- [X] T007 [P] Configure MalievPermissions (Registry.Read) middleware in src/Maliev.RegistryService.Api/Program.cs
- [X] T008 Configure environment variables and Aspire resource linking in src/Maliev.RegistryService.Api/Program.cs
- [X] T009 [P] Implement base API response and error handling in src/Maliev.RegistryService.Api/Infrastructure/
- [X] T010 Setup Testcontainers base class for integration tests in tests/Maliev.RegistryService.Tests/Integration/IntegrationTestBase.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 3 - Data Seeding (Priority: P3)

**Goal**: Load Thai address data from legacy Excel file automatically on deployment.

**Independent Test**: Verify location data exists in the database after service startup with the SeedData file present.

### Tests for User Story 3

- [X] T011 [P] [US3] Create unit tests for Excel parsing and character encoding logic in tests/Maliev.RegistryService.Tests/Unit/Seeding/ExcelParserTests.cs
- [X] T012 [US3] Create integration test to verify database population from sample XLS in tests/Maliev.RegistryService.Tests/Integration/SeedingIntegrationTests.cs

### Implementation for User Story 3

- [X] T013 [P] [US3] Define ThaiLocation entity in src/Maliev.RegistryService.Data/Entities/ThaiLocation.cs
- [X] T014 [US3] Map ThaiLocation to RegistryDbContext with B-Tree and Trigram indices in src/Maliev.RegistryService.Data/Context/RegistryDbContext.cs
- [X] T015 [US3] Implement ThaiLocationSeeder using NPOI for XLS parsing in src/Maliev.RegistryService.Data/Seeding/ThaiLocationSeeder.cs
- [X] T016 [US3] Register and trigger ThaiLocationSeeder on startup in src/Maliev.RegistryService.Api/Program.cs
- [X] T017 [US3] Add skip-if-exists logic to ThaiLocationSeeder.cs to prevent duplication

**Checkpoint**: Data seeding is functional. This is a prerequisite for US1 search testing.

---

## Phase 4: User Story 1 - Thai Address Autocomplete (Priority: P1) 🎯 MVP

**Goal**: Provide high-performance address autocomplete with dual-language support and ranking.

**Independent Test**: Search for "10110" or "Sukhumvit" and receive ranked results within 100ms.

### Tests for User Story 1

- [ ] T018 [P] [US1] Create integration test for autocomplete ranking logic in tests/Maliev.RegistryService.Tests/Integration/AutocompleteRankingTests.cs
- [ ] T019 [P] [US1] Create unit test for language detection logic in tests/Maliev.RegistryService.Tests/Unit/LanguageDetectorTests.cs
- [ ] T020 [US1] Implement a performance/load test to verify SC-001 (<100ms latency) in tests/Maliev.RegistryService.Tests/Performance/AutocompletePerformanceTests.cs

### Implementation for User Story 1

- [X] T021 [P] [US1] Implement IThaiRegistryService with ranking algorithm in src/Maliev.RegistryService.Data/Services/ThaiRegistryService.cs
- [X] T022 [US1] Create LocationsController with GET /registry/v1/thai/addresses/autocomplete in src/Maliev.RegistryService.Api/Controllers/LocationsController.cs
- [X] T023 [US1] Apply Registry.Read permission requirement to LocationsController.cs
- [X] T024 [US1] Implement default limit of 10 results in LocationsController.cs

**Checkpoint**: User Story 1 is functional and testable independently (requires seeded data).

---

## Phase 5: User Story 2 - DBD Company Lookup (Priority: P2)

**Goal**: Proxy DBD company lookups with Tax ID/Name search and 24-hour Redis caching.

**Independent Test**: Provide 13-digit Tax ID and receive company profile from cache or proxy within 2 seconds.

### Tests for User Story 2

- [ ] T025 [P] [US2] Create integration test for DBD proxy with Redis caching in tests/Maliev.RegistryService.Tests/Integration/CompanyLookupTests.cs

### Implementation for User Story 2

- [X] T026 [P] [US2] Define CompanyProfile record/model in src/Maliev.RegistryService.Data/Models/CompanyProfile.cs
- [X] T027 [US2] Implement IDbdProxyService with partial name and Tax ID search logic in src/Maliev.RegistryService.Data/Services/DbdProxyService.cs
- [X] T028 [US2] Add Redis caching (24h) to IDbdProxyService using StackExchange.Redis
- [X] T029 [US2] Create CompaniesController with GET /registry/v1/thai/companies/lookup in src/Maliev.RegistryService.Api/Controllers/CompaniesController.cs
- [X] T030 [US2] Implement circuit breaker for upstream DBD failures in DbdProxyService.cs

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T031 [P] Update Maliev.Intranet.Bff to include RegistryServiceClient
- [X] T032 Add logging and OpenTelemetry signals to all service methods
- [ ] T033 Ensure code coverage exceeds 80% using dotnet-coverage
- [ ] T034 Run quickstart.md validation steps
- [ ] T035 [P] Final documentation cleanup in specs/001-create-registry-service/

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup (Phase 1). BLOCKS all user stories.
- **User Stories**:
  - **US3 (Seeding)**: Highly recommended to complete before testing US1.
  - **US1 (Autocomplete)**: Depends on Foundation and US3 (for data).
  - **US2 (DBD)**: Depends on Foundation and Redis configuration.

### Parallel Opportunities

- T002-T004 can be done in parallel.
- Once Phase 2 is done, US1 and US2 can be implemented in parallel.
- All tests marked [P] can be written in parallel with their implementation counterparts.

---

## Implementation Strategy

### MVP First (User Story 1 & 3)

1. Complete Setup + Foundational.
2. Complete User Story 3 (Data Seeding) to populate the test environment.
3. Complete User Story 1 (Autocomplete) to deliver the core value.
4. **STOP and VALIDATE**: Verify autocomplete performance and ranking.

### Incremental Delivery

1. Foundation + Seeding → Data ready.
2. Autocomplete (US1) → Core search value delivered.
3. Company Lookup (US2) → Regulatory feature added.
4. Polish → Cross-app integration and metrics.
