using System.Net;
using System.Text;
using System.Text.Json;
using Dloizides.Erasure.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace Dloizides.Erasure.Tests;

public sealed class ErasureEndpointTests
{
    private const string Secret = "shared-secret-value";

    private static readonly DateTimeOffset RequestedAt = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Post_WithValidSecret_ErasesAndReturnsReceipt()
    {
        var userId = Guid.NewGuid();
        var erasureId = Guid.NewGuid();
        var completedAt = new DateTimeOffset(2026, 11, 2, 9, 30, 0, TimeSpan.Zero);
        var data = new FakeUserData();
        data.Rows[userId] = 4;
        await using var app = await ErasureTestHost.StartAsync(data, Secret, "jinx-api", "abc1234", completedAt);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(erasureId, userId, null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var receipt = await ErasureTestHost.ReadReceiptAsync(response);
        receipt.ErasureId.ShouldBe(erasureId);
        receipt.UserId.ShouldBe(userId);
        receipt.Service.ShouldBe("jinx-api");
        receipt.BuildSha.ShouldBe("abc1234");
        receipt.RequestedAt.ShouldBe(RequestedAt);
        receipt.CompletedAt.ShouldBe(completedAt);
        receipt.ErasedRowCount.ShouldBe(4);
        receipt.ReadBackRowCount.ShouldBe(0);
        data.Rows[userId].ShouldBe(0);
    }

    [Fact]
    public async Task Post_WhenCalledTwice_ErasesZeroAndReturnsReceipt()
    {
        var userId = Guid.NewGuid();
        var erasureId = Guid.NewGuid();
        var data = new FakeUserData();
        data.Rows[userId] = 3;
        await using var app = await ErasureTestHost.StartAsync(data, Secret);
        var client = app.GetTestClient();
        var request = new ErasureRequest(erasureId, userId, null, RequestedAt);
        await ErasureTestHost.PostAsync(client, request, Secret);

        var second = await ErasureTestHost.PostAsync(client, request, Secret);

        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var receipt = await ErasureTestHost.ReadReceiptAsync(second);
        receipt.ErasureId.ShouldBe(erasureId);
        receipt.ErasedRowCount.ShouldBe(0);
        receipt.ReadBackRowCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-secret")]
    [InlineData("shared-secret-value-and-more")]
    public async Task Post_WithoutTheRightSecret_Returns401AndTouchesNothing(string? presented)
    {
        var userId = Guid.NewGuid();
        var data = new FakeUserData();
        data.Rows[userId] = 2;
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), userId, null, RequestedAt), presented);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        data.EraseCalls.ShouldBe(0);
        data.Rows[userId].ShouldBe(2);
    }

    [Fact]
    public async Task Post_WithMalformedJsonBody_Returns400()
    {
        var data = new FakeUserData();
        await using var app = await ErasureTestHost.StartAsync(data, Secret);
        using var content = new StringContent("{not json", Encoding.UTF8, "application/json");

        var response = await ErasureTestHost.PostRawAsync(app.GetTestClient(), content, Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        data.EraseCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Post_WhenParticipantThrowsJsonException_Returns500()
    {
        var data = new FakeUserData { EraseFailure = new JsonException("a stored column is bad") };
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "6f1a7f3e-0000-4000-8000-000000000001")]
    [InlineData("6f1a7f3e-0000-4000-8000-000000000001", "00000000-0000-0000-0000-000000000000")]
    public async Task Post_WithEmptyIdentifier_Returns400(string erasureId, string userId)
    {
        var data = new FakeUserData();
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.Parse(erasureId), Guid.Parse(userId), null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        data.EraseCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Post_WhenRowsRemainAfterErase_Returns500()
    {
        var userId = Guid.NewGuid();
        var data = new FakeUserData { LeaveRowsBehind = true };
        data.Rows[userId] = 1;
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), userId, null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Post_WhenParticipantReportsNoDatasets_Returns500()
    {
        var data = new FakeUserData { ReportNoDatasets = true };
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Post_WhenDatasetIsRetained_ReturnsOutcomeBasisAndEndDateOnTheWire()
    {
        var data = new FakeUserData();
        data.ExtraDatasets.Add(ErasureDatasetResult.Retained("payments", 7, "AMLD Art 40", new DateOnly(2099, 1, 31)));
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"outcome\":\"Retained\"");
        body.ShouldContain("\"retentionBasis\":\"AMLD Art 40\"");
        body.ShouldContain("\"retainedUntil\":\"2099-01-31\"");
        (await ErasureTestHost.ReadReceiptAsync(response)).RetainedRowCount.ShouldBe(7);
    }

    [Fact]
    public async Task Post_WhenRetainedHasNoEndDate_Returns500()
    {
        var data = new FakeUserData();
        data.ExtraDatasets.Add(new ErasureDatasetResult("payments", ErasureOutcome.Retained, 7, "AMLD Art 40"));
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Post_WhenHostRequiresAuthenticationByDefault_StillAcceptsTheSecret()
    {
        var data = new FakeUserData();
        await using var app = await ErasureTestHost.StartAsync(data, Secret, requireAuthenticationByDefault: true);

        var response = await ErasureTestHost.PostAsync(
            app.GetTestClient(), new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt), Secret);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("", "jinx-api", "abc1234")]
    [InlineData(" ", "jinx-api", "abc1234")]
    [InlineData("shared-secret-value", "", "abc1234")]
    [InlineData("shared-secret-value", "jinx-api", "")]
    public async Task MapErasureEndpoint_WithBlankSetting_ThrowsAtStartup(string secret, string service, string buildSha)
    {
        var data = new FakeUserData();

        var starting = () => ErasureTestHost.StartAsync(data, secret, service, buildSha);

        await starting.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MapErasureEndpoint_WithValidOptions_IsHiddenFromApiDescription()
    {
        var data = new FakeUserData();
        await using var app = await ErasureTestHost.StartAsync(data, Secret);

        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .Single(candidate => candidate.DisplayName!.Contains(ErasureContract.Route));

        endpoint.Metadata.GetMetadata<IExcludeFromDescriptionMetadata>().ShouldNotBeNull();
    }
}
