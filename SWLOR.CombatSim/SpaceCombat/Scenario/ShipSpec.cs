namespace SWLOR.CombatSim.SpaceCombat.Scenario;

/// <summary>One ship: base stats from its hull, the pilot's attributes and the fitted modules.</summary>
public sealed class ShipSpec
{
    public string Name { get; set; }

    /// <summary>How many identical ships; they are numbered Name 1, Name 2... when more than one.</summary>
    public int Count { get; set; } = 1;

    public ShipSize Size { get; set; } = ShipSize.Small;
    public double MaxHull { get; set; }
    public double MaxShield { get; set; }
    public double MaxCapacitor { get; set; }
    public double ShieldRegenPerSecond { get; set; }
    public double CapacitorRegenPerSecond { get; set; }
    public int Attack { get; set; }
    public int Accuracy { get; set; }
    public int Defense { get; set; }
    public int Evasion { get; set; }
    public SpaceResistances Resistances { get; set; } = new();

    /// <summary>Outgoing damage percent per damage type (ShipStatus.DamageBonusPercent plus routing and environment).</summary>
    public SpaceDamageBonuses DamageBonusPercent { get; set; } = new();

    /// <summary>The pilot's damage-dealt percent (StatType.DamageDealtPercentAdjustment), applied to the rolled damage first.</summary>
    public int DamageDealtPercent { get; set; }

    /// <summary>Damage taken percent on this ship (StatType.DamageTakenPercentAdjustment and a Target Painter's paint), after resistance.</summary>
    public int DamageTakenPercent { get; set; }

    /// <summary>Critical rate added to the critical formula's modifier: the pilot's Critical Rate % and the hull's critical bonus.</summary>
    public int CriticalRatePercent { get; set; }

    /// <summary>The pilot's critical damage percent (StatType.CriticalDamagePercentAdjustment), limited as the ground's.</summary>
    public int CriticalDamagePercent { get; set; }

    /// <summary>
    /// Sustained Fire: each consecutive energy hit against the same target adds this much DMG to the shot
    /// (StatType.SpaceEnergyRepeatedTargetDamageBonusPerHit); 0 for none.
    /// </summary>
    public int EnergyRepeatedHitDmg { get; set; }

    /// <summary>The most DMG Sustained Fire adds (StatType.SpaceEnergyRepeatedTargetDamageBonusMax).</summary>
    public int EnergyRepeatedHitMaxDmg { get; set; }

    /// <summary>Proximity Warheads: the chance (percent) a launcher's own cycle hit adds flat damage to its target.</summary>
    public int LauncherSplashChance { get; set; }

    /// <summary>Proximity Warheads: the flat damage of that splash.</summary>
    public int LauncherSplashDamage { get; set; }

    /// <summary>
    /// Saturation Fire: a target hit by two of this ship's area launcher abilities within 30 s becomes Exposed (this
    /// percent off its Defense) for 30 s, and the count starts again; 0 for none.
    /// </summary>
    public int SaturationExposedDefensePercent { get; set; }
    public PilotSpec Pilot { get; set; } = new();
    public List<WeaponModuleSpec> Weapons { get; set; } = new();
    public List<RepairerModuleSpec> Repairers { get; set; } = new();
    public List<PassiveModuleSpec> Passives { get; set; } = new();
    public List<BurstAbilitySpec> Abilities { get; set; } = new();
}
