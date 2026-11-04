using Dloizides.Erasure.Configuration;
using Dloizides.Erasure.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Dloizides.Erasure.Extensions;

/// <summary>Registers a service as an erasure participant.</summary>
public static class ErasureHostingExtensions
{
    /// <summary>Binds the Erasure section, applies configure, and registers the participant as scoped.</summary>
    public static IHostApplicationBuilder AddErasureParticipant<TParticipant>(
        this IHostApplicationBuilder builder,
        Action<ErasureParticipantOptions>? configure = null)
        where TParticipant : class, IErasureParticipant
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services
            .AddOptions<ErasureParticipantOptions>()
            .Bind(builder.Configuration.GetSection(ErasureParticipantOptions.SectionName))
            .Configure(options => configure?.Invoke(options));
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IErasureParticipant, TParticipant>();
        return builder;
    }
}
