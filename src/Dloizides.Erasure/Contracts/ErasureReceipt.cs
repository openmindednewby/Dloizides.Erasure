namespace Dloizides.Erasure.Contracts;

/// <summary>A service's proof of what one erasure request did. Holds no name, email or address.</summary>
/// <param name="ReadBackRowCount">Rows still keyed to the subject outside retained datasets, counted after erasing.</param>
public sealed record ErasureReceipt(
    Guid ErasureId,
    Guid UserId,
    string Service,
    string BuildSha,
    DateTimeOffset RequestedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyList<ErasureDatasetResult> Datasets,
    int ReadBackRowCount)
{
    public int ErasedRowCount => TotalFor(ErasureOutcome.Erased);

    public int AnonymisedRowCount => TotalFor(ErasureOutcome.Anonymised);

    public int RetainedRowCount => TotalFor(ErasureOutcome.Retained);

    private int TotalFor(ErasureOutcome outcome) =>
        Datasets.Where(dataset => dataset.Outcome == outcome).Sum(dataset => dataset.RowCount ?? 0);
}
