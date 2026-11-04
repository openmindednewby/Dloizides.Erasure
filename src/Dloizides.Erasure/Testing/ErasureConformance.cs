namespace Dloizides.Erasure.Testing;

/// <summary>The contract check every consuming service runs in its own test suite.</summary>
public static class ErasureConformance
{
    /// <summary>Runs the whole contract against the scenario; throws ErasureConformanceException on any break.</summary>
    public static Task AssertConformsAsync(ErasureConformanceScenario scenario, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
