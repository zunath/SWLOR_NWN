using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// Pays the Fabrication XP a settled experiment earned, exactly once, even if the server stops between any two of the writes.
/// <para>
/// The <see cref="ResearchJob"/> and the <see cref="Player"/> are separate records and cannot be saved together, so the player record
/// is the single source of truth for the payout. One player save does both things that make the payout real: it adds the job's id to
/// <see cref="Player.PaidExperimentJobs"/> and adds the XP to <see cref="Player.PendingCraftXp"/>. Only then is the job marked
/// (<see cref="ResearchExperimentRecord.XpGranted"/>) and saved, and the pending XP given. The pending XP is given by the craft
/// session's own path, which clears and saves the record before it gives the XP and waits while the player is dead.
/// </para>
/// <para>Invariants, by the point the server stops:</para>
/// <list type="bullet">
/// <item>Before the player save: nothing is paid and the job is unmarked, so the next open pays it.</item>
/// <item>After the player save, before the job save: the XP waits in PendingCraftXp and the id is in the set. The next open sees the id,
/// marks and saves the job, pays nothing more, and gives the waiting XP. Once, never twice.</item>
/// <item>After the job save, before the pending XP is given: the XP waits in PendingCraftXp and is given at the next respawn, login or
/// crafting window, or at the next open of this job. It stays owed and is never added again, because the set holds the id.</item>
/// <item>A job that is already in the set, or already marked, is never paid again: a repeated open only repairs the job's mark and gives
/// the waiting XP. Whatever <see cref="Player.PendingCraftXp"/> holds is given at most once by its own save-first rule.</item>
/// <item>A dead player is paid nothing and nothing is marked, so the job stays owed until they are alive. A job already in the set
/// needs no life to be flagged; its XP simply waits.</item>
/// </list>
/// The id leaves the set when the job is deleted (<see cref="Forget"/>), so the set holds only the ids of jobs that still exist. A
/// crash between deleting the job and clearing the id leaves one unused id, which is harmless.
/// </summary>
public sealed class ResearchExperimentPayout
{
    private readonly IResearchPayoutWorld _world;

    public ResearchExperimentPayout(IResearchPayoutWorld world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    /// <summary>
    /// True when the job has a settled experiment whose XP is neither marked on the job nor recorded as paid on the player, so it is
    /// still owed.
    /// </summary>
    public static bool IsOwed(Player dbPlayer, ResearchJob job) =>
        ResearchExperimentLifecycle.IsExperimentXpOwed(job) && !IsPaid(dbPlayer, job);

    /// <summary>True when the player's record says the job's experiment XP was paid.</summary>
    public static bool IsPaid(Player dbPlayer, ResearchJob job) =>
        job != null && !string.IsNullOrEmpty(job.Id) && dbPlayer?.PaidExperimentJobs != null && dbPlayer.PaidExperimentJobs.Contains(job.Id);

    /// <summary>
    /// Pays the job's experiment XP if it is owed, in the order the class comment gives, and saves the job when it marks it.
    /// Returns the XP newly recorded as paid, or 0 when none was owed.
    /// </summary>
    public int Pay(Player dbPlayer, ResearchJob job)
    {
        if (dbPlayer == null || job?.Experiment == null || job.Experiment.Status != ResearchExperimentStatus.Settled)
            return 0;

        // A terminal's job belongs to the player who started it. Another player who opens the window there (a stale Recipes window
        // left open at a shared terminal) is never paid it, and nothing is marked.
        if (job.PlayerId != dbPlayer.Id)
            return 0;

        dbPlayer.PaidExperimentJobs ??= new HashSet<string>();

        // Paid by an earlier call that stopped before it marked the job, or before it gave the XP: repair the mark and give what waits.
        if (IsPaid(dbPlayer, job))
        {
            if (!job.Experiment.XpGranted)
            {
                ResearchExperimentLifecycle.MarkExperimentXpGranted(job);
                _world.SaveJob(job);
            }

            _world.GrantPendingXp();
            return 0;
        }

        // Marked by a build that paid the XP directly.
        if (job.Experiment.XpGranted)
            return 0;

        if (!_world.CanReceiveXp())
            return 0;

        var xp = ResearchExperimentLifecycle.PendingExperimentXp(job, _world.BaseXp(job));

        // One save makes the payout real: the id and the XP together.
        dbPlayer.PaidExperimentJobs.Add(job.Id);
        if (xp > 0)
        {
            dbPlayer.PendingCraftXp ??= new Dictionary<SkillType, int>();
            dbPlayer.PendingCraftXp[SkillType.Fabrication] = dbPlayer.PendingCraftXp.GetValueOrDefault(SkillType.Fabrication) + xp;
        }

        _world.SavePlayer(dbPlayer);

        ResearchExperimentLifecycle.MarkExperimentXpGranted(job);
        _world.SaveJob(job);

        _world.GrantPendingXp();
        return xp;
    }

    /// <summary>
    /// Takes a deleted job's id off the player's set. Returns true when the set changed, so the caller saves the record.
    /// </summary>
    public static bool Forget(Player dbPlayer, string jobId)
    {
        if (string.IsNullOrEmpty(jobId) || dbPlayer?.PaidExperimentJobs == null)
            return false;

        var experiment = dbPlayer.PaidExperimentJobs.Remove(jobId);
        var completion = dbPlayer.PaidExperimentJobs.Remove(CompletionKey(jobId));
        return experiment || completion;
    }

    /// <summary>The entry in <see cref="Player.PaidExperimentJobs"/> that records a job's completion XP as paid.</summary>
    public static string CompletionKey(string jobId) => jobId + ":completion";
}
