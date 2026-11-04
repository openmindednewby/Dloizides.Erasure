using Dloizides.Erasure.Contracts;
using Shouldly;

namespace Dloizides.Erasure.Tests;

public sealed class ErasureReceiptValidatorTests
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompletedAt = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FindProblems_WithAllThreeOutcomes_ReturnsNone()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0,
            ErasureDatasetResult.Erased("patterns", 4),
            ErasureDatasetResult.Anonymised("templates", 2),
            ErasureDatasetResult.Retained("payments", 1, "AMLD Art 40", new DateOnly(2031, 11, 2)));

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldBeEmpty();
    }

    [Fact]
    public void FindProblems_WithoutReceipt_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);

        var problems = ErasureReceiptValidator.FindProblems(request, null);

        problems.ShouldHaveSingleItem().ShouldContain("no receipt");
    }

    [Fact]
    public void FindProblems_WhenErasureIdDiffers_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0, ErasureDatasetResult.Erased("patterns", 0)) with { ErasureId = Guid.NewGuid() };

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("erasure id");
    }

    [Fact]
    public void FindProblems_WhenUserIdDiffers_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0, ErasureDatasetResult.Erased("patterns", 0)) with { UserId = Guid.NewGuid() };

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("user id");
    }

    [Fact]
    public void FindProblems_WhenReadBackIsNotZero_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 1, ErasureDatasetResult.Erased("patterns", 4));

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("read-back");
    }

    [Fact]
    public void FindProblems_WithoutDatasets_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0);

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("no datasets");
    }

    [Fact]
    public void FindProblems_WithBlankBuildSha_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0, ErasureDatasetResult.Erased("patterns", 0)) with { BuildSha = " " };

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("build sha");
    }

    [Fact]
    public void FindProblems_WithUndefinedOutcome_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0, new ErasureDatasetResult("patterns", (ErasureOutcome)99, 0));

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("outcome");
    }

    [Fact]
    public void FindProblems_WithNegativeRowCount_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0, ErasureDatasetResult.Erased("patterns", -1));

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("row count");
    }

    [Fact]
    public void FindProblems_WhenRetainedHasNoBasis_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0,
            new ErasureDatasetResult("payments", ErasureOutcome.Retained, 1, null, new DateOnly(2031, 11, 2)));

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("basis");
    }

    [Fact]
    public void FindProblems_WhenRetentionEndedBeforeCompletion_ReportsIt()
    {
        var request = new ErasureRequest(Guid.NewGuid(), Guid.NewGuid(), null, RequestedAt);
        var receipt = ReceiptFor(request, 0,
            ErasureDatasetResult.Retained("payments", 1, "AMLD Art 40", new DateOnly(2026, 11, 1)));

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);

        problems.ShouldHaveSingleItem().ShouldContain("end date");
    }

    private static ErasureReceipt ReceiptFor(ErasureRequest request, int readBack, params ErasureDatasetResult[] datasets) =>
        new(request.ErasureId, request.UserId, "jinx-api", "abc1234", request.RequestedAt, CompletedAt, datasets, readBack);
}
