
# Implementation Plan: Mental Health & Mood Tracking Application

**Branch**: `001-build-an-application` | **Date**: 2025-10-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/home/dwalleck/repos/mental-health-bar/specs/001-build-an-application/spec.md`

## Execution Flow (/plan command scope)
```
1. Load feature spec from Input path
   → If not found: ERROR "No feature spec at {path}"
2. Fill Technical Context (scan for NEEDS CLARIFICATION)
   → Detect Project Type from file system structure or context (web=frontend+backend, mobile=app+api)
   → Set Structure Decision based on project type
3. Fill the Constitution Check section based on the content of the constitution document.
4. Evaluate Constitution Check section below
   → If violations exist: Document in Complexity Tracking
   → If no justification possible: ERROR "Simplify approach first"
   → Update Progress Tracking: Initial Constitution Check
5. Execute Phase 0 → research.md
   → If NEEDS CLARIFICATION remain: ERROR "Resolve unknowns"
6. Execute Phase 1 → contracts, data-model.md, quickstart.md, agent-specific template file (e.g., `CLAUDE.md` for Claude Code, `.github/copilot-instructions.md` for GitHub Copilot, `GEMINI.md` for Gemini CLI, `QWEN.md` for Qwen Code, or `AGENTS.md` for all other agents).
7. Re-evaluate Constitution Check section
   → If new violations: Refactor design, return to Phase 1
   → Update Progress Tracking: Post-Design Constitution Check
8. Plan Phase 2 → Describe task generation approach (DO NOT create tasks.md)
9. STOP - Ready for /tasks command
```

**IMPORTANT**: The /plan command STOPS at step 7. Phases 2-4 are executed by other commands:
- Phase 2: /tasks command creates tasks.md
- Phase 3-4: Implementation execution (manual or via tools)

## Summary
A single-user desktop application for tracking mental health and mood over time. Users complete standardized clinical assessments (PHQ-9, Beck Depression Inventory, GAD-7, Beck Anxiety Inventory), log daily mood scores (1-5 scale), track health metrics (sleep hours, water intake), and tag entries with life events. The application provides tables and graphs showing trends over time and supports data export to CSV and JSON formats. All data is stored locally with no authentication required.

## Technical Context
**Language/Version**: .NET 10 RC (C# 13)
**Primary Dependencies**: AvaloniaUI (frontend with MVVM), ASP.NET Core Minimal API (backend), MediatR, Entity Framework Core, Npgsql, FluentValidation, OneOf, Serilog, Polly, Humanizer, NewId
**Storage**: PostgreSQL (with optional TimescaleDB extension for time-series data optimization)
**Testing**: TUnit, Moq (FluentAssertions NOT allowed per requirements)
**Target Platform**: Cross-platform desktop (Windows, macOS, Linux via AvaloniaUI)
**Project Type**: Desktop application with separate frontend (AvaloniaUI) and backend (minimal API) - web architecture pattern
**Architecture Pattern**: Vertical Slice Architecture with Command/CommandHandler pattern (MediatR), "just-enough DDD" with domain events
**Performance Goals**: Sub-100ms UI responsiveness for data entry, <500ms for graph rendering, <1s for data export operations
**Constraints**: Single-user local deployment, offline-capable, data stored locally in PostgreSQL, no cloud dependencies
**Scale/Scope**: Personal use application, estimated 5-10 years of data (~2000 mood entries, ~500 assessments, ~3000 health metric entries), 6 main screens (Dashboard, Assessments, Mood Entry, Health Metrics, Data Visualization, Export)
**Package Management**: Centralized package management (Directory.Packages.props)

## Constitution Check
*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Ship It
- [ ] Can user complete a basic workflow (log mood, view data) in v0.1.0? **YES** - MVP focuses on core CRUD operations
- [ ] Is scope bounded to shippable subset? **YES** - Deferred: advanced analytics, data sharing, reminders/scheduling
- [ ] Are success criteria demonstrable? **YES** - User can log mood, complete assessment, view graph, export data

### Principle II: Developer Experience
- [ ] Can developer run app in <5 minutes? **MUST VALIDATE** - Need Docker Compose for PostgreSQL + API + Desktop app
- [ ] Are error messages helpful? **MUST IMPLEMENT** - FluentValidation for user-friendly validation messages
- [ ] README-first approach? **MUST CREATE** - quickstart.md will define ideal setup experience

### Principle III: Make It Work, Make It Right, Make It Fast
- [ ] Phase 1: Does it work? **TARGET** - Basic CRUD for all entities, simple line charts
- [ ] Phase 2: Is it right? **DEFER to v0.2.0** - Refactor after user feedback
- [ ] Phase 3: Is it fast? **DEFER** - Optimize only if data export >1s or graph render >500ms

### Principle IV: Collaborative Problem-Solving
- [x] NEEDS CLARIFICATION items from spec reviewed - 5 critical items resolved via /clarify
- [ ] Remaining ambiguities documented - 8 minor items deferred with reasonable defaults (see research.md)
- [ ] Trade-offs analyzed - Desktop vs Web (chose Desktop for offline-first), PostgreSQL vs SQLite (chose PostgreSQL for time-series capability)

### Principle VI: README-Driven Development
- [ ] quickstart.md will be written BEFORE implementation
- [ ] Documents ideal user setup: `docker-compose up` → launch app → see dashboard

### Principle IX: Test-First
- [ ] Contract tests for all API endpoints (Phase 1 - will fail before implementation)
- [ ] Integration tests for user stories (Phase 1 - will fail before implementation)
- [ ] Real scenarios, not toy tests - based on acceptance criteria from spec.md

### Prohibited Practices Check
- **Over-engineering?** NO - Using well-established patterns (vertical slice, CQRS-lite with MediatR)
- **Features "just in case"?** NO - Scope limited to spec requirements only
- **Complexity without justification?** POTENTIAL RISK - See Complexity Tracking for backend API rationale

**GATE STATUS**: ✅ PASS

**Post-Design Re-Evaluation**:
- ✅ Complexity justified (see Complexity Tracking table)
- ✅ Quickstart.md achieves <5 minute setup goal (docker-compose + dotnet run)
- ✅ API contracts follow RESTful conventions (simple, discoverable)
- ✅ Error responses use RFC 7807 ProblemDetails (helpful messages)
- ✅ Data model supports all functional requirements without over-engineering
- ✅ No additional complexity violations introduced during design
- ✅ Design enables v0.1.0 shipping (all core features covered)

## Project Structure

### Documentation (this feature)
```
specs/001-build-an-application/
├── spec.md              # Feature specification (input)
├── plan.md              # This file (/plan command output)
├── research.md          # Phase 0 output (/plan command)
├── data-model.md        # Phase 1 output (/plan command)
├── quickstart.md        # Phase 1 output (/plan command)
├── contracts/           # Phase 1 output (/plan command)
│   ├── assessments.yaml # OpenAPI spec for assessment endpoints
│   ├── mood-entries.yaml
│   ├── health-metrics.yaml
│   ├── event-labels.yaml
│   └── export.yaml
└── tasks.md             # Phase 2 output (/tasks command - NOT created by /plan)
```

### Source Code (repository root)
```
# Desktop application with backend API architecture
backend/
├── src/
│   └── MentalHealthBar.Api/
│       ├── Features/              # Vertical slices
│       │   ├── Assessments/
│       │   │   ├── Complete/      # Complete.Command + Handler
│       │   │   ├── GetHistory/
│       │   │   └── GetQuestions/
│       │   ├── MoodEntries/
│       │   │   ├── Create/
│       │   │   ├── GetHistory/
│       │   │   └── GetByDateRange/
│       │   ├── EventLabels/
│       │   │   ├── Create/
│       │   │   ├── List/
│       │   │   └── Delete/
│       │   ├── HealthMetrics/
│       │   │   ├── Record/
│       │   │   └── GetHistory/
│       │   └── Export/
│       │       ├── ToCsv/
│       │       └── ToJson/
│       ├── Domain/                # Domain models & events
│       │   ├── Assessments/
│       │   ├── MoodEntries/
│       │   ├── EventLabels/
│       │   ├── HealthMetrics/
│       │   └── Common/
│       ├── Infrastructure/        # EF Core, persistence
│       │   ├── Data/
│       │   └── Repositories/
│       ├── Program.cs             # Minimal API setup
│       └── appsettings.json
└── tests/
    ├── MentalHealthBar.Api.Tests/
    │   ├── Contract/              # API contract tests
    │   ├── Integration/           # End-to-end feature tests
    │   └── Unit/                  # Domain logic tests
    └── MentalHealthBar.Api.Benchmarks/ # Performance tests

frontend/
├── src/
│   └── MentalHealthBar.Desktop/
│       ├── ViewModels/            # MVVM ViewModels
│       │   ├── DashboardViewModel.cs
│       │   ├── AssessmentsViewModel.cs
│       │   ├── MoodEntryViewModel.cs
│       │   ├── HealthMetricsViewModel.cs
│       │   ├── DataVisualizationViewModel.cs
│       │   └── ExportViewModel.cs
│       ├── Views/                 # Avalonia XAML views
│       │   ├── DashboardView.axaml
│       │   ├── AssessmentsView.axaml
│       │   ├── MoodEntryView.axaml
│       │   ├── HealthMetricsView.axaml
│       │   ├── DataVisualizationView.axaml
│       │   └── ExportView.axaml
│       ├── Services/              # API client, state management
│       │   ├── ApiClient.cs
│       │   └── ChartingService.cs
│       ├── Models/                # DTOs matching API contracts
│       ├── App.axaml              # Application entry
│       └── Program.cs
└── tests/
    └── MentalHealthBar.Desktop.Tests/
        ├── ViewModels/            # ViewModel unit tests
        └── Integration/           # UI integration tests

# Shared infrastructure
docker-compose.yml                  # PostgreSQL + API for local dev
Directory.Build.props               # Common build properties
Directory.Packages.props            # Centralized package versions
global.json                         # .NET 10 RC SDK version
.editorconfig                       # Code style
README.md                           # Quick start guide
```

**Structure Decision**: Web-style architecture (frontend + backend) chosen for separation of concerns. Desktop app (AvaloniaUI) communicates with local API backend via HTTP. This enables:
- Backend reuse (future mobile app or web UI)
- Clear separation: UI logic (MVVM) from business logic (vertical slices)
- Independent testing of API and UI
- Offline-first: API runs locally, no internet required

## Phase 0: Outline & Research
1. **Extract unknowns from Technical Context** above:
   - For each NEEDS CLARIFICATION → research task
   - For each dependency → best practices task
   - For each integration → patterns task

2. **Generate and dispatch research agents**:
   ```
   For each unknown in Technical Context:
     Task: "Research {unknown} for {feature context}"
   For each technology choice:
     Task: "Find best practices for {tech} in {domain}"
   ```

3. **Consolidate findings** in `research.md` using format:
   - Decision: [what was chosen]
   - Rationale: [why chosen]
   - Alternatives considered: [what else evaluated]

**Output**: research.md with all NEEDS CLARIFICATION resolved

## Phase 1: Design & Contracts
*Prerequisites: research.md complete*

1. **Extract entities from feature spec** → `data-model.md`:
   - Entity name, fields, relationships
   - Validation rules from requirements
   - State transitions if applicable

2. **Generate API contracts** from functional requirements:
   - For each user action → endpoint
   - Use standard REST/GraphQL patterns
   - Output OpenAPI/GraphQL schema to `/contracts/`

3. **Generate contract tests** from contracts:
   - One test file per endpoint
   - Assert request/response schemas
   - Tests must fail (no implementation yet)

4. **Extract test scenarios** from user stories:
   - Each story → integration test scenario
   - Quickstart test = story validation steps

5. **Update agent file incrementally** (O(1) operation):
   - Run `.specify/scripts/bash/update-agent-context.sh claude`
     **IMPORTANT**: Execute it exactly as specified above. Do not add or remove any arguments.
   - If exists: Add only NEW tech from current plan
   - Preserve manual additions between markers
   - Update recent changes (keep last 3)
   - Keep under 150 lines for token efficiency
   - Output to repository root

**Output**: data-model.md, /contracts/*, failing tests, quickstart.md, agent-specific file

## Phase 2: Task Planning Approach
*This section describes what the /tasks command will do - DO NOT execute during /plan*

**Task Generation Strategy**:
The /tasks command will load `.specify/templates/tasks-template.md` and generate tasks based on:

1. **Infrastructure Setup** (6-8 tasks)
   - Project creation (.NET solution, projects)
   - Docker Compose setup (PostgreSQL)
   - Package management (Directory.Packages.props)
   - EF Core configuration and initial migration
   - Logging setup (Serilog)
   - API project setup (minimal API, Swagger)

2. **Contract Tests** (5 tasks - one per API contract file)
   - `assessments.yaml` → AssessmentsContractTests.cs [P]
   - `mood-entries.yaml` → MoodEntriesContractTests.cs [P]
   - `health-metrics.yaml` → HealthMetricsContractTests.cs [P]
   - `event-labels.yaml` → EventLabelsContractTests.cs [P]
   - `export.yaml` → ExportContractTests.cs [P]

3. **Domain Models** (5 tasks - one per entity + value objects)
   - Assessment domain model + scoring logic [P]
   - MoodEntry domain model + validation [P]
   - EventLabel domain model + uniqueness [P]
   - HealthMetric domain model + constraints [P]
   - Value objects (MoodScore, DateRange) [P]

4. **Database & Infrastructure** (4 tasks)
   - EF Core configurations for all entities
   - Assessment template seeding (PHQ-9, BDI, GAD-7, BAI)
   - Repository implementations (if needed)
   - Database migration for full schema

5. **Vertical Slices - Backend API** (15-18 tasks, grouped by feature)
   - Assessments: GetTemplates, GetTemplate, Complete, GetHistory, GetById, Delete [6 tasks]
   - MoodEntries: Create, GetHistory, GetById, Update, Delete, GetStats [6 tasks]
   - HealthMetrics: Record, GetHistory, GetById, Update, Delete [5 tasks]
   - EventLabels: Create, List, GetById, Update, Delete [5 tasks]
   - Export: ToCsv, ToJson [2 tasks]

6. **Integration Tests** (9 tasks - one per user story from spec.md)
   - Based on acceptance scenarios 1-9 from spec.md
   - Each test validates complete user workflow

7. **Frontend - AvaloniaUI** (12-15 tasks)
   - Project setup + MVVM infrastructure
   - API client service
   - ViewModels (Dashboard, Assessments, MoodEntry, HealthMetrics, DataVisualization, Export) [6 tasks]
   - Views (matching ViewModels) [6 tasks]
   - Chart integration (ScottPlot setup)

8. **End-to-End Validation** (3 tasks)
   - Quickstart.md execution test
   - Performance validation (graph rendering <500ms)
   - Export functionality test

**Ordering Strategy**:
1. **Phase 1**: Infrastructure (can't proceed without projects/database)
2. **Phase 2**: Contract tests (TDD - write failing tests first)
3. **Phase 3**: Domain models (needed by features)
4. **Phase 4**: Backend vertical slices (make contract tests pass)
5. **Phase 5**: Integration tests (validate user stories)
6. **Phase 6**: Frontend (UI layer depends on working API)
7. **Phase 7**: E2E validation (final acceptance)

**Parallelization**:
- Contract tests can run in parallel [P]
- Domain models can be built in parallel [P]
- Vertical slices within same feature group can be parallel (e.g., all assessment endpoints)
- Frontend ViewModels/Views can be parallel [P]

**Estimated Output**: 60-70 numbered, dependency-ordered tasks in tasks.md

**Dependencies**:
```
Infrastructure → Contract Tests
Infrastructure → Domain Models
Domain Models → Vertical Slices
Vertical Slices → Integration Tests
Vertical Slices → Frontend
Integration Tests → E2E Validation
Frontend → E2E Validation
```

**Task Format** (per template):
```
### Task N: [Task Name]
**Type**: [Setup|Test|Implementation|Validation]
**Parallel**: [Yes/No]
**Dependencies**: [Task numbers]
**Acceptance**: [How to verify completion]
**Commands**: [dotnet/docker commands to run]
```

**IMPORTANT**: This phase is executed by the /tasks command, NOT by /plan

## Phase 3+: Future Implementation
*These phases are beyond the scope of the /plan command*

**Phase 3**: Task execution (/tasks command creates tasks.md)  
**Phase 4**: Implementation (execute tasks.md following constitutional principles)  
**Phase 5**: Validation (run tests, execute quickstart.md, performance validation)

## Complexity Tracking
*Fill ONLY if Constitution Check has violations that must be justified*

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Backend API for single-user desktop app | Enables future extensibility (mobile/web clients), clean separation of UI/business logic, independent testing, follows user's architectural requirements | Direct database access from desktop app rejected - harder to test, couples UI to data layer, no API for future clients |
| PostgreSQL vs SQLite | TimescaleDB extension provides optimized time-series queries for mood/metrics trends, better performance for date-range queries at scale | SQLite rejected - no time-series optimization, performance degrades with years of data, limited analytics capabilities |
| Vertical Slice Architecture | User requirement - explicitly requested in technical constraints | N/A - this is a specified constraint, not a complexity choice |


## Progress Tracking
*This checklist is updated during execution flow*

**Phase Status**:
- [x] Phase 0: Research complete (/plan command)
- [x] Phase 1: Design complete (/plan command)
- [x] Phase 2: Task planning complete (/plan command - describe approach only)
- [x] Phase 3: Tasks generated (/tasks command) - 76 tasks created
- [ ] Phase 4: Implementation complete
- [ ] Phase 5: Validation passed

**Gate Status**:
- [x] Initial Constitution Check: CONDITIONAL PASS
- [x] Post-Design Constitution Check: PASS
- [x] All NEEDS CLARIFICATION resolved (see research.md)
- [x] Complexity deviations documented

---
*Based on Constitution v2.1.1 - See `/memory/constitution.md`*
