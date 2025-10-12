using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Contracts;

/// <summary>
/// Contract tests for Event Labels API endpoints
/// Based on contracts/event-labels.yaml
/// Following TDD: Tests MUST FAIL until endpoints are implemented
/// </summary>
public class EventLabelsContractTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public EventLabelsContractTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task CreateEventLabel_ValidRequest_ReturnsCreated()
    {
        // Arrange - Use unique name to avoid conflicts
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var request = new CreateEventLabelRequestDto
        {
            Name = $"work stress {uniqueSuffix}",
            Description = "Work-related stress and anxiety"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<EventLabelResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.Name).IsEqualTo(request.Name);
        await Assert.That(result.Description).IsEqualTo(request.Description);
    }

    [Test]
    public async Task CreateEventLabel_WithoutDescription_ReturnsCreated()
    {
        // Arrange - Use unique name to avoid conflicts
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var request = new CreateEventLabelRequestDto
        {
            Name = $"exercise {uniqueSuffix}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<EventLabelResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Name).IsEqualTo(request.Name);
        await Assert.That(result.Description).IsNull();
    }

    [Test]
    public async Task CreateEventLabel_InvalidName_ReturnsBadRequest()
    {
        // Arrange - Name with invalid characters
        var request = new CreateEventLabelRequestDto
        {
            Name = "invalid@name!"  // Contains @ and !
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateEventLabel_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateEventLabelRequestDto
        {
            Name = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateEventLabel_DuplicateName_ReturnsConflict()
    {
        // Arrange - Create first label with unique name
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var firstRequest = new CreateEventLabelRequestDto
        {
            Name = $"family time {uniqueSuffix}"
        };
        await _client.PostAsJsonAsync("/api/event-labels", firstRequest);

        // Try to create duplicate
        var duplicateRequest = new CreateEventLabelRequestDto
        {
            Name = $"family time {uniqueSuffix}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/event-labels", duplicateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task GetEventLabels_ReturnsAllLabels()
    {
        // Act
        var response = await _client.GetAsync("/api/event-labels");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var labels = await response.Content.ReadFromJsonAsync<List<EventLabelResponseDto>>();
        await Assert.That(labels).IsNotNull();
    }

    [Test]
    public async Task GetEventLabels_WithSearchFilter_ReturnsFilteredList()
    {
        // Arrange - Create some labels with unique suffixes
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        await _client.PostAsJsonAsync("/api/event-labels", new CreateEventLabelRequestDto { Name = $"work meeting {uniqueSuffix}" });
        await _client.PostAsJsonAsync("/api/event-labels", new CreateEventLabelRequestDto { Name = $"workout {uniqueSuffix}" });
        await _client.PostAsJsonAsync("/api/event-labels", new CreateEventLabelRequestDto { Name = $"family {uniqueSuffix}" });

        // Act - Search for labels containing "work"
        var response = await _client.GetAsync("/api/event-labels?search=work");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var labels = await response.Content.ReadFromJsonAsync<List<EventLabelResponseDto>>();
        await Assert.That(labels).IsNotNull();
        // All returned labels should contain "work" (case-insensitive)
        foreach (var label in labels!)
        {
            await Assert.That(label.Name.ToLower()).Contains("work");
        }
    }

    [Test]
    public async Task GetEventLabel_ValidId_ReturnsLabel()
    {
        // Arrange - Create label first with unique name
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var createRequest = new CreateEventLabelRequestDto
        {
            Name = $"meditation {uniqueSuffix}",
            Description = "Mindfulness and meditation practice"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/event-labels/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var label = await response.Content.ReadFromJsonAsync<EventLabelResponseDto>();
        await Assert.That(label).IsNotNull();
        await Assert.That(label!.Id).IsEqualTo(created.Id);
        await Assert.That(label.Name).IsEqualTo(created.Name);
    }

    [Test]
    public async Task GetEventLabel_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/event-labels/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateEventLabel_ValidRequest_ReturnsOk()
    {
        // Arrange - Create label first with unique name
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var createRequest = new CreateEventLabelRequestDto
        {
            Name = $"social event {uniqueSuffix}",
            Description = "Original description"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        var updateRequest = new UpdateEventLabelRequestDto
        {
            Name = $"social activity {uniqueSuffix}",
            Description = "Updated description"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/event-labels/{created!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<EventLabelResponseDto>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Name).IsEqualTo(updateRequest.Name);
        await Assert.That(updated.Description).IsEqualTo(updateRequest.Description);
        await Assert.That(updated.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateEventLabel_OnlyName_ReturnsOk()
    {
        // Arrange - Create label first with unique name
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var createRequest = new CreateEventLabelRequestDto
        {
            Name = $"hobby {uniqueSuffix}",
            Description = "Original description"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        var updateRequest = new UpdateEventLabelRequestDto
        {
            Name = $"hobbies {uniqueSuffix}"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/event-labels/{created!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<EventLabelResponseDto>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Name).IsEqualTo(updateRequest.Name);
    }

    [Test]
    public async Task UpdateEventLabel_InvalidName_ReturnsBadRequest()
    {
        // Arrange - Create label first with unique name
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var createRequest = new CreateEventLabelRequestDto
        {
            Name = $"valid name {uniqueSuffix}"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        var updateRequest = new UpdateEventLabelRequestDto
        {
            Name = "invalid@name#"  // Invalid characters
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/event-labels/{created!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateEventLabel_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var updateRequest = new UpdateEventLabelRequestDto
        {
            Name = $"updated name {uniqueSuffix}"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/event-labels/{invalidId}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateEventLabel_DuplicateName_ReturnsConflict()
    {
        // Arrange - Create two labels with unique names
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var first = await _client.PostAsJsonAsync("/api/event-labels",
            new CreateEventLabelRequestDto { Name = $"existing label {uniqueSuffix}" });
        var firstCreated = await first.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        var second = await _client.PostAsJsonAsync("/api/event-labels",
            new CreateEventLabelRequestDto { Name = $"another label {uniqueSuffix}" });
        var secondCreated = await second.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        // Try to update second label to have same name as first
        var updateRequest = new UpdateEventLabelRequestDto
        {
            Name = firstCreated!.Name  // Use the actual name of the first label
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/event-labels/{secondCreated!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task DeleteEventLabel_ValidId_ReturnsNoContent()
    {
        // Arrange - Create label first with unique name
        var uniqueSuffix = Random.Shared.Next(10000, 99999);
        var createRequest = new CreateEventLabelRequestDto
        {
            Name = $"temporary label {uniqueSuffix}"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/event-labels", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<EventLabelResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/event-labels/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteEventLabel_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/event-labels/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

// DTOs matching the OpenAPI contract
public record CreateEventLabelRequestDto
{
    public string Name { get; init; } = "";
    public string? Description { get; init; }
}

public record UpdateEventLabelRequestDto
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}

public record EventLabelResponseDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
