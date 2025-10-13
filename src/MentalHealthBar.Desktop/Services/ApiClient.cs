using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MentalHealthBar.Contracts.Requests.Assessments;
using MentalHealthBar.Contracts.Requests.EventLabels;
using MentalHealthBar.Contracts.Requests.Export;
using MentalHealthBar.Contracts.Requests.HealthMetrics;
using MentalHealthBar.Contracts.Requests.MoodEntries;
using MentalHealthBar.Contracts.Responses.Assessments;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.Export;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Polly;
using Polly.Extensions.Http;

namespace MentalHealthBar.Desktop.Services;

public interface IApiClient
{
    // Assessments
    Task<List<AssessmentTemplateResponse>> GetAssessmentTemplatesAsync(CancellationToken cancellationToken = default);
    Task<AssessmentTemplateResponse> GetAssessmentTemplateAsync(string type, CancellationToken cancellationToken = default);
    Task<AssessmentResponse> CompleteAssessmentAsync(CompleteAssessmentRequest request, CancellationToken cancellationToken = default);
    Task<AssessmentPagedResultDto> GetAssessmentHistoryAsync(string? type = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default);
    Task<AssessmentResponse> GetAssessmentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAssessmentAsync(Guid id, CancellationToken cancellationToken = default);

    // Mood Entries
    Task<MoodEntryResponse> CreateMoodEntryAsync(CreateMoodEntryRequest request, CancellationToken cancellationToken = default);
    Task<MoodPagedResultDto> GetMoodHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string[]? tags = null, int page = 1, int pageSize = 500, CancellationToken cancellationToken = default);
    Task<MoodEntryResponse> GetMoodEntryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MoodEntryResponse> UpdateMoodEntryAsync(Guid id, UpdateMoodEntryRequest request, CancellationToken cancellationToken = default);
    Task DeleteMoodEntryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MoodStatsResponse> GetMoodStatsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    // Health Metrics
    Task<HealthMetricResponse> RecordHealthMetricAsync(RecordHealthMetricRequest request, CancellationToken cancellationToken = default);
    Task<HealthMetricPagedResultDto> GetHealthMetricsHistoryAsync(string? type = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 365, CancellationToken cancellationToken = default);
    Task<HealthMetricResponse> GetHealthMetricByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<HealthMetricResponse> UpdateHealthMetricAsync(Guid id, UpdateHealthMetricRequest request, CancellationToken cancellationToken = default);
    Task DeleteHealthMetricAsync(Guid id, CancellationToken cancellationToken = default);

    // Event Labels
    Task<EventLabelResponse> CreateEventLabelAsync(CreateEventLabelRequest request, CancellationToken cancellationToken = default);
    Task<List<EventLabelResponse>> GetEventLabelsAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<EventLabelResponse> GetEventLabelByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventLabelResponse> UpdateEventLabelAsync(Guid id, UpdateEventLabelRequest request, CancellationToken cancellationToken = default);
    Task DeleteEventLabelAsync(Guid id, CancellationToken cancellationToken = default);

    // Export
    Task<byte[]> ExportToCsvAsync(ExportRequest request, CancellationToken cancellationToken = default);
    Task<string> ExportToJsonAsync(ExportRequest request, CancellationToken cancellationToken = default);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://localhost:5001/api/");
        // Increased timeout to 30s to accommodate export operations with large datasets and slow network conditions
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        // Configure retry policy with exponential backoff
        _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
                });

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // Assessments
    public async Task<List<AssessmentTemplateResponse>> GetAssessmentTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync("assessments/templates", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AssessmentTemplateResponse>>(_jsonOptions, cancellationToken)
            ?? new List<AssessmentTemplateResponse>();
    }

    public async Task<AssessmentTemplateResponse> GetAssessmentTemplateAsync(string type, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"assessments/templates/{type}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssessmentTemplateResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve assessment template");
    }

    public async Task<AssessmentResponse> CompleteAssessmentAsync(CompleteAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsJsonAsync("assessments", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssessmentResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to complete assessment");
    }

    public async Task<AssessmentPagedResultDto> GetAssessmentHistoryAsync(string? type = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={type}");
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"assessments{queryString}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssessmentPagedResultDto>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve assessment history");
    }

    public async Task<AssessmentResponse> GetAssessmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"assessments/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AssessmentResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve assessment");
    }

    public async Task DeleteAssessmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.DeleteAsync($"assessments/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
    }

    // Mood Entries
    public async Task<MoodEntryResponse> CreateMoodEntryAsync(CreateMoodEntryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsJsonAsync("mood-entries", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MoodEntryResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to create mood entry");
    }

    public async Task<MoodPagedResultDto> GetMoodHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, string[]? tags = null, int page = 1, int pageSize = 500, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        if (tags?.Length > 0) queryParams.Add($"tags={string.Join(",", tags)}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"mood-entries{queryString}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MoodPagedResultDto>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve mood history");
    }

    public async Task<MoodEntryResponse> GetMoodEntryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"mood-entries/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MoodEntryResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve mood entry");
    }

    public async Task<MoodEntryResponse> UpdateMoodEntryAsync(Guid id, UpdateMoodEntryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PutAsJsonAsync($"mood-entries/{id}", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MoodEntryResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to update mood entry");
    }

    public async Task DeleteMoodEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.DeleteAsync($"mood-entries/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
    }

    public async Task<MoodStatsResponse> GetMoodStatsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"mood-entries/stats{queryString}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MoodStatsResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve mood stats");
    }

    // Health Metrics
    public async Task<HealthMetricResponse> RecordHealthMetricAsync(RecordHealthMetricRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsJsonAsync("health-metrics", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HealthMetricResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to record health metric");
    }

    public async Task<HealthMetricPagedResultDto> GetHealthMetricsHistoryAsync(string? type = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 365, CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={type}");
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"health-metrics{queryString}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HealthMetricPagedResultDto>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve health metrics history");
    }

    public async Task<HealthMetricResponse> GetHealthMetricByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"health-metrics/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HealthMetricResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve health metric");
    }

    public async Task<HealthMetricResponse> UpdateHealthMetricAsync(Guid id, UpdateHealthMetricRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PutAsJsonAsync($"health-metrics/{id}", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HealthMetricResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to update health metric");
    }

    public async Task DeleteHealthMetricAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.DeleteAsync($"health-metrics/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
    }

    // Event Labels
    public async Task<EventLabelResponse> CreateEventLabelAsync(CreateEventLabelRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsJsonAsync("event-labels", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventLabelResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to create event label");
    }

    public async Task<List<EventLabelResponse>> GetEventLabelsAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        var queryString = !string.IsNullOrEmpty(search) ? $"?search={search}" : "";

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"event-labels{queryString}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<EventLabelResponse>>(_jsonOptions, cancellationToken)
            ?? new List<EventLabelResponse>();
    }

    public async Task<EventLabelResponse> GetEventLabelByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync($"event-labels/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventLabelResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve event label");
    }

    public async Task<EventLabelResponse> UpdateEventLabelAsync(Guid id, UpdateEventLabelRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PutAsJsonAsync($"event-labels/{id}", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EventLabelResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Failed to update event label");
    }

    public async Task DeleteEventLabelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.DeleteAsync($"event-labels/{id}", cancellationToken));

        response.EnsureSuccessStatusCode();
    }

    // Export
    public async Task<byte[]> ExportToCsvAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsJsonAsync("export/csv", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<string> ExportToJsonAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.PostAsJsonAsync("export/json", request, _jsonOptions, cancellationToken));

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}