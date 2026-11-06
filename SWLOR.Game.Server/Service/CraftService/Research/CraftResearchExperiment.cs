using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// The parts of an experiment's life that need the database and the game engine: settling a player's experiments when they log in
/// or out, and giving the Fabrication XP they and their jobs earn. The rules are in <see cref="ResearchExperimentLifecycle"/>.
/// </summary>
public static class CraftResearchExperiment
{
    private static ICraftResearchExperimentService Instance => FacadeAccessor.Resolve<ICraftResearchExperimentService>();

    /// <summary>
    /// Settles every experiment of the player that is still open, with the evidence saved so far: a logout, a crash or a restart
    /// ends it the same way closing the window does. The XP they earned is given once the player opens the Research window for the job or completes it.
    /// Returns the number settled.
    /// </summary>
    public static int SettleOpenExperiments(string playerId)
    {
        return Instance.SettleOpenExperiments(playerId);
    }

    /// <summary>
    /// Settles the player's research jobs against the character file just loaded (see <see cref="ResearchJobFileRules"/>): a job whose
    /// start the file predates is discarded, since the file still holds its blueprint and credits; a job whose hand-back the file holds
    /// is deleted; a job whose hand-back the file lacks keeps its blueprint copy to be completed again. Call it once at login.
    /// </summary>
    public static void RecoverAtLogin(uint player)
    {
        Instance.RecoverAtLogin(player);
    }

    /// <summary>
    /// Stamps the job's blueprint and credit removal with the next character file sequence and saves the job. Call it before taking
    /// them, then <see cref="NwnCraftSessionWorld.PersistCharacter"/> once they are taken.
    /// </summary>
    public static void StampStart(uint player, ResearchJob job)
    {
        Instance.StampStart(player, job);
    }

    /// <summary>
    /// Ends a job whose blueprint is now in the player's inventory: stamps the hand-back, saves the job, writes the character file and
    /// only then deletes the job. A stop before the file is written leaves the job and its blueprint copy for the next login to restore.
    /// </summary>
    public static void FinishDelivery(uint player, ResearchJob job)
    {
        Instance.FinishDelivery(player, job);
    }

    /// <summary>
    /// Pays the player the Fabrication XP the job's experiment earned, once, and survives a stop between the writes
    /// (see <see cref="ResearchExperimentPayout"/>). The XP is recorded on the player first, then the job is marked and saved, then the
    /// XP is given. Returns the XP newly recorded as paid, or 0 when none was owed. The job is saved here when it is marked.
    /// </summary>
    public static int GrantExperimentXp(uint player, ResearchJob job)
    {
        return Instance.GrantExperimentXp(player, job);
    }

    /// <summary>True when the job's experiment XP is still owed to the player: settled, not marked on the job and not recorded as paid.</summary>
    public static bool IsExperimentXpOwed(uint player, ResearchJob job)
    {
        return Instance.IsExperimentXpOwed(player, job);
    }

    /// <summary>
    /// Deletes the job and takes its id off its player's paid-experiment set, so the set stays small. The job goes first: a stop in
    /// between leaves one unused id, never an unpaid job.
    /// </summary>
    public static void DeleteJob(ResearchJob job)
    {
        Instance.DeleteJob(job);
    }

    /// <summary>
    /// Gives the player the Fabrication XP for completing a job that brought a blueprint to <paramref name="level"/>, once per job:
    /// one player save records the payout (<see cref="ResearchExperimentPayout.CompletionKey"/> in the paid set) with the XP waiting
    /// in <see cref="Player.PendingCraftXp"/>, and the pending grant gives it. A stop before the job is deleted leaves the job to be
    /// completed again (its blueprint change only reaches the character file at the next export), but the XP is not paid twice.
    /// </summary>
    public static int GrantCompletionXp(uint player, ResearchJob job, int level)
    {
        return Instance.GrantCompletionXp(player, job, level);
    }

    /// <summary>The chart XP for the job's recipe level against the player's Fabrication rank.</summary>
    public static int BaseXp(uint player, ResearchJob job)
    {
        return Instance.BaseXp(player, job);
    }
}
