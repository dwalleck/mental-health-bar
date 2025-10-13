using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using Avalonia.Platform.Storage;
using MentalHealthBar.Desktop.Services;

namespace MentalHealthBar.Desktop.ViewModels;

public class ExportViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private ExportFormat _selectedFormat = ExportFormat.CSV;
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-3));
    private DateOnly _endDate = DateOnly.FromDateTime(DateTime.Now);
    private bool _includeAssessments = true;
    private bool _includeMoodEntries = true;
    private bool _includeHealthMetrics = true;
    private bool _includeEventLabels = true;
    private bool _isExporting;
    private string? _statusMessage;
    private double _exportProgress;

    public ExportViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;

        // Commands with validation
        var canExport = this.WhenAnyValue(
            x => x.IsExporting,
            x => x.IncludeAssessments,
            x => x.IncludeMoodEntries,
            x => x.IncludeHealthMetrics,
            x => x.IncludeEventLabels,
            (exporting, assessments, mood, health, labels) =>
                !exporting && (assessments || mood || health || labels));

        ExportCommand = ReactiveCommand.CreateFromTask(ExportData, canExport);
        SelectAllCommand = ReactiveCommand.Create(SelectAll);
        DeselectAllCommand = ReactiveCommand.Create(DeselectAll);
    }

    public ExportFormat SelectedFormat
    {
        get => _selectedFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedFormat, value);
    }

    public DateOnly StartDate
    {
        get => _startDate;
        set => this.RaiseAndSetIfChanged(ref _startDate, value);
    }

    public DateOnly EndDate
    {
        get => _endDate;
        set => this.RaiseAndSetIfChanged(ref _endDate, value);
    }

    public bool IncludeAssessments
    {
        get => _includeAssessments;
        set => this.RaiseAndSetIfChanged(ref _includeAssessments, value);
    }

    public bool IncludeMoodEntries
    {
        get => _includeMoodEntries;
        set => this.RaiseAndSetIfChanged(ref _includeMoodEntries, value);
    }

    public bool IncludeHealthMetrics
    {
        get => _includeHealthMetrics;
        set => this.RaiseAndSetIfChanged(ref _includeHealthMetrics, value);
    }

    public bool IncludeEventLabels
    {
        get => _includeEventLabels;
        set => this.RaiseAndSetIfChanged(ref _includeEventLabels, value);
    }

    public bool IsExporting
    {
        get => _isExporting;
        set => this.RaiseAndSetIfChanged(ref _isExporting, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public double ExportProgress
    {
        get => _exportProgress;
        set => this.RaiseAndSetIfChanged(ref _exportProgress, value);
    }

    public ReactiveCommand<Unit, Unit> ExportCommand { get; }
    public ReactiveCommand<Unit, Unit> SelectAllCommand { get; }
    public ReactiveCommand<Unit, Unit> DeselectAllCommand { get; }

    // Property to get IStorageProvider from the main window (will be injected)
    public IStorageProvider? StorageProvider { get; set; }

    private async Task ExportData()
    {
        try
        {
            IsExporting = true;
            ExportProgress = 0;
            StatusMessage = "Preparing export...";

            var request = new ExportRequest(
                StartDate: StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate: EndDate.ToDateTime(TimeOnly.MaxValue),
                IncludeAssessments: IncludeAssessments,
                IncludeMoodEntries: IncludeMoodEntries,
                IncludeHealthMetrics: IncludeHealthMetrics
            );

            ExportProgress = 25;
            StatusMessage = "Fetching data from server...";

            string extension;
            string mimeType;

            // Determine format settings
            if (SelectedFormat == ExportFormat.CSV)
            {
                extension = "csv";
                mimeType = "text/csv";
            }
            else
            {
                extension = "json";
                mimeType = "application/json";
            }

            ExportProgress = 50;
            StatusMessage = "Streaming data to file...";

            // Save file using Avalonia's storage provider
            if (StorageProvider != null)
            {
                var fileName = $"mental-health-export-{DateTime.Now:yyyy-MM-dd-HHmmss}.{extension}";

                var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Export File",
                    SuggestedFileName = fileName,
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType(extension.ToUpper())
                        {
                            Patterns = new[] { $"*.{extension}" },
                            MimeTypes = new[] { mimeType }
                        }
                    }
                });

                if (file != null)
                {
                    // Stream directly to file without loading into memory
                    await using var outputStream = await file.OpenWriteAsync();
                    await using var dataStream = SelectedFormat == ExportFormat.CSV
                        ? await _apiClient.ExportToCsvAsync(request, CancellationToken)
                        : await _apiClient.ExportToJsonAsync(request, CancellationToken);

                    await dataStream.CopyToAsync(outputStream, CancellationToken);

                    ExportProgress = 100;
                    StatusMessage = $"Export completed successfully! File saved as {file.Name}";
                }
                else
                {
                    StatusMessage = "Export cancelled by user";
                }
            }
            else
            {
                // Fallback if storage provider is not available
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");

                var fileName = $"mental-health-export-{DateTime.Now:yyyy-MM-dd-HHmmss}.{extension}";
                var filePath = Path.Combine(downloadsPath, fileName);

                // Stream directly to file without loading into memory
                await using var outputStream = File.Create(filePath);
                await using var dataStream = SelectedFormat == ExportFormat.CSV
                    ? await _apiClient.ExportToCsvAsync(request, CancellationToken)
                    : await _apiClient.ExportToJsonAsync(request, CancellationToken);

                await dataStream.CopyToAsync(outputStream, CancellationToken);

                ExportProgress = 100;
                StatusMessage = $"Export completed! File saved to: {filePath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            Console.WriteLine($"Export error: {ex}");
        }
        finally
        {
            IsExporting = false;

            // Clear status message after 5 seconds
            await Task.Delay(5000, CancellationToken);
            if (StatusMessage?.StartsWith("Export completed") == true ||
                StatusMessage?.StartsWith("Export cancelled") == true)
            {
                StatusMessage = null;
                ExportProgress = 0;
            }
        }
    }

    private void SelectAll()
    {
        IncludeAssessments = true;
        IncludeMoodEntries = true;
        IncludeHealthMetrics = true;
        IncludeEventLabels = true;
    }

    private void DeselectAll()
    {
        IncludeAssessments = false;
        IncludeMoodEntries = false;
        IncludeHealthMetrics = false;
        IncludeEventLabels = false;
    }
}

public enum ExportFormat
{
    CSV,
    JSON
}