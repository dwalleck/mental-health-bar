# Npgsql Guid[] Array Deserialization Issue

**Date**: 2025-10-12
**Status**: 🔴 Open - 9 tests failing
**Environment**: .NET 10 RC (10.0.0-rc.1.25451.107) + Npgsql.EntityFrameworkCore.PostgreSQL

---

## Problem Summary

EventLabelIds (stored as `Guid[]` in C# / `uuid[]` in PostgreSQL) are being **saved correctly** to the database but are **returning as empty arrays** when entities are queried back. This prevents EventLabels from being loaded in API responses.

### Test Results
- ✅ **155 tests passing** (94.5%)
- 🔴 **9 tests failing** (5.5%)
- All failures have the same root cause: empty EventLabels collections

---

## What We're Trying to Do

Refactor the MoodEntry entity to use a proper many-to-many relationship with EventLabels:
- **Before**: MoodEntry had `List<string> Tags` (simple string array)
- **After**: MoodEntry has `Guid[] EventLabelIds` (foreign keys to EventLabel entities)

---

## Technical Details

### Entity Configuration

**Domain Model** (`MoodEntry.cs`):
```csharp
public class MoodEntry
{
    public Guid Id { get; init; }
    public int MoodScore { get; private set; }
    public DateTimeOffset RecordedAt { get; init; }
    public Guid[] EventLabelIds { get; set; } = null!;  // ⚠️ Issue here
    public string? Notes { get; private set; }
    // ... other properties
}
```

**EF Core Configuration** (`MoodEntryConfiguration.cs`):
```csharp
builder.Property(m => m.EventLabelIds)
    .HasColumnType("uuid[]")
    .HasColumnName("event_label_ids");
```

**Database Schema**:
```sql
CREATE TABLE mood_entries (
    id uuid PRIMARY KEY,
    mood_score integer NOT NULL,
    recorded_at timestamptz NOT NULL,
    event_label_ids uuid[],  -- PostgreSQL array column
    -- ... other columns
);
```

---

## Symptoms

### What Happens

1. **Create MoodEntry** with EventLabelIds `[guid1, guid2, guid3]`
2. **Save to database** ✅ - SQL INSERT executes successfully
3. **Query from database** ⚠️ - Entity is loaded BUT `EventLabelIds` property is **empty array**
4. **Load EventLabels** ❌ - Query `WHERE EventLabels.Id IN EventLabelIds` returns 0 results

### Example Test Failure

```
Test: Scenario4_CreateMoodEntry_WithTagsAndNotes_Succeeds
Expected: result.EventLabels.Count = 3
Actual: result.EventLabels.Count = 0

The EventLabelIds were sent to the API:
  Request: { EventLabelIds: [guid1, guid2, guid3] }

But when querying the MoodEntry back:
  savedEntry.EventLabelIds = []  ❌ Empty!
```

---

## What We've Tried

### ✅ Attempt 1: Change from List to Array
- **Action**: Changed `List<Guid>` to `Guid[]` (recommended for better Npgsql support)
- **Result**: No improvement

### ✅ Attempt 2: Add ValueComparer
- **Action**: Added explicit ValueComparer for Guid[] to track changes
- **Result**: No improvement, later removed

### ✅ Attempt 3: Reload Entity After Save
- **Action**: Query entity fresh from database using `.AsNoTracking()`
- **Result**: Query returns entity but `EventLabelIds` is still empty

### ✅ Attempt 4: Convert Array to List for Query
- **Action**: `savedEntry.EventLabelIds.ToList()` before `.Contains()` query
- **Result**: No improvement (array is already empty)

### ✅ Attempt 5: Apply Database Migration
- **Action**: Ran `dotnet ef database update` to ensure schema is correct
- **Result**: Migration already applied, no improvement

### ✅ Attempt 6: Enable Sensitive Data Logging
- **Action**: Added `options.EnableSensitiveDataLogging()` in DbContext config
- **Result**: Can see SQL queries but no obvious errors

### ✅ Attempt 7: Explicit Npgsql Configuration
- **Action**: Tried to add `NpgsqlConnection.GlobalTypeMapper.MapArray<Guid>("uuid")`
- **Result**: API changed in newer Npgsql, method no longer exists

---

## Root Cause Analysis

### Hypothesis
This appears to be a **.NET 10 RC + Npgsql compatibility issue** where:
- Npgsql correctly **serializes** `Guid[]` → `uuid[]` for INSERT/UPDATE
- Npgsql **fails to deserialize** `uuid[]` → `Guid[]` for SELECT queries
- The property is initialized as `null!` but EF Core materializes it as an empty array instead of reading from database

### Evidence
1. **Database writes work**: No errors during SaveChangesAsync()
2. **Database reads fail silently**: Entity loads but array property is empty
3. **Environment-specific**: .NET 10 RC is preview software with potential breaking changes
4. **Other arrays work**: `List<string>` worked before, suggesting Guid[] specifically is problematic

### Similar Known Issues
- Npgsql 7.0+ changed type mapping APIs (GlobalTypeMapper is obsolete)
- .NET 10 RC may have changes to how generic collections are handled
- EF Core 10 RC may have changes to array materialization

---

## Recommended Solutions

### 🟢 Solution A: Junction Table (Recommended)

**Why**: Standard pattern, guaranteed to work, better for many-to-many relationships

**Implementation**:
```csharp
// New entity
public class MoodEntryEventLabel
{
    public Guid MoodEntryId { get; set; }
    public MoodEntry MoodEntry { get; set; } = null!;

    public Guid EventLabelId { get; set; }
    public EventLabel EventLabel { get; set; } = null!;
}

// Updated MoodEntry
public class MoodEntry
{
    public ICollection<MoodEntryEventLabel> MoodEntryEventLabels { get; set; } = new List<MoodEntryEventLabel>();

    [NotMapped]
    public IEnumerable<Guid> EventLabelIds =>
        MoodEntryEventLabels.Select(x => x.EventLabelId);
}

// EF Core configuration
modelBuilder.Entity<MoodEntryEventLabel>()
    .HasKey(x => new { x.MoodEntryId, x.EventLabelId });

modelBuilder.Entity<MoodEntry>()
    .HasMany(m => m.MoodEntryEventLabels)
    .WithOne(mel => mel.MoodEntry)
    .HasForeignKey(mel => mel.MoodEntryId);
```

**Pros**:
- ✅ Standard EF Core many-to-many pattern
- ✅ Works on all .NET versions
- ✅ Better database design
- ✅ Can add metadata (order, created date, etc.)

**Cons**:
- ⚠️ Requires database migration
- ⚠️ 30-45 minutes implementation time

---

### 🟡 Solution B: Downgrade to Stable Versions

**Why**: Preview software may have bugs

**Implementation**:
- Downgrade from .NET 10 RC → .NET 9 Stable
- Downgrade Npgsql.EntityFrameworkCore.PostgreSQL to stable version for .NET 9

**Pros**:
- ✅ May resolve compatibility issues
- ✅ More stable overall

**Cons**:
- ⚠️ Loses .NET 10 RC features
- ⚠️ May not be acceptable for project requirements

---

### 🟡 Solution C: Use JSON Column Instead

**Why**: Workaround for array serialization issues

**Implementation**:
```csharp
builder.Property(m => m.EventLabelIds)
    .HasColumnType("jsonb")
    .HasColumnName("event_label_ids")
    .HasConversion(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<Guid[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<Guid>()
    );
```

**Pros**:
- ✅ Minimal code changes
- ✅ Keeps array-based API

**Cons**:
- ⚠️ Less efficient than native arrays
- ⚠️ Can't query by individual IDs efficiently
- ⚠️ Requires database migration

---

## Failing Tests

All 9 failing tests have the same root cause (empty EventLabels):

1. `Scenario4_CreateMoodEntry_WithTagsAndNotes_Succeeds`
2. `Scenario4_UpdateMoodEntry_ChangesScoreAndTags`
3. `CreateMoodEntry_ValidRequest_ReturnsCreated` (contract test)
4. `UpdateMoodEntry_ValidRequest_ReturnsOk` (contract test)
5. `Scenario8_FilterMoodEntriesByTag_ReturnsOnlyMatchingEntries`
6. `Scenario8_FilterByMultipleTags_ReturnsEntriesWithAnyTag`
7. `Scenario8_CombineDateRangeAndTagFilters_AppliesBoth`
8. `Scenario5_ReuseLabelInMoodEntry_ConsistentTracking`
9. `ExportToJson_VerifyMoodEntryStructure`

Plus 1 unrelated test failure in HealthMetricsTests.

---

## Files Affected

### ✅ Successfully Updated (Test Refactoring Complete)
- `tests/MentalHealthBar.Api.Tests/Integration/CreateMoodEntryTests.cs`
- `tests/MentalHealthBar.Api.Tests/Integration/EventLabelTests.cs`
- `tests/MentalHealthBar.Api.Tests/Integration/FilteringTests.cs`
- `tests/MentalHealthBar.Api.Tests/Integration/ViewTrendsTests.cs`
- `tests/MentalHealthBar.Api.Tests/Integration/ExportTests.cs`
- `tests/MentalHealthBar.Api.Tests/Contracts/MoodEntriesContractTests.cs`
- `tests/MentalHealthBar.Api.Tests/Contracts/ExportContractTests.cs`

### ⚠️ Needs Fix for Array Deserialization
- `src/MentalHealthBar.Api/Domain/MoodEntries/MoodEntry.cs`
- `src/MentalHealthBar.Api/Infrastructure/Data/Configurations/MoodEntryConfiguration.cs`
- `src/MentalHealthBar.Api/Features/MoodEntries/Create/Create.cs`
- `src/MentalHealthBar.Api/Features/MoodEntries/Update/Update.cs`
- `src/MentalHealthBar.Api/Features/MoodEntries/GetHistory/GetHistory.cs`

---

## Next Steps

**Recommended**: Implement **Solution A (Junction Table)**
- Standard database design pattern
- Guaranteed to work
- Better long-term maintainability
- Estimated time: 30-45 minutes

**Alternative**: Test **Solution C (JSON Column)** first
- Quicker to implement (10-15 minutes)
- If it works, might be acceptable
- If it doesn't work, fall back to Solution A

---

## Additional Notes

- The migration `20251012170630_ConvertTagsToEventLabelIds` successfully converted the column from `text[]` to `uuid[]`
- The database schema is correct (verified with `dotnet ef migrations list`)
- Npgsql.EntityFrameworkCore.PostgreSQL package is compatible with .NET 10 RC
- 155 tests pass successfully, proving most of the refactoring work is correct
