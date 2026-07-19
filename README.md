# Maliev Registry Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/MALIEV-Co-Ltd/Maliev.RegistryService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)
[![Cache](https://img.shields.io/badge/Cache-Redis%208.4-red)](https://redis.io/)

High-performance registry service for regional reference data and external regulatory lookups.

**Role in MALIEV Architecture**: Acts as the centralized source for regional data (addresses, locations) and provides a unified proxy for external corporate data lookups (DBD). It enables consistent address entry and business verification across all platform services.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Search Engine**: PostgreSQL `pg_trgm` (Trigram similarity ranking)
- **Distributed Cache**: Redis 8.4 (Company lookup caching)
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **No Secrets in Code**: Configuration injected via environment variables.
- ✅ **Conditional Builds**: Uses native `.csproj` conditions for GITHUB_ACTIONS vs local environment.
- ✅ **Flat Project Structure**: All projects reside in the root directory.

---

## ✨ Key Features

- **Thai Address Autocomplete**: Ultra-fast lookup for Thai sub-districts, districts, provinces, and postal codes.
- **Dual-Language Ranking**: Intelligent ranking algorithm prioritizing English matches followed by Thai matches.
- **PostgreSQL Trigram Search**: Leverages `pg_trgm` GIN indices and `similarity()` for high-performance fuzzy matching across ~80,000 records.
- **Live DBD Company Lookup**: Real-time proxy for Thai Department of Business Development (DBD) data via DataForThai.com.
- **Resilient Proxying**: Features live HTML scraping with browser-mimicking headers and circuit breaker patterns.
- **Efficient Caching**: 24-hour Redis caching for external company results to reduce upstream latency and load.
- **Automated Data Seeding**: One-time SQL-based seeding process ensures regional data is populated immediately upon first deployment.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop
- PostgreSQL 18 (Alpine)
- Redis 8.4 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/MALIEV-Co-Ltd/Maliev.RegistryService.git
cd Maliev.RegistryService
```

2. **Run Infrastructure**
```bash
# Using Aspire AppHost (Recommended)
dotnet run --project Maliev.RegistryService.AppHost
```

3. **Database Migration**
```bash
# Data seeding happens automatically on startup if the database is empty
dotnet run --project Maliev.RegistryService.Api
```

The service will be available at `http://localhost:5000/registry`. Access interactive documentation at `http://localhost:5000/registry/scalar`.

---

## 📡 API Endpoints

All endpoints are versioned and prefixed with `/registry/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/thai/addresses/autocomplete` | Search for Thai addresses by name or postal code |
| GET | `/thai/companies/lookup` | Lookup registered Thai companies by Tax ID or Name |

### Permissions
Access to these endpoints requires the `Registry.Read` permission.

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /registry/liveness`
- **Readiness**: `GET /registry/readiness`
- **Aspire Health**: `GET /registry/aspire-liveness`

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18, Redis, and RabbitMQ containers.
- **Dynamic Auth**: Generates fresh RSA-2048 key pairs for each test run to validate JWT policies.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/maliev-website-artifact-dev/maliev-registry-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
