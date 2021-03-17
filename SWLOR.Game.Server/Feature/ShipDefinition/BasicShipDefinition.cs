using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public class BasicShipDefinition: IShipListDefinition
    {
        private readonly ShipBuilder _builder = new ShipBuilder();

        public Dictionary<ShipType, ShipDetail> BuildShips()
        {
            LightFreighter();
            LightEscort();
            
            return _builder.Build();
        }

        private void LightFreighter()
        {
            _builder.Create(ShipType.LightFreighter)
                .Name("Light Freighter")
                .Appearance(AppearanceType.SmallCargoShip)
                .ItemResref("sdeed_freighter")
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6f)
                .HighPowerNodes(3)
                .LowPowerNodes(3);
        }
        private void LightEscort()
        {
            _builder.Create(ShipType.LightEscort)
                .Name("Light Escort")
                .Appearance(AppearanceType.SmallShuttle3)
                .ItemResref("sdeed_escort")
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6f)
                .HighPowerNodes(3)
                .LowPowerNodes(3);
        }
    }
}
