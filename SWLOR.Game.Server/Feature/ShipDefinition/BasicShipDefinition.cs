using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
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
            
            Condor();
            Consular();
            Falchion();
            Hound();
            Merchant();
            Mule();
            Panther();
            Saber();
            Striker();
            Throne();

            return _builder.Build();
        }

        private void LightFreighter()
        {
            _builder.Create("ShipDeedLightFreighter")
                .ItemResref("sdeed_freighter")
                .Name("Light Freighter")
                .Appearance(AppearanceType.RepublicForay)
                .RequirePerk(PerkType.Starships, 1)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.LightFreighter1);
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
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.LightEscort1);
        }

        private void Condor()
        {
            _builder.Create("sdeed_condor")
                .ItemResref("sdeed_condor")
                .Name("Condor")
                .Appearance(AppearanceType.NeutralCondor)
                .RequirePerk(PerkType.Starships, 1)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Condor);
        }

        private void Consular()
        {
            _builder.Create("sdeed_consular")
                .ItemResref("sdeed_consular")
                .Name("Consular")
                .Appearance(AppearanceType.RepublicHammerhead)
                .RequirePerk(PerkType.Starships, 5)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Consular);
        }

        private void Falchion()
        {
            _builder.Create("sdeed_falchion")
                .ItemResref("sdeed_falchion")
                .Name("Falchion")
                .Appearance(AppearanceType.SithScoutC)
                .RequirePerk(PerkType.Starships, 3)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Falchion);
        }

        private void Hound()
        {
            _builder.Create("sdeed_hound")
                .ItemResref("sdeed_hound")
                .Name("Hound")
                .Appearance(AppearanceType.RepublicStrikerC)
                .RequirePerk(PerkType.Starships, 2)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Hound);
        }

        private void Merchant()
        {
            _builder.Create("sdeed_merchant")
                .ItemResref("sdeed_merchant")
                .Name("Merchant")
                .Appearance(AppearanceType.SithGunshipC)
                .RequirePerk(PerkType.Starships, 4)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Merchant);
        }

        private void Mule()
        {
            _builder.Create("sdeed_mule")
                .ItemResref("sdeed_mule")
                .Name("Mule")
                .Appearance(AppearanceType.RepublicGunshipC)
                .RequirePerk(PerkType.Starships, 4)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Mule);
        }

        private void Panther()
        {
            _builder.Create("sdeed_panther")
                .ItemResref("sdeed_panther")
                .Name("Panther")
                .Appearance(AppearanceType.SithScoutA)
                .RequirePerk(PerkType.Starships, 2)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Panther);
        }

        private void Saber()
        {
            _builder.Create("sdeed_saber")
                .ItemResref("sdeed_saber")
                .Name("Saber")
                .Appearance(AppearanceType.RepublicScoutC)
                .RequirePerk(PerkType.Starships, 3)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Saber);
        }

        private void Striker()
        {
            _builder.Create("sdeed_striker")
                .ItemResref("sdeed_striker")
                .Name("Striker")
                .Appearance(AppearanceType.NeutralStriker)
                .RequirePerk(PerkType.Starships, 1)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Striker);
        }

        private void Throne()
        {
            _builder.Create("sdeed_throne")
                .ItemResref("sdeed_throne")
                .Name("Throne")
                .Appearance(AppearanceType.RepublicForay)
                .RequirePerk(PerkType.Starships, 5)
                .MaxArmor(20)
                .MaxCapacitor(20)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(3)
                .LowPowerNodes(3)
                .InteriorLayout(PropertyLayoutType.Throne);
        }

    }
}
