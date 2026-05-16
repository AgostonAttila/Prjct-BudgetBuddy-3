using System.Diagnostics.Metrics;

namespace BudgetBuddy.Shared.Infrastructure.Behaviors;

/// <summary>
/// Application-level MediatR handler metrics (finding #12).
/// Exposed to Prometheus via the "BudgetBuddy.Handlers" meter registered in ObservabilityExtensions.
/// </summary>
public static class HandlerMetrics
{
    private static readonly Meter Meter = new("BudgetBuddy.Handlers");

    /// <summary>
    /// Wall-clock time from when the handler was invoked until it returned (including DB, cache, HTTP).
    /// Dimensions: handler_name, success (true/false).
    /// </summary>
    public static readonly Histogram<double> HandlerDuration =
        Meter.CreateHistogram<double>(
            "handler_duration_seconds",
            unit: "s",
            description: "Time spent executing a MediatR request handler");

    /// <summary>
    /// Total count of handler invocations.
    /// Dimensions: handler_name, success (true/false).
    /// </summary>
    public static readonly Counter<long> HandlerInvocations =
        Meter.CreateCounter<long>(
            "handler_invocations_total",
            unit: "{invocations}",
            description: "Total number of MediatR handler invocations");
}
