using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// The materials each City Project asks anyone to donate (Player City Revamp, "City Projects"). PROPOSED, NEEDS THE OWNER'S
    /// APPROVAL: the plan says they are "drafted with the stock lists and approved before release". This table is the only place
    /// they live; edit a row to change a project.
    /// <para>
    /// How the proposal was sized. The credits stay what facility upgrades cost today, 50,000 times the level built. The materials
    /// follow that curve from the building's own Fabrication recipe (<c>StructureRecipes</c>): a Medical Center and a Starport are
    /// 4 Reinforced Construction Parts and 2 Reinforced Power Supply Units, a Cantina 2 and 1, a Jump Beacon 4 and 3. Building the
    /// next level asks for that recipe times level / 2, rounded up, so level 2 is one building's worth and level 5 is two and a
    /// half, as the credits go from 100,000 to 250,000. Home Ground is 1.5 Starports' worth, for 150,000 credits.
    /// </para>
    /// </summary>
    public static class CityProjectCatalog
    {
        public const string ReinforcedConstructionParts = "r_const_parts";
        public const string ReinforcedPowerSupplyUnit = "r_pow_supp_unit";

        // Type, level built, then Reinforced Construction Parts and Reinforced Power Supply Units.
        private static readonly (CityProjectType Type, int Level, int ConstructionParts, int PowerSupplyUnits)[] Rows =
        {
            (CityProjectType.MedicalCenterUpgrade, 2, 4, 2),
            (CityProjectType.MedicalCenterUpgrade, 3, 6, 3),
            (CityProjectType.MedicalCenterUpgrade, 4, 8, 4),
            (CityProjectType.MedicalCenterUpgrade, 5, 10, 5),

            (CityProjectType.StarportUpgrade, 2, 4, 2),
            (CityProjectType.StarportUpgrade, 3, 6, 3),
            (CityProjectType.StarportUpgrade, 4, 8, 4),
            (CityProjectType.StarportUpgrade, 5, 10, 5),

            (CityProjectType.CantinaUpgrade, 2, 2, 1),
            (CityProjectType.CantinaUpgrade, 3, 3, 2),
            (CityProjectType.CantinaUpgrade, 4, 4, 2),
            (CityProjectType.CantinaUpgrade, 5, 5, 3),

            (CityProjectType.JumpBeaconUpgrade, 2, 4, 3),
            (CityProjectType.JumpBeaconUpgrade, 3, 6, 5),

            (CityProjectType.HomeGround, 0, 6, 3),
        };

        private static readonly Dictionary<(CityProjectType Type, int Level), CityProjectRecipe> _recipes = Rows.ToDictionary(
            row => (row.Type, row.Level),
            row => new CityProjectRecipe(
                row.Type,
                row.Level,
                new Dictionary<string, int>
                {
                    [ReinforcedConstructionParts] = row.ConstructionParts,
                    [ReinforcedPowerSupplyUnit] = row.PowerSupplyUnits
                }));

        /// <summary>The project's materials, or null when the city cannot build that.</summary>
        public static CityProjectRecipe Get(CityProjectType type, int targetLevel)
        {
            return _recipes.GetValueOrDefault((type, type == CityProjectType.HomeGround ? 0 : targetLevel));
        }

        /// <summary>Every project the catalog lists.</summary>
        public static IReadOnlyCollection<CityProjectRecipe> GetAll()
        {
            return _recipes.Values;
        }
    }
}
