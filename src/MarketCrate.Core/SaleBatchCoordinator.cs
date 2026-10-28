using System.Collections.Immutable;

namespace MarketCrate.Core;

public sealed class SaleBatchCoordinator(IMarketListingExecutor executor, IBatchJournal journal)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public static string RequestKey(SaleBatchSnapshot batch, SaleLineSnapshot line) => $"{batch.BatchId:N}:{line.LineId}";

    public async Task<BatchRecord> ExecuteAsync(SaleBatchSnapshot snapshot, IProgress<BatchRecord>? progress = null, CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (executor.Capability != Capability.Mock) throw new InvalidOperationException("IntegrationBlocked: 허용된 실거래 어댑터 없음");
            var existing = await journal.LoadAsync(cancellationToken);
            // Journal is an intentionally single active batch. User must archive/clear completed demo data before another batch.
            if (existing != null) return existing;
            BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
            var record = new BatchRecord(1, snapshot, snapshot.Lines.Select(x => new LineResult(x.LineId, SaleState.Ready, "Mock 대기")).ToImmutableArray());
            await journal.SaveAsync(record, cancellationToken);
            for (var i = 0; i < snapshot.Lines.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var line = snapshot.Lines[i];
                record = Update(record, i, new(line.LineId, SaleState.Submitting, "Mock 전송 기록 중"));
                // Durable intent BEFORE side effect. Save failure prevents submission.
                await journal.SaveAsync(record, CancellationToken.None);
                progress?.Report(record);
                LineResult result;
                try { result = await executor.SubmitAsync(line, RequestKey(snapshot, line), cancellationToken); }
                catch (Exception e) when (e is TimeoutException or OperationCanceledException or IOException)
                { result = new(line.LineId, SaleState.Unknown, "응답 불명: 자동 재전송 금지, 상태 대조 필요"); }
                if (result.LineId != line.LineId) throw new InvalidDataException("응답 행 불일치");
                record = Update(record, i, result);
                await journal.SaveAsync(record, CancellationToken.None);
                progress?.Report(record);
                if (result.State == SaleState.Unknown) break;
            }
            return record;
        }
        finally { gate.Release(); }
    }

    public async Task<BatchRecord?> RecoverAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var record = await journal.LoadAsync(cancellationToken);
            if (record == null) return null;
            for (var i = 0; i < record.Results.Length; i++)
            {
                if (record.Results[i].State is not (SaleState.Submitting or SaleState.Unknown or SaleState.Submitted or SaleState.AwaitingSteamGuard)) continue;
                var line = record.Snapshot.Lines[i];
                LineResult result;
                try { result = await executor.ReconcileAsync(line, RequestKey(record.Snapshot, line), cancellationToken); }
                catch (Exception e) when (e is IOException or TimeoutException)
                { result = new(line.LineId, SaleState.Unknown, "상태 대조 실패: 재전송하지 않았습니다."); }
                if (result.LineId != line.LineId) throw new InvalidDataException("대조 응답 행 불일치");
                record = Update(record, i, result);
                await journal.SaveAsync(record, cancellationToken);
            }
            return record;
        }
        finally { gate.Release(); }
    }

    private static BatchRecord Update(BatchRecord record, int index, LineResult result) => record with { Results = record.Results.SetItem(index, result) };
}
