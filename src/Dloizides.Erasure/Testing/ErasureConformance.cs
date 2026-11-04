namespace Dloizides.Erasure.Testing;

/// <summary>The contract check every consuming service runs in its own test suite.</summary>
public static class ErasureConformance
{
    /// <summary>Runs the whole contract against the scenario; throws ErasureConformanceException on any break.</summary>
    public static async Task AssertConformsAsync(ErasureConformanceScenario scenario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var run = new ErasureConformanceRun(scenario, cancellationToken);
        await run.ExecuteAsync().ConfigureAwait(false);
        if (run.Failures.Count > 0) throw new ErasureConformanceException(run.Failures);
    }
}
