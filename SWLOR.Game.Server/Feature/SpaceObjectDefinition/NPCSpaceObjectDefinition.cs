using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    public class NPCSpaceObjectDefinition : ISpaceObjectListDefinition
    {
        private readonly SpaceObjectBuilder _builder = new();

        public Dictionary<string, SpaceObjectDetail> BuildSpaceObjects()
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
            _builder.Create("pirate_sybil")
                .ItemTag("NPC_Sybil")
                .ShipModule("com_laser_b");

            _builder.Create("pirate_terror")
                .ItemTag("NPC_Terror")
                .ShipModule("com_laser_b")
                .ShipModule("msl_launch_b");

            _builder.Create("npc_courier")
                .ItemTag("npc_courier")
                .ShipModule("blast_can_1")
                .ShipModule("blast_can_1");

            _builder.Create("npc_turret1")
                .ItemTag("npc_turret1")
                .ShipModule("blast_can_1")
                .ShipModule("blast_can_1");
        }

        private void Tier2()
        {
            _builder.Create("pirate_nightmare")
                .ItemTag("NPC_Nightmare")
                .ShipModule("ion_cann_1")
                .ShipModule("com_laser_1")
                .ShipModule("shld_boost_1");

            _builder.Create("pirate_ferron")
                .ItemTag("NPC_Ferron")
                .ShipModule("com_laser_1")
                .ShipModule("msl_launch_1")
                .ShipModule("msl_launch_1");

            _builder.Create("npc_shuttle")
                .ItemTag("NPC_Shuttle")
                .ShipModule("blast_can_2")
                .ShipModule("blast_can_2");

            _builder.Create("npc_turret2")
                .ItemTag("npc_turret2")
                .ShipModule("blast_can_2")
                .ShipModule("blast_can_2");
        }

        private void Tier3()
        {
            _builder.Create("pirate_storm")
                .ItemTag("NPC_Storm")
                .ShipModule("msl_launch_2")
                .ShipModule("msl_launch_2")
                .ShipModule("msl_launch_2");

            _builder.Create("pirate_ranger")
                .ItemTag("NPC_Ranger")
                .ShipModule("ion_cann_2")
                .ShipModule("ion_cann_2")
                .ShipModule("msl_launch_2");

            _builder.Create("npc_freighter")
                .ItemTag("NPC_Freighter")
                .ShipModule("blast_can_3")
                .ShipModule("blast_can_3");

            _builder.Create("npc_turret3")
                .ItemTag("npc_turret3")
                .ShipModule("blast_can_3")
                .ShipModule("blast_can_3");

            _builder.Create("npc_aurek")
                .ItemTag("npc_aurek")
                .ShipModule("blast_can_3");

            _builder.Create("npc_sithfig")
                .ItemTag("npc_sithfig")
                .ShipModule("blast_can_3");
        }

        private void Tier4()
        {
            _builder.Create("pirate_hammer")
                .ItemTag("NPC_Hammer")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("pirate_drake")
                .ItemTag("NPC_Drake")
                .ShipModule("ion_cann_3")
                .ShipModule("msl_launch_3")
                .ShipModule("msl_launch_3")
                .ShipModule("shld_boost_3");

            _builder.Create("npc_bulkf")
                .ItemTag("npc_bulkf")
                .ShipModule("blast_can_4")
                .ShipModule("blast_can_4")
                .ShipModule("blast_can_4");

            _builder.Create("npc_turret4")
                .ItemTag("npc_turret4")
                .ShipModule("blast_can_4")
                .ShipModule("blast_can_4")
                .ShipModule("blast_can_4");
        }

        private void Tier5()
        {
            _builder.Create("pirate_borealis")
                .ItemTag("NPC_Borealis")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("tgt_sys_4")
                .ShipModule("shld_boost_4");

            _builder.Create("pirate_eleyna")
                .ItemTag("NPC_Eleyna")
                .ShipModule("com_laser_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("tgt_sys_4")
                .ShipModule("shld_boost_4");

            _builder.Create("npc_merchant")
                .ItemTag("npc_merchant")
                .ShipModule("blast_can_5")
                .ShipModule("blast_can_5")
                .ShipModule("blast_can_5");

            _builder.Create("npc_turret5")
                .ItemTag("npc_turret5")
                .ShipModule("blast_can_5")
                .ShipModule("blast_can_5")
                .ShipModule("blast_can_5");

            _builder.Create("cap_corvette")
                .ItemTag("cap_corvette")
                .ShipModule("cap_turbo_1")
                .ShipModule("cap_missile_1")
                .ShipModule("cap_pdl_1");

            _builder.Create("cap_frigate")
                .ItemTag("cap_frigate")
                .ShipModule("cap_turbo_2")
                .ShipModule("cap_missile_2")
                .ShipModule("cap_pdl_2");

            _builder.Create("cap_cruiser")
                .ItemTag("cap_cruiser")
                .ShipModule("cap_turbo_3")
                .ShipModule("cap_missile_3")
                .ShipModule("cap_pdl_3");

            _builder.Create("cap_hvycrui")
                .ItemTag("cap_hvycrui")
                .ShipModule("cap_turbo_4")
                .ShipModule("cap_missile_4")
                .ShipModule("cap_pdl_4");

            _builder.Create("cap_btlcrui")
                .ItemTag("cap_btlcrui")
                .ShipModule("cap_turbo_5")
                .ShipModule("cap_missile_5")
                .ShipModule("cap_pdl_5");

            _builder.Create("cap_btlship")
                .ItemTag("cap_btlship")
                .ShipModule("cap_turbo_6")
                .ShipModule("cap_missile_6")
                .ShipModule("cap_pdl_6");

            _builder.Create("cap_dread")
                .ItemTag("cap_dread")
                .ShipModule("cap_turbo_7")
                .ShipModule("cap_missile_7")
                .ShipModule("cap_pdl_7");
        }
    }
}
