using SWLOR.CombatSim.SpaceCombat.Scenario;

namespace SWLOR.CombatSim.Tests.SpaceCombat;

/// <summary>Plain ships and scenarios for the space tests. With these defaults attacker accuracy equals defender evasion, so a shot hits 75% of the time.</summary>
internal static class SpaceTestShips
{
    public static ShipSpec Ship(
        string name = "Ship", ShipSize size = ShipSize.Small, double hull = 1000, double shield = 0, double capacitor = 1000,
        params WeaponModuleSpec[] weapons) =>
        new()
        {
            Name = name,
            Size = size,
            MaxHull = hull,
            MaxShield = shield,
            MaxCapacitor = capacitor,
            Attack = 100,
            Accuracy = 100,
            Defense = 100,
            Evasion = 100,
            Pilot = new PilotSpec { Agi = 10, Per = 10, Vit = 10, Wil = 10, Mgt = 10 },
            Weapons = weapons.ToList()
        };

    public static WeaponModuleSpec Laser(string name = "Laser", int dmg = 20, double cycleSeconds = 1, double capacitorPerCycle = 0) =>
        new()
        {
            Name = name,
            Family = ModuleFamily.Energy,
            DamageType = SpaceDamageType.Thermal,
            Dmg = dmg,
            Rank = 10,
            CycleSeconds = cycleSeconds,
            CapacitorPerCycle = capacitorPerCycle
        };

    /// <summary>The attacker against a defender that does not fire back unless asked to.</summary>
    public static SpaceScenarioSpec Scenario(
        ShipSpec attacker, ShipSpec defender, bool defenderPassive = true, double durationSeconds = 60) =>
        new()
        {
            Name = "test",
            DurationSeconds = durationSeconds,
            Attackers = new SpaceSideSpec { Name = "Attackers", Ships = new List<ShipSpec> { attacker } },
            Defenders = new SpaceSideSpec { Name = "Defenders", Passive = defenderPassive, Ships = new List<ShipSpec> { defender } }
        };
}
