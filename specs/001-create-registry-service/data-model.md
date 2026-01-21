# Data Model: Maliev.RegistryService

## Entities

### ThaiLocation
Represents a geographical unit in Thailand.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | int | PK, Identity | Unique identifier |
| PostalCode | string | Max 10, Indexed (B-Tree) | Thai Postal Code |
| SubDistrictTh | string | Max 200, Indexed (GIN) | Sub-district name in Thai |
| DistrictTh | string | Max 200, Indexed (GIN) | District name in Thai |
| ProvinceTh | string | Max 200, Indexed (GIN) | Province name in Thai |
| SubDistrictEn | string | Max 200, Indexed (GIN) | Sub-district name in English |
| DistrictEn | string | Max 200, Indexed (GIN) | District name in English |
| ProvinceEn | string | Max 200, Indexed (GIN) | Province name in English |

**Indices**:
- B-Tree on `PostalCode`.
- GIN (Trigram) on all `*Th` and `*En` fields for fast partial matching.

### CompanyProfile (External/Cache)
Representation of DBD company data.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| TaxId | string | 13 digits, Unique | Thai Tax Identification Number |
| NameTh | string | Max 500 | Legal name in Thai |
| NameEn | string | Max 500 | Legal name in English |
| Status | string | - | Registration status (e.g., Active) |

## Relationships
- None (Registry service is currently a flat reference data store).
