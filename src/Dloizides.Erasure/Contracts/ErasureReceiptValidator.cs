namespace Dloizides.Erasure.Contracts;

/// <summary>Decides whether a receipt proves its request was completed; used by both sides of the call.</summary>
public static class ErasureReceiptValidator
{
    /// <summary>Lists why the receipt does not count; empty means it does.</summary>
    public static IReadOnlyList<string> FindProblems(ErasureRequest request, ErasureReceipt? receipt) =>
        throw new NotImplementedException();
}
