using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Returns the creature's spell school specialization in the specified class.
        /// </summary>
        /// <param name="creature">The creature to get the specialization for</param>
        /// <param name="playerClass">The class to get the specialization for</param>
        /// <returns>The spell school specialization (SPELL_SCHOOL_* constants). Returns -1 on error</returns>
        /// <remarks>Unless custom content is used, only Wizards have spell schools.</remarks>
        public static SpellSchool GetSpecialization(uint creature, ClassType playerClass)
        {
            return (SpellSchool)global::NWN.Core.NWScript.GetSpecialization(creature, (int)playerClass);
        }

        /// <summary>
        /// Returns the creature's domain in the specified class.
        /// </summary>
        /// <param name="creature">The creature to get the domain for</param>
        /// <param name="DomainIndex">The domain index (1 or 2) (default: 1)</param>
        /// <param name="playerClass">The class to get the domain for (default: ClassType.Cleric)</param>
        /// <returns>The domain (DOMAIN_* constants). Returns -1 on error</returns>
        /// <remarks>Unless custom content is used, only Clerics have domains.</remarks>
        public static ClericDomain GetDomain(uint creature, int DomainIndex = 1, ClassType playerClass = ClassType.Cleric)
        {
            return (ClericDomain)global::NWN.Core.NWScript.GetDomain(creature, DomainIndex, (int)playerClass);
        }
    }
}
