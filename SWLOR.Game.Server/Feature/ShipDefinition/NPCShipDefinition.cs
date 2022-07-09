using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipDefinition
{
    public class NPCShipDefinition: IShipListDefinition
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
                .Appearance(AppearanceType.NeutralDropship)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(10)
                .MaxCapacitor(40)
                .MaxShield(3)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);

            _builder.Create("NPC_Terror")
                .ItemResref("npc_terror")
                .Name("NPC - Terror")
                .Appearance(AppearanceType.NeutralGunship)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(6)
                .MaxCapacitor(40)
                .MaxShield(16)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);
        }

        private void Tier2()
        {
            _builder.Create("NPC_Nightmare")
                .ItemResref("npc_nightmare")
                .Name("NPC - Nightmare")
                .Appearance(AppearanceType.RepublicBomberA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(19)
                .MaxCapacitor(30)
                .MaxShield(25)
                .ShieldRechargeRate(5)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);

            _builder.Create("NPC_Ferron")
                .ItemResref("pirate_ferron")
                .Name("NPC - Ferron")
                .Appearance(AppearanceType.RepublicGunshipB)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(30)
                .MaxCapacitor(70)
                .MaxShield(15)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);
        }

        private void Tier3()
        {
            _builder.Create("NPC_Storm")
                .ItemResref("pirate_storm")
                .Name("NPC - Storm")
                .Appearance(AppearanceType.RepublicGunshipC)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(42)
                .MaxCapacitor(40)
                .MaxShield(28)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);

            _builder.Create("NPC_Ranger")
                .ItemResref("pirate_ranger")
                .Name("NPC - Ranger")
                .Appearance(AppearanceType.RepublicGunshipA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(55)
                .MaxCapacitor(90)
                .MaxShield(20)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);
        }

        private void Tier4()
        {
            _builder.Create("NPC_Hammer")
                .ItemResref("pirate_hammer")
                .Name("NPC - Hammer")
                .Appearance(AppearanceType.RepublicHammerhead)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(15)
                .MaxCapacitor(65)
                .MaxShield(83)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast3)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt3);

            _builder.Create("NPC_Drake")
                .ItemResref("pirate_drake")
                .Name("NPC - Drake")
                .Appearance(AppearanceType.RepublicInfiltratorB)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(73)
                .MaxCapacitor(75)
                .MaxShield(44)
                .ShieldRechargeRate(12)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt2);
        }

        private void Tier5()
        {
            _builder.Create("NPC_Borealis")
                .ItemResref("pirate_borealis")
                .Name("NPC - Borealis")
                .Appearance(AppearanceType.SithGunshipA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(70)
                .MaxCapacitor(120)
                .MaxShield(75)
                .ShieldRechargeRate(6)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast2)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt3);

            _builder.Create("NPC_Eleyna")
                .ItemResref("pirate_eleyna")
                .Name("NPC - Eleyna")
                .Appearance(AppearanceType.SithStrikerA)
                .RequirePerk(PerkType.Starships, 0)
                .MaxArmor(95)
                .MaxCapacitor(95)
                .MaxShield(45)
                .ShieldRechargeRate(18)
                .HighPowerNodes(8)
                .LowPowerNodes(8)
                .LaserSfx(VisualEffect.Vfx_Ship_Blast2)
                .LaserVfx(VisualEffect.Mirv_StarWars_Bolt3);
        }
    }
}
