namespace Dloizides.Erasure.Contracts;

/// <summary>One dataset's outcome for one erasure request.</summary>
/// <param name="RowCount">The subject's rows found; null means this dataset cannot identify the subject, not zero.</param>
public sealed record ErasureDatasetResult(
    string Dataset,
    ErasureOutcome Outcome,
    int? RowCount,
    string? RetentionBasis = null,
    DateOnly? RetainedUntil = null)
{
    public static ErasureDatasetResult Erased(string dataset, int? rowCount) =>
        new(dataset, ErasureOutcome.Erased, rowCount);

    public static ErasureDatasetResult Anonymised(string dataset, int? rowCount) =>
        new(dataset, ErasureOutcome.Anonymised, rowCount);

    public static ErasureDatasetResult Retained(string dataset, int? rowCount, string basis, DateOnly until) =>
        new(dataset, ErasureOutcome.Retained, rowCount, basis, until);
}
