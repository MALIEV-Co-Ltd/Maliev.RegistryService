# Quickstart: Maliev.RegistryService

## Prerequisites
- .NET 10 SDK
- Docker (for PostgreSQL 18 and Redis 7)
- `SeedData/thai_locations.xls` must be present in the root directory.

## Local Setup

1. **Clone & Restore**
   ```bash
   dotnet restore
   ```

2. **Start Infrastructure**
   The project uses Aspire. Start the AppHost:
   ```bash
   dotnet run --project Maliev.RegistryService.AppHost
   ```

3. **Seeding**
   The first time the service starts, it will automatically seed data from the Excel file if the database is empty. Monitor logs for `ThaiLocationSeeder` completion.

## API Endpoints

### Address Autocomplete
- **GET** `/registry/v1/thai/addresses/autocomplete?query={search}&limit={limit}`
- **Default Limit**: 10

### Company Lookup
- **GET** `/registry/v1/thai/companies/lookup?query={search}&limit={limit}`
- **Search Type**: Automatically detects Tax ID (13 digits) or partial Name match.

## Testing
```bash
dotnet test
```
