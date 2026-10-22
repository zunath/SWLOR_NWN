using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// The parts of an experiment's life that need the database and the game engine: settling a player's experiments when they log in
/// or out, and giving the Fabrication XP they and their jobs earn. The rules are in <see cref="ResearchExperimentLifecycle"/>.
/// </summary>
public static class ResearchExperimentService
{
    /// <summary>
    /// Settles every experiment of the player that is still open, with the evidence saved so far: a logout, a crash or a restart
    /// ends it the same way closing the window does. The XP they earned is given when the job is next opened or completed.
    /// Returns the number settled.
    /// </summary>
    public static int SettleOpenExperiments(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return 0;

        var query = new DBQuery<ResearchJob>()
            .AddFieldSearch(nameof(ResearchJob.PlayerId), playerId, false);

        var settled = 0;
        foreach (var job in DB.Search(query).ToList())
        {
            if (!ResearchExperimentLifecycle.Settle(job, ServiceMinigameRandom.Instance))
                continue;

            DB.Set(job);
            settled++;
        }

        return settled;
    }

    /// <summary>
    /// Gives the player the Fabrication XP the job's experiment earned, once. Returns the XP given, or 0 when none is owed.
    /// The caller saves the job.
    /// </summary>
    public static int GrantExperimentXp(uint player, ResearchJob job)
    {
        if (job?.Experiment == null || job.Experiment.Status != ResearchExperimentStatus.Settled || job.Experiment.XpGranted)
            return 0;

        var xp = ResearchExperimentLifecycle.PendingExperimentXp(job, BaseXp(player, job));
        ResearchExperimentLifecycle.MarkExperimentXpGranted(job);
        if (xp > 0)
            Skill.GiveSkillXP(player, SkillType.Fabrication, xp);

        return xp;
    }

    /// <summary>Gives the player the Fabrication XP for completing a job that brought a blueprint to <paramref name="level"/>.</summary>
    public static int GrantCompletionXp(uint player, ResearchJob job, int level)
    {
        var xp = ResearchXpRules.CompletionXp(BaseXp(player, job), level);
        if (xp > 0)
            Skill.GiveSkillXP(player, SkillType.Fabrication, xp);

        return xp;
    }

    /// <summary>The chart XP for the job's recipe level against the player's Fabrication rank.</summary>
    public static int BaseXp(uint player, ResearchJob job) =>
        ResearchXpRules.BaseXp(Craft.GetRecipe(job.Recipe).Level, Skill.GetCreatureSkillRank(player, SkillType.Fabrication));
}
