using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure;

// Single-writer lock is held for the lifetime of the app/journal, including across processes.
// Legacy synthetic JSON journal; production Demo composition uses SqliteDemoStore.
public sealed class FileBatchJournal : IDemoJournal, IDisposable
{
    private readonly string path;
    private readonly FileStream lease;
    public FileBatchJournal(string directory)
    {
        Directory.CreateDirectory(directory);
        path = Path.Combine(directory, "demo-batch.json");
        lease = new FileStream(Path.Combine(directory, "demo-batch.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    public async Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.StoreRecovery,DiagnosticArea.Storage,"F10");
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        var record = await JsonSerializer.DeserializeAsync<BatchRecord>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("저널이 비었습니다. 자동 실행을 차단합니다.");
        if (record.SchemaVersion != 1 || record.Snapshot is null || record.Snapshot.Lines.IsDefaultOrEmpty || record.Results.IsDefault || record.Results.Any(x => !Enum.IsDefined(x.State)) || record.Results.Length != record.Snapshot.Lines.Length ||
            !record.Results.Select(x => x.LineId).SequenceEqual(record.Snapshot.Lines.Select(x => x.LineId)))
            throw new InvalidDataException("지원하지 않거나 손상된 저널: 자동 실행 차단");
        return BatchRecordGuard.Validate(record);
    }
    public async Task SaveAsync(BatchRecord record, CancellationToken cancellationToken)
    {
        using var scope=DiagnosticHub.Scope(batch:record.Snapshot.BatchId);
        using var operation=DiagnosticHub.Operation(DiagnosticEvent.JournalWrite,DiagnosticArea.Storage,"F02");
        var temporary = path + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, record, cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Flush(true);
        }
        File.Move(temporary, path, true);
        DiagnosticHub.Emit(DiagnosticEvent.JournalWrite,DiagnosticArea.Storage,journal:"Committed");
    }
    public void ClearDemo() { if (File.Exists(path)) File.Delete(path); }
    public void Dispose() => lease.Dispose();
}
