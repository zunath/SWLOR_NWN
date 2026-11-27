namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>Stat-driven rewards for ordinary hostile weapon hits across all weapon skills.</summary>
public interface IOrdinaryAttackTraitsService
{
    void RecordTargetChange(uint actor, uint target);

    void RecordHit(uint actor, uint target, long rollId);

    void Reconcile(uint actor);

    void Clear(uint actor);

    void ClearOnDeath(uint actor);

    void ClearActor(uint actor);

    /// <summary>A destroyed actor's object ID may be reused; drop its state without touching the object.</summary>
    void ForgetDestroyed(uint actor);
}
