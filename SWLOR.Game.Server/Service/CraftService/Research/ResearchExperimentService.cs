using System.Collections.Generic;
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
    /// ends it the same way closing the window does. The XP they earned is given once the player opens the Research window for the job or completes it.
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
    /// Pays the player the Fabrication XP the job's experiment earned, once, and survives a stop between the writes
    /// (see <see cref="ResearchExperimentPayout"/>). The XP is recorded on the player first, then the job is marked and saved, then the
    /// XP is given. Returns the XP newly recorded as paid, or 0 when none was owed. The job is saved here when it is marked.
    /// </summary>
    public static int GrantExperimentXp(uint player, ResearchJob job)
    {
        if (job?.Experiment == null || job.Experiment.Status != ResearchExperimentStatus.Settled)
            return 0;

        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? 0 : new ResearchExperimentPayout(new NwnPayoutWorld(player)).Pay(dbPlayer, job);
    }

    /// <summary>True when the job's experiment XP is still owed to the player: settled, not marked on the job and not recorded as paid.</summary>
    public static bool IsExperimentXpOwed(uint player, ResearchJob job) =>
        ResearchExperimentLifecycle.IsExperimentXpOwed(job) && !ResearchExperimentPayout.IsPaid(DB.Get<Player>(GetObjectUUID(player)), job);

    /// <summary>
    /// Deletes the job and takes its id off its player's paid-experiment set, so the set stays small. The job goes first: a stop in
    /// between leaves one unused id, never an unpaid job.
    /// </summary>
    public static void DeleteJob(ResearchJob job)
    {
        if (job == null)
            return;

        DB.Delete<ResearchJob>(job.Id);

        if (string.IsNullOrEmpty(job.PlayerId))
            return;

        var dbPlayer = DB.Get<Player>(job.PlayerId);
        if (ResearchExperimentPayout.Forget(dbPlayer, job.Id))
            DB.Set(dbPlayer);
    }

    /// <summary>
    /// Gives the player the Fabrication XP for completing a job that brought a blueprint to <paramref name="level"/>, once per job:
    /// one player save records the payout (<see cref="ResearchExperimentPayout.CompletionKey"/> in the paid set) with the XP waiting
    /// in <see cref="Player.PendingCraftXp"/>, and the pending grant gives it. A stop before the job is deleted leaves the job to be
    /// completed again (its blueprint change only reaches the character file at the next export), but the XP is not paid twice.
    /// </summary>
    public static int GrantCompletionXp(uint player, ResearchJob job, int level)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        if (dbPlayer == null || job == null)
            return 0;

        dbPlayer.PaidExperimentJobs ??= new HashSet<string>();
        var key = ResearchExperimentPayout.CompletionKey(job.Id);
        if (dbPlayer.PaidExperimentJobs.Contains(key))
        {
            CraftSessionService.GrantPendingXp(player);
            return 0;
        }

        var xp = ResearchXpRules.CompletionXp(BaseXp(player, job), level);
        dbPlayer.PaidExperimentJobs.Add(key);
        if (xp > 0)
        {
            dbPlayer.PendingCraftXp ??= new Dictionary<SkillType, int>();
            dbPlayer.PendingCraftXp[SkillType.Fabrication] = dbPlayer.PendingCraftXp.GetValueOrDefault(SkillType.Fabrication) + xp;
        }

        DB.Set(dbPlayer);
        CraftSessionService.GrantPendingXp(player);
        return xp;
    }

    /// <summary>The chart XP for the job's recipe level against the player's Fabrication rank.</summary>
    public static int BaseXp(uint player, ResearchJob job) =>
        ResearchXpRules.BaseXp(Craft.GetRecipe(job.Recipe).Level, Skill.GetCreatureSkillRank(player, SkillType.Fabrication));

    private sealed class NwnPayoutWorld : IResearchPayoutWorld
    {
        private readonly uint _player;

        public NwnPayoutWorld(uint player)
        {
            _player = player;
        }

        public bool CanReceiveXp() => !GetIsDead(_player) && GetCurrentHitPoints(_player) > 0;

        public int BaseXp(ResearchJob job) => ResearchExperimentService.BaseXp(_player, job);

        public void SavePlayer(Player dbPlayer) => DB.Set(dbPlayer);

        public void SaveJob(ResearchJob job) => DB.Set(job);

        public void GrantPendingXp() => CraftSessionService.GrantPendingXp(_player);
    }
}
