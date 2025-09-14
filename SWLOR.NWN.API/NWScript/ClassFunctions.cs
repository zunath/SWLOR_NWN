using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Returns oCreature's spell school specialization in nClass (SPELL_SCHOOL_* constants)
        /// Unless custom content is used, only Wizards have spell schools
        /// Returns -1 on error
        /// </summary>
        public static SpellSchool GetSpecialization(uint creature, ClassType playerClass)
        {
            return (SpellSchool)global::NWN.Core.NWScript.GetSpecialization(creature, (int)playerClass);
        }

        /// <summary>
        /// Returns oCreature's domain in nClass (DOMAIN_* constants)
        /// nDomainIndex - 1 or 2
        /// Unless custom content is used, only Clerics have domains
        /// Returns -1 on error
        /// </summary>
        public static ClericDomain GetDomain(uint creature, int DomainIndex = 1, ClassType playerClass = ClassType.Cleric)
        {
            return (ClericDomain)global::NWN.Core.NWScript.GetDomain(creature, DomainIndex, (int)playerClass);
        }
    }
}
