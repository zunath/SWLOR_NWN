using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Crafting.Research;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <inheritdoc cref="ICraftResearchExperimentService" />
public class CraftResearchExperimentService : ICraftResearchExperimentService
{
    private readonly IDBService _db;
    private readonly ICraftService _craft;
    private readonly ICraftSessionsService _sessions;
    private readonly ISkillService _skill;

    public CraftResearchExperimentService(IDBService db, ICraftService craft, ICraftSessionsService sessions, ISkillService skill)
    {
        _db = db;
        _craft = craft;
        _sessions = sessions;
        _skill = skill;
    }

    /// <inheritdoc />
    public int SettleOpenExperiments(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            return 0;

        var query = new DBQuery<ResearchJob>()
            .AddFieldSearch(nameof(ResearchJob.PlayerId), playerId, false);

        var settled = 0;
        foreach (var job in _db.Search(query).ToList())
        {
            if (!ResearchExperimentLifecycle.Settle(job, ServiceMinigameRandom.Instance))
                continue;

            _db.Set(job);
            settled++;
        }

        return settled;
    }

    /// <inheritdoc />
    public void RecoverAtLogin(uint player)
    {
        var playerId = GetObjectUUID(player);
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        var loaded = GetLocalInt(player, CraftSessionFileSequence.LoadedVariable);
        var query = new DBQuery<ResearchJob>()
            .AddFieldSearch(nameof(ResearchJob.PlayerId), playerId, false);

        foreach (var job in _db.Search(query).ToList())
        {
            switch (ResearchJobFileRules.AtLogin(job, loaded))
            {
                case ResearchJobFileState.StartNotInFile:
                    WriteLog($"{GetName(player)} ({playerId}) had research job {job.Id} discarded: the character file (sequence {loaded}) predates its start (sequence {job.StartSequence}).");
                    DeleteJob(job);
                    break;
                case ResearchJobFileState.DeliveredInFile:
                    DeleteJob(job);
                    break;
                case ResearchJobFileState.DeliveryNotInFile:
                    WriteLog($"{GetName(player)} ({playerId}) had research job {job.Id} restored: the character file (sequence {loaded}) predates its blueprint hand-back (sequence {job.DeliverySequence}).");
                    job.DeliverySequence = 0;
                    _db.Set(job);
                    break;
            }
        }
    }

    /// <inheritdoc />
    public void StampStart(uint player, ResearchJob job)
    {
        job.StartSequence = _craft.SessionWorld.AdvanceFileSequence(player);
        _db.Set(job);
    }

    /// <inheritdoc />
    public void FinishDelivery(uint player, ResearchJob job)
    {
        job.DeliverySequence = _craft.SessionWorld.AdvanceFileSequence(player);
        _db.Set(job);
        _craft.SessionWorld.FlushCharacter(player);
        DeleteJob(job);
    }

    /// <inheritdoc />
    public int GrantExperimentXp(uint player, ResearchJob job)
    {
        if (job?.Experiment == null || job.Experiment.Status != ResearchExperimentStatus.Settled)
            return 0;

        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? 0 : new ResearchExperimentPayout(new NwnPayoutWorld(player)).Pay(dbPlayer, job);
    }

    /// <summary>True when the job's experiment XP is still owed to the player: settled, not marked on the job and not recorded as paid.</summary>
    public bool IsExperimentXpOwed(uint player, ResearchJob job) =>
        ResearchExperimentLifecycle.IsExperimentXpOwed(job) && !ResearchExperimentPayout.IsPaid(_db.Get<Player>(GetObjectUUID(player)), job);

    /// <inheritdoc />
    public void DeleteJob(ResearchJob job)
    {
        if (job == null)
            return;

        _db.Delete<ResearchJob>(job.Id);

        if (string.IsNullOrEmpty(job.PlayerId))
            return;

        var dbPlayer = _db.Get<Player>(job.PlayerId);
        if (ResearchExperimentPayout.Forget(dbPlayer, job.Id))
            _db.Set(dbPlayer);
    }

    /// <inheritdoc />
    public int GrantCompletionXp(uint player, ResearchJob job, int level)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        if (dbPlayer == null || !ResearchExperimentLifecycle.IsOwnedBy(job, dbPlayer.Id))
            return 0;

        dbPlayer.PaidExperimentJobs ??= new HashSet<string>();
        var key = ResearchExperimentPayout.CompletionKey(job.Id);
        if (dbPlayer.PaidExperimentJobs.Contains(key))
        {
            _sessions.GrantPendingXp(player);
            return 0;
        }

        var xp = ResearchXpRules.CompletionXp(BaseXp(player, job), level);
        dbPlayer.PaidExperimentJobs.Add(key);
        if (xp > 0)
        {
            dbPlayer.PendingCraftXp ??= new Dictionary<SkillType, int>();
            dbPlayer.PendingCraftXp[SkillType.Fabrication] = dbPlayer.PendingCraftXp.GetValueOrDefault(SkillType.Fabrication) + xp;
        }

        _db.Set(dbPlayer);
        _sessions.GrantPendingXp(player);
        return xp;
    }

    /// <summary>The chart XP for the job's recipe level against the player's Fabrication rank.</summary>
    public int BaseXp(uint player, ResearchJob job) =>
        ResearchXpRules.BaseXp(_craft.GetRecipe(job.Recipe).Level, GetFabricationRank(player));

    /// <summary>Writes a crafting log line. Log is not converted yet.</summary>
    protected virtual void WriteLog(string message) => Log.Write(LogGroup.Crafting, message);

    /// <summary>The player's Fabrication rank. Skill is not converted yet.</summary>
    protected virtual int GetFabricationRank(uint player) => _skill.GetCreatureSkillRank(player, SkillType.Fabrication);
}
