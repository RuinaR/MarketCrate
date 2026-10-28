using MarketCrate.Core;
using Microsoft.Data.Sqlite;

namespace MarketCrate.Infrastructure.Diagnostics;

internal static class StorageDiagnostics
{
    private static T Observe<T>(Func<T> action)
    {
        try{return action();}
        catch(SqliteException error)
        {
            DiagnosticHub.Emit(DiagnosticEvent.Transaction,DiagnosticArea.Storage,DiagnosticOutcome.Failed,LogLevel.Error,
                error.SqliteErrorCode switch{5 or 6=>DiagnosticError.Locked,11 or 26=>DiagnosticError.Corrupt,13=>DiagnosticError.Disk,_=>DiagnosticError.Storage},
                serverCode:error.SqliteErrorCode,exception:error,journal:"JournalPreserved");throw;
        }
    }
    internal static int NonQuery(SqliteCommand command)=>Observe(command.ExecuteNonQuery);
    internal static object? Scalar(SqliteCommand command)=>Observe(command.ExecuteScalar);
    internal static SqliteDataReader Reader(SqliteCommand command)=>Observe(command.ExecuteReader);
    internal static SqliteTransaction Begin(SqliteConnection connection)
    {
        var result=Observe(connection.BeginTransaction);
        DiagnosticHub.Emit(DiagnosticEvent.Transaction,DiagnosticArea.Storage,DiagnosticOutcome.Started);return result;
    }
    internal static void Commit(SqliteTransaction transaction)
    {Observe(()=>{transaction.Commit();return true;});DiagnosticHub.Emit(DiagnosticEvent.Transaction,DiagnosticArea.Storage,journal:"Committed");}
}
