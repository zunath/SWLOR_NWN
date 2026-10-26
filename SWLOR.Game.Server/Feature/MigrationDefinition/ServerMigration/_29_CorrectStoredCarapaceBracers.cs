using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Removes the retired elemental Defense (<see cref="RetiredDefenseStatRevisionCatalog"/>) from stored copies and
/// gives stored droid parts and controllers the Unarmed and Twin Pistol skills
/// (<see cref="DroidWeaponSkillMigration"/>) in one stored-item pass.
/// Also sets beasts that hold more XP than the halved beast XP chart now needs for their next level one point short
/// of that level; they keep their level and the extra XP is not refunded.
/// </summary>
public sealed class _29_CorrectStoredCarapaceBracers : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        ClampBeastXP();
        StoredItemSweep.Migrate(Version);
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
