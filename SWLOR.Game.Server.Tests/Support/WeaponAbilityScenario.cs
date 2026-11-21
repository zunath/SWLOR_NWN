namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// One state a weapon ability is driven in: how unscripted checks answer, how many creatures its impact visits, what each hit deals, the stat
/// every read gives, whether a friendly ability is also used on its own caster, the hit points every creature has, and whether the target has
/// recently hit the caster's ward and guarded ally, with an ally guarded by the caster, who casts, whether the impact has a summary to read, and
/// whether the creature it hits is gone by the time its hit lands.
/// </summary>
public sealed record WeaponAbilityScenario(
    string Name,
    WeaponAbilityAnswerPattern Pattern,
    int Targets,
    int HitDamage,
    int Stat,
    bool FriendlySelf = false,
    int HitPoints = 100,
    bool ProtectedAllies = false,
    uint Caster = CombatTranscriptBench.Attacker,
    bool NoSummary = false,
    bool TargetGone = false);
