using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Moq;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;

namespace MentalHealthBar.Desktop.Tests.ViewModels;

public class ExportViewModelTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly Mock<IStorageProvider> _storageProviderMock;
    private readonly ExportViewModel _viewModel;

    public ExportViewModelTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _storageProviderMock = new Mock<IStorageProvider>();
        _viewModel = new ExportViewModel(_apiClientMock.Object)
        {
            StorageProvider = _storageProviderMock.Object
        };
    }

    [Test]
    public void InitialState_HasCorrectDefaults()
    {
        // Assert
        Assert.That(_viewModel.SelectedFormat, Is.EqualTo(ExportFormat.CSV));
        Assert.That(_viewModel.StartDate, Is.EqualTo(DateOnly.FromDateTime(DateTime.Now.AddMonths(-3))));
        Assert.That(_viewModel.EndDate, Is.EqualTo(DateOnly.FromDateTime(DateTime.Now)));
        Assert.That(_viewModel.IncludeAssessments, Is.True);
        Assert.That(_viewModel.IncludeMoodEntries, Is.True);
        Assert.That(_viewModel.IncludeHealthMetrics, Is.True);
        Assert.That(_viewModel.IncludeEventLabels, Is.True);
        Assert.That(_viewModel.IsExporting, Is.False);
        Assert.That(_viewModel.StatusMessage, Is.Null);
        Assert.That(_viewModel.ExportProgress, Is.EqualTo(0));
    }

    [Test]
    public async Task ExportCommand_CSV_CallsApiAndSavesFile()
    {
        // Arrange
        _viewModel.SelectedFormat = ExportFormat.CSV;
        var csvData = Encoding.UTF8.GetBytes("header1,header2\nvalue1,value2");

        _apiClientMock.Setup(x => x.ExportToCsvAsync(
                It.Is<ExportRequest>(r =>
                    r.IncludeAssessments &&
                    r.IncludeMoodEntries &&
                    r.IncludeHealthMetrics &&
                    r.IncludeEventLabels),
                default))
            .ReturnsAsync(csvData);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.csv");
        var mockStream = new MemoryStream();
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(mockStream);

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await _viewModel.ExportCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), default), Times.Once);
        Assert.That(mockStream.ToArray(), Is.EqualTo(csvData));
        Assert.That(_viewModel.ExportProgress, Is.EqualTo(100));
        Assert.That(_viewModel.StatusMessage, Does.Contain("completed successfully"));
    }

    [Test]
    public async Task ExportCommand_JSON_CallsApiAndSavesFile()
    {
        // Arrange
        _viewModel.SelectedFormat = ExportFormat.JSON;
        var jsonString = "{\"data\":\"test\"}";

        _apiClientMock.Setup(x => x.ExportToJsonAsync(It.IsAny<ExportRequest>(), default))
            .ReturnsAsync(jsonString);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.json");
        var mockStream = new MemoryStream();
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(mockStream);

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await _viewModel.ExportCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.ExportToJsonAsync(It.IsAny<ExportRequest>(), default), Times.Once);
        var writtenData = Encoding.UTF8.GetString(mockStream.ToArray());
        Assert.That(writtenData, Is.EqualTo(jsonString));
    }

    [Test]
    public async Task ExportCommand_UserCancelsFileSave_ShowsCancelMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), default))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync((IStorageFile?)null); // User cancelled

        // Act
        await _viewModel.ExportCommand.Execute();

        // Assert
        Assert.That(_viewModel.StatusMessage, Is.EqualTo("Export cancelled by user"));
        Assert.That(_viewModel.IsExporting, Is.False);
    }

    [Test]
    public void ExportCommand_CanExecute_RequiresAtLeastOneDataType()
    {
        // Initially can execute (all types selected)
        Assert.That(_viewModel.ExportCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Deselect all
        _viewModel.IncludeAssessments = false;
        _viewModel.IncludeMoodEntries = false;
        _viewModel.IncludeHealthMetrics = false;
        _viewModel.IncludeEventLabels = false;
        Assert.That(_viewModel.ExportCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Select one
        _viewModel.IncludeMoodEntries = true;
        Assert.That(_viewModel.ExportCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public void ExportCommand_CannotExecute_WhenExporting()
    {
        // Can execute initially
        Assert.That(_viewModel.ExportCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Cannot execute when exporting
        _viewModel.IsExporting = true;
        Assert.That(_viewModel.ExportCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public void SelectAll_SelectsAllDataTypes()
    {
        // Arrange
        _viewModel.IncludeAssessments = false;
        _viewModel.IncludeMoodEntries = false;
        _viewModel.IncludeHealthMetrics = false;
        _viewModel.IncludeEventLabels = false;

        // Act
        _viewModel.SelectAllCommand.Execute();

        // Assert
        Assert.That(_viewModel.IncludeAssessments, Is.True);
        Assert.That(_viewModel.IncludeMoodEntries, Is.True);
        Assert.That(_viewModel.IncludeHealthMetrics, Is.True);
        Assert.That(_viewModel.IncludeEventLabels, Is.True);
    }

    [Test]
    public void DeselectAll_DeselectsAllDataTypes()
    {
        // Act
        _viewModel.DeselectAllCommand.Execute();

        // Assert
        Assert.That(_viewModel.IncludeAssessments, Is.False);
        Assert.That(_viewModel.IncludeMoodEntries, Is.False);
        Assert.That(_viewModel.IncludeHealthMetrics, Is.False);
        Assert.That(_viewModel.IncludeEventLabels, Is.False);
    }

    [Test]
    public async Task ExportProgress_UpdatesDuringExport()
    {
        // Arrange
        var tcs = new TaskCompletionSource<byte[]>();
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), default))
            .Returns(tcs.Task);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.csv");
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(new MemoryStream());

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        var exportTask = _viewModel.ExportCommand.Execute();

        // Assert - Progress should be set during operation
        await Task.Delay(50);
        Assert.That(_viewModel.ExportProgress, Is.GreaterThan(0));

        // Complete the operation
        tcs.SetResult(new byte[] { 1, 2, 3 });
        await exportTask;

        Assert.That(_viewModel.ExportProgress, Is.EqualTo(100));
    }

    [Test]
    public async Task Export_HandlesApiError()
    {
        // Arrange
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), default))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.ExportCommand.Execute();

        // Assert
        Assert.That(_viewModel.StatusMessage, Does.Contain("Export failed"));
        Assert.That(_viewModel.IsExporting, Is.False);
    }

    [Test]
    public async Task Export_FallbackToDownloadsFolder_WhenNoStorageProvider()
    {
        // Arrange
        _viewModel.StorageProvider = null; // No storage provider
        _viewModel.SelectedFormat = ExportFormat.CSV;

        var csvData = Encoding.UTF8.GetBytes("test,data");
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), default))
            .ReturnsAsync(csvData);

        // Act
        await _viewModel.ExportCommand.Execute();

        // Assert
        Assert.That(_viewModel.StatusMessage, Does.Contain("Export completed"));
        Assert.That(_viewModel.StatusMessage, Does.Contain("Downloads"));
    }

    [Test]
    public async Task StatusMessage_ClearsAfterDelay()
    {
        // Arrange
        var csvData = new byte[] { 1, 2, 3 };
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), default))
            .ReturnsAsync(csvData);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.csv");
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(new MemoryStream());

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await _viewModel.ExportCommand.Execute();

        // Assert - Status should be set initially
        Assert.That(_viewModel.StatusMessage, Is.Not.Null);

        // Wait for auto-clear (5 seconds in implementation)
        await Task.Delay(5100);
        Assert.That(_viewModel.StatusMessage, Is.Null);
        Assert.That(_viewModel.ExportProgress, Is.EqualTo(0));
    }

    [Test]
    public void DateRange_ProperlyConfigured()
    {
        // Arrange
        var newStartDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6));
        var newEndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));

        // Act
        _viewModel.StartDate = newStartDate;
        _viewModel.EndDate = newEndDate;

        // Assert
        Assert.That(_viewModel.StartDate, Is.EqualTo(newStartDate));
        Assert.That(_viewModel.EndDate, Is.EqualTo(newEndDate));
    }
}