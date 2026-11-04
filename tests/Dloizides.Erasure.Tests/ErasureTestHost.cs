using System.Net.Http.Json;
using Dloizides.Erasure.Contracts;
using Dloizides.Erasure.Endpoints;
using Dloizides.Erasure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Dloizides.Erasure.Tests;

internal static class ErasureTestHost
{
    public static async Task<WebApplication> StartAsync(
        FakeUserData data,
        string secret,
        string service = "jinx-api",
        string buildSha = "abc1234",
        DateTimeOffset? now = null,
        bool requireAuthenticationByDefault = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(data);
        if (now is not null) builder.Services.AddSingleton<TimeProvider>(new FixedClock(now.Value));
        if (requireAuthenticationByDefault) AddAuthenticatedFallback(builder);
        builder.AddErasureParticipant<FakeParticipant>(options =>
        {
            options.ServiceName = service;
            options.BuildSha = buildSha;
            options.Secret = secret;
        });

        var app = builder.Build();
        app.UseExceptionHandler(handler => handler.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));
        if (requireAuthenticationByDefault) app.UseAuthorization();
        app.MapErasureEndpoint();
        await app.StartAsync();
        return app;
    }

    public static Task<HttpResponseMessage> PostAsync(HttpClient client, ErasureRequest request, string? secret) =>
        PostRawAsync(client, JsonContent.Create(request, options: ErasureContract.JsonOptions), secret);

    public static Task<HttpResponseMessage> PostRawAsync(HttpClient client, HttpContent content, string? secret)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, ErasureContract.Route) { Content = content };
        if (secret is not null) message.Headers.Add(ErasureContract.SecretHeader, secret);
        return client.SendAsync(message);
    }

    public static async Task<ErasureReceipt> ReadReceiptAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<ErasureReceipt>(ErasureContract.JsonOptions))!;

    private static void AddAuthenticatedFallback(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
