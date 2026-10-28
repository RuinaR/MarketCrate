using System.Text.Json;
using MarketCrate.Core;
using Microsoft.Data.Sqlite;

namespace MarketCrate.Infrastructure;

// One app-owned connection/lease. Synthetic Demo data only; no credentials or real account data.
public sealed class SqliteDemoStore : IDemoJournal, IDemoDataStore, IDisposable
{
    private readonly SqliteConnection connection;
    private readonly FileStream lease;
    private readonly object gate = new();
    public SqliteDemoStore(string directory)
    {
        Directory.CreateDirectory(directory);
        lease = new(Path.Combine(directory, "demo-batch.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        connection = new(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "demo.sqlite"), Pooling = false, DefaultTimeout = 2 }.ToString());
        try
        {
            connection.Open();
            var version = Convert.ToInt32(Scalar("PRAGMA user_version;"));
            if (version > 1) throw new InvalidDataException("지원하지 않는 DB 스키마");
            Execute("PRAGMA journal_mode=DELETE; PRAGMA synchronous=FULL;");
            if (version == 0)
            {
                using var transaction = connection.BeginTransaction();
                using var command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "CREATE TABLE objects(kind TEXT NOT NULL, id TEXT NOT NULL, value TEXT NOT NULL, PRIMARY KEY(kind,id)); PRAGMA user_version=1;";
                command.ExecuteNonQuery(); transaction.Commit();
            }
            // Import once, including an explicit tombstone so clearing Demo never reimports an old batch.
            if (Get("migration", "json-v1") is null)
            {
                var legacy = Path.Combine(directory, "demo-batch.json");
                var record = File.Exists(legacy) ? BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(File.ReadAllText(legacy))) : null;
                var hasJournal = Get("journal", "active") != null;
                using var transaction = connection.BeginTransaction();
                if (record != null && !hasJournal)
                { Put("journal", "active", JsonSerializer.Serialize(record), transaction); Put("batch", record.Snapshot.BatchId.ToString("N"), JsonSerializer.Serialize(record), transaction); }
                Put("migration", "json-v1", "done", transaction); transaction.Commit();
            }
        }
        catch { connection.Dispose(); lease.Dispose(); throw; }
    }
    private object? Scalar(string sql) { using var cmd = connection.CreateCommand(); cmd.CommandText = sql; return cmd.ExecuteScalar(); }
    private void Execute(string sql) { using var cmd = connection.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery(); }
    private string? Get(string kind, string id)
    {
        using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT value FROM objects WHERE kind=$kind AND id=$id";
        cmd.Parameters.AddWithValue("$kind", kind); cmd.Parameters.AddWithValue("$id", id); return cmd.ExecuteScalar() as string;
    }
    private void Put(string kind, string id, string value, SqliteTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand(); cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO objects(kind,id,value) VALUES($kind,$id,$value) ON CONFLICT(kind,id) DO UPDATE SET value=excluded.value";
        cmd.Parameters.AddWithValue("$kind", kind); cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$value", value); cmd.ExecuteNonQuery();
    }
    public Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate) { var json = Get("journal", "active"); return Task.FromResult(json == null ? null : BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(json))); }
    }
    public Task SaveAsync(BatchRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); BatchRecordGuard.Validate(record);
        lock (gate)
        {
            if (Get("journal", "active") is { } previous)
                BatchRecordGuard.ValidateTransition(BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(previous)), record);
            using var transaction = connection.BeginTransaction();
            Put("journal", "active", JsonSerializer.Serialize(record), transaction);
            Put("batch", record.Snapshot.BatchId.ToString("N"), JsonSerializer.Serialize(record), transaction);
            transaction.Commit();
        }
        return Task.CompletedTask;
    }
    public string? ReadSetting(string key) { lock (gate) return Get("setting", key); }
    public void WriteSetting(string key, string value) { lock (gate) Put("setting", key, value); }
    public void SaveInventory(IReadOnlyList<InventoryAsset> assets) { lock (gate) Put("data", "inventory", JsonSerializer.Serialize(assets)); }
    public IReadOnlyList<InventoryAsset> ReadInventory() { lock (gate) return JsonSerializer.Deserialize<InventoryAsset[]>(Get("data", "inventory") ?? "[]")!; }
    public IReadOnlyList<MockListing> ReadListings() { lock (gate) return JsonSerializer.Deserialize<MockListing[]>(Get("data", "listings") ?? "[]")!; }
    public void SaveListings(IReadOnlyList<MockListing> listings) { lock (gate) Put("data", "listings", JsonSerializer.Serialize(listings)); }
    public LineResult? ReadEvidence(string requestKey) { lock (gate) { var json = Get("evidence", requestKey); return json == null ? null : JsonSerializer.Deserialize<LineResult>(json); } }
    public void WriteEvidence(string requestKey, LineResult result) { lock (gate) Put("evidence", requestKey, JsonSerializer.Serialize(result)); }
    public IReadOnlyList<BatchRecord> ReadBatches()
    {
        lock (gate)
        {
            using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT value FROM objects WHERE kind='batch'";
            using var rows = cmd.ExecuteReader(); var batches = new List<BatchRecord>();
            while (rows.Read()) batches.Add(BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(rows.GetString(0))));
            return batches;
        }
    }
    public void ClearDemo()
    {
        lock (gate)
        {
            if (Get("journal", "active") is { } json && BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(json)).Results.Any(x => x.State is not (SaleState.Ready or SaleState.Listed or SaleState.Failed)))
                throw new InvalidOperationException("미확정/승인 대기 배치는 해제할 수 없습니다. Mock 결과를 먼저 대조하세요.");
            Execute("DELETE FROM objects WHERE kind='journal';");
        }
    }
    public void DeleteDemoData()
    {
        lock (gate)
        {
            using var transaction = connection.BeginTransaction();
            using (var cmd = connection.CreateCommand()) { cmd.Transaction = transaction; cmd.CommandText = "DELETE FROM objects WHERE kind<>'migration';"; cmd.ExecuteNonQuery(); }
            transaction.Commit(); Execute("VACUUM;");
        }
    }
    public void Dispose() { connection.Dispose(); lease.Dispose(); }
}
