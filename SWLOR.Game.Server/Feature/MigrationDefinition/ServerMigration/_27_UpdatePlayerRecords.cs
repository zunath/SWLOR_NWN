using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Renames relocated dungeons' enum names and snapshots every character's equipped techniques into a
/// "Default" saved build, in one pass over raw Player JSON. It runs after the database loads, before any
/// typed migration or cache load deserializes a Player with the stale names. Stored items are updated by
/// <see cref="_27_UpdateStoredItems"/> once caches load.
/// </summary>
public sealed class _27_UpdatePlayerRecords : IServerMigration
{
    public int Version => 27;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<Player>());
        var count = progress.BeginSection<Player>();
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
        progress.FinishSection();
    }

    /// <summary>Applies both repairs to one raw Player record; the result is saved once when either changed it.</summary>
    public static bool MigratePlayer(JObject raw, out Player player)
    {
        var changed = RelocatedDungeonEnumMigration.MigratePlayer(raw);
        player = JsonConvert.DeserializeObject<Player>(raw.ToString(Formatting.None));
        changed |= CreateDefaultMimicryLoadout(player);
        return changed;
    }

    public static bool CreateDefaultMimicryLoadout(Player player)
    {
        player.MimicryLoadouts ??= new List<MimicryLoadout>();
        if (player.MimicryLoadouts.Count > 0)
            return false;

        var loadout = new MimicryLoadout
        {
            Name = MimicryLoadouts.StartingBuildName,
            Techniques = new List<FeatType>(player.EquippedTechniques)
        };
        player.MimicryLoadouts.Add(loadout);
        player.ActiveMimicryLoadoutId = loadout.Id;
        return true;
    }
}
