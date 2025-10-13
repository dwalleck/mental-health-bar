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

public class MoodEntryViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private int _moodScore = 3; // Default to average
    private string _notes = string.Empty;
    private ObservableCollection<string> _selectedTags;
    private ObservableCollection<EventLabelResponse> _availableLabels;
    private string _newTag = string.Empty;
    private bool _isLoading;
    private DateTime _recordedAt = DateTime.Now;

    public MoodEntryViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _selectedTags = new ObservableCollection<string>();
        _availableLabels = new ObservableCollection<EventLabelResponse>();

        // Create commands with validation
        var canSave = this.WhenAnyValue(
            x => x.MoodScore,
            x => x.IsLoading,
            (score, loading) => score >= 1 && score <= 5 && !loading);

        SaveCommand = ReactiveCommand.CreateFromTask(SaveMoodEntry, canSave);
        LoadLabelsCommand = ReactiveCommand.CreateFromTask(LoadEventLabels);
        AddTagCommand = ReactiveCommand.Create<string>(AddTag);
        RemoveTagCommand = ReactiveCommand.Create<string>(RemoveTag);
        CreateNewLabelCommand = ReactiveCommand.CreateFromTask(CreateNewLabel,
            this.WhenAnyValue(x => x.NewTag, tag => !string.IsNullOrWhiteSpace(tag)));
        ResetCommand = ReactiveCommand.Create(Reset);

        // Load available labels on creation
        _ = LoadEventLabels();
    }

    public int MoodScore
    {
        get => _moodScore;
        set
        {
            this.RaiseAndSetIfChanged(ref _moodScore, value);
            this.RaisePropertyChanged(nameof(MoodLabel));
        }
    }

    public string MoodLabel => MoodScore switch
    {
        1 => "Worst",
        2 => "Below Average",
        3 => "Average",
        4 => "Above Average",
        5 => "Best",
        _ => "Unknown"
    };

    public string Notes
    {
        get => _notes;
        set => this.RaiseAndSetIfChanged(ref _notes, value);
    }

    public ObservableCollection<string> SelectedTags
    {
        get => _selectedTags;
        set => this.RaiseAndSetIfChanged(ref _selectedTags, value);
    }

    public ObservableCollection<EventLabelResponse> AvailableLabels
    {
        get => _availableLabels;
        set => this.RaiseAndSetIfChanged(ref _availableLabels, value);
    }

    public string NewTag
    {
        get => _newTag;
        set => this.RaiseAndSetIfChanged(ref _newTag, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public DateTime RecordedAt
    {
        get => _recordedAt;
        set => this.RaiseAndSetIfChanged(ref _recordedAt, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadLabelsCommand { get; }
    public ReactiveCommand<string, Unit> AddTagCommand { get; }
    public ReactiveCommand<string, Unit> RemoveTagCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateNewLabelCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    private async Task SaveMoodEntry()
    {
        try
        {
            IsLoading = true;

            // Convert tags to EventLabelIds if we have them mapped
            var eventLabelIds = SelectedTags
                .Select(tag => AvailableLabels.FirstOrDefault(l => l.Name == tag)?.Id ?? Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            var request = new CreateMoodEntryRequest(
                MoodScore: MoodScore,
                RecordedAt: Instant.FromDateTimeUtc(RecordedAt.ToUniversalTime()),
                EventLabelIds: eventLabelIds,
                Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
            );

            var result = await _apiClient.CreateMoodEntryAsync(request);

            // TODO: Show success message
            Console.WriteLine($"Mood entry saved with ID: {result.Id}");

            // Reset form
            Reset();
        }
        catch (Exception ex)
        {
            // TODO: Show error message
            Console.WriteLine($"Error saving mood entry: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadEventLabels()
    {
        try
        {
            var labels = await _apiClient.GetEventLabelsAsync();
            AvailableLabels.Clear();
            foreach (var label in labels.OrderBy(l => l.Name))
            {
                AvailableLabels.Add(label);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading labels: {ex.Message}");
        }
    }

    private void AddTag(string tag)
    {
        if (!string.IsNullOrWhiteSpace(tag) && !SelectedTags.Contains(tag) && SelectedTags.Count < 10)
        {
            SelectedTags.Add(tag);
        }
    }

    private void RemoveTag(string tag)
    {
        SelectedTags.Remove(tag);
    }

    private async Task CreateNewLabel()
    {
        if (string.IsNullOrWhiteSpace(NewTag))
            return;

        try
        {
            IsLoading = true;

            var request = new CreateEventLabelRequest(
                Name: NewTag.Trim(),
                Description: null
            );

            var result = await _apiClient.CreateEventLabelAsync(request);

            // Add to available labels
            AvailableLabels.Add(result);

            // Add to selected tags
            AddTag(result.Name);

            // Clear new tag input
            NewTag = string.Empty;

            // Resort labels
            var sorted = AvailableLabels.OrderBy(l => l.Name).ToList();
            AvailableLabels.Clear();
            foreach (var label in sorted)
            {
                AvailableLabels.Add(label);
            }
        }
        catch (Exception ex)
        {
            // TODO: Show error (might be duplicate)
            Console.WriteLine($"Error creating label: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Reset()
    {
        MoodScore = 3;
        Notes = string.Empty;
        SelectedTags.Clear();
        NewTag = string.Empty;
        RecordedAt = DateTime.Now;
    }
}