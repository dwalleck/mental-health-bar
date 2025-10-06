# Research & Technical Decisions

**Feature**: Mental Health & Mood Tracking Application
**Date**: 2025-10-05
**Status**: Complete

## Overview
This document captures research findings and technical decisions for remaining ambiguities from the feature specification, focusing on practical defaults that enable v0.1.0 shipping.

---

## Deferred Clarifications - Resolved with Defaults

### 1. Assessment Scheduling/Reminders (FR-007)
**Decision**: User-initiated only for v0.1.0
**Rationale**:
- Reminders require background services/notifications (increases complexity)
- User can manually track weekly schedule
- Ship simple version first, add reminders in v0.2.0 based on feedback

**Implementation**: No scheduling logic, user opens app and completes assessments when ready

---

### 2. Tag Management - Edit/Delete Behavior (FR-016)
**Decision**: Allow tag deletion, maintain historical references as orphaned labels
**Rationale**:
- Prevents data loss - historical mood entries retain tag text even if tag deleted
- Simpler than preventing deletion (no complex validation)
- User can clean up unused tags without affecting history

**Implementation**:
- EventLabel has soft-delete flag or hard-delete preserves label text in MoodEntry
- Mood entries store tag names (denormalized) for historical integrity

**Alternative considered**: Prevent deletion if tag used in history - rejected because annoying UX

---

### 3. Predefined vs User-Created Tags (FR-017)
**Decision**: Entirely user-created for v0.1.0
**Rationale**:
- Predefined tags are opinionated (what events matter varies by person)
- Simpler data model (no tag categories or defaults to maintain)
- User has full control over taxonomy

**Implementation**: Tag creation UI with autocomplete from existing tags

**Future enhancement**: Suggest common tags based on usage patterns

---

### 4. Water Intake Units (FR-021)
**Decision**: Fluid ounces (fl oz) as default unit, stored as decimal
**Rationale**:
- US market default (most mental health apps use oz)
- Easy conversion: 1 oz = ~29.57 ml
- Decimal storage allows precision (8.5 oz, etc.)

**Implementation**:
- Database stores decimal value in oz
- UI displays "fl oz" label
- Future: Add user preference for ml/cups display with conversion

**Validation**: 0-200 oz reasonable range (0-6 liters)

---

### 5. Health Metric Validation Ranges (FR-022)
**Decision**: Implement reasonable bounds with helpful error messages

| Metric | Min | Max | Rationale |
|--------|-----|-----|-----------|
| Sleep Hours | 0 | 24 | Physical limit, 0 allowed for insomnia tracking |
| Water Intake (oz) | 0 | 200 | ~6 liters upper bound, medical concern if exceeded |

**Implementation**: FluentValidation rules with messages like "Sleep hours must be between 0-24. Did you mean to enter 8 instead of 80?"

**Edge case**: Allow 0 values (user didn't sleep, forgot to drink water) - data point is still valuable

---

### 6. Graph Types (FR-026)
**Decision**: Line charts only for v0.1.0
**Rationale**:
- Line charts ideal for time-series data (mood trends, assessment scores over time)
- Single chart type = simpler UI, consistent experience
- Most requested visualization for mental health tracking

**Implementation**:
- Use AvaloniaUI charting library (e.g., LiveCharts2, ScottPlot, or OxyPlot)
- X-axis: Date, Y-axis: Score/Value
- One chart per metric type (mood, PHQ-9, sleep, water)

**Future enhancement**: Add scatter plots for correlation views (sleep vs mood)

---

### 7. Correlation Analysis (FR-027)
**Decision**: Defer to v0.2.0
**Rationale**:
- Not required for MVP (user can visually compare charts)
- Adds complexity: statistical calculations, chart types, interpretation guidance
- Ship basic tracking first, validate demand

**Placeholder**: Show multiple charts stacked vertically for manual comparison

---

### 8. Date Range Filtering (FR-028)
**Decision**: Preset ranges + custom range for v0.1.0

**Preset Ranges**:
- Last 7 days
- Last 30 days
- Last 90 days
- Last 12 months
- All time

**Custom Range**: Date picker for start/end dates

**Rationale**: Covers common use cases (weekly check-in, monthly therapy review, yearly progress)

**Implementation**: Dropdown with presets + "Custom..." option opens date picker dialog

---

### 9. Historical Data Editing (FR-030)
**Decision**: Allow edit and delete with timestamp tracking (audit trail)
**Rationale**:
- Users make mistakes (typos, wrong date selection)
- Preventing edit is frustrating UX
- Audit trail useful for understanding data patterns (e.g., multiple edits might indicate uncertainty)

**Implementation**:
```csharp
public class MoodEntry
{
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
```

**UI**: Show "Last edited: [timestamp]" on entry details

---

### 10. Data Retention Policy (FR-033)
**Decision**: Indefinite retention, user-controlled deletion only
**Rationale**:
- Long-term tracking is core value (years of trend data)
- No regulatory requirement for auto-deletion (not HIPAA-covered entity)
- Local storage (user owns data, no cloud cost concerns)

**Implementation**: No automatic deletion logic, user can manually delete entries via UI

**Future enhancement**: Export and archive old data (e.g., >5 years) to separate file

---

### 11. Data Sharing (FR-037)
**Decision**: Export feature serves sharing use case (defer direct sharing)
**Rationale**:
- Export to CSV/JSON enables sharing with healthcare providers (via email, USB drive, etc.)
- Direct sharing (email integration, cloud upload) adds significant complexity
- Offline-first architecture makes cloud sharing inconsistent with design

**Implementation**: Export button → file save dialog → user shares file manually

---

### 12. Incomplete Assessment Handling (Edge Case)
**Decision**: Require completion, no draft saves for v0.1.0
**Rationale**:
- Standardized assessments lose validity if incomplete (PHQ-9 scoring requires all 9 items)
- Draft state adds complexity (storage, resume UX, partial score interpretation)
- User can abandon and restart (assessments take <5 min to complete)

**Implementation**: Validation prevents submission if any question unanswered, show "3 of 9 answered" progress

**Future enhancement**: Auto-save draft for resumption

---

### 13. Multiple Mood Entries Per Day - Visualization
**Decision**: Show all entries as individual points on line chart, option to view aggregated (average/median) view
**Rationale**:
- Preserves granularity (user might want to see morning vs evening mood)
- Aggregation reduces noise for trend identification
- Flexibility meets different user needs

**Implementation**:
- Default: Plot all mood entries as points connected by date
- Toggle: "Show daily average" smooths line to one point per day (average of all entries)

**Chart detail view**: Hover shows timestamp for each point

---

## Technical Research

### AvaloniaUI Charting Libraries
**Evaluated Options**:

1. **LiveCharts2**
   - ✅ Cross-platform (Avalonia support)
   - ✅ Modern, actively maintained
   - ✅ Good performance for time-series
   - ❌ Learning curve

2. **ScottPlot**
   - ✅ Excellent performance
   - ✅ Simple API
   - ✅ Avalonia support
   - ✅ Great documentation

3. **OxyPlot**
   - ✅ Mature, stable
   - ✅ Avalonia support
   - ❌ Older API design
   - ⚠️ Less active development

**Decision**: ScottPlot
**Rationale**: Best balance of performance, simplicity, and Avalonia support. Easy to implement line charts quickly (ships faster).

---

### PostgreSQL vs TimescaleDB
**Decision**: Start with PostgreSQL, add TimescaleDB extension if performance issues emerge
**Rationale**:
- PostgreSQL sufficient for estimated data volume (<10k records over 5 years)
- TimescaleDB adds setup complexity (extension installation)
- Can enable TimescaleDB later without data migration (just convert tables to hypertables)

**Implementation**:
- v0.1.0: PostgreSQL with date-indexed queries
- If query performance >500ms: Enable TimescaleDB, create hypertables for MoodEntries, HealthMetrics

**Alternative considered**: SQLite - rejected due to no time-series optimization and concurrency limits

---

### MediatR Command/Query Separation
**Pattern**: CQRS-lite (commands and queries, single database)
**Rationale**:
- Vertical slice architecture pairs well with MediatR handlers
- Clear separation of read (GetHistory) vs write (Create) operations
- No need for event sourcing or separate read/write databases (single-user app)

**Example Structure**:
```
Features/
  MoodEntries/
    Create/
      Create.Command.cs
      Create.Handler.cs
      Create.Validator.cs
    GetHistory/
      GetHistory.Query.cs
      GetHistory.Handler.cs
```

---

### Domain Events - "Just-Enough DDD"
**Scope**: Use domain events only where business logic spans aggregates
**Examples**:
- ✅ MoodEntryCreated → Update daily statistics (future feature)
- ✅ AssessmentCompleted → Trigger insights generation (future feature)
- ❌ EventLabelCreated → No cross-aggregate logic needed

**Implementation**:
- Domain events as simple C# records
- MediatR notifications for publishing
- Handlers in same vertical slice (keep related logic together)

**Rationale**: Avoids over-engineering - only use events where actually needed, not "just in case"

---

### Assessment Scoring Implementation
**Research**: Official scoring methodologies

1. **PHQ-9** (Patient Health Questionnaire)
   - 9 items, each scored 0-3
   - Total score: 0-27
   - Interpretation: 0-4 minimal, 5-9 mild, 10-14 moderate, 15-19 moderately severe, 20-27 severe
   - Source: Validated clinical instrument, public domain

2. **GAD-7** (Generalized Anxiety Disorder)
   - 7 items, each scored 0-3
   - Total score: 0-21
   - Interpretation: 0-4 minimal, 5-9 mild, 10-14 moderate, 15-21 severe

3. **Beck Depression Inventory (BDI-II)**
   - 21 items, each scored 0-3
   - Total score: 0-63
   - Interpretation: 0-13 minimal, 14-19 mild, 20-28 moderate, 29-63 severe
   - **Licensing**: Beck Institute copyright - requires permission for commercial use
   - **Decision for v0.1.0**: Include questions and scoring, add disclaimer "For personal tracking only, not diagnostic"

4. **Beck Anxiety Inventory (BAI)**
   - 21 items, each scored 0-3
   - Total score: 0-63
   - Interpretation: 0-7 minimal, 8-15 mild, 16-25 moderate, 26-63 severe
   - Same licensing considerations as BDI-II

**Implementation**:
- Store questions and scoring rules in database (seeded data)
- Scoring engine in domain layer: `Assessment.CalculateScore(responses)`
- Include disclaimers per Beck licensing (non-diagnostic, personal use)

---

### Entity Framework Core + PostgreSQL Best Practices
**Key Decisions**:

1. **Timestamp Handling**: Use `DateTimeOffset` for timezone-aware storage
   - User might travel, change timezone
   - Historical data preserves exact moment of entry

2. **JSON Columns**: Store assessment responses as JSONB
   - Flexible schema (different assessments have different questions)
   - Queryable via EF Core JSON support (.NET 7+)
   ```csharp
   public class Assessment
   {
       public Dictionary<string, int> Responses { get; set; } // Maps to jsonb
   }
   ```

3. **Indexes**:
   - `MoodEntries(RecordedAt)` - date range queries
   - `Assessments(AssessmentType, CompletedAt)` - history by type
   - `HealthMetrics(MetricType, RecordedDate)` - trend queries

4. **Migrations**: Use EF Core migrations for schema versioning
   - Enables safe schema updates for future features

---

### Polly Resilience Patterns
**Use Case**: Desktop app → local API communication
**Decision**: Minimal resilience for v0.1.0 (local network is reliable)

**Implemented Policies**:
- Retry: 3 attempts with exponential backoff (handles transient API startup delays)
- Timeout: 5 seconds per request
- Circuit Breaker: Deferred (local API unlikely to have sustained failures)

**Rationale**: Don't over-engineer resilience for localhost communication, but handle edge cases (API slow to start after desktop app launch)

---

### Serilog Configuration
**Sinks**:
- File: Rolling daily logs in `logs/` directory
- Console: Development environment

**Log Levels**:
- Development: Debug
- Production: Information

**Structured Logging**:
```csharp
_logger.LogInformation(
    "Mood entry created for {Date} with score {Score}",
    entry.RecordedAt, entry.MoodScore);
```

**Rationale**: Helps debugging user issues (they can share log file), minimal performance overhead

---

## Architectural Decisions

### Decision: Offline-First Architecture
**Rationale**:
- Single-user desktop app has no need for cloud sync
- Local PostgreSQL provides full database capabilities offline
- Eliminates complexity: auth, network error handling, sync conflicts

**Trade-off**: No multi-device sync (accepted - user can export/import data manually if needed)

---

### Decision: API-First Design (Despite Single Client)
**Rationale**:
- Requested by user (follows their technical requirements)
- Testability: API can be tested independently of UI
- Future-proof: Enables mobile app or web UI later
- Separation of concerns: UI logic vs business logic

**Trade-off**: Additional complexity vs direct database access (justified - see Complexity Tracking)

---

### Decision: MVVM for AvaloniaUI
**Rationale**:
- Avalonia best practice (similar to WPF)
- Clear separation: View (XAML) → ViewModel (logic) → Model (data)
- Testable (ViewModels are POCOs, no UI dependencies)

**Implementation**:
- ViewModels use ReactiveUI for property change notifications
- Commands via ReactiveCommand
- Services injected via DI (API client, charting service)

---

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| PostgreSQL setup too complex for users | High | Medium | Docker Compose provides one-command setup; document alternative (SQLite fallback in v0.2.0) |
| AvaloniaUI learning curve delays development | Medium | Medium | Follow official samples, use pre-built controls, focus on functionality over polish |
| Assessment copyright issues (Beck instruments) | High | Low | Include disclaimer, confirm non-commercial personal use is acceptable, consult legal if needed |
| Chart rendering performance on large datasets | Medium | Low | ScottPlot handles 10k+ points well; implement pagination (show 90 days by default) |
| .NET 10 RC stability | Medium | Low | Use stable APIs only, avoid preview features, plan upgrade to RTM in November 2024 |

---

## Open Questions (Lower Priority)
These can be addressed during implementation or deferred to later versions:

1. **Assessment question wording**: Use exact clinical wording or simplified for readability?
   - **Decision**: Use exact clinical wording for validity, provide tooltips for clarity

2. **Export file naming**: Timestamp-based or user-chosen?
   - **Decision**: Default to `mental-health-data-YYYY-MM-DD.csv` with option to rename

3. **Dashboard layout**: Card-based or tab-based?
   - **Decision**: Card-based dashboard showing recent mood, last assessment, quick stats (defer to UI implementation phase)

4. **Dark mode support**: Should UI support dark theme?
   - **Decision**: Yes, follow system theme (Avalonia provides built-in theme support)

---

## Summary
All critical unknowns resolved with pragmatic defaults that enable v0.1.0 shipping. Focus is on core functionality (track, view, export) with clear path for enhancements based on user feedback.

**Next Step**: Phase 1 - Design data model and API contracts
