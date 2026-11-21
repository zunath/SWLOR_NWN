namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>Percent resistance per space damage type, -100 (takes double) to 100 (immune).</summary>
public sealed class SpaceResistances
{
    public const int Minimum = -100;
    public const int Maximum = 100;

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
