using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// One rank step of a revamp module family: the item, how it is used and the numbers it carries.
    /// Rows come from the generated RevampModuleCatalog; unused numbers stay 0.
    /// </summary>
    public class ShipModuleStep
    {
        public ShipModuleFamily Family { get; set; }
        public int Rank { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public ShipModulePowerType Row { get; set; }
        public SkillType Skill { get; set; }
        public ShipModuleActivation Activation { get; set; }
        public double CycleSeconds { get; set; }
        public int CapacitorCost { get; set; }
        public int Range { get; set; }
        public bool Capital { get; set; }
        public string Texture { get; set; }
        public string Description { get; set; }
        public RecipeType RecipeType { get; set; }
        public int RecipeLevel { get; set; }
        public (string Resref, int Quantity)[] Components { get; set; } = new (string Resref, int Quantity)[0];

        // Weapons
        public SpaceWeaponFamily WeaponFamily { get; set; }
        public CombatDamageType DamageType { get; set; }
        public int Dmg { get; set; }
        public int Attack { get; set; }
        public int Accuracy { get; set; }
        public bool ShieldBypass { get; set; }
        public bool IgnoreSizePenalty { get; set; }
        public double ShieldDamageMultiplier { get; set; } = 1.0;

        /// <summary>The size the weapon always fires as. Null means the firing hull's size.</summary>
        public ShipSize? FixedSize { get; set; }

        // Gathering
        /// <summary>The highest asteroid, gas cloud or wreck tier a mining, gas or salvage module works.</summary>
        public int MiningTier { get; set; }

        // Support and passive numbers
        public int Shield { get; set; }
        public int Hull { get; set; }
        public int Repair { get; set; }
        public int MaxCapacitor { get; set; }
        public int CapacitorRestore { get; set; }
        public double CapacitorRegen { get; set; }
        public int SpeedPercent { get; set; }
        public int ModuleAccuracy { get; set; }
        public int ModuleDefense { get; set; }
        public int ModuleEvasion { get; set; }
        public int ResistAll { get; set; }
        public int ResistThermal { get; set; }
        public int ResistEM { get; set; }
        public int ResistExplosive { get; set; }
        public int DamageThermal { get; set; }
        public int DamageEM { get; set; }
        public int DamageExplosive { get; set; }
        public int DamageAll { get; set; }
        public int AuraShieldPercent { get; set; }
        public int AuraAccuracy { get; set; }

        // Trade
        /// <summary>The percentage a Smuggling Compartment cuts the customs scan chance by.</summary>
        public int ScanReduction { get; set; }
    }
}
