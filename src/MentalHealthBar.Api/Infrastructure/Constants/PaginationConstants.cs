namespace MentalHealthBar.Api.Infrastructure.Constants;

/// <summary>
/// Constants for pagination limits across all paginated endpoints.
/// </summary>
public static class PaginationConstants
{
    /// <summary>
    /// Default page size for mood entries if not specified.
    /// </summary>
    public const int MoodEntriesDefaultPageSize = 50;

    /// <summary>
    /// Minimum allowed page size for mood entries.
    /// </summary>
    public const int MoodEntriesMinPageSize = 1;

    /// <summary>
    /// Maximum allowed page size for mood entries.
    /// </summary>
    public const int MoodEntriesMaxPageSize = 500;

    /// <summary>
    /// Default page size for assessments if not specified.
    /// </summary>
    public const int AssessmentsDefaultPageSize = 50;

    /// <summary>
    /// Minimum allowed page size for assessments.
    /// </summary>
    public const int AssessmentsMinPageSize = 1;

    /// <summary>
    /// Maximum allowed page size for assessments.
    /// </summary>
    public const int AssessmentsMaxPageSize = 100;

    /// <summary>
    /// Default page size for health metrics if not specified.
    /// </summary>
    public const int HealthMetricsDefaultPageSize = 100;

    /// <summary>
    /// Minimum allowed page size for health metrics.
    /// </summary>
    public const int HealthMetricsMinPageSize = 1;

    /// <summary>
    /// Maximum allowed page size for health metrics.
    /// </summary>
    public const int HealthMetricsMaxPageSize = 365;
}
