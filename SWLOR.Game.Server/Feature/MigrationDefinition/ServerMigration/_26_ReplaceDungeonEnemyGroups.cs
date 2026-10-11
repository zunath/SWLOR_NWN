using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

public sealed class _26_ReplaceDungeonEnemyGroups : IServerMigration
{
    public int Version => 26;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version,
            ServerMigrationProgress.CountRecords<Player>(),
            ServerMigrationProgress.CountRecords<DMCreature>());
        MigrateRecords<Player>(progress, DungeonEnemyGroupMigration.MigrateQuestProgress);
        MigrateRecords<DMCreature>(progress, creature =>
        {
            if (!DungeonEnemyGroupMigration.MigrateCreature(creature.Data, out var migrated)) return false;
            creature.Data = migrated;
            return true;
        });
    }

    private static void MigrateRecords<T>(ServerMigrationProgress progress, Func<T, bool> migrate) where T : EntityBase
    {
        var query = new DBQuery<T>();
        var count = progress.BeginSection<T>();
        var records = DB.Search(query.AddPaging(count, 0));
        var changed = 0;
        foreach (var record in records)
        {
            try
            {
                var recordChanged = migrate(record);
                if (recordChanged)
                {
                    DB.Set(record);
                    changed++;
                }
                progress.RecordProcessed(recordChanged);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Dungeon enemy group migration failed for {typeof(T).Name} {record.Id}.", ex);
            }
        }
        progress.FinishSection();
        Log.Write(LogGroup.Migration, $"Dungeon enemy groups: {typeof(T).Name} completed, {changed}/{count} records changed.", true);
    }
}
