using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Stat-driven rewards for ordinary hostile weapon hits across all weapon skills. The logic lives in
/// <see cref="IOrdinaryAttackTraitsService"/>; every member forwards to the container's instance.
/// </summary>
public static class OrdinaryAttackTraits
{
    private static IOrdinaryAttackTraitsService Instance => FacadeAccessor.Resolve<IOrdinaryAttackTraitsService>();

    public static void RecordTargetChange(uint actor, uint target) => Instance.RecordTargetChange(actor, target);

    public static void RecordHit(uint actor, uint target, long rollId) => Instance.RecordHit(actor, target, rollId);

    public static void Reconcile(uint actor) => Instance.Reconcile(actor);

    public static void Clear(uint actor) => Instance.Clear(actor);

    public static void ClearOnDeath(uint actor) => Instance.ClearOnDeath(actor);

    public static void ClearActor(uint actor) => Instance.ClearActor(actor);

    /// <summary>A destroyed actor's object ID may be reused; drop its state without touching the object.</summary>
    public static void ForgetDestroyed(uint actor) => Instance.ForgetDestroyed(actor);
}
