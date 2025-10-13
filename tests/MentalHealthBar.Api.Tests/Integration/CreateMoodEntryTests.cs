using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 4: Create mood entry with tags
/// User Story: As a user, I want to log my mood score with tags
/// describing life events so I can track mood patterns
/// </summary>
public class CreateMoodEntryTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testId = Guid.NewGuid().ToString("N")[..8]; // Unique suffix for this test run

    public CreateMoodEntryTests()
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
    public async Task Scenario4_CreateMoodEntry_WithTagsAndNotes_Succeeds()
    {
        // Scenario: User logs mood after stressful work day
        // Expected: Mood entry saved with score, tags, and notes

        // Arrange: Create event labels first
        var workStressId = await CreateEventLabel("work-stress");
        var deadlineId = await CreateEventLabel("deadline");
        var tiredId = await CreateEventLabel("tired");

        // Arrange: User's mood entry data
        var request = new
        {
            MoodScore = 2,  // Below Average
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = new List<Guid> { workStressId, deadlineId, tiredId },
            Notes = "Long day with tight deadlines. Felt overwhelmed."
        };

        // Act: User submits mood entry
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Assert: Entry created successfully
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<MoodEntryDto>();

        // Assert: All fields saved correctly
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.MoodScore).IsEqualTo(2);
        await Assert.That(result.EventLabels).IsNotNull();
        await Assert.That(result.EventLabels!.Count).IsEqualTo(3);
        await Assert.That(result.EventLabels.Any(e => e.Name.StartsWith("work-stress"))).IsTrue();
        await Assert.That(result.Notes).IsEqualTo("Long day with tight deadlines. Felt overwhelmed.");
    }

    [Test]
    public async Task Scenario4_CreateMoodEntry_MinimalData_Succeeds()
    {
        // Scenario: User quickly logs mood without tags or notes
        // Expected: Entry saved with just mood score

        // Arrange: Minimal mood entry
        var request = new
        {
            MoodScore = 4,  // Above Average
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = new List<Guid>(),
            Notes = (string?)null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<MoodEntryDto>();
        await Assert.That(result!.MoodScore).IsEqualTo(4);
        await Assert.That(result.EventLabels).IsNotNull();
        await Assert.That(result.EventLabels!.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Scenario4_CreateMoodEntry_InvalidScore_ReturnsBadRequest()
    {
        // Scenario: User attempts to enter invalid mood score
        // Expected: System rejects with validation error

        // Arrange: Invalid score (must be 1-5)
        var request = new
        {
            MoodScore = 6,  // Invalid
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = new List<Guid>(),
            Notes = (string?)null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Assert: Validation error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Scenario4_CreateMoodEntry_TooManyTags_ReturnsBadRequest()
    {
        // Scenario: User tries to add more than 10 event labels
        // Expected: System rejects with validation error

        // Arrange: Create 11 event labels (max is 10)
        var labelIds = new List<Guid>();
        for (int i = 1; i <= 11; i++)
        {
            labelIds.Add(await CreateEventLabel($"tag{i}"));
        }

        var request = new
        {
            MoodScore = 3,
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = labelIds,
            Notes = (string?)null
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Assert: Validation error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Scenario4_UpdateMoodEntry_ChangesScoreAndTags()
    {
        // Scenario: User realizes they logged wrong mood score
        // Expected: Can update entry with new score and event labels

        // Arrange: Create event labels
        var tiredId = await CreateEventLabel("tired");
        var coffeeId = await CreateEventLabel("coffee-helped");

        // Arrange: Create initial mood entry
        var createRequest = new
        {
            MoodScore = 2,
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = new List<Guid> { tiredId },
            Notes = "Initial note"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/mood-entries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MoodEntryDto>();

        // Act: User updates the entry
        var updateRequest = new
        {
            MoodScore = 3,
            EventLabelIds = new List<Guid> { tiredId, coffeeId },
            Notes = "Feeling better after coffee break"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/mood-entries/{created!.Id}", updateRequest);

        // Assert: Update succeeds
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify updated data
        var getResponse = await _client.GetAsync($"/api/mood-entries/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<MoodEntryDto>();

        await Assert.That(updated!.MoodScore).IsEqualTo(3);
        await Assert.That(updated.EventLabels!.Count).IsEqualTo(2);
        await Assert.That(updated.Notes).IsEqualTo("Feeling better after coffee break");
    }

    [Test]
    public async Task Scenario4_DeleteMoodEntry_SoftDeletes()
    {
        // Scenario: User wants to remove a mood entry
        // Expected: Entry is soft deleted (preserved for audit trail)

        // Arrange: Create mood entry
        var createRequest = new
        {
            MoodScore = 3,
            RecordedAt = SystemClock.Instance.GetCurrentInstant(),
            EventLabelIds = new List<Guid>(),
            Notes = (string?)null
        };

        var createResponse = await _client.PostAsJsonAsync("/api/mood-entries", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MoodEntryDto>();

        // Act: User deletes the entry
        var deleteResponse = await _client.DeleteAsync($"/api/mood-entries/{created!.Id}");

        // Assert: Delete succeeds
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Assert: Entry no longer retrievable (soft deleted)
        var getResponse = await _client.GetAsync($"/api/mood-entries/{created.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Scenario4_CreateMultipleMoodEntriesPerDay_AllowsDuplicates()
    {
        // Scenario: User logs mood multiple times in one day
        // Expected: All entries are saved (no uniqueness constraint on date)

        // Arrange: Create 3 entries for same day
        var now = SystemClock.Instance.GetCurrentInstant();
        var today = Instant.FromUtc(now.InUtc().Year, now.InUtc().Month, now.InUtc().Day, 0, 0, 0);

        await CreateMoodEntry(2, today.Plus(Duration.FromHours(8)));  // Morning
        await CreateMoodEntry(4, today.Plus(Duration.FromHours(14))); // Afternoon
        await CreateMoodEntry(3, today.Plus(Duration.FromHours(20))); // Evening

        // Act: Retrieve all entries for today
        var startDate = Uri.EscapeDataString(today.ToDateTimeOffset().ToString("o"));
        var endDate = Uri.EscapeDataString(today.Plus(Duration.FromDays(1)).ToDateTimeOffset().ToString("o"));
        var response = await _client.GetAsync($"/api/mood-entries?startDate={startDate}&endDate={endDate}");

        // Assert: All 3 entries returned
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;
        var todayDate = today.ToDateTimeOffset().Date;
        var todayEntries = entries.Where(e => e.RecordedAt.ToDateTimeOffset().Date == todayDate).ToList();

        await Assert.That(todayEntries.Count).IsGreaterThanOrEqualTo(3);
    }

    // Helper methods
    private async Task<Guid> CreateMoodEntry(int score, Instant recordedAt, List<Guid>? eventLabelIds = null)
    {
        var request = new
        {
            MoodScore = score,
            RecordedAt = recordedAt,
            EventLabelIds = eventLabelIds ?? new List<Guid>(),
            Notes = (string?)null
        };

        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Check for success and provide detailed error information if it fails
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create mood entry. " +
                              $"Status: {response.StatusCode}, " +
                              $"EventLabelIds: {string.Join(", ", eventLabelIds ?? new List<Guid>())}, " +
                              $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<MoodEntryDto>();
        if (result == null || result.Id == Guid.Empty)
        {
            throw new Exception("Created mood entry but received invalid response");
        }

        return result.Id;
    }

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
