using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dloizides.Erasure.Configuration;
using Dloizides.Erasure.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dloizides.Erasure.Endpoints;

internal static class ErasureEndpointHandler
{
    private const string LoggerCategory = "Dloizides.Erasure";

    public static async Task<IResult> HandleAsync(HttpContext context, ErasureParticipantOptions options)
    {
        if (!SecretMatches(context.Request.Headers[ErasureContract.SecretHeader].ToString(), options.Secret))
        {
            return Results.Unauthorized();
        }

        var request = await TryReadRequestAsync(context).ConfigureAwait(false);
        if (request is null) return Results.BadRequest();

        var services = context.RequestServices;
        var participant = services.GetRequiredService<IErasureParticipant>();
        var cancellationToken = context.RequestAborted;
        var datasets = await participant.EraseAsync(request, cancellationToken).ConfigureAwait(false);
        var remaining = await participant.CountRemainingRowsAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        var completedAt = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var receipt = new ErasureReceipt(
            request.ErasureId, request.UserId, options.ServiceName, options.BuildSha,
            request.RequestedAt, completedAt, datasets, remaining);

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);
        if (problems.Count == 0) return Results.Json(receipt, ErasureContract.JsonOptions);

        services.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory).LogError(
            "erasure_receipt_rejected erasureId={ErasureId} problems={Problems}",
            request.ErasureId,
            string.Join("; ", problems));
        return Results.Problem(
            title: "Erasure did not complete",
            detail: string.Join("; ", problems),
            statusCode: StatusCodes.Status500InternalServerError);
    }

    private static async Task<ErasureRequest?> TryReadRequestAsync(HttpContext context)
    {
        try
        {
            var request = await JsonSerializer
                .DeserializeAsync<ErasureRequest>(context.Request.Body, ErasureContract.JsonOptions, context.RequestAborted)
                .ConfigureAwait(false);
            return IsComplete(request) ? request : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsComplete(ErasureRequest? request) =>
        request is not null
        && request.ErasureId != Guid.Empty
        && request.UserId != Guid.Empty
        && request.RequestedAt != default;

    private static bool SecretMatches(string presented, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(presented)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
