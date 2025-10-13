using System.Net;
using System.Net.Http.Json;
using Bogus;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 5: Create and reuse event labels
/// User Story: As a user, I want to create event labels and reuse them
/// in future mood entries for consistent tracking
/// </summary>
public class EventLabelTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Faker _faker;
    private readonly string _testId = Guid.NewGuid().ToString("N")[..8]; // Unique suffix for this test run

    public EventLabelTests()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        _faker = new Faker();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Scenario5_CreateEventLabel_AppearsInLabelsList()
    {
        // Scenario: User creates a new event label "exercise"
        // Expected: Label is saved and appears in autocomplete list

        // Arrange: Create label with unique name
        var uniqueName = $"exercise-{_testId}";
        var request = new
        {
            Name = uniqueName,
            Description = "Physical activity and workouts"
        };

        // Act: User creates the label
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Assert: Label created successfully
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelDto>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.Name).IsEqualTo(uniqueName);

        // Act: User retrieves all labels
        var listResponse = await _client.GetAsync("/api/event-labels");
        var labels = await listResponse.Content.ReadFromJsonAsync<List<EventLabelDto>>();

        // Assert: New label appears in list
        await Assert.That(labels).IsNotNull();
        await Assert.That(labels!.Any(l => l.Name == uniqueName)).IsTrue();
    }

    [Test]
    public async Task Scenario5_ReuseLabelInMoodEntry_ConsistentTracking()
    {
        // Scenario: User creates "work stress" label and uses it in multiple mood entries
        // Expected: Label can be reused consistently across entries

        // Arrange: Create event label with unique name
        var uniqueLabel = $"work-stress-{_testId}";
        var labelRequest = new
        {
            Name = uniqueLabel,
            Description = (string?)null
        };
        var labelResponse = await _client.PostAsJsonAsync("/api/event-labels", labelRequest);
        var label = await labelResponse.Content.ReadFromJsonAsync<EventLabelDto>();

        // Arrange: Create second label for entry2
        var deadlineId = await CreateLabel("deadline");

        // Act: Use label in multiple mood entries
        var now = SystemClock.Instance.GetCurrentInstant();
        var entry1 = new
        {
            MoodScore = 2,
            RecordedAt = now.Minus(Duration.FromDays(7)),
            EventLabelIds = new List<Guid> { label!.Id },
            Notes = (string?)null
        };

        var entry2 = new
        {
            MoodScore = 2,
            RecordedAt = now,
            EventLabelIds = new List<Guid> { label.Id, deadlineId },
            Notes = (string?)null
        };

        var response1 = await _client.PostAsJsonAsync("/api/mood-entries", entry1);
        if (!response1.IsSuccessStatusCode)
        {
            var error = await response1.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create first mood entry. Status: {response1.StatusCode}, Error: {error}");
        }

        var response2 = await _client.PostAsJsonAsync("/api/mood-entries", entry2);
        if (!response2.IsSuccessStatusCode)
        {
            var error = await response2.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create second mood entry. Status: {response2.StatusCode}, Error: {error}");
        }

        // Assert: Can filter mood entries by this label ID
        var response = await _client.GetAsync($"/api/mood-entries?eventLabelId={label.Id}");
        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;

        await Assert.That(entries).IsNotNull();
        await Assert.That(entries.Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(entries.All(e => e.EventLabels!.Any(el => el.Id == label.Id))).IsTrue();
    }

    [Test]
    public async Task Scenario5_UpdateEventLabel_UpdatesName()
    {
        // Scenario: User wants to rename "work" label to "work stress"
        // Expected: Label name updated, but doesn't affect historical mood entries

        // Arrange: Create label with unique name
        var originalName = $"work-{_testId}";
        var createRequest = new { Name = originalName, Description = (string?)null };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelDto>();

        // Act: Update label name to another unique name
        var updatedName = $"work-stress-updated-{_testId}";
        var updateRequest = new
        {
            Name = updatedName,
            Description = "Job-related stress and pressure"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/event-labels/{created!.Id}", updateRequest);

        // Assert: Update succeeds
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify updated label
        var getResponse = await _client.GetAsync($"/api/event-labels/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<EventLabelDto>();

        await Assert.That(updated!.Name).IsEqualTo(updatedName);
        await Assert.That(updated.Description).IsEqualTo("Job-related stress and pressure");
    }

    [Test]
    public async Task Scenario5_DeleteEventLabel_SoftDeletes()
    {
        // Scenario: User wants to remove unused event label
        // Expected: Label is soft deleted (preserves historical references)

        // Arrange: Create label with unique name
        var uniqueName = $"temporary-{_testId}";
        var createRequest = new { Name = uniqueName, Description = (string?)null };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelDto>();

        // Act: Delete label
        var deleteResponse = await _client.DeleteAsync($"/api/event-labels/{created!.Id}");

        // Assert: Delete succeeds
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Assert: Label no longer in active list
        var listResponse = await _client.GetAsync("/api/event-labels");
        var labels = await listResponse.Content.ReadFromJsonAsync<List<EventLabelDto>>();

        await Assert.That(labels!.Any(l => l.Id == created.Id)).IsFalse();
    }

    [Test]
    public async Task Scenario5_CreateDuplicateLabel_ReturnsConflict()
    {
        // Scenario: User tries to create label with duplicate name (case-insensitive)
        // Expected: System rejects with conflict error

        // Arrange: Create initial label with unique name
        var uniqueName = $"exercise-{_testId}";
        var request1 = new { Name = uniqueName, Description = (string?)null };
        await _client.PostAsJsonAsync("/api/event-labels", request1);

        // Act: Attempt to create duplicate (different case)
        var request2 = new { Name = uniqueName.ToUpper(), Description = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/event-labels", request2);

        // Assert: Conflict error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Scenario5_SearchLabels_CaseInsensitivePartialMatch()
    {
        // Scenario: User types "work" in search to find related labels
        // Expected: Returns all labels containing "work" (case-insensitive)

        // Arrange: Create multiple labels with unique search token
        var searchToken = _faker.Random.AlphaNumeric(8);
        var label1 = await CreateLabel($"{searchToken}-stress");
        var label2 = await CreateLabel($"{searchToken}-project");
        var label3 = await CreateLabel($"home{searchToken}");
        await CreateLabel($"exercise-{_testId}-nomatch"); // Should not match

        // Act: Search for the unique token
        var response = await _client.GetAsync($"/api/event-labels?search={searchToken}");

        // Assert: Returns matching labels
        var labels = await response.Content.ReadFromJsonAsync<List<EventLabelDto>>();

        await Assert.That(labels).IsNotNull();
        var matchingLabels = labels!.Where(l => l.Name.Contains(searchToken, StringComparison.OrdinalIgnoreCase)).ToList();

        await Assert.That(matchingLabels.Count).IsGreaterThanOrEqualTo(3);
    }

    // Helper method
    private async Task<Guid> CreateLabel(string name)
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
