using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{

    public delegate void ShipModuleEquippedDelegate(ShipStatus shipStatus, int moduleBonus);
    public delegate void ShipModuleUnequippedDelegate(ShipStatus shipStatus, int moduleBonus);
    public delegate float ShipModuleCalculateRecastDelegate(uint creature, ShipStatus shipStatus, int moduleBonus);
    public delegate int ShipModuleCalculateCapacitorDelegate(uint creature, ShipStatus shipStatus, int moduleBonus);
    public delegate void ShipModuleActivatedDelegate(uint activator, ShipStatus activatorShipStatus, uint target, ShipStatus targetShipStatus, int moduleBonus);
    public delegate string ShipModuleValidationDelegate(uint activator, ShipStatus activatorShipStatus, uint target, ShipStatus targetShipStatus, int moduleBonus);
    public delegate float ShipModuleCalculateMaxDistanceDelegate(uint activator, ShipStatus activatorShipStatus, uint target, ShipStatus targetShipStatus, int moduleBonus);

    public class ShipModuleDetail
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public ShipModuleType Type { get; set; }
        public string Texture { get; set; }
        public string Description { get; set; }
        public bool CanTargetSelf { get; set; }
        public bool CapitalClassModule { get; set; }

        /// <summary>Toggle or one press. Passive modules stay Invalid.</summary>
        public ShipModuleActivation Activation { get; set; }
        public ShipModulePowerType PowerType { get; set; }

        /// <summary>The revamp module step this module comes from. Null for legacy modules.</summary>
        public ShipModuleStep Step { get; set; }
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
            ValidTargetTypes = new HashSet<ObjectType>();
        }
    }
}
