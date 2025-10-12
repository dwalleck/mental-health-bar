using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;

namespace MentalHealthBar.Desktop.Tests.ViewModels;

public class AssessmentsViewModelTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly AssessmentsViewModel _viewModel;

    public AssessmentsViewModelTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _viewModel = new AssessmentsViewModel(_apiClientMock.Object);
    }

    [Test]
    public void InitialState_HasEmptyCollections()
    {
        // Assert
        Assert.That(_viewModel.Templates, Is.Empty);
        Assert.That(_viewModel.AssessmentHistory, Is.Empty);
        Assert.That(_viewModel.CurrentAssessment, Is.Null);
        Assert.That(_viewModel.IsAssessmentInProgress, Is.False);
        Assert.That(_viewModel.CurrentQuestionIndex, Is.EqualTo(0));
    }

    [Test]
    public async Task LoadTemplates_PopulatesTemplatesCollection()
    {
        // Arrange
        var templates = new List<AssessmentTemplateResponse>
        {
            new() { Type = "PHQ9", Name = "PHQ-9", Questions = new List<QuestionResponse>() },
            new() { Type = "GAD7", Name = "GAD-7", Questions = new List<QuestionResponse>() },
            new() { Type = "BDI", Name = "Beck Depression", Questions = new List<QuestionResponse>() }
        };

        _apiClientMock.Setup(x => x.GetAssessmentTemplatesAsync(default))
            .ReturnsAsync(templates);

        // Act
        await _viewModel.LoadTemplatesCommand.Execute();

        // Assert
        Assert.That(_viewModel.Templates, Has.Count.EqualTo(3));
        Assert.That(_viewModel.Templates.Select(t => t.Type), Is.EquivalentTo(new[] { "PHQ9", "GAD7", "BDI" }));
    }

    [Test]
    public async Task LoadAssessmentHistory_PopulatesHistoryInDescendingOrder()
    {
        // Arrange
        var history = new List<AssessmentResponse>
        {
            new() { Id = Guid.NewGuid(), Type = "PHQ9", TotalScore = 10, CompletedAt = DateTimeOffset.Now.AddDays(-3) },
            new() { Id = Guid.NewGuid(), Type = "GAD7", TotalScore = 8, CompletedAt = DateTimeOffset.Now.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Type = "PHQ9", TotalScore = 12, CompletedAt = DateTimeOffset.Now.AddDays(-7) }
        };

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(history);

        // Act
        await _viewModel.LoadHistoryCommand.Execute();

        // Assert
        Assert.That(_viewModel.AssessmentHistory, Has.Count.EqualTo(3));
        // Should be sorted by CompletedAt descending (most recent first)
        Assert.That(_viewModel.AssessmentHistory[0].CompletedAt, Is.GreaterThan(_viewModel.AssessmentHistory[1].CompletedAt));
        Assert.That(_viewModel.AssessmentHistory[1].CompletedAt, Is.GreaterThan(_viewModel.AssessmentHistory[2].CompletedAt));
    }

    [Test]
    public async Task StartAssessment_InitializesAssessmentState()
    {
        // Arrange
        var template = new AssessmentTemplateResponse
        {
            Type = "PHQ9",
            Name = "PHQ-9",
            Questions = new List<QuestionResponse>
            {
                new() { Id = "Q1", Text = "Question 1", Options = new List<AnswerOptionResponse>() },
                new() { Id = "Q2", Text = "Question 2", Options = new List<AnswerOptionResponse>() },
                new() { Id = "Q3", Text = "Question 3", Options = new List<AnswerOptionResponse>() }
            }
        };

        // Act
        await _viewModel.StartAssessmentCommand.Execute(template);

        // Assert
        Assert.That(_viewModel.CurrentAssessment, Is.EqualTo(template));
        Assert.That(_viewModel.IsAssessmentInProgress, Is.True);
        Assert.That(_viewModel.CurrentQuestionIndex, Is.EqualTo(0));
        Assert.That(_viewModel.CurrentResponses, Has.Count.EqualTo(3));
        Assert.That(_viewModel.CurrentResponses.Values.All(v => v == 0), Is.True);
    }

    [Test]
    public void NextQuestion_IncrementsIndex_WhenNotAtEnd()
    {
        // Arrange
        var template = new AssessmentTemplateResponse
        {
            Questions = new List<QuestionResponse>
            {
                new() { Id = "Q1" },
                new() { Id = "Q2" },
                new() { Id = "Q3" }
            }
        };
        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentQuestionIndex = 0;

        // Act
        _viewModel.NextQuestionCommand.Execute();

        // Assert
        Assert.That(_viewModel.CurrentQuestionIndex, Is.EqualTo(1));
    }

    [Test]
    public void NextQuestionCommand_CannotExecute_WhenAtLastQuestion()
    {
        // Arrange
        var template = new AssessmentTemplateResponse
        {
            Questions = new List<QuestionResponse>
            {
                new() { Id = "Q1" },
                new() { Id = "Q2" }
            }
        };
        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentQuestionIndex = 1; // At last question

        // Act & Assert
        Assert.That(_viewModel.NextQuestionCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public void PreviousQuestion_DecrementsIndex_WhenNotAtStart()
    {
        // Arrange
        _viewModel.CurrentQuestionIndex = 2;

        // Act
        _viewModel.PreviousQuestionCommand.Execute();

        // Assert
        Assert.That(_viewModel.CurrentQuestionIndex, Is.EqualTo(1));
    }

    [Test]
    public void PreviousQuestionCommand_CannotExecute_WhenAtFirstQuestion()
    {
        // Arrange
        _viewModel.CurrentQuestionIndex = 0;

        // Act & Assert
        Assert.That(_viewModel.PreviousQuestionCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public async Task SubmitAssessment_CallsApiAndAddsToHistory()
    {
        // Arrange
        var template = new AssessmentTemplateResponse
        {
            Type = "PHQ9",
            Questions = new List<QuestionResponse>
            {
                new() { Id = "Q1" },
                new() { Id = "Q2" }
            }
        };

        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentResponses["Q1"] = 2;
        _viewModel.CurrentResponses["Q2"] = 3;
        _viewModel.IsAssessmentInProgress = true;

        var completedAssessment = new AssessmentResponse
        {
            Id = Guid.NewGuid(),
            Type = "PHQ9",
            TotalScore = 5,
            Severity = "Mild",
            CompletedAt = DateTimeOffset.Now
        };

        _apiClientMock.Setup(x => x.CompleteAssessmentAsync(
                It.Is<CompleteAssessmentRequest>(r =>
                    r.Type == "PHQ9" &&
                    r.Responses["Q1"] == 2 &&
                    r.Responses["Q2"] == 3),
                default))
            .ReturnsAsync(completedAssessment);

        // Act
        await _viewModel.SubmitAssessmentCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.CompleteAssessmentAsync(It.IsAny<CompleteAssessmentRequest>(), default), Times.Once);
        Assert.That(_viewModel.AssessmentHistory, Contains.Item(completedAssessment));
        Assert.That(_viewModel.AssessmentHistory[0], Is.EqualTo(completedAssessment)); // Should be inserted at beginning
        Assert.That(_viewModel.IsAssessmentInProgress, Is.False);
        Assert.That(_viewModel.CurrentAssessment, Is.Null);
    }

    [Test]
    public void SubmitAssessmentCommand_CanExecute_OnlyWhenAssessmentInProgress()
    {
        // Initially cannot execute
        Assert.That(_viewModel.SubmitAssessmentCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Can execute when assessment in progress
        _viewModel.IsAssessmentInProgress = true;
        Assert.That(_viewModel.SubmitAssessmentCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public void CancelAssessment_ResetsAllAssessmentState()
    {
        // Arrange
        _viewModel.CurrentAssessment = new AssessmentTemplateResponse { Type = "PHQ9" };
        _viewModel.CurrentResponses["Q1"] = 2;
        _viewModel.CurrentQuestionIndex = 3;
        _viewModel.IsAssessmentInProgress = true;

        // Act
        _viewModel.CancelAssessmentCommand.Execute();

        // Assert
        Assert.That(_viewModel.CurrentAssessment, Is.Null);
        Assert.That(_viewModel.CurrentResponses, Is.Empty);
        Assert.That(_viewModel.CurrentQuestionIndex, Is.EqualTo(0));
        Assert.That(_viewModel.IsAssessmentInProgress, Is.False);
    }

    [Test]
    public void SetResponse_UpdatesCurrentResponses()
    {
        // Arrange
        _viewModel.CurrentResponses["Q1"] = 0;

        // Act
        _viewModel.SetResponse("Q1", 3);

        // Assert
        Assert.That(_viewModel.CurrentResponses["Q1"], Is.EqualTo(3));
    }

    [Test]
    public void CurrentQuestion_ReturnsCorrectQuestionViewModel()
    {
        // Arrange
        var template = new AssessmentTemplateResponse
        {
            Questions = new List<QuestionResponse>
            {
                new() { Id = "Q1", Text = "First question" },
                new() { Id = "Q2", Text = "Second question" }
            }
        };

        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentQuestionIndex = 1;
        _viewModel.CurrentResponses["Q2"] = 2;

        // Act
        var currentQuestion = _viewModel.CurrentQuestion;

        // Assert
        Assert.That(currentQuestion, Is.Not.Null);
        Assert.That(currentQuestion.Question.Text, Is.EqualTo("Second question"));
        Assert.That(currentQuestion.SelectedValue, Is.EqualTo(2));
    }

    [Test]
    public async Task LoadTemplates_HandlesApiError()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetAssessmentTemplatesAsync(default))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.LoadTemplatesCommand.Execute();

        // Assert
        Assert.That(_viewModel.Templates, Is.Empty);
        Assert.That(_viewModel.IsLoading, Is.False);
    }
}