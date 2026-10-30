using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Removes the retired elemental Defense (<see cref="RetiredDefenseStatRevisionCatalog"/>) from stored copies and
/// gives stored droid parts and controllers the Unarmed and Twin Pistol skills
/// (<see cref="DroidWeaponSkillMigration"/>) and renames former Display Plinths, their blueprints, listings and placed
/// structures (<see cref="FurnitureRenameMigration"/>) in one stored-item pass.
/// Also sets beasts that hold more XP than the halved beast XP chart now needs for their next level one point short
/// of that level; they keep their level and the extra XP is not refunded. Recipe knowledge of each furniture set's
/// former Display Plinth moves to its renamed recipe in one pass over raw Player JSON.
/// </summary>
public sealed class _29_CorrectStoredCarapaceBracers : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        ClampBeastXP();
        RenameFurnitureRecipes();
        StoredItemSweep.Migrate(Version);
    }

    /// <summary>
    /// Rewrites the raw JSON so renamed recipe keys are saved under their new names; the typed Player
    /// reads them through <see cref="RenamedRecipesJsonConverter"/> either way.
    /// </summary>
    private void RenameFurnitureRecipes()
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
                    changed = RenameFurnitureRecipes(raw, out var player);
                    if (changed)
                        DB.Set(player);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Player recipe rename failed for Player {id}.", ex);
                }
                progress.RecordProcessed(changed);
            }
        }
        progress.FinishSection();
    }

    /// <summary>Renames one raw Player record's recipe knowledge; <paramref name="player"/> is the record to save.</summary>
    public static bool RenameFurnitureRecipes(JObject raw, out Player player)
    {
        var changed = FurnitureRenameMigration.MigratePlayer(raw);
        player = changed ? JsonConvert.DeserializeObject<Player>(raw.ToString(Formatting.None)) : null;
        return changed;
    }

    private void ClampBeastXP()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<Beast>());
        var count = progress.BeginSection<Beast>();
        if (count > 0)
        {
            foreach (var beast in DB.Search(new DBQuery<Beast>().AddPaging(count, 0)))
            {
                var changed = ClampBeastXP(beast);
                if (changed)
                    DB.Set(beast);

                progress.RecordProcessed(changed);
            }
        }

        progress.FinishSection();
    }

    /// <summary>
    /// Sets a beast's XP one point short of its next level when it holds that much or more.
    /// </summary>
    /// <param name="beast">The beast to check</param>
    /// <returns>true if the beast's XP changed</returns>
    public static bool ClampBeastXP(Beast beast)
    {
        if (beast.Level >= BeastMastery.MaxLevel)
            return false;

        var limit = BeastMastery.GetRequiredXP(beast.Level, beast.XPPenaltyPercent) - 1;
        if (beast.XP <= limit)
            return false;

        beast.XP = limit;
        return true;
    }
}
