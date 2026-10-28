using System.Text.Json;
using MarketCrate.Core;
using Microsoft.Data.Sqlite;

namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamCommandRecord(string RequestKey, string Account, SaleLineSnapshot Line, string Kind, SaleState State, string? ListingId = null);

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
            if (version is not ("0" or "1")) throw new InvalidDataException("SteamStoreSchemaUnsupported");
            using var transaction = connection.BeginTransaction();
            Execute("CREATE TABLE IF NOT EXISTS state(kind TEXT NOT NULL,id TEXT NOT NULL,value TEXT NOT NULL,PRIMARY KEY(kind,id))", transaction);
            Execute("PRAGMA user_version=1", transaction); transaction.Commit();
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
        if (previous is not null && (previous.Account != command.Account || previous.Line != command.Line || previous.Kind != command.Kind ||
            previous.ListingId is not null && previous.ListingId != command.ListingId || previous.State == SaleState.Listed && command.State != previous.State))
            throw new InvalidDataException("SteamCommandImmutable");
        Save("command", command.RequestKey, command);
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
