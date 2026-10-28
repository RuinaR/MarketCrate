using MarketCrate.Core;
using MarketCrate.Infrastructure.Diagnostics;
using Microsoft.Data.Sqlite;
namespace MarketCrate.Infrastructure;

// Non-secret production preferences only. Never opens a demo database or Steam transaction journal.
public sealed class UserPreferencesStore:IDisposable
{
    private readonly SqliteConnection connection;
    public UserPreferencesStore(string directory)
    {
        Directory.CreateDirectory(directory);
        connection=new(new SqliteConnectionStringBuilder{DataSource=Path.Combine(directory,"preferences.sqlite3"),Pooling=false}.ToString());
        try
        {
            connection.Open();using var command=connection.CreateCommand();command.CommandText="PRAGMA user_version";
            var version=Convert.ToInt32(StorageDiagnostics.Scalar(command));if(version is not(0 or 1))throw new InvalidDataException("PreferencesSchemaUnsupported");
            using var transaction=StorageDiagnostics.Begin(connection);command.Transaction=transaction;command.CommandText="CREATE TABLE IF NOT EXISTS preference (key TEXT PRIMARY KEY,value INTEGER NOT NULL CHECK(value IN(0,1))); PRAGMA user_version=1";StorageDiagnostics.NonQuery(command);StorageDiagnostics.Commit(transaction);
        }
        catch{connection.Dispose();throw;}
    }
    private static void Validate(string key){if(key is not("diagnostic-mode" or "reduced-motion"))throw new ArgumentException("UnsupportedPreference");}
    public bool Read(string key,bool fallback=false)
    {
        Validate(key);using var command=connection.CreateCommand();command.CommandText="SELECT value FROM preference WHERE key=$key";command.Parameters.AddWithValue("$key",key);var value=StorageDiagnostics.Scalar(command);return value is null?fallback:Convert.ToInt32(value)==1;
    }
    public void Save(string key,bool value)
    {
        Validate(key);using var transaction=StorageDiagnostics.Begin(connection);using var command=connection.CreateCommand();command.Transaction=transaction;command.CommandText="INSERT INTO preference(key,value) VALUES($key,$value) ON CONFLICT(key) DO UPDATE SET value=excluded.value";command.Parameters.AddWithValue("$key",key);command.Parameters.AddWithValue("$value",value?1:0);StorageDiagnostics.NonQuery(command);StorageDiagnostics.Commit(transaction);
        DiagnosticHub.Emit(DiagnosticEvent.Transaction,DiagnosticArea.Storage,DiagnosticOutcome.Completed,journal:"Committed");
    }
    public void Dispose()=>connection.Dispose();
}
