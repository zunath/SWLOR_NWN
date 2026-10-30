using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Moves recipe knowledge of each furniture set's former Display Plinth to its renamed recipe
/// (<see cref="FurnitureRenameMigration"/>), in one pass over raw Player JSON before any typed migration or cache
/// load deserializes a Player. Stored items and placed structures are renamed by <see cref="_29_CorrectStoredCarapaceBracers"/>.
/// </summary>
public sealed class _29_RenameFurnitureRecipes : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<Player>());
        var count = progress.BeginSection<Player>();
        if (count > 0)
        {
            foreach (var json in DB.SearchRawJson(new DBQuery<Player>().AddPaging(count, 0)))
            {
                var raw = JObject.Parse(json);
                var id = raw[nameof(Player.Id)]?.Value<string>();
                bool changed;
                try
                {
                    changed = MigratePlayer(raw, out var player);
                    if (changed)
                        DB.Set(player);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Player record migration failed for Player {id}.", ex);
                }
                progress.RecordProcessed(changed);
            }
        }
        progress.FinishSection();
    }

    /// <summary>Renames one raw Player record's recipe knowledge; <paramref name="player"/> is the record to save.</summary>
    public static bool MigratePlayer(JObject raw, out Player player)
    {
        var changed = FurnitureRenameMigration.MigratePlayer(raw);
        player = changed ? JsonConvert.DeserializeObject<Player>(raw.ToString(Formatting.None)) : null;
        return changed;
    }
}
