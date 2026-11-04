using System.Globalization;

namespace Dloizides.Erasure.Contracts;

/// <summary>Decides whether a receipt proves its request was completed; used by both sides of the call.</summary>
public static class ErasureReceiptValidator
{
    /// <summary>Lists why the receipt does not count; empty means it does.</summary>
    public static IReadOnlyList<string> FindProblems(ErasureRequest request, ErasureReceipt? receipt)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (receipt is null) return ["no receipt"];

        var problems = new List<string>();
        AddIdentityProblems(request, receipt, problems);
        if (receipt.ReadBackRowCount != 0) problems.Add(string.Create(CultureInfo.InvariantCulture, $"read-back found {receipt.ReadBackRowCount} rows"));
        if (receipt.Datasets is null || receipt.Datasets.Count == 0)
        {
            problems.Add("no datasets reported");
            return problems;
        }

        var completedOn = DateOnly.FromDateTime(receipt.CompletedAt.UtcDateTime);
        foreach (var dataset in receipt.Datasets) AddDatasetProblems(dataset, completedOn, problems);
        return problems;
    }

    private static void AddIdentityProblems(ErasureRequest request, ErasureReceipt receipt, List<string> problems)
    {
        if (receipt.ErasureId != request.ErasureId) problems.Add("erasure id does not match the request");
        if (receipt.UserId != request.UserId) problems.Add("user id does not match the request");
        if (string.IsNullOrWhiteSpace(receipt.Service)) problems.Add("service name is blank");
        if (string.IsNullOrWhiteSpace(receipt.BuildSha)) problems.Add("build sha is blank");
    }

    private static void AddDatasetProblems(ErasureDatasetResult dataset, DateOnly completedOn, List<string> problems)
    {
        if (string.IsNullOrWhiteSpace(dataset.Dataset)) problems.Add("a dataset has no name");
        if (!Enum.IsDefined(dataset.Outcome)) problems.Add($"{dataset.Dataset}: undefined outcome");
        if (dataset.RowCount < 0) problems.Add($"{dataset.Dataset}: negative row count");
        if (dataset.Outcome != ErasureOutcome.Retained) return;

        if (string.IsNullOrWhiteSpace(dataset.RetentionBasis)) problems.Add($"{dataset.Dataset}: retained with no basis");
        if (dataset.RetainedUntil is null) problems.Add($"{dataset.Dataset}: retained with no end date");
        if (dataset.RetainedUntil < completedOn) problems.Add($"{dataset.Dataset}: retention end date has passed");
    }
}
