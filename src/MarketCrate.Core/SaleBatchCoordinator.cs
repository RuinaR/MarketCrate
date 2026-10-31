using System.Collections.Immutable;

namespace MarketCrate.Core;

public sealed class SaleBatchCoordinator(IMarketListingExecutor executor, IBatchJournal journal)
{
    private readonly SemaphoreSlim gate = journal is IBatchHistoryJournal history ? history.ExecutionGate : new(1, 1);
    public static string RequestKey(SaleBatchSnapshot batch, SaleLineSnapshot line) => $"{batch.BatchId:N}:{line.LineId}";
    public static string AttemptKey(SaleBatchSnapshot batch, SaleLineSnapshot line, int attempt) => attempt == 0 ? RequestKey(batch, line) : $"{RequestKey(batch, line)}:attempt:{attempt}";

    // Explicit approval only. Original snapshot and successful/uncertain lines remain untouched.
#if MARKETCRATE_DEMO
    public async Task<BatchRecord> RetryFailedAsync(IReadOnlyDictionary<string, int> approvedAttempts, IReadOnlyList<InventoryAsset> current,
        DateTimeOffset pricesCheckedAt, IProgress<BatchRecord>? progress = null, CancellationToken cancellationToken = default)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.Retry,DiagnosticArea.Sales,"F04");
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (executor.Capability != Capability.Mock) throw new InvalidOperationException("IntegrationBlocked");
            var record = BatchRecordGuard.Validate(await journal.LoadAsync(cancellationToken));
            var lineIds = approvedAttempts.Keys.ToArray();
            if (lineIds.Length == 0 || lineIds.Distinct().Count() != lineIds.Length ||
                lineIds.Any(id => !record.Results.Any(x => x.LineId == id && x.State == SaleState.Failed && x.Attempt == approvedAttempts[id])))
                throw new ArgumentException("확정 실패 항목만 명시적으로 재시도할 수 있습니다.");
            var now = DateTimeOffset.UtcNow;
            if (pricesCheckedAt > now.AddSeconds(5) || now - pricesCheckedAt > TimeSpan.FromMinutes(5)) throw new ArgumentException("재시도 시세 검토를 갱신하세요.");
            SalePlanner.ValidateCurrent(record.Snapshot, current);
            foreach (var id in lineIds)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var index = Enumerable.Range(0, record.Results.Length).First(i => record.Results[i].LineId == id);
                var line = record.Snapshot.Lines[index]; var attempt = checked(record.Results[index].Attempt + 1);
                record = Update(record, index, new(id, SaleState.Submitting, "명시적 Mock 재시도 전송 중", attempt));
                await journal.SaveAsync(record, CancellationToken.None); progress?.Report(record);
                if (journal is IDiagnosticStore diagnostics) diagnostics.RecordDiagnostic(DiagnosticCode.RetryStarted, 1);
                LineResult result;
                try { result = await executor.SubmitAsync(line, AttemptKey(record.Snapshot, line, attempt), cancellationToken); }
                catch (Exception error) when (error is IOException or TimeoutException or OperationCanceledException)
                { result = new(id, SaleState.Unknown, "재시도 응답 불명: 재전송 금지"); }
                if (result.LineId != id || result.State is not (SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Listed or SaleState.Failed or SaleState.Unknown))
                    throw new InvalidDataException("재시도 응답 불일치");
                record = Update(record, index, result with { Attempt = attempt });
                await journal.SaveAsync(record, CancellationToken.None); progress?.Report(record);
                if (result.State == SaleState.Unknown) break;
                await Task.Yield(); // Let the caller's UI process cancellation between mock requests.
            }
            DiagnosticHub.Emit(DiagnosticEvent.BatchFinished,DiagnosticArea.Sales,DiagnosticOutcome.Completed,count:record.Results.Length,journal:"Committed");
            return record;
        }
        catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.BatchFinished,DiagnosticArea.Sales,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticHub.FailureCategory(error),exception:error,journal:"JournalPreserved");throw;}
        finally { gate.Release(); }
    }

#endif
    public async Task<BatchRecord> ExecuteAsync(SaleBatchSnapshot snapshot, IProgress<BatchRecord>? progress = null, CancellationToken cancellationToken = default)
    {
        using var scope=DiagnosticHub.Scope(batch:snapshot.BatchId);
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.BatchCreated,DiagnosticArea.Sales,"F01");
        await gate.WaitAsync(cancellationToken);
        try
        {
#if MARKETCRATE_DEMO
            if (executor.Capability != Capability.Mock && executor is not IAuthorizedListingExecutor { CanExecute: true }) throw new InvalidOperationException("IntegrationBlocked");
#else
            if (executor is not IAuthorizedListingExecutor { CanExecute: true }) throw new InvalidOperationException("IntegrationBlocked");
#endif
            var existing = journal is IBatchHistoryJournal historyJournal
                ? await historyJournal.FindBatchAsync(snapshot.BatchId, cancellationToken)
                : await journal.LoadAsync(cancellationToken);
            // A durable multi-batch journal preserves prior work without replaying it.
            if (existing != null)
            {
                if (existing.Snapshot.BatchId != snapshot.BatchId) throw new InvalidOperationException("기존 활성 배치를 먼저 복구/대조하세요. 새 배치를 제출하지 않았습니다.");
                BatchRecordGuard.ValidateTransition(existing,existing with{Snapshot=snapshot});
                return existing;
            }
            BatchValidation.Validate(snapshot, DateTimeOffset.UtcNow);
            DiagnosticHub.Emit(DiagnosticEvent.FinalValidation,DiagnosticArea.Sales,requirement:"E09",count:snapshot.Lines.Length);
            if (executor is IAuthorizedListingExecutor authorized) await authorized.ValidateBatchAsync(snapshot, cancellationToken);
#if MARKETCRATE_DEMO
            if (journal is IDemoDataStore history) SalePlanner.ValidateReservations(snapshot, history.ReadBatches(), MockListingManagement.ReleasedRequests(history.ReadListings()));
            var record = new BatchRecord(1, snapshot, snapshot.Lines.Select(x => new LineResult(x.LineId, SaleState.Ready, executor.Capability == Capability.Mock ? "Mock 대기" : "실험적 Steam 전송 대기")).ToImmutableArray());
#else
            var record = new BatchRecord(1, snapshot, snapshot.Lines.Select(x => new LineResult(x.LineId, SaleState.Ready, "Steam 전송 대기")).ToImmutableArray());
#endif
            await journal.SaveAsync(record, cancellationToken);
            for (var i = 0; i < snapshot.Lines.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var line = snapshot.Lines[i];
                using var itemScope=DiagnosticHub.Scope(item:RequestKey(snapshot,line));
                record = Update(record, i, new(line.LineId, SaleState.Submitting, "전송 전 의도 기록 중"));
                // Durable intent BEFORE side effect. Save failure prevents submission.
                await journal.SaveAsync(record, CancellationToken.None);
                progress?.Report(record);
                DiagnosticHub.Emit(DiagnosticEvent.ItemReserved,DiagnosticArea.Sales,requirement:"F03",to:SaleState.Submitting.ToString(),journal:"IntentDurable");
                LineResult result;var stopAfterLine=false;
                try { result = await executor.SubmitAsync(line, RequestKey(snapshot, line), cancellationToken); }
                catch(Exception error) when(error is ISubmissionFailure{RequestNotSent:true})
                {
                    DiagnosticHub.Emit(DiagnosticEvent.ItemResult,DiagnosticArea.Sales,DiagnosticOutcome.Blocked,LogLevel.Warning,error is OperationCanceledException?DiagnosticError.Cancelled:DiagnosticError.Validation,exception:error,reason:"RequestNotSent");
                    result=new(line.LineId,SaleState.Failed,"전송 전 중단 · 이 항목의 판매 요청은 보내지 않았습니다. 계정·보관함·가격을 확인하고 새로 검토하세요.");stopAfterLine=true;
                }
                catch (Exception e) when (e is TimeoutException or OperationCanceledException or IOException)
                { DiagnosticHub.Emit(DiagnosticEvent.ItemResult,DiagnosticArea.Sales,DiagnosticOutcome.Unknown,LogLevel.Error,DiagnosticError.Transport,exception:e,reason:"ResponseLost"); result = new(line.LineId, SaleState.Unknown, "응답 불명: 자동 재전송 금지, 상태 대조 필요"); }
                if (result.LineId != line.LineId) throw new InvalidDataException("응답 행 불일치");
                if (result.State is not (SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Listed or SaleState.Failed or SaleState.Unknown))
                    throw new InvalidDataException("잘못된 제출 결과: 재전송 금지");
                var previousState=record.Results[i].State;
                record = Update(record, i, result);
                await journal.SaveAsync(record, CancellationToken.None);
                DiagnosticHub.Emit(DiagnosticEvent.ItemResult,DiagnosticArea.Sales,result.State==SaleState.Unknown?DiagnosticOutcome.Unknown:result.State==SaleState.Failed?DiagnosticOutcome.Failed:result.State==SaleState.AwaitingSteamGuard?DiagnosticOutcome.Pending:DiagnosticOutcome.Succeeded,from:previousState.ToString(),to:result.State.ToString(),requirement:"F03",quote:line.UnitQuote,currency:line.Currency,count:line.Quantity,journal:"Committed",reason:result.State==SaleState.Unknown?"EvidenceMissing":"Committed");
                progress?.Report(record);
                if (result.State == SaleState.Unknown||stopAfterLine) break;
                if (i % 8 == 7) await Task.Yield();
            }
            DiagnosticHub.Emit(DiagnosticEvent.BatchFinished,DiagnosticArea.Sales,DiagnosticOutcome.Completed,count:record.Results.Length,journal:"Committed");
            return record;
        }
        catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.BatchFinished,DiagnosticArea.Sales,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticHub.FailureCategory(error),exception:error,journal:"JournalPreserved");throw;}
        finally { gate.Release(); }
    }

    public async Task<BatchRecord?> RecoverAsync(CancellationToken cancellationToken = default)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.BatchRecovered,DiagnosticArea.Sales,"F10");
#if MARKETCRATE_DEMO
        if (executor.Capability != Capability.Mock && executor is not IAuthorizedListingExecutor { CanReconcile: true }) throw new InvalidOperationException("IntegrationBlocked");
#else
        if (executor is not IAuthorizedListingExecutor { CanReconcile: true }) throw new InvalidOperationException("IntegrationBlocked");
#endif
        await gate.WaitAsync(cancellationToken);
        try
        {
            var record = await journal.LoadAsync(cancellationToken);
            if (record == null) return null;
            BatchRecordGuard.Validate(record);
            for (var i = 0; i < record.Results.Length; i++)
            {
                if (record.Results[i].State is not (SaleState.Submitting or SaleState.Unknown or SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Listed)) continue;
                var line = record.Snapshot.Lines[i];
                using var itemScope=DiagnosticHub.Scope(batch:record.Snapshot.BatchId,item:AttemptKey(record.Snapshot,line,record.Results[i].Attempt));
                LineResult result;
                try { result = await executor.ReconcileAsync(line, AttemptKey(record.Snapshot, line, record.Results[i].Attempt), cancellationToken); }
                catch (Exception e) when (e is IOException or TimeoutException)
                { result = new(line.LineId, SaleState.Unknown, "상태 대조 실패: 재전송하지 않았습니다."); }
                if (result.LineId != line.LineId) throw new InvalidDataException("대조 응답 행 불일치");
                if (result.State is not (SaleState.Unknown or SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Listed or SaleState.Sold or SaleState.Failed or SaleState.Reconciled))
                    throw new InvalidDataException("안전하지 않은 대조 상태: 재제출 금지");
                var previousState=record.Results[i].State;
                if(previousState==SaleState.Listed&&result.State!=SaleState.Sold&&result.State!=SaleState.Listed)
                    result=new(line.LineId,SaleState.Listed,"과거 게시 확인 기록 · 현재 체결/취소 결과는 미확인 · "+result.Message);
                record = Update(record, i, result with { Attempt = record.Results[i].Attempt });
                await journal.SaveAsync(record, cancellationToken);
                DiagnosticHub.Emit(DiagnosticEvent.EvidenceChecked,DiagnosticArea.Sales,result.State==SaleState.Unknown?DiagnosticOutcome.Unknown:DiagnosticOutcome.Reconciled,from:previousState.ToString(),to:result.State.ToString(),reason:result.State==SaleState.Unknown?"EvidenceMissing":"Committed",requirement:"F09",journal:"Committed");
            }
            DiagnosticHub.Emit(DiagnosticEvent.BatchFinished,DiagnosticArea.Sales,DiagnosticOutcome.Completed,count:record.Results.Length,journal:"Committed");
            return record;
        }
        catch(Exception error){DiagnosticHub.Emit(DiagnosticEvent.BatchFinished,DiagnosticArea.Sales,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticHub.FailureCategory(error),exception:error,journal:"JournalPreserved");throw;}
        finally { gate.Release(); }
    }

    private static BatchRecord Update(BatchRecord record, int index, LineResult result) => record with { Results = record.Results.SetItem(index, result) };
}
