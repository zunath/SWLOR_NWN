using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>The damage types a character can have and defend against, and adding them to a player who lacks them.</summary>
    public interface ICombatDamageCatalogService
    {
        /// <summary>
        /// Cache all valid character and defense damage types before module load.
        /// </summary>
        void LoadDamageTypes();

        /// <summary>
        /// When a player enters the server, apply any defense and resistance entries they don't already have.
        /// </summary>
        void AddDamageTypeResistances();

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        IReadOnlyList<CombatDamageType> GetAllDamageTypes();

        /// <summary>
        /// Retrieves all damage types which use a defense rating.
        /// </summary>
        /// <returns>A list of defense damage types</returns>
        IReadOnlyList<CombatDamageType> GetDefenseDamageTypes();

        bool EnsureDefenseValues(Dictionary<CombatDamageType, int> defenses, int defaultValue = 0);
    }
}
