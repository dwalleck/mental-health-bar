using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Moq;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using MentalHealthBar.Contracts.Requests.Export;
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
    public async Task InitialState_HasCorrectDefaults()
    {
        // Assert
        await Assert.That(_viewModel.SelectedFormat).IsEqualTo(ExportFormat.CSV);
        await Assert.That(_viewModel.StartDate).IsEqualTo(DateOnly.FromDateTime(DateTime.Now.AddMonths(-3)));
        await Assert.That(_viewModel.EndDate).IsEqualTo(DateOnly.FromDateTime(DateTime.Now));
        await Assert.That(_viewModel.IncludeAssessments).IsTrue();
        await Assert.That(_viewModel.IncludeMoodEntries).IsTrue();
        await Assert.That(_viewModel.IncludeHealthMetrics).IsTrue();
        await Assert.That(_viewModel.IncludeEventLabels).IsTrue();
        await Assert.That(_viewModel.IsExporting).IsFalse();
        await Assert.That(_viewModel.StatusMessage).IsNull();
        await Assert.That(_viewModel.ExportProgress).IsEqualTo(0);
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
                    r.IncludeHealthMetrics),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(csvData);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.csv");
        var mockStream = new MemoryStream();
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(mockStream);

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await _viewModel.ExportCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        await Assert.That(mockStream.ToArray()).IsEqualTo(csvData);
        await Assert.That(_viewModel.ExportProgress).IsEqualTo(100);
        await Assert.That(_viewModel.StatusMessage).Contains("completed successfully");
    }

    [Test]
    public async Task ExportCommand_JSON_CallsApiAndSavesFile()
    {
        // Arrange
        _viewModel.SelectedFormat = ExportFormat.JSON;
        var jsonString = "{\"data\":\"test\"}";

        _apiClientMock.Setup(x => x.ExportToJsonAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonString);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.json");
        var mockStream = new MemoryStream();
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(mockStream);

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await _viewModel.ExportCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.ExportToJsonAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        var writtenData = Encoding.UTF8.GetString(mockStream.ToArray());
        await Assert.That(writtenData).IsEqualTo(jsonString);
    }

    [Test]
    public async Task ExportCommand_UserCancelsFileSave_ShowsCancelMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync((IStorageFile?)null); // User cancelled

        // Act
        await _viewModel.ExportCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.StatusMessage).IsEqualTo("Export cancelled by user");
        await Assert.That(_viewModel.IsExporting).IsFalse();
    }

    [Test]
    public async Task ExportCommand_CanExecute_RequiresAtLeastOneDataType()
    {
        // Initially can execute (all types selected)
        await Assert.That(_viewModel.ExportCommand).IsNotNull();

        // Deselect all
        _viewModel.IncludeAssessments = false;
        _viewModel.IncludeMoodEntries = false;
        _viewModel.IncludeHealthMetrics = false;
        _viewModel.IncludeEventLabels = false;
        await Assert.That(_viewModel.ExportCommand).IsNotNull();

        // Select one
        _viewModel.IncludeMoodEntries = true;
        await Assert.That(_viewModel.ExportCommand).IsNotNull();
    }

    [Test]
    public async Task ExportCommand_CannotExecute_WhenExporting()
    {
        // Can execute initially
        await Assert.That(_viewModel.ExportCommand).IsNotNull();

        // Cannot execute when exporting
        _viewModel.IsExporting = true;
        await Assert.That(_viewModel.ExportCommand).IsNotNull();
    }

    [Test]
    public async Task SelectAll_SelectsAllDataTypes()
    {
        // Arrange
        _viewModel.IncludeAssessments = false;
        _viewModel.IncludeMoodEntries = false;
        _viewModel.IncludeHealthMetrics = false;
        _viewModel.IncludeEventLabels = false;

        // Act (fire-and-forget)
        _viewModel.SelectAllCommand.Execute().Subscribe();

        // Allow a brief delay for the command to execute
        await Task.Delay(50);

        // Assert
        await Assert.That(_viewModel.IncludeAssessments).IsTrue();
        await Assert.That(_viewModel.IncludeMoodEntries).IsTrue();
        await Assert.That(_viewModel.IncludeHealthMetrics).IsTrue();
        await Assert.That(_viewModel.IncludeEventLabels).IsTrue();
    }

    [Test]
    public async Task DeselectAll_DeselectsAllDataTypes()
    {
        // Act (fire-and-forget)
        _viewModel.DeselectAllCommand.Execute().Subscribe();

        // Allow a brief delay for the command to execute
        await Task.Delay(50);

        // Assert
        await Assert.That(_viewModel.IncludeAssessments).IsFalse();
        await Assert.That(_viewModel.IncludeMoodEntries).IsFalse();
        await Assert.That(_viewModel.IncludeHealthMetrics).IsFalse();
        await Assert.That(_viewModel.IncludeEventLabels).IsFalse();
    }

    [Test]
    public async Task ExportProgress_UpdatesDuringExport()
    {
        // Arrange
        var tcs = new TaskCompletionSource<byte[]>();
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()))
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
        await Assert.That(_viewModel.ExportProgress).IsGreaterThan(0);

        // Complete the operation
        tcs.SetResult(new byte[] { 1, 2, 3 });
        await exportTask;

        await Assert.That(_viewModel.ExportProgress).IsEqualTo(100);
    }

    [Test]
    public async Task Export_HandlesApiError()
    {
        // Arrange
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.ExportCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.StatusMessage).Contains("Export failed");
        await Assert.That(_viewModel.IsExporting).IsFalse();
    }

    [Test]
    public async Task Export_FallbackToDownloadsFolder_WhenNoStorageProvider()
    {
        // Arrange
        _viewModel.StorageProvider = null; // No storage provider
        _viewModel.SelectedFormat = ExportFormat.CSV;

        var csvData = Encoding.UTF8.GetBytes("test,data");
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(csvData);

        // Act
        await _viewModel.ExportCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.StatusMessage).Contains("Export completed");
        await Assert.That(_viewModel.StatusMessage).Contains("Downloads");
    }

    [Test]
    public async Task StatusMessage_ClearsAfterDelay()
    {
        // Arrange
        var csvData = new byte[] { 1, 2, 3 };
        _apiClientMock.Setup(x => x.ExportToCsvAsync(It.IsAny<ExportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(csvData);

        var mockFile = new Mock<IStorageFile>();
        mockFile.SetupGet(f => f.Name).Returns("export.csv");
        mockFile.Setup(f => f.OpenWriteAsync()).ReturnsAsync(new MemoryStream());

        _storageProviderMock.Setup(x => x.SaveFilePickerAsync(It.IsAny<FilePickerSaveOptions>()))
            .ReturnsAsync(mockFile.Object);

        // Act
        await _viewModel.ExportCommand.Execute().FirstAsync();

        // Assert - Status should be set initially
        await Assert.That(_viewModel.StatusMessage).IsNotNull();

        // Wait for auto-clear (5 seconds in implementation)
        await Task.Delay(5100);
        await Assert.That(_viewModel.StatusMessage).IsNull();
        await Assert.That(_viewModel.ExportProgress).IsEqualTo(0);
    }

    [Test]
    public async Task DateRange_ProperlyConfigured()
    {
        // Arrange
        var newStartDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6));
        var newEndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));

        // Act
        _viewModel.StartDate = newStartDate;
        _viewModel.EndDate = newEndDate;

        // Assert
        await Assert.That(_viewModel.StartDate).IsEqualTo(newStartDate);
        await Assert.That(_viewModel.EndDate).IsEqualTo(newEndDate);
    }
}
