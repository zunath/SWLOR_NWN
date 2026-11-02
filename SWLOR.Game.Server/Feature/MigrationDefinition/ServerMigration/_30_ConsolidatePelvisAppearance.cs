using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Consolidates pelvis appearances in stored items, and moves quest kill progress of the renamed capstone NPC groups
/// (<see cref="CapstoneNpcGroupRenameMigration"/>) to their new names in one pass over raw Player JSON.
/// </summary>
public sealed class _30_ConsolidatePelvisAppearance : IServerMigration
{
    public int Version => 30;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        RenameCapstoneNpcGroups();
        StoredItemSweep.MigratePelvis(Version);
    }

    /// <summary>
    /// Rewrites the raw JSON so renamed group keys are saved under their new names; the typed Player
    /// reads them through <see cref="RenamedNpcGroupsJsonConverter"/> either way.
    /// </summary>
    private void RenameCapstoneNpcGroups()
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
                    changed = RenameCapstoneNpcGroups(raw, out var player);
                    if (changed)
                        DB.Set(player);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Player capstone group rename failed for Player {id}.", ex);
                }
                progress.RecordProcessed(changed);
            }
        }
        progress.FinishSection();
    }

    /// <summary>Renames one raw Player record's kill progress; <paramref name="player"/> is the record to save.</summary>
    public static bool RenameCapstoneNpcGroups(JObject raw, out Player player)
    {
        var changed = CapstoneNpcGroupRenameMigration.MigratePlayer(raw);
        player = changed ? JsonConvert.DeserializeObject<Player>(raw.ToString(Formatting.None)) : null;
        return changed;
    }
}
