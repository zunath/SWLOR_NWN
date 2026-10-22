namespace SWLOR.CombatSim.Scenario;

/// <summary>Explicit stationary weapon calibration; no spatial movement, native animation or deflection model.</summary>
public sealed class WeaponCombatSpec
{
    public int WeaponDamage { get; set; } = 31;
    /// <summary>Fixed coordinates for weapon area selection only; this model does not simulate movement.</summary>
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public int WeaponSkillRank { get; set; } = 50;
    public int WeaponRequiredSkillRank { get; set; } = 50;
    public int WeaponDelayUnits { get; set; } = 220;
    public int ExternalHastePercent { get; set; }
    public int Attack { get; set; } = 128;
    public int Accuracy { get; set; } = 128;
    public int Defense { get; set; } = 128;
    public int Evasion { get; set; } = 128;
    public int Might { get; set; } = 20;
    public int Perception { get; set; } = 20;
    public int Vitality { get; set; } = 20;
    public int MindResistance { get; set; }
    public int MobilityResistance { get; set; }
    public int TraumaResistance { get; set; }
    public string Stance { get; set; } = "None";
    public bool AllowTechniqueAbilities { get; set; } = true;
    public bool EnableCriticals { get; set; } = true;
    public void Validate()
    {
        if (!float.IsFinite(PositionX) || !float.IsFinite(PositionY) || Math.Abs(PositionX) > 10000 || Math.Abs(PositionY) > 10000 ||
            WeaponDamage is < 0 or > 10000 || WeaponSkillRank is < 0 or > 100 || WeaponRequiredSkillRank is < 0 or > 100 || WeaponDelayUnits is < 1 or > 10000 ||
            Attack is < 1 or > 10000 || Accuracy is < 0 or > 10000 || Defense is < 1 or > 10000 || Evasion is < 0 or > 10000 ||
            Might is < 0 or > 255 || Perception is < 0 or > 255 || Vitality is < 0 or > 255 || ExternalHastePercent is < -50 or > 50 ||
            MindResistance is < -100 or > 100 || MobilityResistance is < -100 or > 100 || TraumaResistance is < -100 or > 100 || Stance is not ("None" or "Flow" or "Bruiser" or "Berserker" or "Scrapper" or "Crusher"))
            throw new InvalidDataException("WeaponCombat has unsupported calibration values or stance.");
    }
}
