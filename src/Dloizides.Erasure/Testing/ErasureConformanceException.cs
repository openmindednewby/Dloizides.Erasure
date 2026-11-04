namespace Dloizides.Erasure.Testing;

/// <summary>Thrown when an endpoint breaks the erasure contract; lists every break found.</summary>
public sealed class ErasureConformanceException(IReadOnlyList<string> failures)
    : Exception("Erasure endpoint does not conform: " + string.Join("; ", failures))
{
    public IReadOnlyList<string> Failures { get; } = failures;
}
