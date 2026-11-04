namespace Dloizides.Erasure.Contracts;

/// <summary>One instruction to erase one data subject from one service.</summary>
/// <param name="ErasureId">The ledger id; the same id on every retry.</param>
/// <param name="UserId">The opaque identity-provider id of the data subject.</param>
public sealed record ErasureRequest(Guid ErasureId, Guid UserId, Guid? TenantId, DateTimeOffset RequestedAt);
