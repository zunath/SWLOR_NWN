using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Loads every record of one type, converts each batch across worker threads and saves the changed records
/// from the calling thread. Conversion edits saved data only, with no engine or database calls, so it is safe
/// off the main thread; loads and saves stay on it because DB's caches are not thread-safe.
/// </summary>
internal sealed class ParallelStoredItemPass
{
    private const int BatchSize = 1000;
    private readonly ServerMigrationProgress _progress;

    public ParallelStoredItemPass(ServerMigrationProgress progress) => _progress = progress;

    public int Process<T>(Func<T, bool> migrate) where T : EntityBase
    {
        var count = _progress.BeginSection<T>();
        var changed = 0;
        foreach (var batch in DB.Search(new DBQuery<T>().AddPaging(count, 0)).Chunk(BatchSize))
        {
            var results = new bool[batch.Length];
            Parallel.For(0, batch.Length, index => results[index] = Migrate(batch[index], migrate));
            for (var index = 0; index < batch.Length; index++)
            {
                if (results[index])
                {
                    DB.Set(batch[index]);
                    changed++;
                }
                _progress.RecordProcessed(results[index]);
            }
        }
        _progress.FinishSection();
        return changed;
    }

    private static bool Migrate<T>(T record, Func<T, bool> migrate) where T : EntityBase
    {
        try
        {
            return migrate(record);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Stored item migration failed for {typeof(T).Name} {record.Id}.", ex);
        }
    }
}
