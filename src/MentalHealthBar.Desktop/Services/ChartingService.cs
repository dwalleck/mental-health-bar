using System;
using System.Collections.Generic;
using System.Linq;
using ScottPlot;
using ScottPlot.Avalonia;
using NodaTime;

namespace MentalHealthBar.Desktop.Services;

public interface IChartingService
{
    AvaPlot CreateMoodChart(List<MoodEntrySummaryResponse> entries, bool showDailyAverage = false);
    AvaPlot CreateAssessmentChart(List<AssessmentSummaryResponse> assessments, string assessmentType);
    AvaPlot CreateHealthMetricChart(List<HealthMetricSummaryResponse> metrics, string metricType);
    AvaPlot CreateCombinedHealthChart(List<HealthMetricSummaryResponse> sleepMetrics, List<HealthMetricSummaryResponse> waterMetrics);
}

public class ChartingService : IChartingService
{
    private readonly Color[] _colorPalette =
    {
        Colors.Blue,
        Colors.Red,
        Colors.Green,
        Colors.Orange,
        Colors.Purple
    };

    public AvaPlot CreateMoodChart(List<MoodEntrySummaryResponse> entries, bool showDailyAverage = false)
    {
        var plot = new AvaPlot();

        if (!entries.Any())
        {
            plot.Plot.Title("No mood data available");
            return plot;
        }

        if (showDailyAverage)
        {
            // Group by date and calculate daily average
            var dailyAverages = entries
                .GroupBy(e => DateOnly.FromDateTime(e.RecordedAt.ToDateTimeUtc()))
                .Select(g => new
                {
                    Date = g.Key,
                    Average = g.Average(e => e.MoodScore)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var dates = dailyAverages.Select(d => d.Date.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
            var scores = dailyAverages.Select(d => d.Average).ToArray();

            var scatter = plot.Plot.Add.Scatter(dates, scores);
            scatter.Label = "Daily Average Mood";
            scatter.LineWidth = 2;
            scatter.MarkerSize = 8;
        }
        else
        {
            // Show all individual entries as connected points
            var orderedEntries = entries.OrderBy(e => e.RecordedAt).ToList();
            var dates = orderedEntries.Select(e => e.RecordedAt.ToDateTimeUtc().ToOADate()).ToArray();
            var scores = orderedEntries.Select(e => (double)e.MoodScore).ToArray();

            var scatter = plot.Plot.Add.Scatter(dates, scores);
            scatter.Label = "Mood Score";
            scatter.LineWidth = 2;
            scatter.MarkerSize = 8;

            // Note: ScottPlot 5 provides built-in tooltip support via MouseMove events
            // Instead of creating markers for each data point (which is inefficient for large datasets),
            // consider implementing interactive tooltips using ScottPlot's event system:
            // plot.MouseMove += (s, e) => { /* Find nearest point and show tooltip */ }
            // For now, markers are colored by mood but tooltips should be implemented at the View level
        }

        // Configure axes
        plot.Plot.Title("Mood Tracker");
        plot.Plot.XLabel("Date");
        plot.Plot.YLabel("Mood Score (1-5)");
        plot.Plot.Axes.DateTimeTicksBottom();
        plot.Plot.Axes.SetLimitsY(0.5, 5.5);

        // Add horizontal reference lines
        for (int i = 1; i <= 5; i++)
        {
            var line = plot.Plot.Add.HorizontalLine(i);
            line.Color = Colors.Gray.WithAlpha(0.3);
            line.LineWidth = 1;
        }

        plot.Plot.Legend.IsVisible = true;
        plot.Refresh();

        return plot;
    }

    public AvaPlot CreateAssessmentChart(List<AssessmentSummaryResponse> assessments, string assessmentType)
    {
        var plot = new AvaPlot();

        var filteredAssessments = string.IsNullOrEmpty(assessmentType)
            ? assessments
            : assessments.Where(a => a.Type.Equals(assessmentType, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!filteredAssessments.Any())
        {
            plot.Plot.Title($"No {assessmentType ?? "assessment"} data available");
            return plot;
        }

        var orderedAssessments = filteredAssessments.OrderBy(a => a.CompletedAt).ToList();
        var dates = orderedAssessments.Select(a => a.CompletedAt.ToDateTimeUtc().ToOADate()).ToArray();
        var scores = orderedAssessments.Select(a => (double)a.TotalScore).ToArray();

        var scatter = plot.Plot.Add.Scatter(dates, scores);
        scatter.Label = $"{assessmentType ?? "Assessment"} Score";
        scatter.LineWidth = 2;
        scatter.MarkerSize = 10;

        // Color markers based on severity
        for (int i = 0; i < orderedAssessments.Count; i++)
        {
            var assessment = orderedAssessments[i];
            var marker = plot.Plot.Add.Marker(dates[i], scores[i]);
            marker.Size = 12;
            marker.Color = GetSeverityColor(assessment.Severity);
        }

        // Configure plot
        plot.Plot.Title($"{assessmentType ?? "Assessment"} History");
        plot.Plot.XLabel("Date");
        plot.Plot.YLabel("Total Score");
        plot.Plot.Axes.DateTimeTicksBottom();

        // Add severity zones (example for PHQ-9)
        if (assessmentType == "PHQ9")
        {
            AddSeverityZones(plot, new[]
            {
                (0, 4, "Minimal", Colors.Green.WithAlpha(0.1)),
                (5, 9, "Mild", Colors.Yellow.WithAlpha(0.1)),
                (10, 14, "Moderate", Colors.Orange.WithAlpha(0.1)),
                (15, 19, "Moderately Severe", Colors.OrangeRed.WithAlpha(0.1)),
                (20, 27, "Severe", Colors.Red.WithAlpha(0.1))
            });
        }

        plot.Plot.Legend.IsVisible = true;
        plot.Refresh();

        return plot;
    }

    public AvaPlot CreateHealthMetricChart(List<HealthMetricSummaryResponse> metrics, string metricType)
    {
        var plot = new AvaPlot();

        var filteredMetrics = string.IsNullOrEmpty(metricType)
            ? metrics
            : metrics.Where(m => m.Type.Equals(metricType, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!filteredMetrics.Any())
        {
            plot.Plot.Title($"No {metricType ?? "health metric"} data available");
            return plot;
        }

        var orderedMetrics = filteredMetrics.OrderBy(m => m.RecordedDate).ToList();
        var dates = orderedMetrics.Select(m => m.RecordedDate.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
        var values = orderedMetrics.Select(m => (double)m.Value).ToArray();

        var scatter = plot.Plot.Add.Scatter(dates, values);
        scatter.Label = metricType == "SleepHours" ? "Sleep (hours)" : "Water Intake (oz)";
        scatter.LineWidth = 2;
        scatter.MarkerSize = 8;
        scatter.Color = metricType == "SleepHours" ? Colors.Purple : Colors.Blue;

        // Configure plot
        plot.Plot.Title($"{(metricType == "SleepHours" ? "Sleep Tracking" : "Water Intake")}");
        plot.Plot.XLabel("Date");
        plot.Plot.YLabel(metricType == "SleepHours" ? "Hours" : "Fluid Ounces");
        plot.Plot.Axes.DateTimeTicksBottom();

        // Add recommended range lines
        if (metricType == "SleepHours")
        {
            var minRecommended = plot.Plot.Add.HorizontalLine(7);
            minRecommended.Color = Colors.Green.WithAlpha(0.5);
            minRecommended.LineWidth = 1;

            var maxRecommended = plot.Plot.Add.HorizontalLine(9);
            maxRecommended.Color = Colors.Green.WithAlpha(0.5);
            maxRecommended.LineWidth = 1;
        }
        else if (metricType == "WaterIntakeOz")
        {
            var recommended = plot.Plot.Add.HorizontalLine(64);
            recommended.Color = Colors.Blue.WithAlpha(0.5);
            recommended.LineWidth = 1;
        }

        plot.Plot.Legend.IsVisible = true;
        plot.Refresh();

        return plot;
    }

    public AvaPlot CreateCombinedHealthChart(List<HealthMetricSummaryResponse> sleepMetrics, List<HealthMetricSummaryResponse> waterMetrics)
    {
        var plot = new AvaPlot();

        if (!sleepMetrics.Any() && !waterMetrics.Any())
        {
            plot.Plot.Title("No health data available");
            return plot;
        }

        // Create dual Y-axis plot
        if (sleepMetrics.Any())
        {
            var sleepOrdered = sleepMetrics.OrderBy(m => m.RecordedDate).ToList();
            var sleepDates = sleepOrdered.Select(m => m.RecordedDate.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
            var sleepValues = sleepOrdered.Select(m => (double)m.Value).ToArray();

            var sleepScatter = plot.Plot.Add.Scatter(sleepDates, sleepValues);
            sleepScatter.Label = "Sleep (hours)";
            sleepScatter.LineWidth = 2;
            sleepScatter.MarkerSize = 8;
            sleepScatter.Color = Colors.Purple;
            sleepScatter.Axes.YAxis = plot.Plot.Axes.Left;
        }

        if (waterMetrics.Any())
        {
            var waterOrdered = waterMetrics.OrderBy(m => m.RecordedDate).ToList();
            var waterDates = waterOrdered.Select(m => m.RecordedDate.ToDateTime(TimeOnly.MinValue).ToOADate()).ToArray();
            var waterValues = waterOrdered.Select(m => (double)m.Value).ToArray();

            // Scale water values for right axis
            var waterScatter = plot.Plot.Add.Scatter(waterDates, waterValues);
            waterScatter.Label = "Water (oz)";
            waterScatter.LineWidth = 2;
            waterScatter.MarkerSize = 8;
            waterScatter.Color = Colors.Blue;
            waterScatter.Axes.YAxis = plot.Plot.Axes.Right;
        }

        // Configure plot
        plot.Plot.Title("Health Metrics Overview");
        plot.Plot.XLabel("Date");
        plot.Plot.Axes.Left.Label.Text = "Sleep (hours)";
        plot.Plot.Axes.Right.Label.Text = "Water Intake (oz)";
        plot.Plot.Axes.DateTimeTicksBottom();

        plot.Plot.Legend.IsVisible = true;
        plot.Plot.Legend.Location = Alignment.UpperLeft;
        plot.Refresh();

        return plot;
    }

    private Color GetMoodColor(int moodScore)
    {
        return moodScore switch
        {
            1 => Colors.Red,
            2 => Colors.OrangeRed,
            3 => Colors.Yellow,
            4 => Colors.LightGreen,
            5 => Colors.Green,
            _ => Colors.Gray
        };
    }

    private Color GetSeverityColor(string severity)
    {
        return severity?.ToLower() switch
        {
            "minimal" => Colors.Green,
            "mild" => Colors.Yellow,
            "moderate" => Colors.Orange,
            "moderatelysevere" => Colors.OrangeRed,
            "severe" => Colors.Red,
            _ => Colors.Gray
        };
    }

    private void AddSeverityZones(AvaPlot plot, (int min, int max, string label, Color color)[] zones)
    {
        foreach (var zone in zones)
        {
            var rect = plot.Plot.Add.Rectangle(
                plot.Plot.Axes.GetLimits().Left,
                plot.Plot.Axes.GetLimits().Right,
                zone.min,
                zone.max);
            rect.FillColor = zone.color;
            rect.LineWidth = 0;
        }
    }
}