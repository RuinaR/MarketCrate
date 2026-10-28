using System.Text.Json;
using MarketCrate.Core;

namespace MarketCrate.Infrastructure;

// Single-writer lock is held for the lifetime of the app/journal, including across processes.
// The demo stores synthetic IDs only. SQLite migrations are deferred until real permitted data exists.
public sealed class FileBatchJournal : IBatchJournal, IDisposable
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
        if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        var record = await JsonSerializer.DeserializeAsync<BatchRecord>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException("저널이 비었습니다. 자동 실행을 차단합니다.");
        if (record.SchemaVersion != 1 || record.Snapshot is null || record.Snapshot.Lines.IsDefaultOrEmpty || record.Results.IsDefault || record.Results.Any(x => !Enum.IsDefined(x.State)) || record.Results.Length != record.Snapshot.Lines.Length ||
            !record.Results.Select(x => x.LineId).SequenceEqual(record.Snapshot.Lines.Select(x => x.LineId)))
            throw new InvalidDataException("지원하지 않거나 손상된 저널: 자동 실행 차단");
        return record;
    }
    public async Task SaveAsync(BatchRecord record, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, record, cancellationToken: cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Flush(true);
        }
        File.Move(temporary, path, true);
    }
    public void ClearDemo() { if (File.Exists(path)) File.Delete(path); }
    public void Dispose() => lease.Dispose();
}
