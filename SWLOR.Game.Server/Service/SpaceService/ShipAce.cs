using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A named rare-spawn pilot of one orbit or zone (plan section 11). Rows come from the generated RevampAceCatalog;
    /// the ship's numbers are its RevampNpcShipCatalog preset.
    /// </summary>
    public class ShipAce
    {
        /// <summary>The creature tag and resref.</summary>
        public string Tag { get; set; }
        public string Name { get; set; }

        /// <summary>The area the ace belongs to, and the creature spawn table that area uses.</summary>
        public string AreaResref { get; set; }
        public string SpawnTable { get; set; }

        /// <summary>The faction tier (1-6) and the preset's level (tier x 10).</summary>
        public int Tier { get; set; }
        public int Level { get; set; }

        /// <summary>The preset role, as <see cref="ShipNpcRoleRules.ParseRole"/> reads it.</summary>
        public string Role { get; set; }

        /// <summary>The legacy NPC ship item the space object registers.</summary>
        public string ShipItemTag { get; set; }

        /// <summary>The Hull Design the ace flies, whose Design Schematic it drops.</summary>
        public AppearanceType Appearance { get; set; }

        /// <summary>The maker whose manufacturer component the ace and its wreck give.</summary>
        public ShipManufacturer Manufacturer { get; set; }

        /// <summary>The ace's own loot table.</summary>
        public string LootTable { get; set; }

        public string DesignResref { get; set; }
        public string ModuleSchematicResref { get; set; }
        public string TrophyResref { get; set; }
        public StructureType Trophy { get; set; }

        /// <summary>The component of the ace's tier its table gives, with a rare trait.</summary>
        public string RareComponentResref { get; set; }
        public ShipComponentTraitType RareTrait { get; set; }

        public string ManufacturerComponentResref { get; set; }
        public ShipComponentTraitType ManufacturerTrait { get; set; }

        /// <summary>The achievement the killing party earns.</summary>
        public AchievementType Achievement { get; set; }
    }
}
