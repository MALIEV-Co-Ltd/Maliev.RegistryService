# Research: Maliev.RegistryService Implementation

## Decisions & Rationale

### 1. Excel Parsing with NPOI
- **Decision**: Use NPOI for reading the legacy `.xls` (HSSF) binary format.
- **Rationale**: NPOI is the most robust and widely used .NET library for handling legacy Excel binary formats (`.xls`) without requiring Microsoft Office installed. It supports batch reading which is essential for the 80k+ records seeding requirement.
- **Alternatives**: ExcelDataReader (lighter but NPOI offers better control over cell types and legacy encoding).

### 2. Trigram Search in PostgreSQL
- **Decision**: Enable `pg_trgm` extension and use GIN indices for Thai/English name fields.
- **Rationale**: Trigram indices provide high-performance `LIKE` and `%` matching across large text datasets, which is required for the "partial matching" (Contains) requirement.
- **Alternatives**: Full-Text Search (FTS) with `tsvector`. While powerful, FTS is less intuitive for partial "middle-of-word" matching than trigrams for Thai names.

### 3. Ranking Algorithm (Dual-Language)
- **Decision**: Implement a weighted `ORDER BY` using `similarity()` scores from `pg_trgm` combined with priority flags (Postal Code first, then English matches, then Thai).
- **Rationale**: Meets the requirement: "Detect language... prioritization English matches, then Thai". PostgreSQL `similarity()` allows quantifying match quality.

### 4. DBD Proxy Caching
- **Decision**: Use `StackExchange.Redis` for 24-hour result caching of Company Profiles.
- **Rationale**: Reduces upstream load and meets the "high-performance" goal for regulatory lookups.

### 5. Access Control Integration
- **Decision**: Integrate with `MalievPermissions` using standard platform middleware.
- **Rationale**: Ensures the "Registry.Read" requirement is enforced consistently with other Maliev services.

## Needs Clarification (Resolved via Specification)
- **Mixed Language**: Intersect (AND) logic confirmed.
- **Seeding**: One-time skip-if-exists logic confirmed.
- **Access**: Role-based (Registry.Read) confirmed.
