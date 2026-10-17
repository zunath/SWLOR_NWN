using System;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Loads every record of one type once, migrates it and saves the records that changed.</summary>
internal interface IStoredItemPass
{
    int Process<T>(Func<T, bool> migrate) where T : EntityBase;
}

/// <summary>The in-server pass: one record at a time on the main thread.</summary>
internal sealed class ServerStoredItemPass : IStoredItemPass
{
    private readonly ServerMigrationProgress _progress;

    public ServerStoredItemPass(ServerMigrationProgress progress) => _progress = progress;

    public int Process<T>(Func<T, bool> migrate) where T : EntityBase
    {
        var count = _progress.BeginSection<T>();
        var changed = 0;
        foreach (var record in DB.Search(new DBQuery<T>().AddPaging(count, 0)))
        {
            var recordChanged = Migrate(record, migrate);
            if (recordChanged)
            {
                DB.Set(record);
                changed++;
            }
            _progress.RecordProcessed(recordChanged);
        }
        _progress.FinishSection();
        return changed;
    }

    internal static bool Migrate<T>(T record, Func<T, bool> migrate) where T : EntityBase
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

/// <summary>
/// The offline pass: converts each batch of records across worker threads, then saves the changed ones
/// from the calling thread, because DB's caches are not thread-safe.
/// </summary>
internal sealed class OfflineStoredItemPass : IStoredItemPass
{
    private readonly ServerMigrationProgress _progress;
    private readonly OfflineMigrationOptions _options;

    public OfflineStoredItemPass(ServerMigrationProgress progress, OfflineMigrationOptions options)
    {
        _progress = progress;
        _options = options;
    }

    public int Process<T>(Func<T, bool> migrate) where T : EntityBase
    {
        var count = _progress.BeginSection<T>();
        var changed = 0;
        var parallel = new ParallelOptions { MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism };
        foreach (var batch in DB.Search(new DBQuery<T>().AddPaging(count, 0)).Chunk(_options.BatchSize))
        {
            var results = new bool[batch.Length];
            Parallel.For(0, batch.Length, parallel, index => results[index] = ServerStoredItemPass.Migrate(batch[index], migrate));
            for (var index = 0; index < batch.Length; index++)
            {
                if (results[index])
                {
                    if (!_options.DryRun)
                        DB.Set(batch[index]);
                    changed++;
                }
                _progress.RecordProcessed(results[index]);
            }
        }
        _progress.FinishSection();
        return changed;
    }
}
