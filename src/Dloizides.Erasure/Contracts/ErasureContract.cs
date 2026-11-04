using System.Text.Json;

namespace Dloizides.Erasure.Contracts;

/// <summary>The wire facts both sides of an erasure call share.</summary>
public static class ErasureContract
{
    public const string Route = "/internal/erasures";
    public const string SecretHeader = "X-Erasure-Secret";

    /// <summary>Fixed JSON settings, so a host's own serializer settings cannot change the wire shape.</summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
