using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public class BasicShipDefinition: IShipListDefinition
    {
        private readonly ShipBuilder _builder = new ShipBuilder();

        public Dictionary<string, ShipDetail> BuildShips()
        {
            LightFreighter();
            LightEscort();
            
            return _builder.Build();
        }

        private void LightFreighter()
        {
            _builder.Create("ShipDeedLightFreighter")
                .Name("Light Freighter")
                .Appearance(AppearanceType.RepublicForay)
                .RequirePerk(PerkType.Starships, 1)
                .ItemResref("sdeed_freighter")
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3);
        }
        private void LightEscort()
        {
            _builder.Create("ShipDeedLightEscort")
                .Name("Light Escort")
                .Appearance(AppearanceType.RepublicAurek)
                .RequirePerk(PerkType.Starships, 1)
                .ItemResref("sdeed_escort")
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3);
        }
    }
}
