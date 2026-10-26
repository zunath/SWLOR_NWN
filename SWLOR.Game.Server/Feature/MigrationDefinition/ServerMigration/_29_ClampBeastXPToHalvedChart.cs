using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// Halving the beast XP chart can leave a beast holding more XP than its next level now needs.
/// Such a beast keeps its level and is set one point short of the next one; the extra XP is not refunded.
/// </summary>
public sealed class _29_ClampBeastXPToHalvedChart : IServerMigration
{
    public int Version => 29;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostDatabaseLoad;

    public void Migrate()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<Beast>());
        var count = progress.BeginSection<Beast>();
        if (count > 0)
        {
            foreach (var beast in DB.Search(new DBQuery<Beast>().AddPaging(count, 0)))
            {
                var changed = ClampXP(beast);
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
    public static bool ClampXP(Beast beast)
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
