namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>
/// A ship's outgoing damage percent per damage type: the server's ShipStatus.DamageBonusPercent (hull, manufacturer
/// and module bonuses) plus the power routing's and the environment's percent. It multiplies a shot's damage on its
/// own stage, before the target's resistance (Space.RollWeaponShot).
/// </summary>
public sealed class SpaceDamageBonuses
{
    public int EM { get; set; }
    public int Thermal { get; set; }
    public int Explosive { get; set; }

    public int Get(SpaceDamageType type) => type switch
    {
        SpaceDamageType.EM => EM,
        SpaceDamageType.Thermal => Thermal,
        SpaceDamageType.Explosive => Explosive,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
