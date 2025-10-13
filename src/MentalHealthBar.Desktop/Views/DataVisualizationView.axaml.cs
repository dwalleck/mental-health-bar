using Avalonia.Controls;
using ScottPlot.Avalonia;

namespace MentalHealthBar.Desktop.Views;

public partial class DataVisualizationView : UserControl
{
    public DataVisualizationView()
    {
        InitializeComponent();

        // Configure chart appearance after initialization
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Configure mood chart
        if (this.FindControl<AvaPlot>("MoodChart") is { } moodChart)
        {
            moodChart.Plot.Title("Mood Trends Over Time");
            moodChart.Plot.XLabel("Date");
            moodChart.Plot.YLabel("Mood Score (1-5)");
            moodChart.Plot.Axes.SetLimitsY(0, 6);
        }

        // Configure assessment chart
        if (this.FindControl<AvaPlot>("AssessmentChart") is { } assessmentChart)
        {
            assessmentChart.Plot.Title("Assessment Scores");
            assessmentChart.Plot.XLabel("Date");
            assessmentChart.Plot.YLabel("Score");
        }

        // Configure sleep chart
        if (this.FindControl<AvaPlot>("SleepChart") is { } sleepChart)
        {
            sleepChart.Plot.Title("Sleep Hours");
            sleepChart.Plot.XLabel("Date");
            sleepChart.Plot.YLabel("Hours");
            sleepChart.Plot.Axes.SetLimitsY(0, 12);
        }

        // Configure water chart
        if (this.FindControl<AvaPlot>("WaterChart") is { } waterChart)
        {
            waterChart.Plot.Title("Water Intake");
            waterChart.Plot.XLabel("Date");
            waterChart.Plot.YLabel("Ounces");
            waterChart.Plot.Axes.SetLimitsY(0, 150);
        }

        // Configure correlation chart
        if (this.FindControl<AvaPlot>("CorrelationChart") is { } correlationChart)
        {
            correlationChart.Plot.Title("Health Metrics vs Mood Score");
            correlationChart.Plot.XLabel("Health Metric Value");
            correlationChart.Plot.YLabel("Mood Score");
        }
    }
}