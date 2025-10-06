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

- [ ] **T010** [P]: Contract test for Assessments API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Contract/AssessmentsContractTests.cs`
  - **Acceptance**: Tests exist for all assessment endpoints and FAIL (404/501)
  - **Endpoints to test**:
    - GET /api/assessments/templates
    - GET /api/assessments/templates/{type}
    - POST /api/assessments
    - GET /api/assessments
    - GET /api/assessments/{id}
    - DELETE /api/assessments/{id}

- [ ] **T011** [P]: Contract test for Mood Entries API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Contract/MoodEntriesContractTests.cs`
  - **Acceptance**: Tests exist for all mood entry endpoints and FAIL
  - **Endpoints to test**:
    - POST /api/mood-entries
    - GET /api/mood-entries
    - GET /api/mood-entries/{id}
    - PUT /api/mood-entries/{id}
    - DELETE /api/mood-entries/{id}
    - GET /api/mood-entries/stats

- [ ] **T012** [P]: Contract test for Health Metrics API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Contract/HealthMetricsContractTests.cs`
  - **Acceptance**: Tests exist for all health metric endpoints and FAIL
  - **Endpoints to test**:
    - POST /api/health-metrics
    - GET /api/health-metrics
    - GET /api/health-metrics/{id}
    - PUT /api/health-metrics/{id}
    - DELETE /api/health-metrics/{id}

- [ ] **T013** [P]: Contract test for Event Labels API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Contract/EventLabelsContractTests.cs`
  - **Acceptance**: Tests exist for all event label endpoints and FAIL
  - **Endpoints to test**:
    - POST /api/event-labels
    - GET /api/event-labels
    - GET /api/event-labels/{id}
    - PUT /api/event-labels/{id}
    - DELETE /api/event-labels/{id}

- [ ] **T014** [P]: Contract test for Export API
  - **Type**: Test
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T001, T006
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Contract/ExportContractTests.cs`
  - **Acceptance**: Tests exist for export endpoints and FAIL
  - **Endpoints to test**:
    - POST /api/export/csv
    - POST /api/export/json

---

## Phase 3.3: Domain Models (TDD)

**CRITICAL**: Build domain models BEFORE vertical slices

- [ ] **T015** [P]: Assessment domain model with scoring logic
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `backend/src/MentalHealthBar.Api/Domain/Assessments/Assessment.cs`
  - **Acceptance**: Assessment entity with scoring calculation for all 4 types (PHQ9, BDI, GAD7, BAI)
  - **Details**: Include AssessmentType enum, SeverityLevel enum, CalculateScore() method, validation

- [ ] **T016** [P]: MoodEntry domain model
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `backend/src/MentalHealthBar.Api/Domain/MoodEntries/MoodEntry.cs`
  - **Acceptance**: MoodEntry entity with soft delete, tag support (denormalized)
  - **Details**: Validation: MoodScore 1-5, RecordedAt not >30 days future, max 10 tags, max 500 char notes

- [ ] **T017** [P]: EventLabel domain model
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `backend/src/MentalHealthBar.Api/Domain/EventLabels/EventLabel.cs`
  - **Acceptance**: EventLabel entity with soft delete, case-insensitive uniqueness
  - **Details**: Validation: Name 1-50 chars, alphanumeric + space/hyphen, unique (case-insensitive)

- [ ] **T018** [P]: HealthMetric domain model
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `backend/src/MentalHealthBar.Api/Domain/HealthMetrics/HealthMetric.cs`
  - **Acceptance**: HealthMetric entity with unique constraint on (Type, RecordedDate)
  - **Details**: MetricType enum (SleepHours, WaterIntakeOz), validation: Sleep 0-24, Water 0-200

- [ ] **T019** [P]: Value objects (MoodScore, DateRange)
  - **Type**: Implementation
  - **Parallel**: Yes (independent file)
  - **Dependencies**: T004
  - **File Path**: `backend/src/MentalHealthBar.Api/Domain/Common/ValueObjects.cs`
  - **Acceptance**: MoodScore with label mapping (1=Worst...5=Best), DateRange factory methods
  - **Details**: MoodScore validation throws ArgumentOutOfRangeException, DateRange: Last7Days(), Last30Days(), Last90Days(), AllTime()

- [ ] **T020**: EF Core entity configurations
  - **Type**: Implementation
  - **Parallel**: No (modifies DbContext)
  - **Dependencies**: T014, T015, T016, T017
  - **File Path**: `backend/src/MentalHealthBar.Api/Infrastructure/Data/Configurations/`
  - **Acceptance**: All 4 entities configured with indexes, JSONB for Assessment responses, global query filter for soft delete
  - **Details**: AssessmentConfiguration, MoodEntryConfiguration, EventLabelConfiguration, HealthMetricConfiguration

- [ ] **T021**: Initial database migration
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T020
  - **File Path**: `backend/src/MentalHealthBar.Api/Migrations/`
  - **Acceptance**: Migration creates all tables with indexes
  - **Commands**:
    ```bash
    cd backend/src/MentalHealthBar.Api
    dotnet ef migrations add InitialCreate
    dotnet ef database update
    ```

- [ ] **T022**: Assessment template seeding
  - **Type**: Setup
  - **Parallel**: No
  - **Dependencies**: T020
  - **File Path**: `backend/src/MentalHealthBar.Api/Data/Seeds/assessment-templates.json`
  - **Acceptance**: 4 assessment templates (PHQ-9, BDI, GAD-7, BAI) with questions and scoring rules
  - **Details**: Load JSON on startup, seed templates idempotently (check if exists before insert)

---

## Phase 3.4: Backend Vertical Slices (Make Tests Pass)

### Assessments Feature (6 endpoints)

- [ ] **T023**: GET /api/assessments/templates - List all templates
  - **Type**: Implementation
  - **Parallel**: No (modifies Program.cs)
  - **Dependencies**: T022, T010
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetTemplates/`
  - **Acceptance**: Contract test T010 passes for GetTemplates endpoint
  - **Details**: Query.cs, Handler.cs (MediatR), endpoint registration in Program.cs

- [ ] **T024**: GET /api/assessments/templates/{type} - Get specific template
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T022
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetTemplate/`
  - **Acceptance**: Contract test T010 passes for GetTemplate endpoint, returns 404 if not found
  - **Details**: Query.cs, Handler.cs, endpoint in Program.cs

- [ ] **T025**: POST /api/assessments - Complete assessment
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T022
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/Complete/`
  - **Acceptance**: Contract test T010 passes, calculates score, validates responses
  - **Details**: Command.cs, Handler.cs, Validator.cs (FluentValidation), scoring logic, domain event

- [ ] **T026**: GET /api/assessments - Get history
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T024
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetHistory/`
  - **Acceptance**: Contract test T010 passes, supports filtering by type, date range, pagination
  - **Details**: Query.cs, Handler.cs, pagination (max 100 items), date filtering

- [ ] **T027**: GET /api/assessments/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T024
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/GetById/`
  - **Acceptance**: Contract test T010 passes, returns 404 if not found
  - **Details**: Query.cs, Handler.cs

- [ ] **T028**: DELETE /api/assessments/{id} - Delete assessment
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T024
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Assessments/Delete/`
  - **Acceptance**: Contract test T010 passes, hard delete (no soft delete for assessments)
  - **Details**: Command.cs, Handler.cs, returns 204 No Content on success

### Mood Entries Feature (6 endpoints)

- [ ] **T029**: POST /api/mood-entries - Create entry
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028, T011
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/Create/`
  - **Acceptance**: Contract test T011 passes, validates mood score 1-5, max 10 tags
  - **Details**: Command.cs, Handler.cs, Validator.cs, domain event MoodEntryCreated

- [ ] **T030**: GET /api/mood-entries - Get history
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/GetHistory/`
  - **Acceptance**: Contract test T011 passes, supports date range, tag filtering, pagination
  - **Details**: Query.cs, Handler.cs, pagination (max 500 items), global query filter excludes soft-deleted

- [ ] **T031**: GET /api/mood-entries/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/GetById/`
  - **Acceptance**: Contract test T011 passes, returns 404 if not found or soft-deleted
  - **Details**: Query.cs, Handler.cs

- [ ] **T032**: PUT /api/mood-entries/{id} - Update entry
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/Update/`
  - **Acceptance**: Contract test T011 passes, updates UpdatedAt timestamp
  - **Details**: Command.cs, Handler.cs, Validator.cs, can update mood score, tags, notes

- [ ] **T033**: DELETE /api/mood-entries/{id} - Soft delete
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T028
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/Delete/`
  - **Acceptance**: Contract test T011 passes, sets IsDeleted=true, DeletedAt=now
  - **Details**: Command.cs, Handler.cs, returns 204 No Content

- [ ] **T034**: GET /api/mood-entries/stats - Get statistics
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T029
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/MoodEntries/GetStats/`
  - **Acceptance**: Contract test T011 passes, returns count, average, median, score distribution
  - **Details**: Query.cs, Handler.cs, calculates stats from date range

### Health Metrics Feature (5 endpoints)

- [ ] **T035**: POST /api/health-metrics - Record metric
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034, T012
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/Record/`
  - **Acceptance**: Contract test T012 passes, validates ranges (Sleep 0-24, Water 0-200), unique constraint
  - **Details**: Command.cs, Handler.cs, Validator.cs, returns 409 Conflict if duplicate (Type, RecordedDate)

- [ ] **T036**: GET /api/health-metrics - Get history
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/GetHistory/`
  - **Acceptance**: Contract test T012 passes, supports filtering by type, date range, pagination
  - **Details**: Query.cs, Handler.cs, pagination (max 365 items)

- [ ] **T037**: GET /api/health-metrics/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/GetById/`
  - **Acceptance**: Contract test T011 passes, returns 404 if not found
  - **Details**: Query.cs, Handler.cs

- [ ] **T038**: PUT /api/health-metrics/{id} - Update metric
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/Update/`
  - **Acceptance**: Contract test T012 passes, updates value only (Type and RecordedDate immutable)
  - **Details**: Command.cs, Handler.cs, Validator.cs

- [ ] **T039**: DELETE /api/health-metrics/{id} - Soft delete
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T034
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/HealthMetrics/Delete/`
  - **Acceptance**: Contract test T012 passes, soft delete
  - **Details**: Command.cs, Handler.cs

### Event Labels Feature (5 endpoints)

- [ ] **T040**: POST /api/event-labels - Create label
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039, T013
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/Create/`
  - **Acceptance**: Contract test T013 passes, validates name uniqueness (case-insensitive), returns 409 if exists
  - **Details**: Command.cs, Handler.cs, Validator.cs (alphanumeric + space/hyphen pattern)

- [ ] **T041**: GET /api/event-labels - List all labels
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/List/`
  - **Acceptance**: Contract test T013 passes, supports search filter (case-insensitive partial match)
  - **Details**: Query.cs, Handler.cs, returns all non-deleted labels

- [ ] **T042**: GET /api/event-labels/{id} - Get by ID
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/GetById/`
  - **Acceptance**: Contract test T012 passes
  - **Details**: Query.cs, Handler.cs

- [ ] **T043**: PUT /api/event-labels/{id} - Update label
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/Update/`
  - **Acceptance**: Contract test T013 passes, validates new name uniqueness, returns 409 if conflict
  - **Details**: Command.cs, Handler.cs, Validator.cs

- [ ] **T044**: DELETE /api/event-labels/{id} - Soft delete
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T039
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/EventLabels/Delete/`
  - **Acceptance**: Contract test T013 passes, soft delete (preserves historical references in mood entries)
  - **Details**: Command.cs, Handler.cs

### Export Feature (2 endpoints)

- [ ] **T045**: POST /api/export/csv - Export to CSV
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T044, T014
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Export/ToCsv/`
  - **Acceptance**: Contract test T014 passes, generates CSV with all data types
  - **Details**: Command.cs, Handler.cs, CSV columns: assessments, mood_entries, health_metrics, Content-Disposition header

- [ ] **T046**: POST /api/export/json - Export to JSON
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T044
  - **File Path**: `backend/src/MentalHealthBar.Api/Features/Export/ToJson/`
  - **Acceptance**: Contract test T014 passes, generates structured JSON
  - **Details**: Command.cs, Handler.cs, JSON structure matches ExportDataResponse schema

---

## Phase 3.5: Integration Tests (User Stories)

Based on quickstart.md acceptance scenarios

- [ ] **T047** [P]: Integration test - View assessment templates
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T022
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/AssessmentTemplatesTests.cs`
  - **Acceptance**: Test passes - can retrieve all 4 assessment types
  - **User Story**: Scenario 1 from quickstart.md

- [ ] **T048** [P]: Integration test - Complete assessment and calculate score
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T024
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/CompleteAssessmentTests.cs`
  - **Acceptance**: Test passes - PHQ-9 with all answers returns correct score and severity
  - **User Story**: Scenario 2 from quickstart.md

- [ ] **T049** [P]: Integration test - View assessment history
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T026
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/AssessmentHistoryTests.cs`
  - **Acceptance**: Test passes - table and graph data available for multiple assessments
  - **User Story**: Scenario 3 from quickstart.md

- [ ] **T050** [P]: Integration test - Create mood entry with tags
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T028
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/CreateMoodEntryTests.cs`
  - **Acceptance**: Test passes - mood entry saved with score, tags, notes
  - **User Story**: Scenario 4 from quickstart.md

- [ ] **T051** [P]: Integration test - Create and reuse event labels
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T039
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/EventLabelTests.cs`
  - **Acceptance**: Test passes - label created, available for future mood entries
  - **User Story**: Scenario 5 from quickstart.md

- [ ] **T052** [P]: Integration test - Record health metrics
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T034
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/HealthMetricsTests.cs`
  - **Acceptance**: Test passes - sleep and water intake stored for same date
  - **User Story**: Scenario 6 from quickstart.md

- [ ] **T053** [P]: Integration test - View trends (mood, health, assessments)
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T030, T036
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/ViewTrendsTests.cs`
  - **Acceptance**: Test passes - data retrieved for tables and graphs
  - **User Story**: Scenario 7 from quickstart.md

- [ ] **T054** [P]: Integration test - Filter by date range and tags
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T029
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/FilteringTests.cs`
  - **Acceptance**: Test passes - filtered results match criteria
  - **User Story**: Scenario 8 from quickstart.md

- [ ] **T055** [P]: Integration test - Export data to CSV and JSON
  - **Type**: Test
  - **Parallel**: Yes
  - **Dependencies**: T045, T046
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Integration/ExportTests.cs`
  - **Acceptance**: Test passes - files generated with all data types
  - **User Story**: Scenario 9 from quickstart.md

---

## Phase 3.6: Frontend - AvaloniaUI Desktop App

### Infrastructure & Services

- [ ] **T056**: Configure API client service
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T045
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Services/ApiClient.cs`
  - **Acceptance**: HttpClient configured with base URL (https://localhost:5001), Polly retry policy
  - **Details**: 3 retries with exponential backoff, 5s timeout per request

- [ ] **T057**: Configure charting service (ScottPlot)
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

### ViewModels (MVVM - can be parallel)

- [ ] **T058** [P]: DashboardViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/DashboardViewModel.cs`
  - **Acceptance**: Shows recent mood, last assessment, quick action commands
  - **Details**: ReactiveUI, commands for LogMood, TakeAssessment, ViewTrends

- [ ] **T059** [P]: AssessmentsViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/AssessmentsViewModel.cs`
  - **Acceptance**: List templates, display questions, submit answers, view history
  - **Details**: ObservableCollection for templates, CurrentAssessment property, SubmitCommand

- [ ] **T060** [P]: MoodEntryViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/MoodEntryViewModel.cs`
  - **Acceptance**: Select mood score 1-5, add tags, enter notes, save
  - **Details**: MoodScore property (1-5), Tags ObservableCollection, SaveCommand

- [ ] **T061** [P]: HealthMetricsViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/HealthMetricsViewModel.cs`
  - **Acceptance**: Record sleep hours and water intake for date
  - **Details**: SleepHours (decimal), WaterIntake (decimal), RecordedDate (DateOnly), SaveCommand

- [ ] **T062** [P]: DataVisualizationViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055, T056
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/DataVisualizationViewModel.cs`
  - **Acceptance**: Display line charts for mood, assessments, health metrics, date range selector. Mood chart plots ALL entries (multiple per day allowed) as individual points with timestamp in hover tooltip. Option to toggle daily average view.
  - **Details**: ChartData properties, DateRangeCommand (7/30/90 days), RefreshCommand, ShowDailyAverage toggle for mood chart

- [ ] **T063** [P]: ExportViewModel
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T055
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/ViewModels/ExportViewModel.cs`
  - **Acceptance**: Select format (CSV/JSON), trigger export, save file dialog
  - **Details**: ExportFormat property, ExportCommand, file save dialog integration

### Views (AXAML - can be parallel)

- [ ] **T064** [P]: DashboardView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T058
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/DashboardView.axaml`
  - **Acceptance**: Cards for recent mood, last assessment, quick action buttons
  - **Details**: Bind to DashboardViewModel, responsive layout

- [ ] **T065** [P]: AssessmentsView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T058
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/AssessmentsView.axaml`
  - **Acceptance**: List of assessment types, question form, history table
  - **Details**: ItemsControl for questions, DataGrid for history

- [ ] **T066** [P]: MoodEntryView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T060
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/MoodEntryView.axaml`
  - **Acceptance**: Mood score selector (1-5 with labels), tag autocomplete, notes textbox
  - **Details**: RadioButtons or Slider for mood score, AutoCompleteBox for tags

- [ ] **T067** [P]: HealthMetricsView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T060
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/HealthMetricsView.axaml`
  - **Acceptance**: NumericUpDown for sleep/water, DatePicker, save button
  - **Details**: Validation for ranges (Sleep 0-24, Water 0-200)

- [ ] **T068** [P]: DataVisualizationView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T061
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/DataVisualizationView.axaml`
  - **Acceptance**: ScottPlot charts for mood, assessments, health metrics, date range buttons. Mood chart displays all entries per day as connected points, hover tooltip shows exact timestamp and score. Toggle button for "Show Daily Average" view.
  - **Details**: AvaPlot control from ScottPlot.Avalonia, ComboBox for date range, CheckBox for daily average toggle, configure plot markers with hover labels

- [ ] **T069** [P]: ExportView
  - **Type**: Implementation
  - **Parallel**: Yes
  - **Dependencies**: T063
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/ExportView.axaml`
  - **Acceptance**: Format radio buttons (CSV/JSON), export button, progress indicator
  - **Details**: SaveFileDialog integration

### App Configuration

- [ ] **T070**: Configure MainWindow with navigation
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T064, T065, T066, T067, T068, T069
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/Views/MainWindow.axaml`
  - **Acceptance**: Tab control or side menu navigation to all 6 views
  - **Details**: TabControl with headers: Dashboard, Assessments, Mood, Health, Trends, Export

- [ ] **T071**: Configure DI and startup
  - **Type**: Implementation
  - **Parallel**: No
  - **Dependencies**: T055, T056
  - **File Path**: `frontend/src/MentalHealthBar.Desktop/App.axaml.cs`
  - **Acceptance**: Services registered (ApiClient, ChartingService), ViewModels, Views
  - **Details**: Use Microsoft.Extensions.DependencyInjection

---

## Phase 3.7: Polish & Validation

- [ ] **T072**: Unit tests for domain logic
  - **Type**: Test
  - **Parallel**: No
  - **Dependencies**: T015, T016, T017, T018, T019
  - **File Path**: `backend/tests/MentalHealthBar.Api.Tests/Unit/`
  - **Acceptance**: Tests for Assessment scoring (all 4 types), MoodScore value object, validation rules
  - **Details**: Use TUnit and Moq, test PHQ9/BDI/GAD7/BAI scoring algorithms

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

- [ ] **T075**: Create README.md
  - **Type**: Documentation
  - **Parallel**: No
  - **Dependencies**: T074
  - **File Path**: `README.md`
  - **Acceptance**: README contains quickstart steps, architecture overview, links to docs
  - **Details**: Copy content from quickstart.md, add badges, screenshots (optional)

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
- **Total Tasks**: 76 tasks
- **Estimated Time**: 40-60 hours for full implementation

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
