using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public delegate int ShipModuleCalculateCapacitorDelegate(uint player, PlayerShip playerShip);
    public delegate float ShipModuleCalculateRecastDelegate(uint player, PlayerShip playerShip);
    public delegate void ShipModuleEquippedDelegate(uint player, uint item, PlayerShip playerShip);
    public delegate void ShipModuleUnequippedDelegate(uint player, uint item, PlayerShip playerShip);
    public delegate void ShipModuleActivatedDelegate(uint player, ShipStatus target, PlayerShip playerShip);

    public class ShipModuleDetail
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public bool IsPassive { get; set; }
        public ShipModulePowerType PowerType { get; set; }
        public Dictionary<PerkType, int> RequiredPerks { get; set; }
        public ShipModuleCalculateCapacitorDelegate CalculateCapacitorAction { get; set; }
        public ShipModuleCalculateRecastDelegate CalculateRecastAction { get; set; }
        public ShipModuleEquippedDelegate ModuleEquippedAction { get; set; }
        public ShipModuleUnequippedDelegate ModuleUnequippedAction { get; set; }
        public ShipModuleActivatedDelegate ModuleActivatedAction { get; set; }

        public ShipModuleDetail()
        {
            IsPassive = true;
            RequiredPerks = new Dictionary<PerkType, int>();
        }
    }
}
