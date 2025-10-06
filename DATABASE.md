# Database Setup Guide

## Overview

This project uses **PostgreSQL 16** with **Entity Framework Core** for data persistence.

---

## Quick Start (Development)

### 1. Start PostgreSQL

```bash
docker-compose up -d
```

This starts PostgreSQL on `localhost:5432` with:
- Database: `mentalhealthbar`
- User: `postgres`
- Password: `postgres`

### 2. Verify Database Connection

```bash
# Check container is healthy
docker-compose ps

# Test API health check (includes database check)
curl http://localhost:5152/health
```

---

## Current Status (Phase 3.1)

⚠️ **No migrations exist yet** - This is intentional!

### Why No Migrations?

Following **TDD and incremental development**:

1. **Phase 3.1** (Current) - Infrastructure setup
   - DbContext created (`AppDbContext.cs`)
   - Connection configured
   - Database health checks working
   - **No domain models yet** = No migrations needed

2. **Phase 3.3** (Upcoming) - Domain Models
   - Add Assessment, MoodEntry, EventLabel, HealthMetric entities
   - Configure EF Core mappings
   - **Then create initial migration**

---

## Database Migrations (Coming in Phase 3.3)

### When Domain Models Are Added

```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project src/MentalHealthBar.Api

# Review the generated migration
# Located at: src/MentalHealthBar.Api/Migrations/

# Apply migration to database
dotnet ef database update --project src/MentalHealthBar.Api

# Verify migration
psql -h localhost -U postgres -d mentalhealthbar -c "\dt"
```

### Migration Strategy

- **One migration per phase** when possible
- **Descriptive names**: `InitialCreate`, `AddEventLabels`, etc.
- **Review before applying**: Always check generated SQL
- **Never edit applied migrations**: Create new ones instead
- **Production deployments**: Use `dotnet ef migrations script` for SQL generation

---

## Database Schema (Planned)

Based on `specs/001-build-an-application/data-model.md`:

### Entities

1. **Assessment** - PHQ-9 questionnaire responses
   - Id (Guid), UserId, CreatedAt, Severity, etc.

2. **MoodEntry** - Daily mood tracking
   - Id (Guid), UserId, RecordedAt, MoodLevel, Notes

3. **EventLabel** - User-defined activity tags
   - Id (Guid), UserId, Name, Color, Category

4. **HealthMetric** - Sleep, exercise, medication tracking
   - Id (Guid), UserId, RecordedAt, Type, Value, Unit

### Relationships

- All entities belong to a User (soft ownership, no User table yet)
- MoodEntry ↔ EventLabel (many-to-many)
- Assessment references related MoodEntry records

---

## Troubleshooting

### Connection Issues

```bash
# Check PostgreSQL logs
docker-compose logs postgres

# Restart database
docker-compose down
docker-compose up -d
```

### Reset Database (Development Only)

```bash
# ⚠️ WARNING: Destroys all data
docker-compose down -v
docker-compose up -d
```

### EF Core Tools

```bash
# Install/Update EF Core CLI
dotnet tool update --global dotnet-ef

# Verify installation
dotnet ef --version
```

---

## Production Configuration

**NEVER use development credentials in production!**

### Environment Variables

Set these in your production environment:

```bash
CONNECTIONSTRINGS__DEFAULTCONNECTION="Host=prod-db;Port=5432;Database=mentalhealthbar;Username=prod_user;Password=SECURE_PASSWORD;SSL Mode=Require"
```

### appsettings.Production.json

Already configured with production template:
- SSL Mode=Require
- Placeholder credentials (replace before deployment)
- See `src/MentalHealthBar.Api/appsettings.Production.json`

---

**Status**: Phase 3.1 Complete - Ready for Phase 3.3 (Domain Models + Migrations)
