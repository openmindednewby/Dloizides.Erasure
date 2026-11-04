using Dloizides.Erasure.Configuration;
using Dloizides.Erasure.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dloizides.Erasure.Endpoints;

/// <summary>Maps the erasure endpoint.</summary>
public static class ErasureEndpointExtensions
{
    /// <summary>Maps POST /internal/erasures; throws at startup when the service name, build sha or secret is blank.</summary>
    public static RouteHandlerBuilder MapErasureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ErasureParticipantOptions>>().Value;
        RequireSetting(options.ServiceName, nameof(options.ServiceName));
        RequireSetting(options.BuildSha, nameof(options.BuildSha));
        RequireSetting(options.Secret, nameof(options.Secret));

        Func<HttpContext, Task<IResult>> handler = context => ErasureEndpointHandler.HandleAsync(context, options);
        return endpoints
            .MapPost(ErasureContract.Route, handler)
            .AllowAnonymous()
            .ExcludeFromDescription();
    }

    private static void RequireSetting(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{ErasureParticipantOptions.SectionName}:{name} must be set.");
        }
    }
}
