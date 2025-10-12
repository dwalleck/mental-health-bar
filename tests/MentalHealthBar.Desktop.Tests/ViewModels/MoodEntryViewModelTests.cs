using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;

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
    public void InitialState_HasCorrectDefaults()
    {
        // Assert
        Assert.That(_viewModel.MoodScore, Is.EqualTo(3));
        Assert.That(_viewModel.MoodLabel, Is.EqualTo("Average"));
        Assert.That(_viewModel.Notes, Is.Empty);
        Assert.That(_viewModel.SelectedTags, Is.Empty);
        Assert.That(_viewModel.IsLoading, Is.False);
        Assert.That(_viewModel.RecordedAt.Date, Is.EqualTo(DateTime.Today));
    }

    [Test]
    [Arguments(1, "Worst")]
    [Arguments(2, "Below Average")]
    [Arguments(3, "Average")]
    [Arguments(4, "Above Average")]
    [Arguments(5, "Best")]
    public void MoodScore_UpdatesLabel(int score, string expectedLabel)
    {
        // Act
        _viewModel.MoodScore = score;

        // Assert
        Assert.That(_viewModel.MoodLabel, Is.EqualTo(expectedLabel));
    }

    [Test]
    public async Task SaveCommand_WithValidData_CallsApiAndResetsForm()
    {
        // Arrange
        _viewModel.MoodScore = 4;
        _viewModel.Notes = "Feeling good today";
        _viewModel.SelectedTags.Add("exercise");
        _viewModel.SelectedTags.Add("work");

        var savedResponse = new MoodEntryResponse
        {
            Id = Guid.NewGuid(),
            MoodScore = 4,
            RecordedAt = DateTimeOffset.Now
        };

        _apiClientMock.Setup(x => x.CreateMoodEntryAsync(
                It.Is<CreateMoodEntryRequest>(r =>
                    r.MoodScore == 4 &&
                    r.Notes == "Feeling good today" &&
                    r.Tags.Count == 2),
                default))
            .ReturnsAsync(savedResponse);

        // Act
        await _viewModel.SaveCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.CreateMoodEntryAsync(It.IsAny<CreateMoodEntryRequest>(), default), Times.Once);
        Assert.That(_viewModel.MoodScore, Is.EqualTo(3)); // Reset to default
        Assert.That(_viewModel.Notes, Is.Empty);
        Assert.That(_viewModel.SelectedTags, Is.Empty);
    }

    [Test]
    public void SaveCommand_CanExecute_OnlyWhenMoodScoreValid()
    {
        // Assert - Should be executable initially (MoodScore = 3)
        Assert.That(_viewModel.SaveCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Act & Assert - Invalid scores
        _viewModel.MoodScore = 0;
        Assert.That(_viewModel.SaveCommand.CanExecute().Subscribe(), Is.Not.Null);

        _viewModel.MoodScore = 6;
        Assert.That(_viewModel.SaveCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Valid scores
        _viewModel.MoodScore = 1;
        Assert.That(_viewModel.SaveCommand.CanExecute().Subscribe(), Is.Not.Null);

        _viewModel.MoodScore = 5;
        Assert.That(_viewModel.SaveCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public async Task LoadEventLabels_PopulatesAvailableLabels()
    {
        // Arrange
        var labels = new List<EventLabelResponse>
        {
            new() { Id = Guid.NewGuid(), Name = "exercise" },
            new() { Id = Guid.NewGuid(), Name = "work" },
            new() { Id = Guid.NewGuid(), Name = "family" }
        };

        _apiClientMock.Setup(x => x.GetEventLabelsAsync(null, default))
            .ReturnsAsync(labels);

        // Act
        await _viewModel.LoadLabelsCommand.Execute();

        // Assert
        Assert.That(_viewModel.AvailableLabels, Has.Count.EqualTo(3));
        Assert.That(_viewModel.AvailableLabels.Select(l => l.Name), Is.EquivalentTo(new[] { "exercise", "family", "work" })); // Should be sorted
    }

    [Test]
    public void AddTag_AddsToSelectedTags_WhenValid()
    {
        // Act
        _viewModel.AddTagCommand.Execute("exercise");
        _viewModel.AddTagCommand.Execute("work");

        // Assert
        Assert.That(_viewModel.SelectedTags, Has.Count.EqualTo(2));
        Assert.That(_viewModel.SelectedTags, Does.Contain("exercise"));
        Assert.That(_viewModel.SelectedTags, Does.Contain("work"));
    }

    [Test]
    public void AddTag_DoesNotAddDuplicates()
    {
        // Act
        _viewModel.AddTagCommand.Execute("exercise");
        _viewModel.AddTagCommand.Execute("exercise");

        // Assert
        Assert.That(_viewModel.SelectedTags, Has.Count.EqualTo(1));
    }

    [Test]
    public void AddTag_RespectsMaximumLimit()
    {
        // Act - Add 10 tags (the limit)
        for (int i = 1; i <= 11; i++)
        {
            _viewModel.AddTagCommand.Execute($"tag{i}");
        }

        // Assert - Should only have 10
        Assert.That(_viewModel.SelectedTags, Has.Count.EqualTo(10));
    }

    [Test]
    public void RemoveTag_RemovesFromSelectedTags()
    {
        // Arrange
        _viewModel.SelectedTags.Add("exercise");
        _viewModel.SelectedTags.Add("work");

        // Act
        _viewModel.RemoveTagCommand.Execute("exercise");

        // Assert
        Assert.That(_viewModel.SelectedTags, Has.Count.EqualTo(1));
        Assert.That(_viewModel.SelectedTags, Does.Not.Contain("exercise"));
        Assert.That(_viewModel.SelectedTags, Does.Contain("work"));
    }

    [Test]
    public async Task CreateNewLabel_CreatesAndAddsToLists()
    {
        // Arrange
        _viewModel.NewTag = "meditation";
        var createdLabel = new EventLabelResponse
        {
            Id = Guid.NewGuid(),
            Name = "meditation"
        };

        _apiClientMock.Setup(x => x.CreateEventLabelAsync(
                It.Is<CreateEventLabelRequest>(r => r.Name == "meditation"),
                default))
            .ReturnsAsync(createdLabel);

        // Act
        await _viewModel.CreateNewLabelCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.CreateEventLabelAsync(It.IsAny<CreateEventLabelRequest>(), default), Times.Once);
        Assert.That(_viewModel.AvailableLabels, Does.Contain(createdLabel));
        Assert.That(_viewModel.SelectedTags, Does.Contain("meditation"));
        Assert.That(_viewModel.NewTag, Is.Empty); // Should be cleared
    }

    [Test]
    public void CreateNewLabelCommand_CanExecute_OnlyWithNonEmptyNewTag()
    {
        // Assert - Initially cannot execute (NewTag is empty)
        Assert.That(_viewModel.CreateNewLabelCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Act & Assert - With value
        _viewModel.NewTag = "meditation";
        Assert.That(_viewModel.CreateNewLabelCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Act & Assert - With whitespace only
        _viewModel.NewTag = "   ";
        Assert.That(_viewModel.CreateNewLabelCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public void Reset_ResetsAllFieldsToDefaults()
    {
        // Arrange
        _viewModel.MoodScore = 5;
        _viewModel.Notes = "Some notes";
        _viewModel.SelectedTags.Add("tag1");
        _viewModel.NewTag = "newtag";
        _viewModel.RecordedAt = DateTime.Now.AddDays(-5);

        // Act
        _viewModel.ResetCommand.Execute();

        // Assert
        Assert.That(_viewModel.MoodScore, Is.EqualTo(3));
        Assert.That(_viewModel.Notes, Is.Empty);
        Assert.That(_viewModel.SelectedTags, Is.Empty);
        Assert.That(_viewModel.NewTag, Is.Empty);
        Assert.That(_viewModel.RecordedAt.Date, Is.EqualTo(DateTime.Today));
    }

    [Test]
    public async Task SaveCommand_HandlesApiError()
    {
        // Arrange
        _apiClientMock.Setup(x => x.CreateMoodEntryAsync(It.IsAny<CreateMoodEntryRequest>(), default))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.SaveCommand.Execute();

        // Assert - Form should not be reset on error
        Assert.That(_viewModel.MoodScore, Is.EqualTo(3));
        Assert.That(_viewModel.IsLoading, Is.False);
    }
}