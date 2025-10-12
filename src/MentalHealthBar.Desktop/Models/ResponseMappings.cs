// Type aliases to map between the DTO names and the names used in the ViewModels
// This helps maintain cleaner code in the ViewModels while using the actual DTOs

global using CompleteAssessmentRequest = MentalHealthBar.Contracts.Requests.Assessments.CompleteAssessmentRequest;

global using CreateMoodEntryRequest = MentalHealthBar.Contracts.Requests.MoodEntries.CreateMoodEntryRequest;
global using UpdateMoodEntryRequest = MentalHealthBar.Contracts.Requests.MoodEntries.UpdateMoodEntryRequest;

global using RecordHealthMetricRequest = MentalHealthBar.Contracts.Requests.HealthMetrics.RecordHealthMetricRequest;
global using UpdateHealthMetricRequest = MentalHealthBar.Contracts.Requests.HealthMetrics.UpdateHealthMetricRequest;

global using CreateEventLabelRequest = MentalHealthBar.Contracts.Requests.EventLabels.CreateEventLabelRequest;
global using UpdateEventLabelRequest = MentalHealthBar.Contracts.Requests.EventLabels.UpdateEventLabelRequest;

global using ExportRequest = MentalHealthBar.Contracts.Requests.Export.ExportRequest;

global using AssessmentResponse = MentalHealthBar.Contracts.Responses.Assessments.AssessmentDetailDto;
global using AssessmentTemplateResponse = MentalHealthBar.Contracts.Responses.Assessments.TemplateDetailDto;
global using QuestionResponse = MentalHealthBar.Contracts.Responses.Assessments.QuestionDto;
global using AnswerOptionResponse = MentalHealthBar.Contracts.Responses.Assessments.AnswerOptionDto;

global using MoodEntryResponse = MentalHealthBar.Contracts.Responses.MoodEntries.MoodEntryDto;
global using MoodStatsResponse = MentalHealthBar.Contracts.Responses.MoodEntries.MoodStatsDto;

global using HealthMetricResponse = MentalHealthBar.Contracts.Responses.HealthMetrics.HealthMetricDto;

global using EventLabelResponse = MentalHealthBar.Contracts.Responses.EventLabels.EventLabelDto;

global using ExportDataResponse = MentalHealthBar.Contracts.Responses.Export.ExportDataResponse;