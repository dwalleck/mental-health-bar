# API DTO Reference for Integration Tests

This document maps all API response DTOs to their namespace locations for use in integration tests.

## Assessments Feature

### GetTemplates
```csharp
using TemplateDto = MentalHealthBar.Api.Features.Assessments.GetTemplates.TemplateDto;
// TemplateDto(Guid Id, string Type, string Name, string Description)
```

### GetTemplate
```csharp
using TemplateDetailDto = MentalHealthBar.Api.Features.Assessments.GetTemplate.TemplateDetailDto;
using QuestionDto = MentalHealthBar.Api.Features.Assessments.GetTemplate.QuestionDto;
using AnswerOptionDto = MentalHealthBar.Api.Features.Assessments.GetTemplate.AnswerOptionDto;
using ScoringRulesDto = MentalHealthBar.Api.Features.Assessments.GetTemplate.ScoringRulesDto;
// TemplateDetailDto(Guid Id, string Type, string Name, string Description, List<QuestionDto> Questions, ScoringRulesDto ScoringRules)
// QuestionDto(string Id, string Text, List<AnswerOptionDto> Options)
// AnswerOptionDto(int Value, string Label)
// ScoringRulesDto(int MinScore, int MaxScore, Dictionary<string, string> SeverityRanges)
```

### Complete
```csharp
using AssessmentResultDto = MentalHealthBar.Api.Features.Assessments.Complete.AssessmentResultDto;
// AssessmentResultDto(Guid Id, string Type, int TotalScore, string Severity, DateTimeOffset CompletedAt)
```

### GetHistory
```csharp
using AssessmentPagedResultDto = MentalHealthBar.Api.Features.Assessments.GetHistory.PagedResultDto;
using AssessmentSummaryDto = MentalHealthBar.Api.Features.Assessments.GetHistory.AssessmentSummaryDto;
// PagedResultDto(List<AssessmentSummaryDto> Items, int TotalCount, int Page, int PageSize)
// AssessmentSummaryDto(Guid Id, string Type, int TotalScore, string Severity, DateTimeOffset CompletedAt, DateTimeOffset CreatedAt)
```

### GetById
```csharp
using AssessmentDetailDto = MentalHealthBar.Api.Features.Assessments.GetById.AssessmentDetailDto;
// AssessmentDetailDto(Guid Id, string Type, Dictionary<string, int> Responses, int TotalScore, string Severity, DateTimeOffset CompletedAt, DateTimeOffset CreatedAt)
```

## MoodEntries Feature

### Create
```csharp
using MoodEntryDto = MentalHealthBar.Api.Features.MoodEntries.Create.MoodEntryDto;
// MoodEntryDto(Guid Id, int MoodScore, DateTimeOffset RecordedAt, List<string> Tags, string? Notes, DateTimeOffset CreatedAt)
```

### GetHistory
```csharp
using MoodPagedResultDto = MentalHealthBar.Api.Features.MoodEntries.GetHistory.PagedResultDto;
using MoodEntrySummaryDto = MentalHealthBar.Api.Features.MoodEntries.GetHistory.MoodEntrySummaryDto;
// PagedResultDto(List<MoodEntrySummaryDto> Items, int TotalCount, int Page, int PageSize)
// MoodEntrySummaryDto(Guid Id, int MoodScore, DateTimeOffset RecordedAt, List<string> Tags, string? Notes, DateTimeOffset CreatedAt)
```

### GetById
```csharp
using MoodEntryDetailDto = MentalHealthBar.Api.Features.MoodEntries.GetById.MoodEntryDetailDto;
// MoodEntryDetailDto(Guid Id, int MoodScore, DateTimeOffset RecordedAt, List<string> Tags, string? Notes, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
```

### GetStats
```csharp
using MoodStatsDto = MentalHealthBar.Api.Features.MoodEntries.GetStats.StatsDto;
// StatsDto(int Count, double Average, int Median, Dictionary<int, int> ScoreDistribution)
```

## HealthMetrics Feature

### Record
```csharp
using HealthMetricDto = MentalHealthBar.Api.Features.HealthMetrics.Record.HealthMetricDto;
// HealthMetricDto(Guid Id, string Type, decimal Value, DateOnly RecordedDate, DateTimeOffset CreatedAt)
```

### GetHistory
```csharp
using HealthMetricPagedResultDto = MentalHealthBar.Api.Features.HealthMetrics.GetHistory.PagedResultDto;
using HealthMetricSummaryDto = MentalHealthBar.Api.Features.HealthMetrics.GetHistory.HealthMetricSummaryDto;
// PagedResultDto(List<HealthMetricSummaryDto> Items, int TotalCount, int Page, int PageSize)
// HealthMetricSummaryDto(Guid Id, string Type, decimal Value, DateOnly RecordedDate, DateTimeOffset CreatedAt)
```

### GetById
```csharp
using HealthMetricDetailDto = MentalHealthBar.Api.Features.HealthMetrics.GetById.HealthMetricDetailDto;
// HealthMetricDetailDto(Guid Id, string Type, decimal Value, DateOnly RecordedDate, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
```

## EventLabels Feature

### List & Create
```csharp
using EventLabelDto = MentalHealthBar.Api.Features.EventLabels.List.EventLabelDto;
// EventLabelDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt)
// NOTE: Create also returns same DTO structure
```

### GetById
```csharp
using EventLabelDetailDto = MentalHealthBar.Api.Features.EventLabels.GetById.EventLabelDetailDto;
// EventLabelDetailDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt)
```

## Export Feature

### ToJson
```csharp
using ExportDataResponse = MentalHealthBar.Api.Features.Export.ToJson.ExportDataResponse;
using AssessmentExport = MentalHealthBar.Api.Features.Export.ToJson.AssessmentExport;
using MoodEntryExport = MentalHealthBar.Api.Features.Export.ToJson.MoodEntryExport;
using HealthMetricExport = MentalHealthBar.Api.Features.Export.ToJson.HealthMetricExport;
using DateRangeExport = MentalHealthBar.Api.Features.Export.ToJson.DateRangeExport;
// ExportDataResponse(List<AssessmentExport> Assessments, List<MoodEntryExport> MoodEntries, List<HealthMetricExport> HealthMetrics, List<string> EventLabels, DateRangeExport DateRange, DateTimeOffset ExportedAt)
// AssessmentExport(Guid Id, string Type, Dictionary<string, int> Responses, int TotalScore, string Severity, DateTimeOffset CompletedAt, DateTimeOffset CreatedAt)
// MoodEntryExport(Guid Id, int MoodScore, string MoodLabel, DateTimeOffset RecordedAt, List<string> Tags, string? Notes, DateTimeOffset CreatedAt)
// HealthMetricExport(Guid Id, string Type, decimal Value, DateOnly RecordedDate, DateTimeOffset CreatedAt)
// DateRangeExport(DateTimeOffset Start, DateTimeOffset End)
```

## Important Notes

1. **PagedResultDto** - Different features have their own `PagedResultDto` with different generic types:
   - `Assessments.GetHistory.PagedResultDto` contains `List<AssessmentSummaryDto>`
   - `MoodEntries.GetHistory.PagedResultDto` contains `List<MoodEntrySummaryDto>`
   - `HealthMetrics.GetHistory.PagedResultDto` contains `List<HealthMetricSummaryDto>`

2. **Avoid Duplicate DTOs** - Always use the actual API DTOs via using aliases instead of creating duplicates in tests

3. **Response vs Summary DTOs**:
   - Full detail DTOs (e.g., `MoodEntryDto`) are used for Create/Update operations
   - Summary DTOs (e.g., `MoodEntrySummaryDto`) are used in list/history endpoints
   - Detail DTOs (e.g., `MoodEntryDetailDto`) are used for GetById operations

4. **Request Objects** - For POST/PUT requests, use anonymous objects or create inline requests (not DTOs from the API)
