namespace MarketCrate.Core;

public static class BatchRecordGuard
{
    public static void ValidateTransition(BatchRecord previous, BatchRecord next)
    {
        if (next.SchemaVersion != previous.SchemaVersion || next.Results.IsDefault || next.Results.Length != previous.Results.Length || next.Results.Any(x => x is null || x.Attempt < 0 || !Enum.IsDefined(x.State)))
            throw new InvalidDataException("저널 결과 구조 변경 금지");
        if (!ReferenceEquals(previous.Snapshot, next.Snapshot) && System.Text.Json.JsonSerializer.Serialize(previous.Snapshot) != System.Text.Json.JsonSerializer.Serialize(next.Snapshot))
            throw new InvalidDataException("확정 스냅샷 변경 금지");
        for (var i = 0; i < previous.Results.Length; i++)
        {
            if (next.Results[i].LineId != previous.Results[i].LineId) throw new InvalidDataException("저널 결과 ID 변경 금지");
            var from = previous.Results[i].State; var to = next.Results[i].State;
            var oldAttempt = previous.Results[i].Attempt; var newAttempt = next.Results[i].Attempt;
            if (from == SaleState.Failed && to == SaleState.Submitting && newAttempt == oldAttempt + 1) continue;
            if (newAttempt != oldAttempt) throw new InvalidDataException("시도 번호 불일치");
            if (from == to) continue;
            var allowed = from switch
            {
                SaleState.Ready => to == SaleState.Submitting,
                SaleState.Submitting => to is SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Listed or SaleState.Failed or SaleState.Unknown or SaleState.Reconciled or SaleState.Sold,
                SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Unknown => to is SaleState.Submitted or SaleState.AwaitingSteamGuard or SaleState.Listed or SaleState.Failed or SaleState.Unknown or SaleState.Reconciled or SaleState.Sold,
                SaleState.Listed => to == SaleState.Sold,
                _ => false
            };
            if (!allowed) throw new InvalidDataException("확정 결과 역행/자동 재전송 상태 금지");
        }
    }
    public static BatchRecord Validate(BatchRecord? record)
    {
        if (record is null || record.SchemaVersion != 1 || record.Snapshot is null || record.Snapshot.BatchId == Guid.Empty ||
            record.Snapshot.Lines.IsDefaultOrEmpty || record.Results.IsDefault || record.Snapshot.Lines.Length != record.Results.Length ||
            string.IsNullOrWhiteSpace(record.Snapshot.AccountReference) ||
            record.Snapshot.Lines.Any(x => x is null || x.Asset is null || x.Asset.Key is null || x.Asset.Key.AppId < 1 || string.IsNullOrWhiteSpace(x.Asset.Key.ContextId) || string.IsNullOrWhiteSpace(x.Asset.Key.AssetId) || string.IsNullOrWhiteSpace(x.LineId)) ||
            record.Snapshot.Lines.Select(x => x.LineId).Distinct().Count() != record.Snapshot.Lines.Length ||
            record.Results.Any(x => x is null || x.Attempt < 0 || !Enum.IsDefined(x.State)) ||
            !record.Results.Select(x => x.LineId).SequenceEqual(record.Snapshot.Lines.Select(x => x.LineId)))
            throw new InvalidDataException("손상 또는 미지원 저널: 자동 실행 차단");
        try { BatchValidation.Validate(record.Snapshot, record.Snapshot.FinalizedAt); }
        catch (Exception error) when (error is ArgumentException or OverflowException) { throw new InvalidDataException("저널 스냅샷 정합성 오류", error); }
        return record;
    }
}
