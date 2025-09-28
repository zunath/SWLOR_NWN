using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for class information
        private readonly Dictionary<uint, Dictionary<ClassType, SpellSchool>> _specializations = new();
        private readonly Dictionary<uint, Dictionary<ClassType, Dictionary<int, ClericDomainType>>> _domains = new();

        public SpellSchool GetSpecialization(uint creature, ClassType playerClass) 
        {
            return _specializations.GetValueOrDefault(creature, new Dictionary<ClassType, SpellSchool>())
                .GetValueOrDefault(playerClass, SpellSchool.General);
        }

        public ClericDomainType GetDomain(uint creature, int DomainIndex = 1, ClassType playerClass = ClassType.Cleric) 
        {
            return _domains.GetValueOrDefault(creature, new Dictionary<ClassType, Dictionary<int, ClericDomainType>>())
                .GetValueOrDefault(playerClass, new Dictionary<int, ClericDomainType>())
                .GetValueOrDefault(DomainIndex, ClericDomainType.Air);
        }

        // Helper methods for testing


    }
}
