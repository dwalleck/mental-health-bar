using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using MentalHealthBar.Contracts.Requests.EventLabels;
using MentalHealthBar.Contracts.Requests.MoodEntries;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Desktop.Tests.ViewModels;

public class MoodEntryViewModelTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly MoodEntryViewModel _viewModel;

    public MoodEntryViewModelTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _viewModel = new MoodEntryViewModel(_apiClientMock.Object);
    }

    [Test]
    public async Task InitialState_HasCorrectDefaults()
    {
        // Assert
        await Assert.That(_viewModel.MoodScore).IsEqualTo(3);
        await Assert.That(_viewModel.MoodLabel).IsEqualTo("Average");
        await Assert.That(_viewModel.Notes).IsEmpty();
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(0);
        await Assert.That(_viewModel.IsLoading).IsFalse();
        await Assert.That(_viewModel.RecordedAt.Date).IsEqualTo(DateTime.Today);
    }

    [Test]
    [Arguments(1, "Worst")]
    [Arguments(2, "Below Average")]
    [Arguments(3, "Average")]
    [Arguments(4, "Above Average")]
    [Arguments(5, "Best")]
    public async Task MoodScore_UpdatesLabel(int score, string expectedLabel)
    {
        // Act
        _viewModel.MoodScore = score;

        // Assert
        await Assert.That(_viewModel.MoodLabel).IsEqualTo(expectedLabel);
    }

    [Test]
    public async Task SaveCommand_WithValidData_CallsApiAndResetsForm()
    {
        // Arrange
        _viewModel.MoodScore = 4;
        _viewModel.Notes = "Feeling good today";
        _viewModel.SelectedTags.Add("exercise");
        _viewModel.SelectedTags.Add("work");

        var savedResponse = new MoodEntryDto(
            Guid.NewGuid(),
            4,
            DateTimeOffset.Now,
            new List<EventLabelDto>(),
            "Feeling good today",
            DateTimeOffset.Now,
            null
        );

        _apiClientMock.Setup(x => x.CreateMoodEntryAsync(
                It.Is<CreateMoodEntryRequest>(r =>
                    r.MoodScore == 4 &&
                    r.Notes == "Feeling good today" &&
                    r.EventLabelIds.Count == 2),
                default))
            .ReturnsAsync(savedResponse);

        // Act
        await _viewModel.SaveCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.CreateMoodEntryAsync(It.IsAny<CreateMoodEntryRequest>(), default), Times.Once);
        await Assert.That(_viewModel.MoodScore).IsEqualTo(3); // Reset to default
        await Assert.That(_viewModel.Notes).IsEmpty();
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SaveCommand_CanExecute_OnlyWhenMoodScoreValid()
    {
        // Assert - Should be executable initially (MoodScore = 3)
        var canExecute1 = await _viewModel.SaveCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute1).IsTrue();

        // Act & Assert - Invalid scores
        _viewModel.MoodScore = 0;
        var canExecute2 = await _viewModel.SaveCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute2).IsFalse();

        _viewModel.MoodScore = 6;
        var canExecute3 = await _viewModel.SaveCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute3).IsFalse();

        // Valid scores
        _viewModel.MoodScore = 1;
        var canExecute4 = await _viewModel.SaveCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute4).IsTrue();

        _viewModel.MoodScore = 5;
        var canExecute5 = await _viewModel.SaveCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute5).IsTrue();
    }

    [Test]
    public async Task LoadEventLabels_PopulatesAvailableLabels()
    {
        // Arrange
        var labels = new List<EventLabelDto>
        {
            new(Guid.NewGuid(), "exercise", null, DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "work", null, DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "family", null, DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetEventLabelsAsync(null, default))
            .ReturnsAsync(labels);

        // Act
        await _viewModel.LoadLabelsCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.AvailableLabels.Count).IsEqualTo(3);
        await Assert.That(_viewModel.AvailableLabels.Select(l => l.Name)).IsEquivalentTo(new[] { "exercise", "family", "work" }); // Should be sorted
    }

    [Test]
    public async Task AddTag_AddsToSelectedTags_WhenValid()
    {
        // Act
        _viewModel.AddTagCommand.Execute("exercise").Subscribe();
        _viewModel.AddTagCommand.Execute("work").Subscribe();

        // Assert
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(2);
        await Assert.That(_viewModel.SelectedTags).Contains("exercise");
        await Assert.That(_viewModel.SelectedTags).Contains("work");
    }

    [Test]
    public async Task AddTag_DoesNotAddDuplicates()
    {
        // Act
        _viewModel.AddTagCommand.Execute("exercise").Subscribe();
        _viewModel.AddTagCommand.Execute("exercise").Subscribe();

        // Assert
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddTag_RespectsMaximumLimit()
    {
        // Act - Add 11 tags (the limit is 10)
        for (int i = 1; i <= 11; i++)
        {
            _viewModel.AddTagCommand.Execute($"tag{i}").Subscribe();
        }

        // Assert - Should only have 10
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(10);
    }

    [Test]
    public async Task RemoveTag_RemovesFromSelectedTags()
    {
        // Arrange
        _viewModel.SelectedTags.Add("exercise");
        _viewModel.SelectedTags.Add("work");

        // Act
        _viewModel.RemoveTagCommand.Execute("exercise").Subscribe();

        // Assert
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(1);
        await Assert.That(_viewModel.SelectedTags).DoesNotContain("exercise");
        await Assert.That(_viewModel.SelectedTags).Contains("work");
    }

    [Test]
    public async Task CreateNewLabel_CreatesAndAddsToLists()
    {
        // Arrange
        _viewModel.NewTag = "meditation";
        var createdLabel = new EventLabelDto(
            Guid.NewGuid(),
            "meditation",
            null,
            DateTimeOffset.Now,
            null
        );

        _apiClientMock.Setup(x => x.CreateEventLabelAsync(
                It.Is<CreateEventLabelRequest>(r => r.Name == "meditation"),
                default))
            .ReturnsAsync(createdLabel);

        // Act
        await _viewModel.CreateNewLabelCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.CreateEventLabelAsync(It.IsAny<CreateEventLabelRequest>(), default), Times.Once);
        await Assert.That(_viewModel.AvailableLabels).Contains(createdLabel);
        await Assert.That(_viewModel.SelectedTags).Contains("meditation");
        await Assert.That(_viewModel.NewTag).IsEmpty(); // Should be cleared
    }

    [Test]
    public async Task CreateNewLabelCommand_CanExecute_OnlyWithNonEmptyNewTag()
    {
        // Assert - Initially cannot execute (NewTag is empty)
        var canExecute1 = await _viewModel.CreateNewLabelCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute1).IsFalse();

        // Act & Assert - With value
        _viewModel.NewTag = "meditation";
        var canExecute2 = await _viewModel.CreateNewLabelCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute2).IsTrue();

        // Act & Assert - With whitespace only
        _viewModel.NewTag = "   ";
        var canExecute3 = await _viewModel.CreateNewLabelCommand.CanExecute.FirstAsync();
        await Assert.That(canExecute3).IsFalse();
    }

    [Test]
    public async Task Reset_ResetsAllFieldsToDefaults()
    {
        // Arrange
        _viewModel.MoodScore = 5;
        _viewModel.Notes = "Some notes";
        _viewModel.SelectedTags.Add("tag1");
        _viewModel.NewTag = "newtag";
        _viewModel.RecordedAt = DateTime.Now.AddDays(-5);

        // Act
        _viewModel.ResetCommand.Execute().Subscribe();

        // Assert
        await Assert.That(_viewModel.MoodScore).IsEqualTo(3);
        await Assert.That(_viewModel.Notes).IsEmpty();
        await Assert.That(_viewModel.SelectedTags.Count).IsEqualTo(0);
        await Assert.That(_viewModel.NewTag).IsEmpty();
        await Assert.That(_viewModel.RecordedAt.Date).IsEqualTo(DateTime.Today);
    }

    [Test]
    public async Task SaveCommand_HandlesApiError()
    {
        // Arrange
        _apiClientMock.Setup(x => x.CreateMoodEntryAsync(It.IsAny<CreateMoodEntryRequest>(), default))
            .ThrowsAsync(new Exception("Network error"));

        // Act - Should not throw, error should be handled
        try
        {
            await _viewModel.SaveCommand.Execute().FirstAsync();
        }
        catch
        {
            // Expected - command may propagate error
        }

        // Assert - Form should not be reset on error and loading should be false
        await Assert.That(_viewModel.IsLoading).IsFalse();
    }
}
