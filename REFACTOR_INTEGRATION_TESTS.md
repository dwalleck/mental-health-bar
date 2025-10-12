# Integration Test Refactoring Status

## ✅ Completed (3/9 files)
1. **AssessmentTemplatesTests.cs** - Using actual `TemplateDto`, `TemplateDetailDto`
2. **CompleteAssessmentTests.cs** - Using actual `AssessmentResultDto`
3. **AssessmentHistoryTests.cs** - Using actual `AssessmentPagedResultDto`, `AssessmentSummaryDto`, `AssessmentDetailDto`

## ⏳ Remaining (6/9 files) - Quick Reference

### 4. CreateMoodEntryTests.cs
**Add using statements:**
```csharp
using MoodEntryDto = MentalHealthBar.Api.Features.MoodEntries.Create.MoodEntryDto;
```
**Remove duplicate DTOs at end of file** (lines with `private record MoodEntryDto`, `CreateMoodEntryRequest`, `UpdateMoodEntryRequest`)

### 5. EventLabelTests.cs
**Add using statements:**
```csharp
using EventLabelDto = MentalHealthBar.Api.Features.EventLabels.List.EventLabelDto;
using MoodEntryDto = MentalHealthBar.Api.Features.MoodEntries.Create.MoodEntryDto;
```
**Remove duplicate DTOs** (EventLabelDto, CreateEventLabelRequest, UpdateEventLabelRequest, MoodEntryDto, CreateMoodEntryRequest)

### 6. HealthMetricsTests.cs
**Add using statements:**
```csharp
using HealthMetricDto = MentalHealthBar.Api.Features.HealthMetrics.Record.HealthMetricDto;
using HealthMetricPagedResultDto = MentalHealthBar.Api.Features.HealthMetrics.GetHistory.PagedResultDto;
```
**Change:** `List<HealthMetricDto>` responses to `HealthMetricPagedResultDto` with `.Items`
**Remove duplicate DTOs** (RecordHealthMetricRequest, UpdateHealthMetricRequest, HealthMetricDto)

### 7. ViewTrendsTests.cs
**Add using statements:**
```csharp
using MoodEntryDto = MentalHealthBar.Api.Features.MoodEntries.Create.MoodEntryDto;
using MoodPagedResultDto = MentalHealthBar.Api.Features.MoodEntries.GetHistory.PagedResultDto;
using MoodStatsDto = MentalHealthBar.Api.Features.MoodEntries.GetStats.StatsDto;
using AssessmentPagedResultDto = MentalHealthBar.Api.Features.Assessments.GetHistory.PagedResultDto;
using AssessmentResultDto = MentalHealthBar.Api.Features.Assessments.Complete.AssessmentResultDto;
using HealthMetricDto = MentalHealthBar.Api.Features.HealthMetrics.Record.HealthMetricDto;
using HealthMetricPagedResultDto = MentalHealthBar.Api.Features.HealthMetrics.GetHistory.PagedResultDto;
```
**Change:** All `List<...>` responses to `PagedResultDto` with `.Items`
**Remove duplicate DTOs**

### 8. FilteringTests.cs
**Same as ViewTrendsTests.cs** (uses same DTOs)

### 9. ExportTests.cs
**Add using statements:**
```csharp
using ExportDataResponse = MentalHealthBar.Api.Features.Export.ToJson.ExportDataResponse;
using MoodEntryDto = MentalHealthBar.Api.Features.MoodEntries.Create.MoodEntryDto;
using AssessmentResultDto = MentalHealthBar.Api.Features.Assessments.Complete.AssessmentResultDto;
using HealthMetricDto = MentalHealthBar.Api.Features.HealthMetrics.Record.HealthMetricDto;
```
**Remove duplicate DTOs** (ExportRequest, ExportDataDto, AssessmentExportDto, MoodEntryExportDto, HealthMetricExportDto)

## Current Test Results
- Total: 164 tests
- Passing: 144 (87.8%)
- Failing: 20 (12.2%)

**Expected after completion:** All 164 tests passing ✅
