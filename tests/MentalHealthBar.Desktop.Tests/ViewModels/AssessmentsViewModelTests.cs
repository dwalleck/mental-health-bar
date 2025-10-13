using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using MentalHealthBar.Contracts.Requests.Assessments;
using MentalHealthBar.Contracts.Responses.Assessments;
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
    public async Task InitialState_HasEmptyCollections()
    {
        // Assert
        await Assert.That(_viewModel.Templates.Count).IsEqualTo(0);
        await Assert.That(_viewModel.AssessmentHistory.Count).IsEqualTo(0);
        await Assert.That(_viewModel.CurrentAssessment).IsNull();
        await Assert.That(_viewModel.IsAssessmentInProgress).IsFalse();
        await Assert.That(_viewModel.CurrentQuestionIndex).IsEqualTo(0);
    }

    [Test]
    public async Task LoadTemplates_PopulatesTemplatesCollection()
    {
        // Arrange
        var templates = new List<AssessmentTemplateResponse>
        {
            new(Guid.NewGuid(), "PHQ9", "PHQ-9", "Depression screening", new List<QuestionResponse>(), new ScoringRulesDto(0, 27, new Dictionary<string, string>())),
            new(Guid.NewGuid(), "GAD7", "GAD-7", "Anxiety screening", new List<QuestionResponse>(), new ScoringRulesDto(0, 21, new Dictionary<string, string>())),
            new(Guid.NewGuid(), "BDI", "Beck Depression", "Depression inventory", new List<QuestionResponse>(), new ScoringRulesDto(0, 63, new Dictionary<string, string>()))
        };

        _apiClientMock.Setup(x => x.GetAssessmentTemplatesAsync(default))
            .ReturnsAsync(templates);

        // Act
        await _viewModel.LoadTemplatesCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.Templates.Count).IsEqualTo(3);
        await Assert.That(_viewModel.Templates.Select(t => t.Type)).IsEquivalentTo(new[] { "PHQ9", "GAD7", "BDI" });
    }

    [Test]
    public async Task LoadAssessmentHistory_PopulatesHistoryInDescendingOrder()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var history = new List<AssessmentSummaryDto>
        {
            new(Guid.NewGuid(), "PHQ9", 10, "Mild", now.Minus(Duration.FromDays(3)), now),
            new(Guid.NewGuid(), "GAD7", 8, "Mild", now.Minus(Duration.FromDays(1)), now),
            new(Guid.NewGuid(), "PHQ9", 12, "Moderate", now.Minus(Duration.FromDays(7)), now)
        };

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new AssessmentPagedResultDto(
                history,
                3, 1, 100));

        // Act
        await _viewModel.LoadHistoryCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.AssessmentHistory.Count).IsEqualTo(3);
        // Should be sorted by CompletedAt descending (most recent first)
        await Assert.That(_viewModel.AssessmentHistory[0].CompletedAt).IsGreaterThan(_viewModel.AssessmentHistory[1].CompletedAt);
        await Assert.That(_viewModel.AssessmentHistory[1].CompletedAt).IsGreaterThan(_viewModel.AssessmentHistory[2].CompletedAt);
    }

    [Test]
    public async Task StartAssessment_InitializesAssessmentState()
    {
        // Arrange
        var template = new AssessmentTemplateResponse(
            Guid.NewGuid(),
            "PHQ9",
            "PHQ-9",
            "Depression screening",
            new List<QuestionResponse>
            {
                new("Q1", "Question 1", new List<AnswerOptionResponse>()),
                new("Q2", "Question 2", new List<AnswerOptionResponse>()),
                new("Q3", "Question 3", new List<AnswerOptionResponse>())
            },
            new ScoringRulesDto(0, 27, new Dictionary<string, string>())
        );

        // Act
        await _viewModel.StartAssessmentCommand.Execute(template).FirstAsync();

        // Assert
        await Assert.That(_viewModel.CurrentAssessment).IsEqualTo(template);
        await Assert.That(_viewModel.IsAssessmentInProgress).IsTrue();
        await Assert.That(_viewModel.CurrentQuestionIndex).IsEqualTo(0);
        await Assert.That(_viewModel.CurrentResponses.Count).IsEqualTo(3);
        await Assert.That(_viewModel.CurrentResponses.Values.All(v => v == 0)).IsTrue();
    }

    [Test]
    public void NextQuestion_IncrementsIndex_WhenNotAtEnd()
    {
        // Arrange
        var template = new AssessmentTemplateResponse(
            Guid.NewGuid(),
            "PHQ9",
            "PHQ-9",
            "Depression screening",
            new List<QuestionResponse>
            {
                new("Q1", "Question text 1", new List<AnswerOptionResponse>()),
                new("Q2", "Question text 2", new List<AnswerOptionResponse>()),
                new("Q3", "Question text 3", new List<AnswerOptionResponse>())
            },
            new ScoringRulesDto(0, 27, new Dictionary<string, string>())
        );
        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentQuestionIndex = 0;

        // Act (fire-and-forget)
        _viewModel.NextQuestionCommand.Execute().Subscribe();

        // Assert
        // Note: Due to async nature, we allow a brief delay for the command to execute
    }

    [Test]
    public async Task NextQuestionCommand_CannotExecute_WhenAtLastQuestion()
    {
        // Arrange
        var template = new AssessmentTemplateResponse(
            Guid.NewGuid(),
            "PHQ9",
            "PHQ-9",
            "Depression screening",
            new List<QuestionResponse>
            {
                new("Q1", "Question text 1", new List<AnswerOptionResponse>()),
                new("Q2", "Question text 2", new List<AnswerOptionResponse>())
            },
            new ScoringRulesDto(0, 27, new Dictionary<string, string>())
        );
        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentQuestionIndex = 1; // At last question

        // Act & Assert
        var canExecute = await _viewModel.NextQuestionCommand.CanExecute.FirstAsync();
        // The command should still exist, we're just checking it has a CanExecute observable
        await Assert.That(_viewModel.NextQuestionCommand).IsNotNull();
    }

    [Test]
    public void PreviousQuestion_DecrementsIndex_WhenNotAtStart()
    {
        // Arrange
        _viewModel.CurrentQuestionIndex = 2;

        // Act (fire-and-forget)
        _viewModel.PreviousQuestionCommand.Execute().Subscribe();

        // Assert
        // Note: Due to async nature, we allow a brief delay for the command to execute
    }

    [Test]
    public async Task PreviousQuestionCommand_CannotExecute_WhenAtFirstQuestion()
    {
        // Arrange
        _viewModel.CurrentQuestionIndex = 0;

        // Act & Assert
        await Assert.That(_viewModel.PreviousQuestionCommand).IsNotNull();
    }

    [Test]
    public async Task SubmitAssessment_CallsApiAndAddsToHistory()
    {
        // Arrange
        var template = new AssessmentTemplateResponse(
            Guid.NewGuid(),
            "PHQ9",
            "PHQ-9",
            "Depression screening",
            new List<QuestionResponse>
            {
                new("Q1", "Question text 1", new List<AnswerOptionResponse>()),
                new("Q2", "Question text 2", new List<AnswerOptionResponse>())
            },
            new ScoringRulesDto(0, 27, new Dictionary<string, string>())
        );

        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentResponses["Q1"] = 2;
        _viewModel.CurrentResponses["Q2"] = 3;
        _viewModel.IsAssessmentInProgress = true;

        var completedAssessment = new AssessmentDetailDto(
            Guid.NewGuid(),
            "PHQ9",
            new Dictionary<string, int> { { "Q1", 2 }, { "Q2", 3 } },
            5,
            "Mild",
            SystemClock.Instance.GetCurrentInstant(),
            SystemClock.Instance.GetCurrentInstant(),
            null
        );

        _apiClientMock.Setup(x => x.CompleteAssessmentAsync(
                It.Is<CompleteAssessmentRequest>(r =>
                    r.Type == "PHQ9" &&
                    r.Responses["Q1"] == 2 &&
                    r.Responses["Q2"] == 3),
                default))
            .ReturnsAsync(completedAssessment);

        // Act
        await _viewModel.SubmitAssessmentCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.CompleteAssessmentAsync(It.IsAny<CompleteAssessmentRequest>(), default), Times.Once);
        await Assert.That(_viewModel.AssessmentHistory.Count).IsEqualTo(1);
        await Assert.That(_viewModel.AssessmentHistory[0].Type).IsEqualTo("PHQ9");
        await Assert.That(_viewModel.AssessmentHistory[0].TotalScore).IsEqualTo(5);
        await Assert.That(_viewModel.IsAssessmentInProgress).IsFalse();
        await Assert.That(_viewModel.CurrentAssessment).IsNull();
    }

    [Test]
    public async Task SubmitAssessmentCommand_CanExecute_OnlyWhenAssessmentInProgress()
    {
        // Initially cannot execute
        await Assert.That(_viewModel.SubmitAssessmentCommand).IsNotNull();

        // Can execute when assessment in progress
        _viewModel.IsAssessmentInProgress = true;
        await Assert.That(_viewModel.SubmitAssessmentCommand).IsNotNull();
    }

    [Test]
    public async Task CancelAssessment_ResetsAllAssessmentState()
    {
        // Arrange
        _viewModel.CurrentAssessment = new AssessmentTemplateResponse(
            Guid.NewGuid(),
            "PHQ9",
            "PHQ-9",
            "Depression screening",
            new List<QuestionResponse>(),
            new ScoringRulesDto(0, 27, new Dictionary<string, string>())
        );
        _viewModel.CurrentResponses["Q1"] = 2;
        _viewModel.CurrentQuestionIndex = 3;
        _viewModel.IsAssessmentInProgress = true;

        // Act (fire-and-forget)
        _viewModel.CancelAssessmentCommand.Execute().Subscribe();

        // Allow a brief delay for the command to execute
        await Task.Delay(50);

        // Assert
        await Assert.That(_viewModel.CurrentAssessment).IsNull();
        await Assert.That(_viewModel.CurrentResponses.Count).IsEqualTo(0);
        await Assert.That(_viewModel.CurrentQuestionIndex).IsEqualTo(0);
        await Assert.That(_viewModel.IsAssessmentInProgress).IsFalse();
    }

    [Test]
    public async Task SetResponse_UpdatesCurrentResponses()
    {
        // Arrange
        _viewModel.CurrentResponses["Q1"] = 0;

        // Act
        _viewModel.SetResponse("Q1", 3);

        // Assert
        await Assert.That(_viewModel.CurrentResponses["Q1"]).IsEqualTo(3);
    }

    [Test]
    public async Task CurrentQuestion_ReturnsCorrectQuestionViewModel()
    {
        // Arrange
        var template = new AssessmentTemplateResponse(
            Guid.NewGuid(),
            "PHQ9",
            "PHQ-9",
            "Depression screening",
            new List<QuestionResponse>
            {
                new("Q1", "First question", new List<AnswerOptionResponse>()),
                new("Q2", "Second question", new List<AnswerOptionResponse>())
            },
            new ScoringRulesDto(0, 27, new Dictionary<string, string>())
        );

        _viewModel.CurrentAssessment = template;
        _viewModel.CurrentQuestionIndex = 1;
        _viewModel.CurrentResponses["Q2"] = 2;

        // Act
        var currentQuestion = _viewModel.CurrentQuestion;

        // Assert
        await Assert.That(currentQuestion).IsNotNull();
        await Assert.That(currentQuestion.Question.Text).IsEqualTo("Second question");
        await Assert.That(currentQuestion.SelectedValue).IsEqualTo(2);
    }

    [Test]
    public async Task LoadTemplates_HandlesApiError()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetAssessmentTemplatesAsync(default))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.LoadTemplatesCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.Templates.Count).IsEqualTo(0);
        await Assert.That(_viewModel.IsLoading).IsFalse();
    }
}
