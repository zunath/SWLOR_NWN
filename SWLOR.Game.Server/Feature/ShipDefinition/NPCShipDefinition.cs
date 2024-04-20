using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public class NPCShipDefinition : IShipListDefinition
    {
        private readonly ShipBuilder _builder = new();
        public Dictionary<string, ShipDetail> BuildShips()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();

            return _builder.Build();
        }

        private void Tier1()
        {
            _builder.Create("NPC_Sybil")
                .ItemResref("npc_sybil")
                .Name("NPC - Sybil")
                .Appearance(AppearanceType.SWLORShipNeutralDropship)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(8)
                .MaxCapacitor(40)
                .MaxShield(5)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("NPC_Terror")
                .ItemResref("npc_terror")
                .Name("NPC - Terror")
                .Appearance(AppearanceType.SWLORShipNeutralGunship)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(10)
                .MaxCapacitor(40)
                .MaxShield(12)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_courier")
                .ItemResref("npc_courier")
                .Name("NPC - Courier")
                .Appearance(AppearanceType.SWLORShipNeutralStriker)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(15)
                .MaxCapacitor(40)
                .MaxShield(15)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_turret1")
                .ItemResref("npc_turret1")
                .Name("NPC - Mk 1 Defense Turret")
                .Appearance(AppearanceType.SWLORTurret1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(15)
                .MaxCapacitor(40)
                .MaxShield(15)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);
        }

        private void Tier2()
        {
            _builder.Create("NPC_Nightmare")
                .ItemResref("npc_nightmare")
                .Name("NPC - Nightmare")
                .Appearance(AppearanceType.SWLORShipRepublicBomberA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(25)
                .MaxCapacitor(30)
                .MaxShield(25)
                .ShieldRechargeRate(5)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("NPC_Ferron")
                .ItemResref("pirate_ferron")
                .Name("NPC - Ferron")
                .Appearance(AppearanceType.SWLORShipRepublicGunshipB)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(30)
                .MaxCapacitor(70)
                .MaxShield(25)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_shuttle")
                .ItemResref("npc_shuttle")
                .Name("NPC - Shuttle")
                .Appearance(AppearanceType.Mdrnvlambda)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(30)
                .MaxCapacitor(40)
                .MaxShield(30)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_turret2")
                .ItemResref("npc_turret2")
                .Name("NPC - Mk 2 Defense Turret")
                .Appearance(AppearanceType.SWLORTurret1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(30)
                .MaxCapacitor(40)
                .MaxShield(30)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);
        }

        private void Tier3()
        {
            _builder.Create("NPC_Storm")
                .ItemResref("pirate_storm")
                .Name("NPC - Storm")
                .Appearance(AppearanceType.SWLORShipRepublicGunshipC)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(42)
                .MaxCapacitor(40)
                .MaxShield(35)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("NPC_Ranger")
                .ItemResref("pirate_ranger")
                .Name("NPC - Ranger")
                .Appearance(AppearanceType.SWLORShipRepublicGunshipA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(55)
                .MaxCapacitor(90)
                .MaxShield(40)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_aurek")
                .ItemResref("npc_aurek")
                .Name("NPC - Aurek")
                .Appearance(AppearanceType.SWLORShipRepublicAurek)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(60)
                .MaxCapacitor(100)
                .MaxShield(50)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_sithfig")
                .ItemResref("npc_sithfig")
                .Name("NPC - Sith Fighter")
                .Appearance(AppearanceType.SWLORShipSithFighter)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(100)
                .MaxCapacitor(100)
                .MaxShield(0)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_freighter")
                .ItemResref("npc_freighter")
                .Name("NPC - Freighter")
                .Appearance(AppearanceType.MdrnFreighterSmall)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(60)
                .MaxCapacitor(40)
                .MaxShield(40)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_turret3")
                .ItemResref("npc_turret3")
                .Name("NPC - Mk 3 Defense Turret")
                .Appearance(AppearanceType.SWLORTurret1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(60)
                .MaxCapacitor(40)
                .MaxShield(40)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);
        }

        private void Tier4()
        {
            _builder.Create("NPC_Hammer")
                .ItemResref("pirate_hammer")
                .Name("NPC - Hammer")
                .Appearance(AppearanceType.SWLORShipRepublicGunshipC)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(60)
                .MaxCapacitor(65)
                .MaxShield(75)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("NPC_Drake")
                .ItemResref("pirate_drake")
                .Name("NPC - Drake")
                .Appearance(AppearanceType.SWLORShipRepublicInfiltratorB)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(75)
                .MaxCapacitor(75)
                .MaxShield(60)
                .ShieldRechargeRate(12)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_bulkf")
                .ItemResref("npc_bulkf")
                .Name("NPC - Bulk Freighter")
                .Appearance(AppearanceType.MdrndLargeCargoship)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(75)
                .MaxCapacitor(75)
                .MaxShield(60)
                .ShieldRechargeRate(12)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_turret4")
                .ItemResref("npc_turret4")
                .Name("NPC - Mk 4 Defense Turret")
                .Appearance(AppearanceType.SWLORTurret1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(75)
                .MaxCapacitor(75)
                .MaxShield(60)
                .ShieldRechargeRate(12)
                .HighPowerNodes(8)
                .LowPowerNodes(8);
        }

        private void Tier5()
        {
            _builder.Create("NPC_Borealis")
                .ItemResref("pirate_borealis")
                .Name("NPC - Borealis")
                .Appearance(AppearanceType.SWLORShipSithGunshipA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(100)
                .MaxCapacitor(120)
                .MaxShield(90)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("NPC_Eleyna")
                .ItemResref("pirate_eleyna")
                .Name("NPC - Eleyna")
                .Appearance(AppearanceType.SWLORShipSithStrikerA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(70)
                .MaxCapacitor(95)
                .MaxShield(100)
                .ShieldRechargeRate(18)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_merchant")
                .ItemResref("npc_merchant")
                .Name("NPC - Merchantman")
                .Appearance(AppearanceType.SWLORShipRepublicForay)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(125)
                .MaxCapacitor(75)
                .MaxShield(125)
                .ShieldRechargeRate(12)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("npc_turret5")
                .ItemResref("npc_turret5")
                .Name("NPC - Mk 5 Defense Turret")
                .Appearance(AppearanceType.SWLORTurret1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(125)
                .MaxCapacitor(75)
                .MaxShield(125)
                .ShieldRechargeRate(12)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_corvette")
                .ItemResref("cap_corvette")
                .Name("Foray-Class Blockade Runner")
                .Appearance(AppearanceType.SWLORShipRepublicForay)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(350)
                .MaxCapacitor(5000)
                .MaxShield(200)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_frigate")
                .ItemResref("cap_frigate")
                .Name("Praetorian-Class Frigate")
                .Appearance(AppearanceType.SWLORShipRepublicHammerhead)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(500)
                .MaxCapacitor(5000)
                .MaxShield(300)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_cruiser")
                .ItemResref("cap_cruiser")
                .Name("Hammerhead-Class Cruiser")
                .Appearance(AppearanceType.SWLORShipRepublicHammerhead)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(800)
                .MaxCapacitor(5000)
                .MaxShield(450)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_hvycrui")
                .ItemResref("cap_hvycrui")
                .Name("Interdictor-Class Heavy Cruiser")
                .Appearance(AppearanceType.SWLORShipSithLeviathan)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(1200)
                .MaxCapacitor(5000)
                .MaxShield(700)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_btlcrui")
                .ItemResref("cap_btlcrui")
                .Name("Centurion-Class Battlecruiser")
                .Appearance(AppearanceType.SWLORISD1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(1800)
                .MaxCapacitor(5000)
                .MaxShield(1000)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_btlship")
                .ItemResref("cap_btlship")
                .Name("Derriphan-Class Battleship")
                .Appearance(AppearanceType.SWLORISD1)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(5000)
                .MaxCapacitor(5000)
                .MaxShield(0)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);

            _builder.Create("cap_dread")
                .ItemResref("cap_dread")
                .Name("Kandosii-Class Dreadnought")
                .Appearance(AppearanceType.MdrnbLargeShuttle)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(4000)
                .MaxCapacitor(5000)
                .MaxShield(2500)
                .ShieldRechargeRate(1)
                .HighPowerNodes(8)
                .LowPowerNodes(8);
        }
    }
}
