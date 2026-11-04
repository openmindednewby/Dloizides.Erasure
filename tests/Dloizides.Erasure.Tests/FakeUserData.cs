using Dloizides.Erasure.Contracts;

namespace Dloizides.Erasure.Tests;

internal sealed class FakeUserData
{
    public Dictionary<Guid, int> Rows { get; } = [];
    public int EraseCalls { get; set; }
    public Exception? EraseFailure { get; set; }
    public Exception? SecondEraseFailure { get; set; }
    public bool LeaveRowsBehind { get; set; }
    public bool ReportZeroRemaining { get; set; }
    public bool EraseEveryone { get; set; }
    public bool ReportNoDatasets { get; set; }
    public List<ErasureDatasetResult> ExtraDatasets { get; } = [];
}

internal sealed class FakeParticipant(FakeUserData data) : IErasureParticipant
{
    public Task<IReadOnlyList<ErasureDatasetResult>> EraseAsync(ErasureRequest request, CancellationToken cancellationToken)
    {
        data.EraseCalls++;
        if (data.EraseFailure is not null) throw data.EraseFailure;
        if (data.EraseCalls > 1 && data.SecondEraseFailure is not null) throw data.SecondEraseFailure;
        if (data.ReportNoDatasets) return Task.FromResult<IReadOnlyList<ErasureDatasetResult>>([]);

        var found = data.Rows.GetValueOrDefault(request.UserId);
        if (!data.LeaveRowsBehind) data.Rows[request.UserId] = 0;
        if (data.EraseEveryone) data.Rows.Keys.ToList().ForEach(key => data.Rows[key] = 0);

        List<ErasureDatasetResult> results = [ErasureDatasetResult.Erased("patterns", found), .. data.ExtraDatasets];
        return Task.FromResult<IReadOnlyList<ErasureDatasetResult>>(results);
    }

    public Task<int> CountRemainingRowsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(data.ReportZeroRemaining ? 0 : data.Rows.GetValueOrDefault(userId));
}
