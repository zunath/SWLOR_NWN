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
        MigrateRecords<Player>(DungeonEnemyGroupMigration.MigrateQuestProgress);
        MigrateRecords<DMCreature>(creature =>
        {
            if (!DungeonEnemyGroupMigration.MigrateCreature(creature.Data, out var migrated)) return false;
            creature.Data = migrated;
            return true;
        });
    }

    private static void MigrateRecords<T>(Func<T, bool> migrate) where T : EntityBase
    {
        var query = new DBQuery<T>();
        var count = checked((int)DB.SearchCount(query));
        var records = DB.Search(query.AddPaging(count, 0));
        var changed = 0;
        foreach (var record in records)
        {
            try
            {
                if (!migrate(record)) continue;
                DB.Set(record);
                changed++;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Dungeon enemy group migration failed for {typeof(T).Name} {record.Id}.", ex);
            }
        }
        Log.Write(LogGroup.Migration, $"Dungeon enemy groups: {typeof(T).Name} completed, {changed}/{count} records changed.", true);
    }
}
