using Dloizides.Erasure.Contracts;
using Dloizides.Erasure.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace Dloizides.Erasure.Tests;

public sealed class ErasureConformanceTests
{
    private const string Secret = "shared-secret-value";

    [Fact]
    public async Task AssertConformsAsync_WithConformingEndpoint_Passes()
    {
        var data = new FakeUserData();
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var check = () => ErasureConformance.AssertConformsAsync(ScenarioFor(app, data));

        await check.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task AssertConformsAsync_WhenNoSecretIsChecked_Fails()
    {
        var data = new FakeUserData();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.MapPost(ErasureContract.Route, () => Results.Ok());
        await app.StartAsync();

        var check = () => ErasureConformance.AssertConformsAsync(ScenarioFor(app, data));

        var thrown = await check.ShouldThrowAsync<ErasureConformanceException>();
        thrown.Failures.ShouldContain(failure => failure.Contains("401"));
    }

    [Fact]
    public async Task AssertConformsAsync_WhenReadBackLiesAboutRemainingRows_Fails()
    {
        var data = new FakeUserData { LeaveRowsBehind = true, ReportZeroRemaining = true };
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var check = () => ErasureConformance.AssertConformsAsync(ScenarioFor(app, data));

        var thrown = await check.ShouldThrowAsync<ErasureConformanceException>();
        thrown.Failures.ShouldContain(failure => failure.Contains("still holds"));
    }

    [Fact]
    public async Task AssertConformsAsync_WhenBystanderIsAlsoErased_Fails()
    {
        var data = new FakeUserData { EraseEveryone = true };
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var check = () => ErasureConformance.AssertConformsAsync(ScenarioFor(app, data));

        var thrown = await check.ShouldThrowAsync<ErasureConformanceException>();
        thrown.Failures.ShouldContain(failure => failure.Contains("bystander"));
    }

    [Fact]
    public async Task AssertConformsAsync_WhenSecondCallFails_Fails()
    {
        var data = new FakeUserData { SecondEraseFailure = new InvalidOperationException("already erased") };
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var check = () => ErasureConformance.AssertConformsAsync(ScenarioFor(app, data));

        var thrown = await check.ShouldThrowAsync<ErasureConformanceException>();
        thrown.Failures.ShouldContain(failure => failure.Contains("second"));
    }

    [Fact]
    public async Task AssertConformsAsync_WhenSeededRowsAreNotReported_Fails()
    {
        var data = new FakeUserData();
        await using var app = await ErasureTestHost.StartAsync(data, Secret);
        var underReporting = new ErasureConformanceScenario
        {
            Client = app.GetTestClient(),
            Secret = Secret,
            SeedAsync = (_, _, _) => Task.CompletedTask,
            CountRowsAsync = (_, _) => Task.FromResult(data.EraseCalls == 0 ? 3 : 0),
        };

        var check = () => ErasureConformance.AssertConformsAsync(underReporting);

        var thrown = await check.ShouldThrowAsync<ErasureConformanceException>();
        thrown.Failures.ShouldContain(failure => failure.Contains("reported"));
    }

    private static ErasureConformanceScenario ScenarioFor(WebApplication app, FakeUserData data) =>
        new()
        {
            Client = app.GetTestClient(),
            Secret = Secret,
            SeedAsync = (subject, bystander, _) =>
            {
                data.Rows[subject] = 3;
                data.Rows[bystander] = 2;
                return Task.CompletedTask;
            },
            CountRowsAsync = (userId, _) => Task.FromResult(data.Rows.GetValueOrDefault(userId)),
        };
}
