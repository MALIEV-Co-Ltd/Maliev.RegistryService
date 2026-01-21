# Feature Specification: Maliev.RegistryService

**Feature Branch**: `001-create-registry-service`  
**Created**: 2026-01-21  
**Status**: Draft  
**Input**: Create a high-performance registry for Thai Address Autocomplete and DBD Company lookups using .NET 10, PostgreSQL, and Redis.

## Clarifications

### Session 2026-01-21
- Q: Mixed language search behavior → A: Intersect (AND) logic.
- Q: Access control (Public vs Authenticated) → A: Role-Based (Registry.Read) via MalievPermissions.
- Q: Data refresh strategy → A: One-time Seed.
- Q: DBD company name search precision → A: Partial (Contains) matching.
- Q: Autocomplete default result limit → A: 10 results.
- Q: Data seeding existing data behavior → A: Skip if data exists.
- Q: DBD proxy caching strategy → A: Cache for 24 hours in Redis.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Thai Address Autocomplete (Priority: P1)

As a user entering a shipping or billing address, I want to quickly find my sub-district, district, province, and postal code by typing any part of the address so that I can complete forms accurately and quickly.

**Why this priority**: Core functionality of the registry service; provides immediate value to any application requiring Thai address entry.

**Independent Test**: Can be tested via a search query (e.g., "10110" or "Sukhumvit") and verifying that a list of matching Thai/English address objects is returned within milliseconds.

**Acceptance Scenarios**:

1. **Given** the Thai location data is seeded, **When** I search for a postal code "10110", **Then** I receive address suggestions where the postal code starts with "10110", prioritized first.
2. **Given** a search query in English (e.g., "Bangkok"), **When** I request autocomplete, **Then** I receive matches where the province, district, or sub-district name in English matches the query.
3. **Given** a search query in Thai, **When** I request autocomplete, **Then** I receive matches where the Thai name fields match the query.

---

### User Story 2 - DBD Company Lookup (Priority: P2)

As a business analyst or compliance officer, I want to look up Thai company details using either a Tax ID or a Company Name so that I can verify business registrations.

**Why this priority**: High-value regulatory feature for regional business operations.

**Independent Test**: Can be tested by providing a valid 13-digit Tax ID and receiving the corresponding company profile.

**Acceptance Scenarios**:

1. **Given** a 13-digit numeric query, **When** I search for a company, **Then** the system prioritizes lookup by Tax ID.
2. **Given** a partial company name in Thai or English, **When** I search, **Then** the system returns a list of matching registered companies.
3. **Given** the upstream DBD service is unavailable, **When** I search, **Then** I receive a clear "Service Temporarily Unavailable" message rather than a generic system error.

---

### User Story 3 - Data Seeding (Priority: P3)

As a system administrator, I want the Thai address data to be automatically loaded from the legacy Excel file so that the service is ready for use immediately after deployment.

**Why this priority**: Essential for the service to function, but primarily an internal operational task.

**Independent Test**: Can be verified by checking if the location data exists and is accessible after the initial system startup.

**Acceptance Scenarios**:

1. **Given** the `thai_locations.xls` file exists in the SeedData directory, **When** the seeder runs, **Then** all valid records are imported into the database.
2. **Given** the seeder has already run (data exists), **When** the system restarts, **Then** it skips the seeding process to prevent duplication.

---

### Edge Cases

- **Mixed Language Queries**: How does the system handle a query like "Bangkok สุขุมวิท"? 
  - *Decision*: Treat as a multi-term search where both terms must match (AND logic).
- **Upstream DBD Failures**: The DBD proxy relies on an external service.
  - *Assumption*: The system should implement a timeout and circuit breaker to prevent cascading failures.
- **Malformed Excel Data**: The legacy `.xls` file might contain empty or invalid rows.
  - *Assumption*: The seeder skips invalid rows and logs warnings rather than failing the entire batch.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a REST API for Thai address autocomplete that supports English, Thai, and numeric (postal code) inputs. If no limit is specified, it MUST default to 10 results.
- **FR-002**: System MUST prioritize Postal Code matches when the input is numeric.
- **FR-003**: System MUST provide a proxy endpoint for Thailand Department of Business Development (DBD) company lookups and cache results for 24 hours.
- **FR-004**: System MUST support searching for companies by 13-digit Tax ID or partial Name (contains match).
- **FR-005**: System MUST implement a high-performance batch seeder to import address data from Excel.
- **FR-006**: System MUST enforce Role-Based Access Control (RBAC) via MalievPermissions (Registry.Read).
- **FR-007**: System MUST implement a one-time data seed migration.

### Key Entities *(include if feature involves data)*

- **ThaiLocation**: Represents a unique geographical unit in Thailand. Includes Postal Code, Sub-district (TH/EN), District (TH/EN), and Province (TH/EN).
- **CompanyProfile**: A representation of a Thai registered entity, typically containing Tax ID, Name (TH/EN), and registration status.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Address autocomplete results return in under 100ms for 95% of requests.
- **SC-002**: Data seeding of the provided dataset completes in under 30 seconds.
- **SC-003**: DBD lookup results are returned within 2 seconds (excluding upstream latency).
- **SC-004**: System achieves >80% code coverage across all layers.
