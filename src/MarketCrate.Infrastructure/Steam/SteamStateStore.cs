using System.Text.Json;
using MarketCrate.Core;
using Microsoft.Data.Sqlite;

namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamCommandRecord(string RequestKey, string Account, SaleLineSnapshot Line, string Kind, SaleState State, string? ListingId = null,
    DateTimeOffset? SentAt = null, string[]? BeforeListingIds = null, int AvailableBefore = 0, bool Accepted = false);

// Separate from Demo; no delete/reset UI and no auth material. A missing response never removes intent.
public sealed class SteamStateStore : IBatchJournal, IDisposable
{
    private readonly FileStream lease;
    private readonly SqliteConnection connection;
    private BatchRecord? batchCache;
    public SteamStateStore(string directory)
    {
        Directory.CreateDirectory(directory);
        lease = new(Path.Combine(directory, "steam-state.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        connection = new(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "steam-state.sqlite"), Pooling = false }.ToString());
        try
        {
            connection.Open(); Execute("PRAGMA synchronous=FULL");
            if (Scalar("PRAGMA quick_check") != "ok") throw new InvalidDataException("SteamStoreCorrupt");
            var version = Scalar("PRAGMA user_version");
            if (version is not ("0" or "1" or "2")) throw new InvalidDataException("SteamStoreSchemaUnsupported");
            using var transaction = connection.BeginTransaction();
            Execute("CREATE TABLE IF NOT EXISTS state(kind TEXT NOT NULL,id TEXT NOT NULL,value TEXT NOT NULL,PRIMARY KEY(kind,id))", transaction);
            Execute("PRAGMA user_version=2", transaction); transaction.Commit();
        }
        catch { connection.Dispose(); lease.Dispose(); throw; }
    }
    private void Execute(string sql, SqliteTransaction? transaction = null) { using var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction; command.ExecuteNonQuery(); }
    private string? Scalar(string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; return Convert.ToString(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture); }
    private T? Read<T>(string kind, string id)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT value FROM state WHERE kind=$k AND id=$i";
        command.Parameters.AddWithValue("$k", kind); command.Parameters.AddWithValue("$i", id);
        var raw = command.ExecuteScalar() as string;
        return raw is null ? default : JsonSerializer.Deserialize<T>(raw) ?? throw new InvalidDataException("SteamStoreRecordCorrupt");
    }
    private void Save<T>(string kind, string id, T value)
    {
        using var transaction = connection.BeginTransaction(); Upsert(kind, id, value, transaction); transaction.Commit();
    }
    private void Upsert<T>(string kind, string id, T value, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO state(kind,id,value) VALUES($k,$i,$v) ON CONFLICT(kind,id) DO UPDATE SET value=excluded.value";
        command.Parameters.AddWithValue("$k", kind); command.Parameters.AddWithValue("$i", id); command.Parameters.AddWithValue("$v", JsonSerializer.Serialize(value));
        command.ExecuteNonQuery();
    }
    public SteamCommandRecord? ReadCommand(string requestKey)
    {
        var record = Read<SteamCommandRecord>("command", requestKey);
        if (record is not null) ValidateCommand(record);
        return record;
    }
    public IReadOnlyList<SteamCommandRecord> Commands()
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT value FROM state WHERE kind='command'"; using var reader = command.ExecuteReader();
        var result = new List<SteamCommandRecord>(); while (reader.Read()) { var record = JsonSerializer.Deserialize<SteamCommandRecord>(reader.GetString(0)) ?? throw new InvalidDataException("SteamStoreRecordCorrupt"); ValidateCommand(record); result.Add(record); }
        return result;
    }
    public void SaveCommand(SteamCommandRecord command)
    {
        ValidateCommand(command);
        var previous = ReadCommand(command.RequestKey);
        if (previous is not null && (previous.Account != command.Account || previous.Line != command.Line || previous.Kind != command.Kind || previous.SentAt != command.SentAt || previous.AvailableBefore != command.AvailableBefore ||
            !(previous.BeforeListingIds ?? []).SequenceEqual(command.BeforeListingIds ?? []) ||
            previous.ListingId is not null && previous.ListingId != command.ListingId || previous.State == SaleState.Listed && command.State != previous.State))
            throw new InvalidDataException("SteamCommandImmutable");
        using var transaction=connection.BeginTransaction();Upsert("command",command.RequestKey,command,transaction);
        Audit(command.RequestKey,command.Kind=="sell"?SteamAuditAction.Sell:SteamAuditAction.Cancel,command.State.ToString(),transaction);transaction.Commit();
    }
    internal void SaveEvidence(SteamEvidenceReport report) => Save("evidence", report.RequestKey, report);
    public SteamEvidenceReport? Evidence(string key) => Read<SteamEvidenceReport>("evidence", key);
    internal void SaveReturn(SteamReturnProof proof)
    {
        using var transaction=connection.BeginTransaction();Upsert("return",proof.CancelKey,proof,transaction);Audit(proof.CancelKey,SteamAuditAction.Return,"ConfirmedAssetReturn",transaction);transaction.Commit();
    }
    private void Audit(string key,SteamAuditAction action,string state,SqliteTransaction transaction)=>Upsert("audit",Guid.NewGuid().ToString("N"),new SteamAuditEvent(key,DateTimeOffset.UtcNow,action,state),transaction);
    public IReadOnlyList<SteamAuditEvent> AuditTrail(string key)
    {
        using var query=connection.CreateCommand();query.CommandText="SELECT value FROM state WHERE kind='audit'";using var reader=query.ExecuteReader();var result=new List<SteamAuditEvent>();
        while(reader.Read()){var value=JsonSerializer.Deserialize<SteamAuditEvent>(reader.GetString(0))??throw new InvalidDataException("AuditInvalid");if(value.RequestKey==key)result.Add(value);}return result.OrderBy(x=>x.At).ToArray();
    }
    public SteamReturnProof? Return(string cancelKey) => Read<SteamReturnProof>("return", cancelKey);
    internal bool IsReleased(string sellKey)
    {
        using var query=connection.CreateCommand();query.CommandText="SELECT value FROM state WHERE kind='return'";using var reader=query.ExecuteReader();
        while(reader.Read())if(JsonSerializer.Deserialize<SteamReturnProof>(reader.GetString(0))?.SellKey==sellKey)return true;return false;
    }
    public void ArchiveCompleted()
    {
        var record=LoadAsync(default).GetAwaiter().GetResult()??throw new InvalidDataException("NoActiveBatch");
        if(record.Results.Any(x=>x.State is not(SaleState.Listed or SaleState.Failed or SaleState.Reconciled or SaleState.Ready)))throw new InvalidOperationException("UnresolvedBatch");
        using var transaction=connection.BeginTransaction();Upsert("archive",record.Snapshot.BatchId.ToString("N"),record,transaction);
        Execute("DELETE FROM state WHERE kind IN ('batch','result')",transaction);Audit(record.Snapshot.BatchId.ToString("N"),SteamAuditAction.Archive,"Preserved",transaction);transaction.Commit();batchCache=null;
    }
    internal void SaveReprice(SteamRepriceIntent intent)
    {
        var previous = Reprice(intent.Key);
        if (previous is not null && (previous.Account != intent.Account || previous.Listing != intent.Listing || previous.BuyerPrice != intent.BuyerPrice || previous.CreatedAt != intent.CreatedAt))
            throw new InvalidDataException("RepriceSnapshotImmutable");
        using var transaction=connection.BeginTransaction();Upsert("reprice",intent.Key,intent,transaction);Audit(intent.Key,SteamAuditAction.Reprice,intent.State.ToString(),transaction);transaction.Commit();
    }
    public SteamRepriceIntent? Reprice(string key) => Read<SteamRepriceIntent>("reprice", key);
    public IReadOnlyList<SteamRepriceIntent> Reprices()
    {
        using var command=connection.CreateCommand();command.CommandText="SELECT value FROM state WHERE kind='reprice'";using var reader=command.ExecuteReader();var result=new List<SteamRepriceIntent>();
        while(reader.Read())result.Add(JsonSerializer.Deserialize<SteamRepriceIntent>(reader.GetString(0))??throw new InvalidDataException("RepriceRecordInvalid"));return result;
    }
    public int Available(InventoryAsset current)
        => AvailableAll([current])[current.Key];
    public IReadOnlyDictionary<AssetKey,int> AvailableAll(IReadOnlyList<InventoryAsset> assets)
    {
        var groups=Commands().Where(x=>x.Kind=="sell").GroupBy(x=>x.Line.Asset.Key).ToDictionary(x=>x.Key,x=>x.ToArray());
        using var query = connection.CreateCommand(); query.CommandText = "SELECT value FROM state WHERE kind='return'"; using var reader = query.ExecuteReader();
        var released = new HashSet<string>(); while (reader.Read()) { var proof = JsonSerializer.Deserialize<SteamReturnProof>(reader.GetString(0))!; if (proof.SellKey is not null) released.Add(proof.SellKey); }
        return assets.ToDictionary(x=>x.Key,x=>
        {
            if(!groups.TryGetValue(x.Key,out var commands))return x.Quantity;
            var capacity=Math.Max(x.Quantity,commands.Max(c=>c.Line.Asset.Quantity));
            var reserved=commands.Where(c=>c.State!=SaleState.Failed&&!released.Contains(c.RequestKey)).Sum(c=>c.Line.Quantity);
            return Math.Max(0,Math.Min(x.Quantity,checked(capacity-reserved)));
        });
    }
    private static void ValidateCommand(SteamCommandRecord command)
    {
        if (string.IsNullOrWhiteSpace(command.RequestKey) || command.RequestKey.Length > 200 || command.Kind is not ("sell" or "cancel") ||
            command.Account is null || command.Account.Length != 17 || command.Account.Any(c => c is < '0' or > '9') || !Enum.IsDefined(command.State) ||
            command.Line is null || command.Line.Asset is null || command.Line.Quantity < 1 || command.Line.Asset.Key.AppId < 1 ||
            command.Line.Asset.Key.AssetId.Any(c => c is < '0' or > '9') || command.Line.UnitQuote.SellerReceives < 1)
            throw new InvalidDataException("SteamCommandInvalid");
    }
    public Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batchCache is not null) return Task.FromResult<BatchRecord?>(batchCache);
        var record = Read<BatchRecord>("batch", "active");
        if (record is null) return Task.FromResult<BatchRecord?>(null);
        using var command = connection.CreateCommand(); command.CommandText = "SELECT id,value FROM state WHERE kind='result'"; using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var index = int.Parse(reader.GetString(0), System.Globalization.CultureInfo.InvariantCulture);
            if (index < 0 || index >= record.Results.Length) throw new InvalidDataException("SteamResultInvalid");
            var result = JsonSerializer.Deserialize<LineResult>(reader.GetString(1)) ?? throw new InvalidDataException("SteamResultInvalid");
            record = record with { Results = record.Results.SetItem(index, result) };
        }
        batchCache = BatchRecordGuard.Validate(record); return Task.FromResult<BatchRecord?>(batchCache);
    }
    public Task SaveAsync(BatchRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); BatchRecordGuard.Validate(record);
        var old = LoadAsync(cancellationToken).GetAwaiter().GetResult(); if (old is not null) BatchRecordGuard.ValidateTransition(old, record);
        using var transaction = connection.BeginTransaction();
        if (old is null) Upsert("batch", "active", record, transaction);
        else for (var i = 0; i < record.Results.Length; i++)
            if (old.Results[i] != record.Results[i]) Upsert("result", i.ToString(System.Globalization.CultureInfo.InvariantCulture), record.Results[i], transaction);
        transaction.Commit(); batchCache = record; return Task.CompletedTask;
    }
    public void Dispose() { connection.Dispose(); lease.Dispose(); }
}
