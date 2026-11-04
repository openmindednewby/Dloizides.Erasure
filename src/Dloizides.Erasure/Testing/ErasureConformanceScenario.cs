namespace Dloizides.Erasure.Testing;

/// <summary>What a consuming service supplies to run the conformance check against its real endpoint and store.</summary>
public sealed class ErasureConformanceScenario
{
    public required HttpClient Client { get; init; }

    public required string Secret { get; init; }

    /// <summary>Writes rows for the subject (first id) and for a bystander (second id).</summary>
    public required Func<Guid, Guid, CancellationToken, Task> SeedAsync { get; init; }

    /// <summary>Counts a user's rows outside retained datasets, without going through the participant.</summary>
    public required Func<Guid, CancellationToken, Task<int>> CountRowsAsync { get; init; }
}
