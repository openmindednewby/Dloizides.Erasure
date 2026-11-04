namespace Dloizides.Erasure.Configuration;

/// <summary>Identifies this service on its receipts and holds the shared secret.</summary>
public sealed class ErasureParticipantOptions
{
    public const string SectionName = "Erasure";

    public string ServiceName { get; set; } = string.Empty;

    public string BuildSha { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}
