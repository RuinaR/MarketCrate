using MarketCrate.Infrastructure.Diagnostics;
using System.Text.Json;
using System.Collections.Immutable;
using MarketCrate.Core;
using Microsoft.Data.Sqlite;

namespace MarketCrate.Infrastructure;

// One app-owned connection/lease. Synthetic Demo data only; no credentials or real account data.
public sealed class SqliteDemoStore : IDemoJournal, IDemoDataStore, IDiagnosticStore, IDisposable
{
    private readonly SqliteConnection connection;
    private readonly FileStream lease;
    private readonly object gate = new();
    private BatchRecord? activeCache;
    private Dictionary<string, MockListing>? listingCache;
    public SqliteDemoStore(string directory)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.StoreOpen,DiagnosticArea.Storage,"SYS-02");
        Directory.CreateDirectory(directory);
        lease = new(Path.Combine(directory, "demo-batch.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        connection = new(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "demo.sqlite"), Pooling = false, DefaultTimeout = 2 }.ToString());
        try
        {
            DiagnosticHub.Emit(DiagnosticEvent.StoreOpen,DiagnosticArea.Storage,DiagnosticOutcome.Started); connection.Open();
            var version = Convert.ToInt32(Scalar("PRAGMA user_version;"));
            DiagnosticHub.Emit(DiagnosticEvent.Migration,DiagnosticArea.Storage,DiagnosticOutcome.Started,count:Convert.ToInt32(version,System.Globalization.CultureInfo.InvariantCulture));
            if (version > 2) throw new InvalidDataException("지원하지 않는 DB 스키마");
            if (!string.Equals(Scalar("PRAGMA quick_check;") as string, "ok", StringComparison.Ordinal)) throw new InvalidDataException("DB 정합성 검사 실패");
            Execute("PRAGMA journal_mode=DELETE; PRAGMA synchronous=FULL;");
            if (version == 0)
            {
                using var transaction = StorageDiagnostics.Begin(connection);
                using var command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "CREATE TABLE objects(kind TEXT NOT NULL, id TEXT NOT NULL, value TEXT NOT NULL, PRIMARY KEY(kind,id)); PRAGMA user_version=1;";
                StorageDiagnostics.NonQuery(command); StorageDiagnostics.Commit(transaction);
            }
            if (version <= 1)
            {
                using var transaction = StorageDiagnostics.Begin(connection);
                using var command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = "CREATE TABLE diagnostics(at TEXT NOT NULL, code INTEGER NOT NULL, count INTEGER NOT NULL); PRAGMA user_version=2;";
                StorageDiagnostics.NonQuery(command); StorageDiagnostics.Commit(transaction);
            }
            // Import once, including an explicit tombstone so clearing Demo never reimports an old batch.
            if (Get("migration", "json-v1") is null)
            {
                var legacy = Path.Combine(directory, "demo-batch.json");
                var record = File.Exists(legacy) ? BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(File.ReadAllText(legacy))) : null;
                var hasJournal = Get("journal", "active") != null;
                using var transaction = StorageDiagnostics.Begin(connection);
                if (record != null && !hasJournal)
                { Put("journal", "active", JsonSerializer.Serialize(record), transaction); Put("batch", record.Snapshot.BatchId.ToString("N"), JsonSerializer.Serialize(record), transaction); }
                Put("migration", "json-v1", "done", transaction); StorageDiagnostics.Commit(transaction);
            }
            if (Get("migration", "listings-v2") is null)
            {
                var legacy = JsonSerializer.Deserialize<MockListing[]>(Get("data", "listings") ?? "[]") ?? throw new InvalidDataException("판매 목록 이관 오류");
                ValidateListings(legacy);
                using var transaction = StorageDiagnostics.Begin(connection);
                foreach (var listing in legacy) Put("listing", listing.ListingId, JsonSerializer.Serialize(listing), transaction);
                using (var delete = connection.CreateCommand()) { delete.Transaction = transaction; delete.CommandText = "DELETE FROM objects WHERE kind='data' AND id='listings'"; delete.ExecuteNonQuery(); }
                Put("migration", "listings-v2", "done", transaction); StorageDiagnostics.Commit(transaction);
            }
            RecordDiagnostic(DiagnosticCode.StoreOpened);
        }
        catch(Exception error) { DiagnosticHub.Emit(DiagnosticEvent.StoreOpen,DiagnosticArea.Storage,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Storage,exception:error); connection.Dispose(); lease.Dispose(); throw; }
    }
    private object? Scalar(string sql) { using var cmd = connection.CreateCommand(); cmd.CommandText = sql; return StorageDiagnostics.Scalar(cmd); }
    private void Execute(string sql) { using var cmd = connection.CreateCommand(); cmd.CommandText = sql; StorageDiagnostics.NonQuery(cmd); }
    private string? Get(string kind, string id)
    {
        using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT value FROM objects WHERE kind=$kind AND id=$id";
        cmd.Parameters.AddWithValue("$kind", kind); cmd.Parameters.AddWithValue("$id", id); return StorageDiagnostics.Scalar(cmd) as string;
    }
    private void Put(string kind, string id, string value, SqliteTransaction? transaction = null)
    {
        using var cmd = connection.CreateCommand(); cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO objects(kind,id,value) VALUES($kind,$id,$value) ON CONFLICT(kind,id) DO UPDATE SET value=excluded.value";
        cmd.Parameters.AddWithValue("$kind", kind); cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$value", value); StorageDiagnostics.NonQuery(cmd);
    }
    public Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate) { if (activeCache != null) return Task.FromResult<BatchRecord?>(activeCache); var json = Get("journal", "active"); return Task.FromResult(activeCache = json == null ? null : ReadRecord(json)); }
    }
    public Task SaveAsync(BatchRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            var previous = LoadAsync(cancellationToken).GetAwaiter().GetResult();
            if (previous == null || !ReferenceEquals(previous.Snapshot, record.Snapshot)) BatchRecordGuard.Validate(record);
            if (previous != null) BatchRecordGuard.ValidateTransition(previous, record);
            using var transaction = StorageDiagnostics.Begin(connection);
            if (previous == null)
            {
                var header = JsonSerializer.Serialize(record);
                Put("journal", "active", header, transaction);
                Put("batch", record.Snapshot.BatchId.ToString("N"), header, transaction);
            }
            else for (var i = 0; i < record.Results.Length; i++)
                if (previous.Results[i] != record.Results[i]) Put("result", $"{record.Snapshot.BatchId:N}:{record.Results[i].LineId}", JsonSerializer.Serialize(record.Results[i]), transaction);
            StorageDiagnostics.Commit(transaction);
            activeCache = record;
            RecordDiagnostic(DiagnosticCode.BatchSaved, record.Results.Length);
        }
        return Task.CompletedTask;
    }
    public string? ReadSetting(string key) { lock (gate) return Get("setting", key); }
    public void WriteSetting(string key, string value) { lock (gate) { Put("setting", key, value); RecordDiagnostic(DiagnosticCode.SettingsSaved); } }
    public void SaveInventory(IReadOnlyList<InventoryAsset> assets) { lock (gate) Put("data", "inventory", JsonSerializer.Serialize(assets)); }
    public IReadOnlyList<InventoryAsset> ReadInventory() { lock (gate) return JsonSerializer.Deserialize<InventoryAsset[]>(Get("data", "inventory") ?? "[]")!; }
    public IReadOnlyList<MockListing> ReadListings()
    {
        lock (gate)
        {
            if (listingCache == null)
            {
                using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT value FROM objects WHERE kind='listing' ORDER BY rowid";
                using var reader = StorageDiagnostics.Reader(cmd); var rows = new List<MockListing>();
                while (reader.Read()) rows.Add(JsonSerializer.Deserialize<MockListing>(reader.GetString(0)) ?? throw new InvalidDataException("판매 목록 손상"));
                ValidateListings(rows); listingCache = rows.ToDictionary(x => x.ListingId);
            }
            return listingCache.Values.ToArray();
        }
    }
    private static void ValidateListings(IReadOnlyList<MockListing> rows)
    {
        if (rows.Any(x => x is null || string.IsNullOrWhiteSpace(x.ListingId) || x.Quantity < 1 || x.BuyerPays < 3 || x.Attempt < 0 || !Enum.IsDefined(x.State) || !Enum.IsDefined(x.Action) || x.PendingPrice is < 3 or > 1_000_000_000) || rows.Select(x => x.ListingId).Distinct().Count() != rows.Count)
            throw new InvalidDataException("판매 목록 상태/수량/가격 오류");
    }
    public void SaveListings(IReadOnlyList<MockListing> listings)
    {
        ValidateListings(listings);
        lock (gate)
        {
            _ = ReadListings(); var previous = listingCache!; var current = listings.ToDictionary(x => x.ListingId);
            using var transaction = StorageDiagnostics.Begin(connection);
            foreach (var id in previous.Keys.Where(x => !current.ContainsKey(x)))
            { using var delete = connection.CreateCommand(); delete.Transaction = transaction; delete.CommandText = "DELETE FROM objects WHERE kind='listing' AND id=$id"; delete.Parameters.AddWithValue("$id", id); delete.ExecuteNonQuery(); }
            foreach (var listing in listings)
                if (!previous.TryGetValue(listing.ListingId, out var old) || old != listing) Put("listing", listing.ListingId, JsonSerializer.Serialize(listing), transaction);
            StorageDiagnostics.Commit(transaction); listingCache = current;
            RecordDiagnostic(DiagnosticCode.ListingsSaved, listings.Count);
        }
    }
    private BatchRecord ReadRecord(string json)
    {
        var header = BatchRecordGuard.Validate(JsonSerializer.Deserialize<BatchRecord>(json));
        var results = header.Results.ToArray(); var indices = header.Results.Select((x, i) => (x.LineId, i)).ToDictionary(x => x.LineId, x => x.i);
        var prefix = $"{header.Snapshot.BatchId:N}:";
        using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT id,value FROM objects WHERE kind='result' AND id LIKE $prefix"; cmd.Parameters.AddWithValue("$prefix", prefix + "%");
        using var rows = StorageDiagnostics.Reader(cmd);
        while (rows.Read())
        {
            var result = JsonSerializer.Deserialize<LineResult>(rows.GetString(1)) ?? throw new InvalidDataException("결과 행 손상");
            if (!indices.TryGetValue(result.LineId, out var index) || rows.GetString(0) != prefix + result.LineId) throw new InvalidDataException("결과 행 ID 오류");
            results[index] = result;
        }
        return BatchRecordGuard.Validate(header with { Results = results.ToImmutableArray() });
    }
    public void RecordDiagnostic(DiagnosticCode code, int count = 0)
    {
        DiagnosticHub.Emit(code switch{DiagnosticCode.BatchSaved=>DiagnosticEvent.JournalWrite,DiagnosticCode.SettingsSaved=>DiagnosticEvent.Settings,DiagnosticCode.StoreOpened=>DiagnosticEvent.StoreOpen,DiagnosticCode.UiFailure=>DiagnosticEvent.UiFailure,_=>DiagnosticEvent.Transaction},DiagnosticArea.Storage,count:count);
        if (!Enum.IsDefined(code) || count < 0) throw new ArgumentException("진단 코드 오류");
        lock (gate)
        {
            using var cmd = connection.CreateCommand(); cmd.CommandText = "INSERT INTO diagnostics(at,code,count) VALUES($at,$code,$count); DELETE FROM diagnostics WHERE rowid NOT IN (SELECT rowid FROM diagnostics ORDER BY rowid DESC LIMIT 500);";
            cmd.Parameters.AddWithValue("$at", DateTimeOffset.UtcNow.ToString("O")); cmd.Parameters.AddWithValue("$code", (int)code); cmd.Parameters.AddWithValue("$count", count); StorageDiagnostics.NonQuery(cmd);
        }
    }
    public IReadOnlyList<DiagnosticEntry> ReadDiagnostics()
    {
        lock (gate)
        {
            using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT at,code,count FROM diagnostics ORDER BY rowid DESC LIMIT 30";
            using var rows = StorageDiagnostics.Reader(cmd); var result = new List<DiagnosticEntry>();
            while (rows.Read()) result.Add(new(DateTimeOffset.Parse(rows.GetString(0), System.Globalization.CultureInfo.InvariantCulture), (DiagnosticCode)rows.GetInt32(1), rows.GetInt32(2)));
            return result;
        }
    }
    public LineResult? ReadEvidence(string requestKey) { lock (gate) { var json = Get("evidence", requestKey); return json == null ? null : JsonSerializer.Deserialize<LineResult>(json); } }
    public void WriteEvidence(string requestKey, LineResult result) { lock (gate) Put("evidence", requestKey, JsonSerializer.Serialize(result)); }
    public IReadOnlyList<BatchRecord> ReadBatches()
    {
        lock (gate)
        {
            using var cmd = connection.CreateCommand(); cmd.CommandText = "SELECT value FROM objects WHERE kind='batch'";
            var headers = new List<string>(); using (var rows = StorageDiagnostics.Reader(cmd)) while (rows.Read()) headers.Add(rows.GetString(0));
            return headers.Select(ReadRecord).ToArray();
        }
    }
    public void ClearDemo()
    {
        lock (gate)
        {
            if (LoadAsync(default).GetAwaiter().GetResult() is { } active && active.Results.Any(x => x.State is not (SaleState.Ready or SaleState.Listed or SaleState.Failed)))
                throw new InvalidOperationException("미확정/승인 대기 배치는 해제할 수 없습니다. Mock 결과를 먼저 대조하세요.");
            Execute("DELETE FROM objects WHERE kind='journal';");
            activeCache = null;
        }
    }
    public void DeleteDemoData()
    {
        lock (gate)
        {
            using var transaction = StorageDiagnostics.Begin(connection);
            using (var cmd = connection.CreateCommand()) { cmd.Transaction = transaction; cmd.CommandText = "DELETE FROM objects WHERE kind<>'migration';"; StorageDiagnostics.NonQuery(cmd); }
            StorageDiagnostics.Commit(transaction); Execute("VACUUM;");
            Execute("DELETE FROM diagnostics;");
            activeCache = null; listingCache = null;
        }
    }
    public void Dispose() { connection.Dispose(); lease.Dispose(); }
}
