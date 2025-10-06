# Data Model

**Feature**: Mental Health & Mood Tracking Application
**Date**: 2025-10-05
**Status**: Complete

## Overview
This document defines the domain entities, relationships, validation rules, and database schema for the mental health tracking application. Models follow "just-enough DDD" principles with rich domain behavior where needed.

---

## Domain Entities

### 1. Assessment
Represents a completed standardized mental health evaluation.

**Properties**:
```csharp
public class Assessment
{
    public Guid Id { get; init; }                    // NewId.NextGuid()
    public AssessmentType Type { get; init; }        // Enum: PHQ9, BDI, GAD7, BAI
    public DateTimeOffset CompletedAt { get; init; }
    public Dictionary<string, int> Responses { get; init; } // Question ID → Answer (0-3)
    public int TotalScore { get; private set; }
    public SeverityLevel Severity { get; private set; } // Calculated from score
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

**Business Rules**:
- Total score calculated from responses using scoring algorithm
- Severity level derived from total score and assessment type
- Cannot modify responses after creation (immutable for clinical validity)
- Can delete entire assessment (user mistake scenario)

**Validation**:
- Responses must contain exactly the required number of questions for the assessment type
- Each response value must be 0-3
- CompletedAt cannot be in the future

**State Transitions**:
- Created → Completed (automatic when all questions answered)

---

### 2. AssessmentTemplate
Defines the questions and scoring for each assessment type (read-only, seeded data).

**Properties**:
```csharp
public class AssessmentTemplate
{
    public Guid Id { get; init; }
    public AssessmentType Type { get; init; }
    public string Name { get; init; }                // "PHQ-9", "Beck Depression Inventory"
    public string Description { get; init; }
    public List<Question> Questions { get; init; }
    public ScoringRules ScoringRules { get; init; }
}

public record Question(
    string Id,              // "Q1", "Q2", etc.
    string Text,            // "Little interest or pleasure in doing things"
    List<AnswerOption> Options
);

public record AnswerOption(int Value, string Label); // (0, "Not at all"), (1, "Several days"), etc.

public record ScoringRules(
    int MinScore,
    int MaxScore,
    Dictionary<Range, SeverityLevel> SeverityRanges
);
```

**Business Rules**:
- Templates are immutable (seeded on application startup)
- One template per assessment type
- Questions maintain clinical wording for validity

---

### 3. MoodEntry
Represents a mood check-in with optional tags.

**Properties**:
```csharp
public class MoodEntry
{
    public Guid Id { get; init; }
    public int MoodScore { get; init; }              // 1-5 scale
    public DateTimeOffset RecordedAt { get; init; }  // When entry was created
    public List<string> Tags { get; init; }          // Denormalized tag names
    public string? Notes { get; set; }               // Optional user notes
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

**Business Rules**:
- Multiple entries allowed per day (no uniqueness constraint on date)
- MoodScore must be 1-5 where 1=Worst, 5=Best
- RecordedAt can be backdated (for retroactive entry)
- Tags are denormalized (preserves history if EventLabel deleted)
- Soft delete (IsDeleted flag) preserves audit trail

**Validation**:
- MoodScore: 1-5 (required)
- RecordedAt: Cannot be more than 30 days in future (allows some clock skew)
- Tags: Maximum 10 tags per entry
- Notes: Maximum 500 characters

---

### 4. EventLabel
Represents a user-created tag for categorizing life events.

**Properties**:
```csharp
public class EventLabel
{
    public Guid Id { get; init; }
    public string Name { get; set; }                 // "work stress", "exercise", etc.
    public string? Description { get; set; }         // Optional explanation
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

**Business Rules**:
- Names are case-insensitive unique (prevent duplicates like "Exercise" and "exercise")
- Soft delete (allows cleanup without orphaning historical data)
- Can be renamed (updates all future uses, doesn't change denormalized historical data)

**Validation**:
- Name: 1-50 characters, alphanumeric + spaces/hyphens
- Description: Maximum 200 characters

---

### 5. HealthMetric
Represents a daily health measurement (sleep or water intake).

**Properties**:
```csharp
public class HealthMetric
{
    public Guid Id { get; init; }
    public MetricType Type { get; init; }            // Enum: SleepHours, WaterIntakeOz
    public decimal Value { get; init; }              // Numeric value
    public DateOnly RecordedDate { get; init; }      // Date of measurement (no time)
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

**Business Rules**:
- One entry per metric type per date (can have both sleep and water for same day)
- Unique constraint: (Type, RecordedDate)
- Can be updated (e.g., user logs water intake multiple times, updates total)
- Soft delete for audit trail

**Validation**:
- Value for SleepHours: 0-24 (decimal for precision, e.g., 7.5 hours)
- Value for WaterIntakeOz: 0-200 (decimal for precision, e.g., 32.5 oz)
- RecordedDate: Cannot be more than 7 days in future

---

### 6. UserPreferences
Stores application settings (implicit single user, no user ID).

**Properties**:
```csharp
public class UserPreferences
{
    public Guid Id { get; init; }                    // Single row (singleton)
    public string Theme { get; set; }                // "System", "Light", "Dark"
    public string WaterUnit { get; set; }            // "FluidOunces", "Milliliters" (display only, stored as oz)
    public int DefaultDateRangeDays { get; set; }    // For visualization (default: 30)
    public DateTimeOffset UpdatedAt { get; set; }
}
```

**Business Rules**:
- Single row in database (enforced via unique constraint or code check)
- Changes apply immediately (no save/cancel needed)

**Validation**:
- DefaultDateRangeDays: 7-365

---

## Enumerations

```csharp
public enum AssessmentType
{
    PHQ9 = 1,           // Patient Health Questionnaire-9
    BDI = 2,            // Beck Depression Inventory
    GAD7 = 3,           // Generalized Anxiety Disorder-7
    BAI = 4             // Beck Anxiety Inventory
}

public enum SeverityLevel
{
    Minimal = 0,
    Mild = 1,
    Moderate = 2,
    ModeratelySevere = 3,  // PHQ-9 only
    Severe = 4
}

public enum MetricType
{
    SleepHours = 1,
    WaterIntakeOz = 2
}
```

---

## Relationships

```
MoodEntry 1 ──> 0..* EventLabel (via Tags - denormalized)
HealthMetric (no relationships)
Assessment ──> 1 AssessmentTemplate (via Type)
UserPreferences (singleton, no relationships)
```

**Notes**:
- No foreign keys between MoodEntry and EventLabel (denormalized for historical integrity)
- AssessmentTemplate referenced by type enum, not FK (templates are immutable reference data)

---

## Database Schema (PostgreSQL)

### Tables

#### assessments
```sql
CREATE TABLE assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    type INTEGER NOT NULL,  -- AssessmentType enum
    completed_at TIMESTAMPTZ NOT NULL,
    responses JSONB NOT NULL,
    total_score INTEGER NOT NULL,
    severity INTEGER NOT NULL,  -- SeverityLevel enum
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ
);

CREATE INDEX idx_assessments_type_completed ON assessments(type, completed_at DESC);
CREATE INDEX idx_assessments_completed ON assessments(completed_at DESC);
```

#### mood_entries
```sql
CREATE TABLE mood_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mood_score INTEGER NOT NULL CHECK (mood_score >= 1 AND mood_score <= 5),
    recorded_at TIMESTAMPTZ NOT NULL,
    tags TEXT[] DEFAULT '{}',  -- Array of tag names
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_mood_entries_recorded ON mood_entries(recorded_at DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_mood_entries_tags ON mood_entries USING GIN(tags) WHERE is_deleted = FALSE;
```

#### event_labels
```sql
CREATE TABLE event_labels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(50) NOT NULL,
    description VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ
);

CREATE UNIQUE INDEX idx_event_labels_name ON event_labels(LOWER(name)) WHERE is_deleted = FALSE;
```

#### health_metrics
```sql
CREATE TABLE health_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    type INTEGER NOT NULL,  -- MetricType enum
    value DECIMAL(5,2) NOT NULL CHECK (value >= 0),
    recorded_date DATE NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ,
    CONSTRAINT uq_health_metrics_type_date UNIQUE (type, recorded_date)
);

CREATE INDEX idx_health_metrics_date ON health_metrics(recorded_date DESC) WHERE is_deleted = FALSE;
CREATE INDEX idx_health_metrics_type_date ON health_metrics(type, recorded_date DESC) WHERE is_deleted = FALSE;
```

#### user_preferences
```sql
CREATE TABLE user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    theme VARCHAR(20) NOT NULL DEFAULT 'System',
    water_unit VARCHAR(20) NOT NULL DEFAULT 'FluidOunces',
    default_date_range_days INTEGER NOT NULL DEFAULT 30,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Ensure single row
CREATE UNIQUE INDEX idx_user_preferences_singleton ON user_preferences((1));
```

---

## Entity Framework Core Configuration

### Assessment Configuration
```csharp
public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever(); // Use NewId

        builder.Property(a => a.Type).HasConversion<int>();
        builder.Property(a => a.Severity).HasConversion<int>();

        builder.Property(a => a.Responses)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions)null));

        builder.HasIndex(a => new { a.Type, a.CompletedAt });
        builder.HasIndex(a => a.CompletedAt);
    }
}
```

### MoodEntry Configuration
```csharp
public class MoodEntryConfiguration : IEntityTypeConfiguration<MoodEntry>
{
    public void Configure(EntityTypeBuilder<MoodEntry> builder)
    {
        builder.ToTable("mood_entries");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Tags)
            .HasConversion(
                v => v.ToArray(),
                v => v.ToList());

        builder.Property(m => m.Notes).HasMaxLength(500);

        builder.HasIndex(m => m.RecordedAt).HasFilter("is_deleted = false");
        builder.HasIndex(m => m.Tags).HasMethod("GIN").HasFilter("is_deleted = false");

        builder.HasQueryFilter(m => !m.IsDeleted); // Global query filter
    }
}
```

---

## Domain Events

Following "just-enough DDD" - events only where cross-aggregate logic exists.

```csharp
// Future use: Trigger insights, statistics updates, etc.
public record MoodEntryCreated(Guid MoodEntryId, int MoodScore, DateTimeOffset RecordedAt);
public record AssessmentCompleted(Guid AssessmentId, AssessmentType Type, int TotalScore, SeverityLevel Severity);
public record HealthMetricRecorded(Guid MetricId, MetricType Type, decimal Value, DateOnly Date);
```

**Implementation Note**: Events published via MediatR INotification, handlers in same feature slice for now.

---

## Value Objects

```csharp
// Mood score with validation and label
public record MoodScore
{
    public int Value { get; }
    public string Label { get; }

    public MoodScore(int value)
    {
        if (value < 1 || value > 5)
            throw new ArgumentOutOfRangeException(nameof(value), "Mood score must be 1-5");

        Value = value;
        Label = value switch
        {
            1 => "Worst",
            2 => "Below Average",
            3 => "Average",
            4 => "Above Average",
            5 => "Best",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

// Date range for queries
public record DateRange(DateOnly Start, DateOnly End)
{
    public static DateRange Last7Days() => new(DateOnly.FromDateTime(DateTime.Now.AddDays(-7)), DateOnly.FromDateTime(DateTime.Now));
    public static DateRange Last30Days() => new(DateOnly.FromDateTime(DateTime.Now.AddDays(-30)), DateOnly.FromDateTime(DateTime.Now));
    public static DateRange Last90Days() => new(DateOnly.FromDateTime(DateTime.Now.AddDays(-90)), DateOnly.FromDateTime(DateTime.Now));
    public static DateRange AllTime() => new(DateOnly.MinValue, DateOnly.FromDateTime(DateTime.Now));
}
```

---

## Aggregate Boundaries

Each entity is its own aggregate (simple bounded context, no complex invariants across entities):
- **Assessment** aggregate: Assessment + responses (immutable after creation)
- **MoodEntry** aggregate: Single entry with tags (tags denormalized, no consistency needed)
- **EventLabel** aggregate: Single label (soft delete doesn't affect mood entries)
- **HealthMetric** aggregate: Single metric (unique per type/date enforced at DB level)

**Rationale**: No complex business rules spanning multiple entities, simple CRUD operations

---

## Validation Rules Summary

| Entity | Field | Rule |
|--------|-------|------|
| Assessment | Responses | Exact question count for type, each 0-3 |
| Assessment | CompletedAt | Not in future |
| MoodEntry | MoodScore | 1-5 (required) |
| MoodEntry | RecordedAt | Not >30 days future |
| MoodEntry | Tags | Max 10 per entry |
| MoodEntry | Notes | Max 500 chars |
| EventLabel | Name | 1-50 chars, alphanumeric + space/hyphen, unique (case-insensitive) |
| EventLabel | Description | Max 200 chars |
| HealthMetric | Value (Sleep) | 0-24 decimal |
| HealthMetric | Value (Water) | 0-200 decimal |
| HealthMetric | RecordedDate | Not >7 days future |
| HealthMetric | Type + Date | Unique combination |

**Implementation**: FluentValidation validators in command handlers

---

## Data Seeding

### Assessment Templates
Seed data for 4 assessment types with questions and scoring rules.

**PHQ-9 Example**:
```json
{
  "type": "PHQ9",
  "name": "Patient Health Questionnaire (PHQ-9)",
  "description": "9-item depression screening tool",
  "questions": [
    {
      "id": "Q1",
      "text": "Little interest or pleasure in doing things",
      "options": [
        { "value": 0, "label": "Not at all" },
        { "value": 1, "label": "Several days" },
        { "value": 2, "label": "More than half the days" },
        { "value": 3, "label": "Nearly every day" }
      ]
    },
    // ... 8 more questions
  ],
  "scoringRules": {
    "minScore": 0,
    "maxScore": 27,
    "severityRanges": {
      "0-4": "Minimal",
      "5-9": "Mild",
      "10-14": "Moderate",
      "15-19": "ModeratelySevere",
      "20-27": "Severe"
    }
  }
}
```

**Storage**: JSON files in `backend/src/MentalHealthBar.Api/Data/Seeds/`, loaded on application startup

---

## Migration Strategy

**Initial Migration**: Create all tables with indexes
**Future Migrations**: EF Core migrations for schema changes
**Data Migration**: Seed assessment templates on first run (idempotent check)

**Rollback Strategy**: EF Core migration rollback for schema, data seeds are append-only (no deletion)

---

## Performance Considerations

### Indexes
- **Time-series queries**: Indexes on timestamp/date columns with DESC (recent data most common)
- **Filtered indexes**: `WHERE is_deleted = FALSE` reduces index size
- **GIN index**: For tag array queries (rare, but enables tag filtering)

### Query Optimization
- **Pagination**: Limit results to 100 entries per query (frontend paging)
- **Date range filtering**: All history queries include date bounds
- **Global query filters**: EF Core filters soft-deleted rows automatically

### Future Optimization (if needed)
- **TimescaleDB hypertables**: Convert mood_entries, health_metrics to hypertables for time-series optimization
- **Materialized views**: Daily/weekly aggregates for dashboard (if calculations become slow)

---

## Testing Strategy

### Unit Tests (Domain Logic)
- Assessment scoring calculation (all 4 types)
- MoodScore value object validation
- DateRange factory methods

### Integration Tests (Database)
- CRUD operations for each entity
- Unique constraints (EventLabel name, HealthMetric type+date)
- Soft delete query filtering
- JSONB querying (Assessment responses)

### Contract Tests
- API request/response schemas match DTOs
- Validation error responses

---

## Summary
Data model supports all functional requirements with:
- **4 core entities**: Assessment, MoodEntry, EventLabel, HealthMetric
- **Audit trail**: CreatedAt, UpdatedAt, soft delete
- **Validation**: FluentValidation for business rules
- **Performance**: Strategic indexing for time-series queries
- **Extensibility**: JSONB for assessment responses, denormalized tags for historical integrity

**Next Step**: Generate API contracts from functional requirements
