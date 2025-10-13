using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Api.Tests;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Contracts;

/// <summary>
/// Contract tests for Mood Entries API endpoints
/// Based on contracts/mood-entries.yaml
/// Following TDD: Tests MUST FAIL until endpoints are implemented
/// </summary>
public class MoodEntriesContractTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testId = Guid.NewGuid().ToString("N")[..8]; // Unique suffix for this test run

    public MoodEntriesContractTests()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CreateMoodEntry_ValidRequest_ReturnsCreated()
    {
        // Arrange - Create event labels first
        var workId = await CreateEventLabel("work");
        var exerciseId = await CreateEventLabel("exercise");

        var request = new CreateMoodEntryRequestDto
        {
            MoodScore = 4,
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = new List<Guid> { workId, exerciseId },
            Notes = "Feeling productive after morning workout"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<MoodEntryResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.MoodScore).IsEqualTo(request.MoodScore);
        await Assert.That(result.EventLabels.Count).IsEqualTo(2);
    }

    [Test]
    public async Task CreateMoodEntry_InvalidMoodScore_ReturnsBadRequest()
    {
        // Arrange - Invalid score (out of range 1-5)
        var request = new CreateMoodEntryRequestDto
        {
            MoodScore = 10,  // Invalid
            EventLabelIds = new List<Guid>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetMoodEntryHistory_WithoutFilters_ReturnsPagedList()
    {
        // Act
        var response = await _client.GetAsync("/api/mood-entries");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<MoodEntryHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        await Assert.That(history!.Items).IsNotNull();
        await Assert.That(history.TotalCount).IsGreaterThanOrEqualTo(0);
        await Assert.That(history.Page).IsGreaterThan(0);
    }

    [Test]
    public async Task GetMoodEntryHistory_WithDateFilter_ReturnsFilteredList()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var startDate = now.Minus(Duration.FromDays(7));
        var endDate = now;

        // Act
        var response = await _client.GetAsync(
            $"/api/mood-entries?startDate={startDate.ToDateTimeOffset():O}&endDate={endDate.ToDateTimeOffset():O}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<MoodEntryHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        // All entries should be within date range
        foreach (var item in history!.Items)
        {
            await Assert.That(item.RecordedAt).IsGreaterThanOrEqualTo(startDate);
            await Assert.That(item.RecordedAt).IsLessThanOrEqualTo(endDate);
        }
    }

    [Test]
    public async Task GetMoodEntry_ValidId_ReturnsMoodEntry()
    {
        // Arrange - Create event label and entry first
        var testId = await CreateEventLabel("test");
        var createRequest = new CreateMoodEntryRequestDto
        {
            MoodScore = 3,
            EventLabelIds = new List<Guid> { testId }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/mood-entries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MoodEntryResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/mood-entries/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var entry = await response.Content.ReadFromJsonAsync<MoodEntryResponseDto>();
        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Id).IsEqualTo(created.Id);
    }

    [Test]
    public async Task GetMoodEntry_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/mood-entries/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateMoodEntry_ValidRequest_ReturnsOk()
    {
        // Arrange - Create event labels and entry first
        var initialId = await CreateEventLabel("initial");
        var updatedId = await CreateEventLabel("updated");
        var betterId = await CreateEventLabel("better");

        var createRequest = new CreateMoodEntryRequestDto
        {
            MoodScore = 3,
            EventLabelIds = new List<Guid> { initialId }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/mood-entries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MoodEntryResponseDto>();

        var updateRequest = new UpdateMoodEntryRequestDto
        {
            MoodScore = 5,
            EventLabelIds = new List<Guid> { updatedId, betterId },
            Notes = "Updated notes"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mood-entries/{created!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<MoodEntryResponseDto>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.MoodScore).IsEqualTo(updateRequest.MoodScore!.Value);
        await Assert.That(updated.EventLabels.Count).IsEqualTo(2);
    }

    [Test]
    public async Task UpdateMoodEntry_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var updateRequest = new UpdateMoodEntryRequestDto
        {
            MoodScore = 4
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/mood-entries/{invalidId}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteMoodEntry_ValidId_ReturnsNoContent()
    {
        // Arrange - Create entry first
        var createRequest = new CreateMoodEntryRequestDto
        {
            MoodScore = 3,
            EventLabelIds = new List<Guid>()
        };
        var createResponse = await _client.PostAsJsonAsync("/api/mood-entries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MoodEntryResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/mood-entries/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteMoodEntry_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/mood-entries/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetMoodStats_WithDateRange_ReturnsStatistics()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var startDate = now.Minus(Duration.FromDays(30));
        var endDate = now;

        // Act
        var response = await _client.GetAsync(
            $"/api/mood-entries/stats?startDate={startDate.ToDateTimeOffset():O}&endDate={endDate.ToDateTimeOffset():O}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<MoodStatsResponseDto>();
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats!.EntryCount).IsGreaterThanOrEqualTo(0);
        await Assert.That(stats.ScoreDistribution).IsNotNull();
    }

    // Helper method
    private async Task<Guid> CreateEventLabel(string name)
    {
        // Add test ID suffix to ensure uniqueness across test runs
        var uniqueName = $"{name}-{_testId}";
        var request = new { Name = uniqueName, Description = (string?)null };

        var response = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Check for success and provide detailed error information if it fails
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create event label '{uniqueName}'. " +
                              $"Status: {response.StatusCode}, " +
                              $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<EventLabelDto>();
        if (result == null || result.Id == Guid.Empty)
        {
            throw new Exception($"Created event label '{uniqueName}' but received invalid response");
        }

        return result.Id;
    }
}

// DTOs matching the OpenAPI contract
public record CreateMoodEntryRequestDto
{
    public int MoodScore { get; init; }
    public Instant? RecordedAt { get; init; }
    public List<Guid> EventLabelIds { get; init; } = new();
    public string? Notes { get; init; }
}

public record UpdateMoodEntryRequestDto
{
    public int? MoodScore { get; init; }
    public List<Guid>? EventLabelIds { get; init; }
    public string? Notes { get; init; }
}

public record MoodEntryResponseDto(
    Guid Id,
    int MoodScore,
    string MoodLabel,
    Instant RecordedAt,
    List<EventLabelDto> EventLabels,
    string? Notes,
    Instant CreatedAt,
    Instant? UpdatedAt
);

public record MoodEntryHistoryResponseDto(
    List<MoodEntryResponseDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record MoodStatsResponseDto(
    int EntryCount,
    double AverageScore,
    int MedianScore,
    int? MinScore,
    int? MaxScore,
    Dictionary<string, int> ScoreDistribution,
    List<TagCountDto>? CommonTags
);

public record TagCountDto(string Tag, int Count);
