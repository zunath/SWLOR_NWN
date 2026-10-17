namespace SWLOR.CombatSim.Scenario;

/// <summary>
/// Opt-in, stat-based weapon rolls. Ratings are the character sheet's effective values before
/// modeled perks/statuses; this is not a gear or character builder. Geometry and projectile travel
/// remain explicit scenario assumptions, not NWN pathfinding or client-animation simulation.
/// </summary>
public sealed class WeaponLoadoutSpec
{
    public int MainDamage { get; set; } = 22;
    public int MainDelay { get; set; } = 250;
    public int OffhandDamage { get; set; }
    public int OffhandDelay { get; set; }
    public int OffhandDelayReductionPercent { get; set; }
    public int Attack { get; set; } = 150;
    public int Accuracy { get; set; } = 150;
    public int DamageAbility { get; set; } = 26;
    public int Perception { get; set; } = 26;
    public int SkillRank { get; set; } = 50;
    public int WeaponRank { get; set; } = 50;
    public int HastePercent { get; set; }
    public int OrdinaryCriticalRateAdjustment { get; set; }
    public int AbilityCriticalRateAdjustment { get; set; }
    public int SingleWeaponDamagePercent { get; set; }
    public bool HasOffhand => OffhandDelay > 0;

    public void Validate()
    {
        if (MainDamage < 1 || MainDamage > 10000 || MainDelay < 1 || MainDelay > 10000 ||
            OffhandDamage < 0 || OffhandDamage > 10000 || OffhandDelay < 0 || OffhandDelay > 10000 ||
            (OffhandDamage > 0) != HasOffhand || OffhandDelayReductionPercent is < 0 or > 50 ||
            Attack is < 1 or > 10000 || Accuracy is < 0 or > 10000 ||
            DamageAbility is < 0 or > 255 || Perception is < 0 or > 255 || SkillRank is < 0 or > 50 || WeaponRank is < 0 or > 50 ||
            HastePercent is < -50 or > 50 || SingleWeaponDamagePercent is < 0 or > 100 ||
            OrdinaryCriticalRateAdjustment is < -100 or > 100 || AbilityCriticalRateAdjustment is < -100 or > 100)
            throw new InvalidDataException("WeaponLoadout has an invalid rating, damage, delay, or percentage.");
    }
}
