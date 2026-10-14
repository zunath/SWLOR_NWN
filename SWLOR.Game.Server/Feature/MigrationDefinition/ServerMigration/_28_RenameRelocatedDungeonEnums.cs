using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Rewrites relocated dungeons' enum names in raw Player JSON. It runs after the database loads, before any
/// typed migration or cache load deserializes a Player with the stale names.
/// </summary>
public sealed class _28_RenameRelocatedDungeonEnums : IServerMigration
{
    public int Version => 28;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        var query = new DBQuery<Player>();
        var count = checked((int)DB.SearchCount(query));
        var changed = 0;
        var scanned = 0;
        foreach (var json in DB.SearchRawJson(query.AddPaging(count, 0)))
        {
            var player = JObject.Parse(json);
            var id = player[nameof(Player.Id)]?.Value<string>();
            try
            {
                if (RelocatedDungeonEnumMigration.MigratePlayer(player))
                {
                    foreach (var property in new[] { nameof(Player.KeyItems), nameof(Player.Quests) })
                        if (player[property] is JObject value)
                            DB.SetUnindexedJsonProperty<Player>(id, property, value.ToString(Formatting.None));
                    changed++;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Relocated dungeon enum migration failed for Player {id}.", ex);
            }
            scanned++;
            if (scanned % 100 == 0)
                Log.Write(LogGroup.Migration, $"Relocated dungeon enums: Player {scanned}/{count} scanned, {changed} changed.", true);
        }
        Log.Write(LogGroup.Migration, $"Relocated dungeon enums: Player completed, {changed}/{scanned} records changed.", true);
    }
}
