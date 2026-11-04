# Dloizides.Erasure

The contract a service implements to take part in GDPR account erasure. A coordinator (for us, `tenant-api`) POSTs one
request per service and counts the service as done only on a 2xx with a valid receipt.

## Consume it

```csharp
public sealed class JinxErasure(IMediator mediator, JinxDbContext db) : IErasureParticipant
{
    public async Task<IReadOnlyList<ErasureDatasetResult>> EraseAsync(ErasureRequest request, CancellationToken ct)
    {
        var erased = await mediator.Send(new EraseUserDataCommand(request.UserId), ct);
        return [ErasureDatasetResult.Erased("patterns", erased.Patterns)];
    }

    public Task<int> CountRemainingRowsAsync(Guid userId, CancellationToken ct) =>
        db.Patterns.CountAsync(pattern => pattern.UserId == userId, ct);
}

builder.AddErasureParticipant<JinxErasure>(options =>
{
    options.ServiceName = "jinx-api";
    options.BuildSha = buildSha;
});
app.MapErasureEndpoint();
```

Settings bind from the `Erasure` section (`Erasure__Secret` in the environment). A blank service name, build sha or
secret throws at startup. Do not route `/internal/erasures` on a public ingress: the package cannot see your ingress.

## The call

`POST /internal/erasures` with header `X-Erasure-Secret`:

```json
{
  "erasureId": "6f1a7f3e-0000-4000-8000-000000000001", // ledger id, the same on every retry
  "userId": "0b9d2c11-0000-4000-8000-000000000002",    // opaque login id of the data subject
  "tenantId": null,                                    // optional
  "requestedAt": "2026-11-01T08:00:00+00:00"
}
```

| Status | Meaning | Coordinator |
|---|---|---|
| 200 | valid receipt | Confirmed |
| 400 | body unreadable, or an empty id or time | stays Pending |
| 401 | missing or wrong secret, nothing touched | stays Pending |
| 500 | participant threw, rows remain, or receipt invalid | stays Pending |

Receipt:

```json
{
  "erasureId": "6f1a7f3e-0000-4000-8000-000000000001",
  "userId": "0b9d2c11-0000-4000-8000-000000000002",
  "service": "jinx-api",
  "buildSha": "abc1234",
  "requestedAt": "2026-11-01T08:00:00+00:00",
  "completedAt": "2026-11-02T09:30:00+00:00",
  "datasets": [
    { "dataset": "patterns", "outcome": "Erased", "rowCount": 4, "retentionBasis": null, "retainedUntil": null },
    // retained is a completed answer, and needs both a basis and an end date
    { "dataset": "payments", "outcome": "Retained", "rowCount": 7, "retentionBasis": "AMLD Art 40", "retainedUntil": "2031-11-02" }
  ],
  "readBackRowCount": 0, // rows left outside retained datasets, from CountRemainingRowsAsync
  "erasedRowCount": 4,
  "anonymisedRowCount": 0,
  "retainedRowCount": 7
}
```

A null `rowCount` means the dataset cannot identify the subject, not zero rows.

Let failures throw from the participant. The endpoint catches only a JSON error while reading the request, so an
unclassified table or a database error reaches the host's exception handling as a 500.

The coordinator checks a response with `ErasureReceiptValidator.FindProblems(request, receipt)`, using
`ErasureContract.JsonOptions`.

## Conformance test

Run this in the consumer's test suite, against the real endpoint and store:

```csharp
await ErasureConformance.AssertConformsAsync(new ErasureConformanceScenario
{
    Client = factory.CreateClient(),
    Secret = secret,
    SeedAsync = (subject, bystander, ct) => SeedPatternsAsync(subject, bystander, ct),
    CountRowsAsync = (userId, ct) => CountPatternsDirectlyAsync(userId, ct),
});
```

It fails on: no 401 for a missing or wrong secret, a rejected request changing rows, an invalid receipt, rows left in
the store whatever the receipt says, fewer rows reported than the store held, a bystander's rows changing, and a second
POST that fails or reports rows removed.
