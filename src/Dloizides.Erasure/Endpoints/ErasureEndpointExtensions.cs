using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Dloizides.Erasure.Endpoints;

/// <summary>Maps the erasure endpoint.</summary>
public static class ErasureEndpointExtensions
{
    /// <summary>Maps POST /internal/erasures; throws at startup when the service name, build sha or secret is blank.</summary>
    public static RouteHandlerBuilder MapErasureEndpoint(this IEndpointRouteBuilder endpoints) =>
        throw new NotImplementedException();
}
