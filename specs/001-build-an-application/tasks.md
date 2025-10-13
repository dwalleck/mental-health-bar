# Tasks: Mental Health & Mood Tracking Application

**Input**: Design documents from `/home/dwalleck/repos/mental-health-bar/specs/001-build-an-application/`
**Prerequisites**: plan.md, research.md, data-model.md, contracts/, quickstart.md
**Architecture**: Desktop app (AvaloniaUI) + Backend API (.NET 10 RC Minimal API) + PostgreSQL
**Pattern**: Vertical Slice Architecture with MediatR, "just-enough DDD"

---

## Execution Flow (main)
```
1. Load plan.md from feature directory
   → Extract: .NET 10 RC, AvaloniaUI, Minimal API, Vertical Slices, MediatR
2. Load design documents:
   → data-model.md: 4 entities (Assessment, MoodEntry, EventLabel, HealthMetric)
   → contracts/: 5 contract files → 5 contract test tasks
   → quickstart.md: 9 user scenarios → integration tests
3. Generate tasks by category:
   → Setup: Solution, Docker, packages, EF Core
   → Tests: Contract tests (5), Integration tests (9)
   → Core: Domain models (5), EF configs, vertical slices (24)
   → Frontend: ViewModels (6), Views (6), services
   → Polish: Performance, E2E validation
4. Apply task rules:
   → Contract tests in parallel [P] (different files)
   → Domain models in parallel [P] (different files)
   → Vertical slices sequential within feature (shared Program.cs)
   → ViewModels/Views in parallel [P]
5. Number tasks T001-T076
6. Generate dependency graph
7. SUCCESS (tasks ready for TDD execution)
```

---

## Format: `[ID] [P?] Description`
- **[P]**: Can run in parallel (different files, no dependencies)
- All paths are absolute from repository root
- Tasks follow TDD: Tests first, then implementation

---

## Phase 3.1: Infrastructure Setup

- [x] **T001**: Create .NET solution structure
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: None
  - **File Path**: Repository root
  - **Acceptance**: Solution file exists with 4 projects (API, Desktop, 2 TUnit test projects with code coverage support)
  - **Commands**:
    ```bash
    dotnet new sln -n MentalHealthBar
    dotnet new webapi -n MentalHealthBar.Api -o backend/src/MentalHealthBar.Api --use-minimal-apis
    dotnet new avalonia.mvvm -n MentalHealthBar.Desktop -o frontend/src/MentalHealthBar.Desktop
    dotnet new TUnit -n MentalHealthBar.Api.Tests -o backend/tests/MentalHealthBar.Api.Tests
    dotnet new TUnit -n MentalHealthBar.Desktop.Tests -o frontend/tests/MentalHealthBar.Desktop.Tests
    dotnet sln add backend/src/MentalHealthBar.Api
    dotnet sln add frontend/src/MentalHealthBar.Desktop
    dotnet sln add backend/tests/MentalHealthBar.Api.Tests
    dotnet sln add frontend/tests/MentalHealthBar.Desktop.Tests
    # Add code coverage support
    cd backend/tests/MentalHealthBar.Api.Tests && dotnet add package Microsoft.Testing.Extensions.CodeCoverage
    cd ../../.. && cd frontend/tests/MentalHealthBar.Desktop.Tests && dotnet add package Microsoft.Testing.Extensions.CodeCoverage
    ```

- [x] **T002**: Configure centralized package management
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T001
  - **File Path**: `Directory.Packages.props`, `Directory.Build.props`
  - **Acceptance**: All projects use centralized package versions
  - **Details**: Create Directory.Packages.props with versions for: MediatR, EF Core, Npgsql, FluentValidation, OneOf, Serilog, Polly, Humanizer, NewId, TUnit, Microsoft.Testing.Extensions.CodeCoverage, Moq, AvaloniaUI, ScottPlot

- [x] **T003**: Set up Docker Compose for PostgreSQL
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T001
  - **File Path**: `docker-compose.yml`
  - **Acceptance**: `docker-compose up -d` starts PostgreSQL on port 5432
  - **Details**: PostgreSQL 16, database `mentalhealthbar`, user `postgres`, password `postgres`, volume for persistence

- [x] **T004**: Initialize EF Core with PostgreSQL
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T002, T003
  - **File Path**: `src/MentalHealthBar.Api/Infrastructure/Data/AppDbContext.cs`
  - **Acceptance**: DbContext configured, connection string in appsettings.json
  - **Verified**: ✅ API starts without database errors, DbContext registered in DI

- [x] **T005**: Configure Serilog logging
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T002
  - **File Path**: `src/MentalHealthBar.Api/Program.cs`
  - **Acceptance**: Structured logging to console and file (logs/api.log)
  - **Verified**: ✅ Logs written to `logs/api-20251005.log`, structured format confirmed

- [x] **T006**: Set up Minimal API with Swagger
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T001
  - **File Path**: `src/MentalHealthBar.Api/Program.cs`
  - **Acceptance**: API starts, OpenAPI endpoint accessible
  - **Verified**: ✅ `/openapi/v1.json` returns valid OpenAPI 3.1 schema, `/health` endpoint works

- [x] **T007**: Configure MediatR for vertical slices
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T002
  - **File Path**: `src/MentalHealthBar.Api/Program.cs`
  - **Acceptance**: MediatR registered in DI, ready for command/query handlers
  - **Verified**: ✅ API starts without DI errors, MediatR registered successfully

- [x] **T008**: Set up global.json for .NET 10 RC
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: None
  - **File Path**: `global.json`
  - **Acceptance**: SDK version pinned to .NET 10 RC
  - **Details**: `{ "sdk": { "version": "10.0.0-rc.1" } }`

- [x] **T009**: Set up GitHub Actions CI/CD
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T001, T002, T008
  - **File Path**: `.github/workflows/ci.yml`
  - **Acceptance**: GitHub Actions workflow builds solution, runs all tests, reports code coverage on every push and PR
  - **Verified**: ✅ Workflow created with build-and-test (ubuntu/windows/macos), lint, and status-check jobs. Tests run successfully (104 tests passed)
  - **Details**: Triggers on push to main/001-build-an-application and PRs, runs TUnit tests with code coverage, uploads artifacts

---

## Phase 3.2: Contract Tests (TDD) ⚠️ MUST FAIL BEFORE IMPLEMENTATION

**CRITICAL**: These tests MUST be written and MUST FAIL before ANY implementation in Phase 3.4

- [x] **T010** [P]: Contract test for Assessments API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `tests/MentalHealthBar.Api.Tests/Contracts/AssessmentsContractTests.cs`
  - **Verified**: ✅ 11 tests created, all failing as expected (TDD Red)
  - **Endpoints tested**: Templates GET, POST assessments, GET history, GET by ID, DELETE

- [x] **T011** [P]: Contract test for Mood Entries API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `tests/MentalHealthBar.Api.Tests/Contracts/MoodEntriesContractTests.cs`
  - **Verified**: ✅ 11 tests created, all failing as expected (TDD Red)
  - **Endpoints tested**: POST, GET, GET by ID, PUT, DELETE, GET stats

- [x] **T012** [P]: Contract test for Health Metrics API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `tests/MentalHealthBar.Api.Tests/Contracts/HealthMetricsContractTests.cs`
  - **Verified**: ✅ 16 tests created, all failing as expected (TDD Red)
  - **Endpoints tested**: POST, GET, GET by ID, PUT, DELETE with full validation coverage

- [x] **T013** [P]: Contract test for Event Labels API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `tests/MentalHealthBar.Api.Tests/Contracts/EventLabelsContractTests.cs`
  - **Verified**: ✅ 16 tests created, all failing as expected (TDD Red)
  - **Endpoints tested**: POST, GET, GET by ID, PUT, DELETE with validation and conflict checks

- [x] **T014** [P]: Contract test for Export API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `tests/MentalHealthBar.Api.Tests/Contracts/ExportContractTests.cs`
  - **Verified**: ✅ 13 tests created, all failing as expected (TDD Red)
  - **Endpoints tested**: POST /api/export/csv, POST /api/export/json with validation

---

## Phase 3.3: Domain Models (TDD)

**CRITICAL**: Build domain models BEFORE vertical slices

- [x] **T015** [P]: Assessment domain model with scoring logic
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `src/MentalHealthBar.Api/Domain/Assessments/Assessment.cs`
  - **Acceptance**: Assessment entity with scoring calculation for all 4 types (PHQ9, BDI, GAD7, BAI)
  - **Verified**: ✅ Domain model created with CalculateScore() and CalculateSeverity() methods for all 4 assessment types
  - **Details**: Include AssessmentType enum, SeverityLevel enum, CalculateScore() method, validation

- [x] **T016** [P]: MoodEntry domain model
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `src/MentalHealthBar.Api/Domain/MoodEntries/MoodEntry.cs`
  - **Acceptance**: MoodEntry entity with soft delete, tag support (denormalized)
  - **Verified**: ✅ Domain model created with soft delete, Update(), SetMoodScore(), SetTags(), SetNotes() methods
  - **Details**: Validation: MoodScore 1-5, RecordedAt not >30 days future, max 10 tags, max 500 char notes

- [x] **T017** [P]: EventLabel domain model
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `src/MentalHealthBar.Api/Domain/EventLabels/EventLabel.cs`
  - **Acceptance**: EventLabel entity with soft delete, case-insensitive uniqueness
  - **Verified**: ✅ Domain model created with regex validation, Update(), SoftDelete() methods
  - **Details**: Validation: Name 1-50 chars, alphanumeric + space/hyphen, unique (case-insensitive)

- [x] **T018** [P]: HealthMetric domain model
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `src/MentalHealthBar.Api/Domain/HealthMetrics/HealthMetric.cs`
  - **Acceptance**: HealthMetric entity with unique constraint on (Type, RecordedDate)
  - **Verified**: ✅ Domain model created with SetValue(), Update(), SoftDelete() methods, validation for both metric types
  - **Details**: MetricType enum (SleepHours, WaterIntakeOz), validation: Sleep 0-24, Water 0-200

- [x] **T019** [P]: Value objects (MoodScore, DateRange)
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `src/MentalHealthBar.Api/Domain/Common/ValueObjects.cs`
  - **Acceptance**: MoodScore with label mapping (1=Worst...5=Best), DateRange factory methods
  - **Verified**: ✅ Value objects created with MoodScore validation and DateRange factory methods (Last7Days, Last30Days, Last90Days, AllTime)
  - **Details**: MoodScore validation throws ArgumentOutOfRangeException, DateRange: Last7Days(), Last30Days(), Last90Days(), AllTime()

- [x] **T020**: EF Core entity configurations
  - **Type**: Implementation
  - **Parallel**: No (modifies DbContext)
  - **Dependencies**: T015, T016, T017, T018
  - **File Path**: `src/MentalHealthBar.Api/Infrastructure/Data/Configurations/`
  - **Acceptance**: All 4 entities configured with indexes, JSONB for Assessment responses, global query filter for soft delete
  - **Verified**: ✅ All configurations created: AssessmentConfiguration (JSONB), MoodEntryConfiguration (text[] tags, soft delete filter), EventLabelConfiguration (unique name index), HealthMetricConfiguration (unique type+date)
  - **Details**: AssessmentConfiguration, MoodEntryConfiguration, EventLabelConfiguration, HealthMetricConfiguration

- [x] **T021**: Initial database migration
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T020
  - **File Path**: `src/MentalHealthBar.Api/Migrations/`
  - **Acceptance**: Migration creates all tables with indexes
  - **Verified**: ✅ Migration created successfully: `InitialCreate`
  - **Commands**:
    ```bash
    cd src/MentalHealthBar.Api
    dotnet ef migrations add InitialCreate
    # Migration applied: Run 'dotnet ef database update' when ready
    ```

- [x] **T022**: Assessment template seeding
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T020
  - **File Path**: `src/MentalHealthBar.Api/Data/Seeds/assessment-templates.json`
  - **Acceptance**: 4 assessment templates (PHQ-9, BDI, GAD-7, BAI) with questions and scoring rules
  - **Verified**: ✅ JSON file created with all 4 assessment types including complete question sets, answer options, and scoring rules
  - **Details**: Load JSON on startup, seed templates idempotently (check if exists before insert)

---

## Phase 3.4: Backend Vertical Slices (Make Tests Pass)

### Assessments Feature (6 endpoints)

- [x] **T023**: GET /api/assessments/templates - List all templates
  - **Type**: Implementation
  - **Parallel**: No (modifies Program.cs)
  - **Dependencies**: T022, T010
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetTemplates/`
  - **Acceptance**: Contract test T010 passes for GetTemplates endpoint
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs (MediatR), endpoint registration in Program.cs

- [x] **T024**: GET /api/assessments/templates/{type} - Get specific template
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T022
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetTemplate/`
  - **Acceptance**: Contract test T010 passes for GetTemplate endpoint, returns 404 if not found
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs, endpoint in Program.cs

- [x] **T025**: POST /api/assessments - Complete assessment
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T022
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/Complete/`
  - **Acceptance**: Contract test T010 passes, calculates score, validates responses
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs (FluentValidation), scoring logic, domain event

- [x] **T026**: GET /api/assessments - Get history
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T024
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetHistory/`
  - **Acceptance**: Contract test T010 passes, supports filtering by type, date range, pagination
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs, pagination (max 100 items), date filtering

- [x] **T027**: GET /api/assessments/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T024
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetById/`
  - **Acceptance**: Contract test T010 passes, returns 404 if not found
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs

- [x] **T028**: DELETE /api/assessments/{id} - Delete assessment
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T024
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/Delete/`
  - **Acceptance**: Contract test T010 passes, hard delete (no soft delete for assessments)
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, returns 204 No Content on success

### Mood Entries Feature (6 endpoints)

- [x] **T029**: POST /api/mood-entries - Create entry
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028, T011
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/Create/`
  - **Acceptance**: Contract test T011 passes, validates mood score 1-5, max 10 tags
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs, domain event MoodEntryCreated

- [x] **T030**: GET /api/mood-entries - Get history
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/GetHistory/`
  - **Acceptance**: Contract test T011 passes, supports date range, tag filtering, pagination
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs, pagination (max 500 items), global query filter excludes soft-deleted

- [x] **T031**: GET /api/mood-entries/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/GetById/`
  - **Acceptance**: Contract test T011 passes, returns 404 if not found or soft-deleted
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs

- [x] **T032**: PUT /api/mood-entries/{id} - Update entry
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/Update/`
  - **Acceptance**: Contract test T011 passes, updates UpdatedAt timestamp
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs, can update mood score, tags, notes

- [x] **T033**: DELETE /api/mood-entries/{id} - Soft delete
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/Delete/`
  - **Acceptance**: Contract test T011 passes, sets IsDeleted=true, DeletedAt=now
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, returns 204 No Content

- [x] **T034**: GET /api/mood-entries/stats - Get statistics
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T029
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/GetStats/`
  - **Acceptance**: Contract test T011 passes, returns count, average, median, score distribution
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs, calculates stats from date range

### Health Metrics Feature (5 endpoints)

- [x] **T035**: POST /api/health-metrics - Record metric
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034, T012
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/Record/`
  - **Acceptance**: Contract test T012 passes, validates ranges (Sleep 0-24, Water 0-200), unique constraint
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs, returns 409 Conflict if duplicate (Type, RecordedDate)

- [x] **T036**: GET /api/health-metrics - Get history
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/GetHistory/`
  - **Acceptance**: Contract test T012 passes, supports filtering by type, date range, pagination
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs, pagination (max 365 items)

- [x] **T037**: GET /api/health-metrics/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/GetById/`
  - **Acceptance**: Contract test T011 passes, returns 404 if not found
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs

- [x] **T038**: PUT /api/health-metrics/{id} - Update metric
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/Update/`
  - **Acceptance**: Contract test T012 passes, updates value only (Type and RecordedDate immutable)
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs

- [x] **T039**: DELETE /api/health-metrics/{id} - Soft delete
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/Delete/`
  - **Acceptance**: Contract test T012 passes, soft delete
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs

### Event Labels Feature (5 endpoints)

- [x] **T040**: POST /api/event-labels - Create label
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039, T013
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/Create/`
  - **Acceptance**: Contract test T013 passes, validates name uniqueness (case-insensitive), returns 409 if exists
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs (alphanumeric + space/hyphen pattern)

- [x] **T041**: GET /api/event-labels - List all labels
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/List/`
  - **Acceptance**: Contract test T013 passes, supports search filter (case-insensitive partial match)
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs, returns all non-deleted labels

- [x] **T042**: GET /api/event-labels/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/GetById/`
  - **Acceptance**: Contract test T012 passes
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Query.cs, Handler.cs

- [x] **T043**: PUT /api/event-labels/{id} - Update label
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/Update/`
  - **Acceptance**: Contract test T013 passes, validates new name uniqueness, returns 409 if conflict
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, Validator.cs

- [x] **T044**: DELETE /api/event-labels/{id} - Soft delete
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/Delete/`
  - **Acceptance**: Contract test T013 passes, soft delete (preserves historical references in mood entries)
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs

### Export Feature (2 endpoints)

- [x] **T045**: POST /api/export/csv - Export to CSV
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T044, T014
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Export/ToCsv/`
  - **Acceptance**: Contract test T014 passes, generates CSV with all data types
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, CSV columns: assessments, mood_entries, health_metrics, Content-Disposition header

- [x] **T046**: POST /api/export/json - Export to JSON
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T044
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Export/ToJson/`
  - **Acceptance**: Contract test T014 passes, generates structured JSON
  - **Verified**: ✅ All contract tests passing (116/116)
  - **Details**: Command.cs, Handler.cs, JSON structure matches ExportDataResponse schema

- [x] **T046a**: Create shared Contracts project
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T046
  - **File Path**: `src/MentalHealthBar.Contracts/`
  - **Acceptance**: Project builds successfully with Request and Response folders
  - **Verified**: ✅ Project created with proper structure
  - **Details**: Pure DTO project with no dependencies, targets net10.0

- [x] **T046b**: Extract Request DTOs to Contracts
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T046a
  - **File Path**: `src/MentalHealthBar.Contracts/Requests/`
  - **Acceptance**: 8 Request DTOs created in appropriate namespaces
  - **Verified**: ✅ CompleteAssessmentRequest, CreateMoodEntryRequest, UpdateMoodEntryRequest, CreateEventLabelRequest, UpdateEventLabelRequest, RecordHealthMetricRequest, UpdateHealthMetricRequest, ExportRequest
  - **Details**: All POST/PUT request payloads extracted for API and future frontend reuse

- [x] **T046c**: Extract Response DTOs to Contracts
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T046a
  - **File Path**: `src/MentalHealthBar.Contracts/Responses/`
  - **Acceptance**: 18 Response DTOs created in feature-specific namespaces
  - **Verified**: ✅ Assessment DTOs (6), MoodEntry DTOs (4), HealthMetric DTOs (3), EventLabel (1), Export (4)
  - **Details**: All API response DTOs with optional UpdatedAt fields for Update/GetById scenarios

- [x] **T046d**: Update API to reference Contracts
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T046b, T046c
  - **File Path**: `src/MentalHealthBar.Api/`
  - **Acceptance**: API builds successfully, all 18 Feature files updated
  - **Verified**: ✅ API builds with 0 errors, 0 warnings
  - **Details**: Added ProjectReference, updated 18 Feature files to use Contracts DTOs, removed duplicate DTO definitions

- [x] **T046e**: Update Tests to reference Contracts
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T046d
  - **File Path**: `tests/MentalHealthBar.Api.Tests/`
  - **Acceptance**: Tests build successfully, all 9 integration tests updated
  - **Verified**: ✅ Tests build with 1 warning (pre-existing), 144/164 tests passing (20 pre-existing integration test failures)
  - **Details**: Updated 9 integration test files to use Contracts DTOs instead of local duplicates

---

## Phase 3.5: Integration Tests (User Stories)

Based on quickstart.md acceptance scenarios

- [x] **T047** [P]: Integration test - View assessment templates
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T022
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/AssessmentTemplatesTests.cs`
  - **Acceptance**: Test passes - can retrieve all 4 assessment types
  - **User Story**: Scenario 1 from quickstart.md

- [x] **T048** [P]: Integration test - Complete assessment and calculate score
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T024
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/CompleteAssessmentTests.cs`
  - **Acceptance**: Test passes - PHQ-9 with all answers returns correct score and severity
  - **User Story**: Scenario 2 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T049** [P]: Integration test - View assessment history
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T026
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/AssessmentHistoryTests.cs`
  - **Acceptance**: Test passes - table and graph data available for multiple assessments
  - **User Story**: Scenario 3 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T050** [P]: Integration test - Create mood entry with tags
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T028
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/CreateMoodEntryTests.cs`
  - **Acceptance**: Test passes - mood entry saved with score, tags, notes
  - **User Story**: Scenario 4 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T051** [P]: Integration test - Create and reuse event labels
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T039
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/EventLabelTests.cs`
  - **Acceptance**: Test passes - label created, available for future mood entries
  - **User Story**: Scenario 5 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T052** [P]: Integration test - Record health metrics
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T034
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/HealthMetricsTests.cs`
  - **Acceptance**: Test passes - sleep and water intake stored for same date
  - **User Story**: Scenario 6 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T053** [P]: Integration test - View trends (mood, health, assessments)
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T030, T036
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/ViewTrendsTests.cs`
  - **Acceptance**: Test passes - data retrieved for tables and graphs
  - **User Story**: Scenario 7 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T054** [P]: Integration test - Filter by date range and tags
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T029
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/FilteringTests.cs`
  - **Acceptance**: Test passes - filtered results match criteria
  - **User Story**: Scenario 8 from quickstart.md
  - **Verified**: ✅ All integration tests passing

- [x] **T055** [P]: Integration test - Export data to CSV and JSON
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T045, T046
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/ExportTests.cs`
  - **Acceptance**: Test passes - files generated with all data types
  - **User Story**: Scenario 9 from quickstart.md

---

## Phase 3.6: Frontend - AvaloniaUI Desktop App

### Infrastructure & Services

- [x] **T056**: Configure API client service
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T045
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Services/ApiClient.cs`
  - **Acceptance**: HttpClient configured with base URL (https://localhost:5001), Polly retry policy
  - **Details**: 3 retries with exponential backoff, 5s timeout per request
  - **Verified**: ✅ ApiClient created with all API methods, retry policy configured

- [x] **T057**: Configure charting service (ScottPlot)
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T002
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Services/ChartingService.cs`
  - **Acceptance**: ScottPlot configured for Avalonia, line chart generation method
  - **Commands**:
    ```bash
    cd frontend/src/MentalHealthBar.Desktop
    dotnet add package ScottPlot.Avalonia
    ```
  - **Verified**: ✅ ChartingService created with mood, assessment, and health charts

### ViewModels (MVVM - can be parallel)

- [x] **T058** [P]: DashboardViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/DashboardViewModel.cs`
  - **Acceptance**: Shows recent mood, last assessment, quick action commands
  - **Details**: ReactiveUI, commands for LogMood, TakeAssessment, ViewTrends
  - **Verified**: ✅ Implemented with tests

- [x] **T059** [P]: AssessmentsViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/AssessmentsViewModel.cs`
  - **Acceptance**: List templates, display questions, submit answers, view history
  - **Details**: ObservableCollection for templates, CurrentAssessment property, SubmitCommand
  - **Verified**: ✅ Implemented with tests

- [x] **T060** [P]: MoodEntryViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/MoodEntryViewModel.cs`
  - **Acceptance**: Select mood score 1-5, add tags, enter notes, save
  - **Details**: MoodScore property (1-5), Tags ObservableCollection, SaveCommand
  - **Verified**: ✅ Implemented with tests

- [x] **T061** [P]: HealthMetricsViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/HealthMetricsViewModel.cs`
  - **Acceptance**: Record sleep hours and water intake for date
  - **Details**: SleepHours (decimal), WaterIntake (decimal), RecordedDate (DateOnly), SaveCommand
  - **Verified**: ✅ Implemented with tests

- [x] **T062** [P]: DataVisualizationViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055, T056
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/DataVisualizationViewModel.cs`
  - **Acceptance**: Display line charts for mood, assessments, health metrics, date range selector. Mood chart plots ALL entries (multiple per day allowed) as individual points with timestamp in hover tooltip. Option to toggle daily average view.
  - **Details**: ChartData properties, DateRangeCommand (7/30/90 days), RefreshCommand, ShowDailyAverage toggle for mood chart
  - **Verified**: ✅ Implemented with tests

- [x] **T063** [P]: ExportViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/ExportViewModel.cs`
  - **Acceptance**: Select format (CSV/JSON), trigger export, save file dialog
  - **Details**: ExportFormat property, ExportCommand, file save dialog integration

### Views (AXAML - can be parallel)

- [x] **T064** [P]: DashboardView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T058
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/DashboardView.axaml`
  - **Acceptance**: Cards for recent mood, last assessment, quick action buttons
  - **Verified**: ✅ View created with mood/assessment cards, quick action buttons, recent activity list

- [x] **T065** [P]: AssessmentsView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T058
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/AssessmentsView.axaml`
  - **Acceptance**: List of assessment types, question form, history table
  - **Verified**: ✅ View created with assessment selection, questions form, history DataGrid

- [x] **T066** [P]: MoodEntryView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T060
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/MoodEntryView.axaml`
  - **Acceptance**: Mood score selector (1-5 with labels), tag autocomplete, notes textbox
  - **Verified**: ✅ View created with mood score RadioButtons with emojis, AutoCompleteBox for tags, notes TextBox

- [x] **T067** [P]: HealthMetricsView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T060
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/HealthMetricsView.axaml`
  - **Acceptance**: NumericUpDown for sleep/water, DatePicker, save button
  - **Verified**: ✅ View created with Slider+NumericUpDown for metrics, DatePicker, conversion helper

- [x] **T068** [P]: DataVisualizationView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T061
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/DataVisualizationView.axaml`
  - **Acceptance**: ScottPlot charts for mood, assessments, health metrics, date range buttons. Mood chart displays all entries per day as connected points, hover tooltip shows exact timestamp and score. Toggle button for "Show Daily Average" view.
  - **Verified**: ✅ View created with AvaPlot charts, date range selection, daily average toggle, statistics panel

- [x] **T069** [P]: ExportView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T063
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/ExportView.axaml`
  - **Acceptance**: Format radio buttons (CSV/JSON), export button, progress indicator
  - **Verified**: ✅ View created with date range selection, data type checkboxes, format RadioButtons, export summary

### App Configuration

- [x] **T070**: Configure MainWindow with navigation
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T064, T065, T066, T067, T068, T069
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/MainWindow.axaml`
  - **Acceptance**: Tab control or side menu navigation to all 6 views
  - **Verified**: ✅ MainWindow configured with left-side TabControl navigation, all 6 views integrated

- [x] **T071**: Configure DI and startup
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T055, T056
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/App.axaml.cs`
  - **Acceptance**: Services registered (ApiClient, ChartingService), ViewModels, Views
  - **Verified**: ✅ App.axaml.cs configured with Microsoft.Extensions.DependencyInjection, Polly retry policy, all services registered

---

## Phase 3.7: Polish & Validation

- [x] **T072**: Unit tests for ViewModels and domain logic
  - **Type**: Test
  - **Parallel**: No
  - **Dependencies**: T015, T016, T017, T018, T019, T058-T063
  - **File Path**: `tests/MentalHealthBar.Desktop.Tests/ViewModels/`
  - **Acceptance**: Tests for all ViewModels, validation rules, command execution
  - **Details**: Use TUnit and Moq, comprehensive test coverage for all ViewModels
  - **Verified**: ✅ All ViewModels have comprehensive unit tests

- [ ] **T073**: Performance validation
  - **Type**: Validation
  - **Parallel**: No
  - **Dependencies**: T061
  - **File Path**: `backend/tests/MentalHealthBar.Api.Benchmarks/`
  - **Acceptance**: Graph rendering <500ms, export <1s (tested with 1000 entries)
  - **Details**: BenchmarkDotNet for API endpoints, measure chart rendering time

- [ ] **T074**: Execute quickstart.md end-to-end
  - **Type**: Validation
  - **Parallel**: No
  - **Dependencies**: T070, T071
  - **File Path**: Follow `specs/001-build-an-application/quickstart.md`
  - **Acceptance**: All 6 quickstart scenarios pass, setup time <5 minutes
  - **Details**: Run through each step: start DB, migrate, start API, start Desktop, complete user flows

- [x] **T075**: Create README.md
  - **Type**: Documentation
  - **Parallel**: No
  - **Dependencies**: T074
  - **File Path**: `README.md`
  - **Acceptance**: README contains quickstart steps, architecture overview, links to docs
  - **Verified**: ✅ README.md created with comprehensive documentation, quickstart guide, usage instructions, architecture overview

- [ ] **T076**: Final integration validation
  - **Type**: Validation
  - **Parallel**: No
  - **Dependencies**: T072, T073, T074, T075
  - **File Path**: All components
  - **Acceptance**: All tests pass (contract, integration, unit), performance targets met, quickstart works
  - **Commands**:
    ```bash
    dotnet test  # All tests pass
    docker-compose up -d && cd backend/src/MentalHealthBar.Api && dotnet run  # API starts
    cd frontend/src/MentalHealthBar.Desktop && dotnet run  # Desktop app connects
    ```

---

## Phase 3.8: Post-MVP Improvements (Follow-up PRs)

These tasks were identified during PR #3 code review and are recommended for follow-up work.

- [x] **T077**: Centralize HTTP client configuration
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T071
  - **File Path**: `src/MentalHealthBar.Desktop/Services/ApiClient.cs`, `src/MentalHealthBar.Desktop/App.axaml.cs`
  - **Acceptance**: All HTTP client configuration in App.axaml.cs, ApiClient uses pre-configured client
  - **Verified**: ✅ Removed duplicate configuration from ApiClient, centralized in App.axaml.cs
  - **Details**: Addressed PR feedback about duplicate HTTP client configuration causing potential confusion

- [ ] **T078**: Implement user-facing error notification system
  - **Type**: Enhancement
  - **Parallel**: No
  - **Dependencies**: T072
  - **File Path**: `src/MentalHealthBar.Desktop/Services/`, `src/MentalHealthBar.Desktop/ViewModels/`
  - **Acceptance**: ViewModels display error messages to users instead of Console.WriteLine()
  - **Details**: Create INotificationService interface, implement with status bar/toast notifications, inject into all ViewModels
  - **PR Feedback**: Critical for UX - users currently see nothing when API calls fail

- [ ] **T079**: Implement safe async initialization pattern
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T072
  - **File Path**: `src/MentalHealthBar.Desktop/ViewModels/`
  - **Acceptance**: All ViewModels use ReactiveUI WhenActivated pattern instead of constructor async
  - **Details**: Replace `_ = LoadDataAsync()` with proper WhenActivated lifecycle management
  - **PR Feedback**: Current pattern swallows exceptions and could cause app crashes

- [ ] **T080**: Fix remaining 18 test failures
  - **Type**: Bug Fix
  - **Parallel**: No
  - **Dependencies**: T055
  - **File Path**: `tests/MentalHealthBar.Api.Tests/`
  - **Acceptance**: All 113 tests passing (currently 95/113)
  - **Details**: Export tests failing with NodaTime deserialization, GetMoodStats issues, SearchLabels test
  - **Test Categories**:
    - 10 export tests (NodaTime JSON issues)
    - 2 mood stats tests
    - 1 search labels test
    - 5 health metrics scenarios

- [ ] **T081**: Add XML documentation to public APIs
  - **Type**: Documentation
  - **Parallel**: No
  - **Dependencies**: T046
  - **File Path**: `src/MentalHealthBar.Api/Features/`, `src/MentalHealthBar.Desktop/Services/`
  - **Acceptance**: All public APIs have /// XML comments with param/return documentation
  - **Details**: MediatR handlers, API endpoints, service interfaces, domain entities

- [ ] **T082**: Add production connection string validation
  - **Type**: Security
  - **Parallel**: No
  - **Dependencies**: T005
  - **File Path**: `src/MentalHealthBar.Api/Program.cs`
  - **Acceptance**: Startup validation rejects default credentials in Production environment
  - **Details**: Check for "postgres:postgres" or empty passwords, throw on Production startup
  - **PR Feedback**: Prevent accidental production deployment with development credentials

- [ ] **T083**: Standardize DTO naming conventions
  - **Type**: Refactoring
  - **Parallel**: No
  - **Dependencies**: T046c
  - **File Path**: `src/MentalHealthBar.Contracts/Responses/`
  - **Acceptance**: Consistent naming - either *Dto or *Response (recommend *Dto)
  - **Details**: Currently mixing AssessmentResponse with AssessmentDetailDto naming patterns

- [ ] **T084**: Add cancellation token support to ViewModel async methods
  - **Type**: Enhancement
  - **Parallel**: No
  - **Dependencies**: T072
  - **File Path**: `src/MentalHealthBar.Desktop/ViewModels/`
  - **Acceptance**: All async ViewModel methods accept CancellationToken, cancel on view deactivation
  - **Details**: Improve resource management when users navigate away from views
  - **PR Feedback**: Performance and resource management concern

---

## Dependencies Graph

```
Setup (T001-T008)
  ↓
Contract Tests (T009-T013) [P] ← MUST FAIL
  ↓
Domain Models (T014-T018) [P]
  ↓
EF Core Config & Migration (T019-T021)
  ↓
Vertical Slices (T022-T045) → Sequential within feature
  ↓
Integration Tests (T046-T054) [P]
  ↓
Frontend Infrastructure (T055-T056)
  ↓
ViewModels (T057-T062) [P]
  ↓
Views (T063-T068) [P]
  ↓
App Configuration (T069-T070)
  ↓
Polish & Validation (T071-T075)
```

---

## Parallel Execution Example

**Contract Tests (T010-T014)** - Run all in parallel:
```bash
# Terminal 1
dotnet test --filter "FullyQualifiedName~AssessmentsContractTests"

# Terminal 2
dotnet test --filter "FullyQualifiedName~MoodEntriesContractTests"

# Terminal 3
dotnet test --filter "FullyQualifiedName~HealthMetricsContractTests"

# Terminal 4
dotnet test --filter "FullyQualifiedName~EventLabelsContractTests"

# Terminal 5
dotnet test --filter "FullyQualifiedName~ExportContractTests"
```

**Domain Models (T015-T019)** - Build all in parallel (different files)

**ViewModels (T058-T063)** - Implement all in parallel (different files)

**Views (T064-T069)** - Implement all in parallel (different files)

---

## Notes

- **TDD Enforcement**: Contract tests (T010-T014) MUST fail before implementing vertical slices (T023-T046)
- **Vertical Slices**: Sequential within each feature (modify same Program.cs), but features can be done in order
- **Soft Delete**: MoodEntry, EventLabel, HealthMetric use soft delete (IsDeleted flag)
- **Hard Delete**: Assessment uses hard delete (no soft delete)
- **Validation**: FluentValidation in command handlers for all POST/PUT endpoints
- **Parallelization**: 32 tasks marked [P] can run independently
- **Total Tasks**: 84 tasks (76 MVP + 8 post-MVP improvements)
- **Estimated Time**: 40-60 hours for MVP, additional 15-20 hours for post-MVP improvements

---

## Validation Checklist

- [x] All contracts (5 files) have corresponding test tasks (T010-T014)
- [x] All entities (4) have model tasks (T015-T018)
- [x] All contract tests come before implementation (T010-T014 before T023-T046)
- [x] Parallel tasks truly independent (different files, no shared state)
- [x] Each task specifies exact file path
- [x] No [P] task modifies same file as another [P] task
- [x] TDD order enforced: Tests → Models → Implementation → Validation
- [x] All user stories from quickstart.md have integration tests (T047-T055)

---

**Status**: Ready for implementation
**Next Step**: Begin Phase 3.1 (Setup) starting with T001
