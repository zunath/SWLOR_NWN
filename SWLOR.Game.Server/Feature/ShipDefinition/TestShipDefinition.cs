using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public class TestShipDefinition: IShipListDefinition
    {
        private readonly ShipBuilder _builder = new ShipBuilder();

        public Dictionary<ShipType, ShipDetail> BuildShips()
        {
            _builder.Create(ShipType.TestShip)
                .Name("Test Ship")
                .Appearance(AppearanceType.SmallFighter1)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6f)
                .HighPowerNodes(3)
                .LowPowerNodes(3);
            
            return _builder.Build();
        }
    }
}
