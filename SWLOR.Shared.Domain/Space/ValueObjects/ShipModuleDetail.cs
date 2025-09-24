using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Space.Enums;

namespace SWLOR.Shared.Domain.Space.ValueObjects
{

    public class ShipModuleDetail
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public ShipModuleType Type { get; set; }
        public string Texture { get; set; }
        public string Description { get; set; }
        public bool CanTargetSelf { get; set; }
        public bool CapitalClassModule { get; set; }
        public ShipModulePowerType PowerType { get; set; }
        public Dictionary<PerkType, int> RequiredPerks { get; set; }
        public HashSet<ObjectType> ValidTargetTypes { get; set; }
        public ShipModuleCalculateCapacitorDelegate CalculateCapacitorAction { get; set; }
        public ShipModuleCalculateRecastDelegate CalculateRecastAction { get; set; }
        public ShipModuleEquippedDelegate ModuleEquippedAction { get; set; }
        public ShipModuleUnequippedDelegate ModuleUnequippedAction { get; set; }
        public ShipModuleActivatedDelegate ModuleActivatedAction { get; set; }
        public ShipModuleValidationDelegate ModuleValidationAction { get; set; }
        public ShipModuleCalculateMaxDistanceDelegate ModuleMaxDistanceAction { get; set; }

        public ShipModuleDetail()
        {
            Texture = string.Empty;
            Type = ShipModuleType.Passive;
            RequiredPerks = new Dictionary<PerkType, int>();
            ValidTargetTypes = new HashSet<ObjectType>();
        }
    }
}
