using System.Text.Json.Serialization;

namespace Dloizides.Erasure.Contracts;

/// <summary>What happened to one dataset. Numbers are pinned; 0 is deliberately undefined.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ErasureOutcome>))]
public enum ErasureOutcome
{
    Erased = 1,
    Anonymised = 2,
    Retained = 3,
}
