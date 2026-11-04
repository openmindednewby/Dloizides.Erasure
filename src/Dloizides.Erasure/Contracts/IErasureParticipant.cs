namespace Dloizides.Erasure.Contracts;

/// <summary>What a service implements to take part in account erasure. Never catch a failure here: it must surface as a 500.</summary>
public interface IErasureParticipant
{
    /// <summary>Erases the subject and reports every dataset; a repeat call erases 0 and reports again.</summary>
    Task<IReadOnlyList<ErasureDatasetResult>> EraseAsync(ErasureRequest request, CancellationToken cancellationToken);

    /// <summary>Counts the subject's rows outside retained datasets, by a fresh query.</summary>
    Task<int> CountRemainingRowsAsync(Guid userId, CancellationToken cancellationToken);
}
