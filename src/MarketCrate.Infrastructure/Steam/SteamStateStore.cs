using MarketCrate.Infrastructure.Diagnostics;
using System.Text.Json;
using MarketCrate.Core;
using Microsoft.Data.Sqlite;

namespace MarketCrate.Infrastructure.Steam;

public sealed record SteamCommandRecord(string RequestKey, string Account, SaleLineSnapshot Line, string Kind, SaleState State, string? ListingId = null,
    DateTimeOffset? SentAt = null, string[]? BeforeListingIds = null, int AvailableBefore = 0, bool Accepted = false)
{public SteamApprovalChannel Approval{get;init;}}

// Separate from Demo; no delete/reset UI and no auth material. A missing response never removes intent.
public sealed partial class SteamStateStore : IBatchHistoryJournal, IDisposable
{
    private readonly FileStream lease;
    private readonly SqliteConnection connection;
    private BatchRecord? batchCache;
    internal SteamAssetOperationGuard AssetOperations{get;}=new();
    internal SemaphoreSlim Mutations{get;}=new(1,1);
    public SemaphoreSlim ExecutionGate{get;}=new(1,1);
    public SteamStateStore(string directory)
    {
        using var diagnosticOperation=DiagnosticHub.Operation(DiagnosticEvent.StoreOpen,DiagnosticArea.Storage,"SYS-02");
        Directory.CreateDirectory(directory);
        lease = new(Path.Combine(directory, "steam-state.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        connection = new(new SqliteConnectionStringBuilder { DataSource = Path.Combine(directory, "steam-state.sqlite"), Pooling = false }.ToString());
        try
        {
            DiagnosticHub.Emit(DiagnosticEvent.StoreOpen,DiagnosticArea.Storage,DiagnosticOutcome.Started); connection.Open(); Execute("PRAGMA synchronous=FULL");
            if (Scalar("PRAGMA quick_check") != "ok") throw new InvalidDataException("SteamStoreCorrupt");
            var version = Scalar("PRAGMA user_version");
            DiagnosticHub.Emit(DiagnosticEvent.Migration,DiagnosticArea.Storage,DiagnosticOutcome.Started,count:Convert.ToInt32(version,System.Globalization.CultureInfo.InvariantCulture));
            if (version is not ("0" or "1" or "2" or "3")) throw new InvalidDataException("SteamStoreSchemaUnsupported");
            using var transaction = StorageDiagnostics.Begin(connection);
            Execute("CREATE TABLE IF NOT EXISTS state(kind TEXT NOT NULL,id TEXT NOT NULL,value TEXT NOT NULL,PRIMARY KEY(kind,id))", transaction);
            Execute("PRAGMA user_version=3", transaction); StorageDiagnostics.Commit(transaction);
        }
        catch(Exception error) { DiagnosticHub.Emit(DiagnosticEvent.StoreOpen,DiagnosticArea.Storage,DiagnosticOutcome.Failed,LogLevel.Error,DiagnosticError.Storage,exception:error); connection.Dispose(); lease.Dispose(); throw; }
    }
    private void Execute(string sql, SqliteTransaction? transaction = null) { using var command = connection.CreateCommand(); command.CommandText = sql; command.Transaction = transaction; StorageDiagnostics.NonQuery(command); }
    private string? Scalar(string sql) { using var command = connection.CreateCommand(); command.CommandText = sql; return Convert.ToString(StorageDiagnostics.Scalar(command), System.Globalization.CultureInfo.InvariantCulture); }
    private T? Read<T>(string kind, string id)
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT value FROM state WHERE kind=$k AND id=$i";
        command.Parameters.AddWithValue("$k", kind); command.Parameters.AddWithValue("$i", id);
        var raw = StorageDiagnostics.Scalar(command) as string;
        return raw is null ? default : JsonSerializer.Deserialize<T>(raw) ?? throw new InvalidDataException("SteamStoreRecordCorrupt");
    }
    private void Save<T>(string kind, string id, T value)
    {
        using var transaction = StorageDiagnostics.Begin(connection); Upsert(kind, id, value, transaction); StorageDiagnostics.Commit(transaction);
    }
    private void Upsert<T>(string kind, string id, T value, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "INSERT INTO state(kind,id,value) VALUES($k,$i,$v) ON CONFLICT(kind,id) DO UPDATE SET value=excluded.value";
        command.Parameters.AddWithValue("$k", kind); command.Parameters.AddWithValue("$i", id); command.Parameters.AddWithValue("$v", JsonSerializer.Serialize(value));
        StorageDiagnostics.NonQuery(command);
    }
    public IReadOnlyList<InventoryScope> ReadCatalog(string account)
    {
        if(account.Length!=17||account.Any(c=>!char.IsAsciiDigit(c)))throw new ArgumentException("CatalogAccount");
        var scopes=Read<InventoryScope[]>("catalog",account)??[];foreach(var scope in scopes)scope.Validate();if(scopes.Length>100||scopes.Select(x=>(x.AppId,x.ContextId)).Distinct().Count()!=scopes.Length)throw new InvalidDataException("CatalogInvalid");return scopes;
    }
    public void SaveCatalog(string account,IReadOnlyList<InventoryScope> scopes)
    {
        if(account.Length!=17||account.Any(c=>!char.IsAsciiDigit(c))||scopes.Count>100||scopes.Select(x=>(x.AppId,x.ContextId)).Distinct().Count()!=scopes.Count)throw new ArgumentException("CatalogInvalid");foreach(var scope in scopes)scope.Validate();
        Save("catalog",account,scopes);DiagnosticHub.Emit(DiagnosticEvent.GameCatalog,DiagnosticArea.Storage,count:scopes.Count,journal:"Committed",requirement:"C02");
    }
    public void AddRequestedTbhOnce(string account,IReadOnlyList<InventoryScope> defaults)
    {
        var scopes=ReadCatalog(account).ToList();var marker=account+":tbh-v1";
        if(Read<bool>("catalog-request",marker))return;
        if(scopes.Count==0)scopes.AddRange(defaults);
        if(!scopes.Any(x=>x.AppId==3678970&&x.ContextId=="2"))scopes.Add(new(3678970,"2","TBH: Task Bar Hero"){Pinned=true});
        if(scopes.Count>100)return;foreach(var scope in scopes)scope.Validate();
        using var transaction=StorageDiagnostics.Begin(connection);Upsert("catalog",account,scopes,transaction);Upsert("catalog-request",marker,true,transaction);StorageDiagnostics.Commit(transaction);
        DiagnosticHub.Emit(DiagnosticEvent.GameCatalog,DiagnosticArea.Storage,count:scopes.Count,journal:"Committed",reason:"UserRequested",requirement:"C02");
    }
    public SteamCommandRecord? ReadCommand(string requestKey)
    {
        var record = Read<SteamCommandRecord>("command", requestKey);
        if (record is not null) ValidateCommand(record);
        return record;
    }
    public IReadOnlyList<SteamCommandRecord> Commands()
    {
        using var command = connection.CreateCommand(); command.CommandText = "SELECT value FROM state WHERE kind='command'"; using var reader = StorageDiagnostics.Reader(command);
        var result = new List<SteamCommandRecord>(); while (reader.Read()) { var record = JsonSerializer.Deserialize<SteamCommandRecord>(reader.GetString(0)) ?? throw new InvalidDataException("SteamStoreRecordCorrupt"); ValidateCommand(record); result.Add(record); }
        return result;
    }
    public void SaveCommand(SteamCommandRecord command)
    {
        using var diagnosticScope=DiagnosticHub.Scope(item:command.RequestKey);
        DiagnosticHub.Emit(DiagnosticEvent.JournalWrite,DiagnosticArea.Storage,DiagnosticOutcome.Started,to:command.State.ToString());
        ValidateCommand(command);
        var previous = ReadCommand(command.RequestKey);
        if (previous is not null && (previous.Account != command.Account || previous.Line != command.Line || previous.Kind != command.Kind || previous.SentAt != command.SentAt || previous.AvailableBefore != command.AvailableBefore ||
            !(previous.BeforeListingIds ?? []).SequenceEqual(command.BeforeListingIds ?? []) ||
            previous.ListingId is not null && previous.ListingId != command.ListingId || previous.Approval!=SteamApprovalChannel.None&&previous.Approval!=command.Approval || previous.State == SaleState.Listed && command.State != previous.State))
            throw new InvalidDataException("SteamCommandImmutable");
        using var transaction=StorageDiagnostics.Begin(connection);Upsert("command",command.RequestKey,command,transaction);
        Audit(command.RequestKey,command.Kind=="sell"?SteamAuditAction.Sell:SteamAuditAction.Cancel,command.State.ToString(),transaction);StorageDiagnostics.Commit(transaction);
    }
    internal void SaveEvidence(SteamEvidenceReport report) => Save("evidence", report.RequestKey, report);
    public SteamEvidenceReport? Evidence(string key) => Read<SteamEvidenceReport>("evidence", key);
    internal void SaveReturn(SteamReturnProof proof)
    {
        using var transaction=StorageDiagnostics.Begin(connection);Upsert("return",proof.CancelKey,proof,transaction);Audit(proof.CancelKey,SteamAuditAction.Return,"ConfirmedAssetReturn",transaction);StorageDiagnostics.Commit(transaction);
    }
    private void Audit(string key,SteamAuditAction action,string state,SqliteTransaction transaction)=>Upsert("audit",Guid.NewGuid().ToString("N"),new SteamAuditEvent(key,DateTimeOffset.UtcNow,action,state),transaction);
    public IReadOnlyList<SteamAuditEvent> AuditTrail(string key)
    {
        using var query=connection.CreateCommand();query.CommandText="SELECT value FROM state WHERE kind='audit'";using var reader=StorageDiagnostics.Reader(query);var result=new List<SteamAuditEvent>();
        while(reader.Read()){var value=JsonSerializer.Deserialize<SteamAuditEvent>(reader.GetString(0))??throw new InvalidDataException("AuditInvalid");if(value.RequestKey==key)result.Add(value);}return result.OrderBy(x=>x.At).ToArray();
    }
    public SteamReturnProof? Return(string cancelKey) => Read<SteamReturnProof>("return", cancelKey);
    internal bool IsReleased(string sellKey)
    {
        using var query=connection.CreateCommand();query.CommandText="SELECT value FROM state WHERE kind='return'";using var reader=StorageDiagnostics.Reader(query);
        while(reader.Read())if(JsonSerializer.Deserialize<SteamReturnProof>(reader.GetString(0))?.SellKey==sellKey)return true;return false;
    }
    public void ArchiveCompleted(string? account=null)
    {
        var record=LoadAsync(default).GetAwaiter().GetResult()??throw new InvalidDataException("NoActiveBatch");
        if(account is not null&&record.Snapshot.AccountReference!=account)throw new SteamException(SteamFailure.Restricted);
        if(record.Results.Any(x=>x.State is not(SaleState.Listed or SaleState.Failed or SaleState.Reconciled or SaleState.Ready)))throw new InvalidOperationException("UnresolvedBatch");
        using var transaction=StorageDiagnostics.Begin(connection);Upsert("archive",record.Snapshot.BatchId.ToString("N"),record,transaction);
        Execute("DELETE FROM state WHERE kind IN ('batch','result')",transaction);Audit(record.Snapshot.BatchId.ToString("N"),SteamAuditAction.Archive,"Preserved",transaction);StorageDiagnostics.Commit(transaction);batchCache=null;
    }
    internal void SaveReprice(SteamRepriceIntent intent)
    {
        var previous = Reprice(intent.Key);
        if (previous is not null && (previous.Account != intent.Account || previous.Listing != intent.Listing || previous.BuyerPrice != intent.BuyerPrice || previous.OrderbookDigest != intent.OrderbookDigest || previous.CreatedAt != intent.CreatedAt || previous.CancellationKey != intent.CancellationKey || previous.SupersededBy != intent.SupersededBy || previous.State==SteamRepriceState.Superseded))
            throw new InvalidDataException("RepriceSnapshotImmutable");
        using var transaction=StorageDiagnostics.Begin(connection);Upsert("reprice",intent.Key,intent,transaction);Audit(intent.Key,SteamAuditAction.Reprice,intent.State.ToString(),transaction);StorageDiagnostics.Commit(transaction);
    }
    internal void SupersedeReprice(SteamRepriceIntent previous,SteamRepriceIntent next)
    {
        if(Reprice(previous.Key)!=previous||previous.ReplacementKey is not null||previous.State==SteamRepriceState.Superseded||Reprice(next.Key) is not null||
            next.Account!=previous.Account||next.Listing!=previous.Listing||next.CancellationKey!=(previous.CancellationKey??previous.Key+":cancel")||next.Key==previous.Key)
            throw new InvalidDataException("RepriceRevisionConflict");
        using var transaction=StorageDiagnostics.Begin(connection);
        Upsert("reprice",previous.Key,previous with{State=SteamRepriceState.Superseded,SupersededBy=next.Key},transaction);
        Upsert("reprice",next.Key,next,transaction);
        Audit(previous.Key,SteamAuditAction.Reprice,"Superseded",transaction);Audit(next.Key,SteamAuditAction.Reprice,next.State.ToString(),transaction);
        StorageDiagnostics.Commit(transaction);DiagnosticHub.Emit(DiagnosticEvent.Reprice,DiagnosticArea.Storage,reason:"Superseded",journal:"Committed");
    }
    public SteamRepriceIntent? Reprice(string key) => Read<SteamRepriceIntent>("reprice", key);
    public IReadOnlyList<SteamRepriceIntent> Reprices()
    {
        using var command=connection.CreateCommand();command.CommandText="SELECT value FROM state WHERE kind='reprice'";using var reader=StorageDiagnostics.Reader(command);var result=new List<SteamRepriceIntent>();
        while(reader.Read())result.Add(JsonSerializer.Deserialize<SteamRepriceIntent>(reader.GetString(0))??throw new InvalidDataException("RepriceRecordInvalid"));return result;
    }
    public int Available(InventoryAsset current)
        => AvailableAll([current])[current.Key];
    public IReadOnlyList<SteamCommandRecord> ReservedSells()
    {
        var released=ReleasedSellKeys();
        return Commands().Where(x=>x.Kind=="sell"&&x.State!=SaleState.Failed&&!released.Contains(x.RequestKey)).ToArray();
    }
    private HashSet<string> ReleasedSellKeys()
    {
        using var query=connection.CreateCommand();query.CommandText="SELECT value FROM state WHERE kind='return'";using var reader=StorageDiagnostics.Reader(query);
        var released=new HashSet<string>();while(reader.Read()){var proof=JsonSerializer.Deserialize<SteamReturnProof>(reader.GetString(0))!;if(proof.SellKey is not null)released.Add(proof.SellKey);}return released;
    }
    public IReadOnlyDictionary<AssetKey,int> AvailableAll(IReadOnlyList<InventoryAsset> assets)
    {
        var groups=Commands().Where(x=>x.Kind=="sell").GroupBy(x=>x.Line.Asset.Key).ToDictionary(x=>x.Key,x=>x.ToArray());
        var released=ReleasedSellKeys();
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
            command.Account is null || command.Account.Length != 17 || command.Account.Any(c => c is < '0' or > '9') || !Enum.IsDefined(command.State) || !Enum.IsDefined(command.Approval) ||
            command.Line is null || command.Line.Asset is null || command.Line.Quantity < 1 || command.Line.Asset.Key.AppId < 1 ||
            command.Line.Asset.Key.AssetId.Any(c => c is < '0' or > '9') || command.Line.UnitQuote.SellerReceives < 1)
            throw new InvalidDataException("SteamCommandInvalid");
    }
    public async Task<BatchRecord?> FindBatchAsync(Guid batchId,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current=await LoadAsync(cancellationToken);
        var found=current?.Snapshot.BatchId==batchId?current:Read<BatchRecord>("archive",batchId.ToString("N"));
        return found is null?null:BatchRecordGuard.Validate(found);
    }
    public Task<BatchRecord?> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (batchCache is not null) return Task.FromResult<BatchRecord?>(batchCache);
        var record = Read<BatchRecord>("batch", "active");
        if (record is null) return Task.FromResult<BatchRecord?>(null);
        using var command = connection.CreateCommand(); command.CommandText = "SELECT id,value FROM state WHERE kind='result'"; using var reader = StorageDiagnostics.Reader(command);
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
        var old = LoadAsync(cancellationToken).GetAwaiter().GetResult();
        var newBatch=old is not null&&old.Snapshot.BatchId!=record.Snapshot.BatchId;
        if(newBatch&&Read<BatchRecord>("archive",record.Snapshot.BatchId.ToString("N")) is not null)throw new InvalidDataException("ArchivedBatchImmutable");
        if (old is not null&&!newBatch) BatchRecordGuard.ValidateTransition(old, record);
        SteamRelistPlan? relist=null;
        if((old is null||newBatch)&&relistApproval is {} approval&&approval.Snapshot.BatchId==record.Snapshot.BatchId)
        {
            if(approval.Snapshot!=record.Snapshot)throw new InvalidDataException("RelistApprovalMismatch");
            relist=Read<SteamRelistPlan>("relist",approval.Key)??throw new InvalidDataException("RelistPlanMissing");
            if(relist.SubmissionBatch is not null||relist.Account!=record.Snapshot.AccountReference)throw new InvalidDataException("RelistApprovalMismatch");
        }
        using var transaction = StorageDiagnostics.Begin(connection);
        if(newBatch)
        {
            Upsert("archive",old!.Snapshot.BatchId.ToString("N"),old,transaction);
            Execute("DELETE FROM state WHERE kind='result'",transaction);
            Audit(old.Snapshot.BatchId.ToString("N"),SteamAuditAction.Archive,"PreservedWithoutReplay",transaction);
        }
        if (old is null||newBatch) Upsert("batch", "active", record, transaction);
        else for (var i = 0; i < record.Results.Length; i++)
            if (old.Results[i] != record.Results[i]) Upsert("result", i.ToString(System.Globalization.CultureInfo.InvariantCulture), record.Results[i], transaction);
        if(relist is not null)Upsert("relist",relist.Key,relist with{SubmissionBatch=record.Snapshot.BatchId},transaction);
        StorageDiagnostics.Commit(transaction); batchCache = record;
        if(newBatch)DiagnosticHub.Emit(DiagnosticEvent.BatchRecovered,DiagnosticArea.Storage,DiagnosticOutcome.Completed,reason:"PreservedWithoutReplay",journal:"Committed",requirement:"F10");
        if(relist is not null){relistApproval=null;DiagnosticHub.Emit(DiagnosticEvent.Reprice,DiagnosticArea.Storage,journal:"Committed",requirement:"B10",reason:"SubmissionStarted");}
        return Task.CompletedTask;
    }
    public void Dispose() { connection.Dispose(); lease.Dispose(); }
}
