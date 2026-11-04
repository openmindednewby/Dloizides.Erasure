using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dloizides.Erasure.Contracts;

namespace Dloizides.Erasure.Testing;

internal sealed class ErasureConformanceRun(ErasureConformanceScenario scenario, CancellationToken cancellationToken)
{
    private readonly List<string> failures = [];

    public IReadOnlyList<string> Failures => failures;

    public async Task ExecuteAsync()
    {
        var subject = Guid.NewGuid();
        var bystander = Guid.NewGuid();
        await scenario.SeedAsync(subject, bystander, cancellationToken).ConfigureAwait(false);
        var subjectRows = await CountAsync(subject).ConfigureAwait(false);
        var bystanderRows = await CountAsync(bystander).ConfigureAwait(false);
        var request = new ErasureRequest(Guid.NewGuid(), subject, TenantId: null, DateTimeOffset.UtcNow);

        await CheckSecretIsEnforcedAsync(request, subjectRows).ConfigureAwait(false);
        var first = await PostForReceiptAsync(request, "first").ConfigureAwait(false);
        if (first is not null) CheckFirstReceipt(first, subjectRows);
        await CheckStoreAsync(subject, bystander, bystanderRows).ConfigureAwait(false);

        var second = await PostForReceiptAsync(request, "second").ConfigureAwait(false);
        if (second is not null) CheckSecondReceipt(second);
        await CheckStoreAsync(subject, bystander, bystanderRows).ConfigureAwait(false);
    }

    private async Task CheckSecretIsEnforcedAsync(ErasureRequest request, int subjectRows)
    {
        await CheckRejectedAsync(request, secret: null, "no secret").ConfigureAwait(false);
        await CheckRejectedAsync(request, scenario.Secret + "-wrong", "a wrong secret").ConfigureAwait(false);
        if (await CountAsync(request.UserId).ConfigureAwait(false) != subjectRows)
        {
            failures.Add("a rejected request changed the subject's rows");
        }
    }

    private async Task CheckRejectedAsync(ErasureRequest request, string? secret, string label)
    {
        using var response = await PostAsync(request, secret).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"{label}: expected 401, got {(int)response.StatusCode}"));
        }
    }

    private void CheckFirstReceipt(ErasureReceipt receipt, int subjectRows)
    {
        var removed = receipt.ErasedRowCount + receipt.AnonymisedRowCount;
        if (removed < subjectRows)
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"first receipt reported {removed} rows removed, the store held {subjectRows}"));
        }

        if (subjectRows == 0 && receipt.RetainedRowCount == 0)
        {
            failures.Add("the seed wrote no rows, so the receipt proves nothing");
        }
    }

    private void CheckSecondReceipt(ErasureReceipt receipt)
    {
        var removed = receipt.ErasedRowCount + receipt.AnonymisedRowCount;
        if (removed != 0) failures.Add(string.Create(CultureInfo.InvariantCulture, $"second receipt reported {removed} rows removed, expected 0"));
    }

    private async Task CheckStoreAsync(Guid subject, Guid bystander, int bystanderRows)
    {
        var left = await CountAsync(subject).ConfigureAwait(false);
        if (left != 0) failures.Add(string.Create(CultureInfo.InvariantCulture, $"the store still holds {left} rows for the subject"));

        if (await CountAsync(bystander).ConfigureAwait(false) != bystanderRows)
        {
            failures.Add("the bystander's rows changed");
        }
    }

    private async Task<ErasureReceipt?> PostForReceiptAsync(ErasureRequest request, string label)
    {
        using var response = await PostAsync(request, scenario.Secret).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"{label} call: expected 200, got {(int)response.StatusCode}"));
            return null;
        }

        var receipt = await ReadReceiptAsync(response, label).ConfigureAwait(false);
        if (receipt is null) return null;

        var problems = ErasureReceiptValidator.FindProblems(request, receipt);
        failures.AddRange(problems.Select(problem => $"{label} receipt: {problem}"));
        return problems.Count == 0 ? receipt : null;
    }

    private async Task<ErasureReceipt?> ReadReceiptAsync(HttpResponseMessage response, string label)
    {
        try
        {
            var receipt = await response.Content
                .ReadFromJsonAsync<ErasureReceipt>(ErasureContract.JsonOptions, cancellationToken)
                .ConfigureAwait(false);
            if (receipt is null) failures.Add($"{label} call: empty receipt");
            return receipt;
        }
        catch (JsonException)
        {
            failures.Add($"{label} call: body is not a receipt");
            return null;
        }
    }

    private async Task<HttpResponseMessage> PostAsync(ErasureRequest request, string? secret)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, ErasureContract.Route)
        {
            Content = JsonContent.Create(request, options: ErasureContract.JsonOptions),
        };
        if (secret is not null) message.Headers.Add(ErasureContract.SecretHeader, secret);
        return await scenario.Client.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private Task<int> CountAsync(Guid userId) => scenario.CountRowsAsync(userId, cancellationToken);
}
