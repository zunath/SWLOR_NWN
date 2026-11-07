using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service
{
    public static partial class Combat
    {
        private static ICombatDamageCatalogService DamageCatalog => FacadeAccessor.Resolve<ICombatDamageCatalogService>();

        /// <summary>
        /// Cache all valid character and defense damage types before module load.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadDamageTypes()
        {
            DamageCatalog.LoadDamageTypes();
        }

        /// <summary>
        /// When a player enters the server, apply any defense and resistance entries they don't already have.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddDamageTypeResistances()
        {
            DamageCatalog.AddDamageTypeResistances();
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetAllDamageTypes()
        {
            return DamageCatalog.GetAllDamageTypes();
        }

        /// <summary>
        /// Retrieves all damage types which use a defense rating.
        /// </summary>
        /// <returns>A list of defense damage types</returns>
        public static IReadOnlyList<CombatDamageType> GetDefenseDamageTypes()
        {
            return DamageCatalog.GetDefenseDamageTypes();
        }

        // Pure and needs no container: the Beast, IncubationJob and BeastLevel constructors call this one.
        public static Dictionary<CombatDamageType, int> CreateDefaultDefenseValues(int defaultValue = 0) =>
            CombatRules.CreateDefaultDefenseValues(defaultValue);

        public static bool EnsureDefenseValues(Dictionary<CombatDamageType, int> defenses, int defaultValue = 0)
        {
            return DamageCatalog.EnsureDefenseValues(defenses, defaultValue);
        }
    }
}
