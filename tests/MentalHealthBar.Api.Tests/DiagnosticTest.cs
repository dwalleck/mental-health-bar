using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MentalHealthBar.Api.Tests;

public class DiagnosticTest : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DiagnosticTest()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TUnit.Core.Test]
    public async Task DiagnoseCreateMoodEntry()
    {
        // First, test if the health endpoint works
        var healthResponse = await _client.GetAsync("/health");
        var healthBody = await healthResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"Health endpoint - Status: {healthResponse.StatusCode}, Body: {healthBody}");

        // Try to create a simple mood entry
        var request = new
        {
            MoodScore = 3,
            RecordedAt = DateTimeOffset.UtcNow,
            EventLabelIds = new List<Guid>(),
            Notes = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Get the actual response body
        var responseBody = await response.Content.ReadAsStringAsync();

        // Get first 500 chars of body for diagnostics
        var shortBody = responseBody.Length > 500 ? responseBody.Substring(0, 500) : responseBody;

        Console.WriteLine($"Status Code: {response.StatusCode}");
        Console.WriteLine($"Content-Type: {response.Content.Headers.ContentType}");
        Console.WriteLine($"Response Body (first 500 chars): {shortBody}");

        // This will help us see what the actual error is
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"API returned {response.StatusCode}. Body starts with: {shortBody}");
        }
    }
}