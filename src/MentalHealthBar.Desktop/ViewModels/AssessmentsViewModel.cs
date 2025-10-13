using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using MentalHealthBar.Desktop.Services;
using NodaTime;

namespace MentalHealthBar.Desktop.ViewModels;

public class AssessmentsViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private ObservableCollection<AssessmentTemplateResponse> _templates;
    private ObservableCollection<AssessmentSummaryResponse> _assessmentHistory;
    private AssessmentTemplateResponse? _selectedTemplate;
    private AssessmentTemplateResponse? _currentAssessment;
    private Dictionary<string, int> _currentResponses;
    private bool _isLoading;
    private bool _isAssessmentInProgress;
    private int _currentQuestionIndex;

    public AssessmentsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _templates = [];
        _assessmentHistory = new ObservableCollection<AssessmentSummaryResponse>();
        _currentResponses = new Dictionary<string, int>();

        // Commands
        LoadTemplatesCommand = ReactiveCommand.CreateFromTask(LoadTemplates);
        LoadHistoryCommand = ReactiveCommand.CreateFromTask(LoadAssessmentHistory);
        StartAssessmentCommand = ReactiveCommand.CreateFromTask<AssessmentTemplateResponse>(StartAssessment);
        SubmitAssessmentCommand = ReactiveCommand.CreateFromTask(SubmitAssessment,
            this.WhenAnyValue(x => x.IsAssessmentInProgress));
        NextQuestionCommand = ReactiveCommand.Create(NextQuestion,
            this.WhenAnyValue(
                x => x.CurrentQuestionIndex,
                x => x.CurrentAssessment,
                (index, assessment) => assessment != null && index < assessment.Questions.Count - 1));
        PreviousQuestionCommand = ReactiveCommand.Create(PreviousQuestion,
            this.WhenAnyValue(x => x.CurrentQuestionIndex, index => index > 0));
        CancelAssessmentCommand = ReactiveCommand.Create(CancelAssessment);

        // Load data on creation
        _ = Task.Run(async () =>
        {
            await LoadTemplates();
            await LoadAssessmentHistory();
        });
    }

    public ObservableCollection<AssessmentTemplateResponse> Templates
    {
        get => _templates;
        set => this.RaiseAndSetIfChanged(ref _templates, value);
    }

    public ObservableCollection<AssessmentSummaryResponse> AssessmentHistory
    {
        get => _assessmentHistory;
        set => this.RaiseAndSetIfChanged(ref _assessmentHistory, value);
    }

    public AssessmentTemplateResponse? SelectedTemplate
    {
        get => _selectedTemplate;
        set => this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
    }

    public AssessmentTemplateResponse? CurrentAssessment
    {
        get => _currentAssessment;
        set => this.RaiseAndSetIfChanged(ref _currentAssessment, value);
    }

    public Dictionary<string, int> CurrentResponses
    {
        get => _currentResponses;
        set => this.RaiseAndSetIfChanged(ref _currentResponses, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public bool IsAssessmentInProgress
    {
        get => _isAssessmentInProgress;
        set => this.RaiseAndSetIfChanged(ref _isAssessmentInProgress, value);
    }

    public int CurrentQuestionIndex
    {
        get => _currentQuestionIndex;
        set => this.RaiseAndSetIfChanged(ref _currentQuestionIndex, value);
    }

    public QuestionViewModel? CurrentQuestion =>
        CurrentAssessment != null && CurrentQuestionIndex < CurrentAssessment.Questions.Count
            ? new QuestionViewModel(
                CurrentAssessment.Questions[CurrentQuestionIndex],
                CurrentResponses.GetValueOrDefault(CurrentAssessment.Questions[CurrentQuestionIndex].Id))
            : null;

    public ReactiveCommand<Unit, Unit> LoadTemplatesCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadHistoryCommand { get; }
    public ReactiveCommand<AssessmentTemplateResponse, Unit> StartAssessmentCommand { get; }
    public ReactiveCommand<Unit, Unit> SubmitAssessmentCommand { get; }
    public ReactiveCommand<Unit, Unit> NextQuestionCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviousQuestionCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelAssessmentCommand { get; }

    private async Task LoadTemplates()
    {
        try
        {
            IsLoading = true;
            var templates = await _apiClient.GetAssessmentTemplatesAsync(CancellationToken);
            Templates.Clear();
            foreach (var template in templates)
            {
                Templates.Add(template);
            }
        }
        catch (Exception ex)
        {
            // TODO: Handle error with user notification
            Console.WriteLine($"Error loading templates: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAssessmentHistory()
    {
        try
        {
            IsLoading = true;
            var history = await _apiClient.GetAssessmentHistoryAsync(
                startDate: DateTime.Now.AddMonths(-3),
                endDate: DateTime.Now,
                cancellationToken: CancellationToken);

            AssessmentHistory.Clear();
            foreach (var assessment in history.Items.OrderByDescending(a => a.CompletedAt))
            {
                AssessmentHistory.Add(assessment);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading history: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartAssessment(AssessmentTemplateResponse template)
    {
        CurrentAssessment = template;
        CurrentResponses.Clear();
        CurrentQuestionIndex = 0;
        IsAssessmentInProgress = true;

        // Initialize responses with default values (0)
        foreach (var question in template.Questions)
        {
            CurrentResponses[question.Id] = 0;
        }

        await Task.CompletedTask;
    }

    private async Task SubmitAssessment()
    {
        if (CurrentAssessment == null || CurrentResponses.Count == 0)
            return;

        try
        {
            IsLoading = true;

            var request = new CompleteAssessmentRequest(
                Type: CurrentAssessment.Type,
                CompletedAt: SystemClock.Instance.GetCurrentInstant(),
                Responses: CurrentResponses
            );

            var result = await _apiClient.CompleteAssessmentAsync(request, CancellationToken);

            // Add to history (convert detail to summary)
            var summary = new AssessmentSummaryResponse(
                result.Id,
                result.Type,
                result.TotalScore,
                result.Severity,
                result.CompletedAt,
                result.CreatedAt
            );
            AssessmentHistory.Insert(0, summary);

            // Reset assessment state
            CancelAssessment();

            // TODO: Show success message with score and severity
        }
        catch (Exception ex)
        {
            // TODO: Show error message
            Console.WriteLine($"Error submitting assessment: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NextQuestion()
    {
        if (CurrentAssessment != null && CurrentQuestionIndex < CurrentAssessment.Questions.Count - 1)
        {
            CurrentQuestionIndex++;
            this.RaisePropertyChanged(nameof(CurrentQuestion));
        }
    }

    private void PreviousQuestion()
    {
        if (CurrentQuestionIndex > 0)
        {
            CurrentQuestionIndex--;
            this.RaisePropertyChanged(nameof(CurrentQuestion));
        }
    }

    private void CancelAssessment()
    {
        CurrentAssessment = null;
        CurrentResponses.Clear();
        CurrentQuestionIndex = 0;
        IsAssessmentInProgress = false;
    }

    public void SetResponse(string questionId, int value)
    {
        CurrentResponses[questionId] = value;
    }
}

public class QuestionViewModel : ViewModelBase
{
    public QuestionViewModel(QuestionResponse question, int selectedValue = 0)
    {
        Question = question;
        SelectedValue = selectedValue;
    }

    public QuestionResponse Question { get; }

    private int _selectedValue;
    public int SelectedValue
    {
        get => _selectedValue;
        set => this.RaiseAndSetIfChanged(ref _selectedValue, value);
    }
}