using System.Collections.Generic;
using System.Runtime.InteropServices;
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
            _builder.Create("t1bomber")
                .ItemTag("NPC_Bomber1")
                .ShipModule("con_bmb1")
                .ShipModule("msl_launch_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1fighter")
                .ItemTag("NPC_Fighter1")
                .ShipModule("con_fig1")
                .ShipModule("ion_cann_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1interceptor")
                .ItemTag("NPC_Interceptor1")
                .ShipModule("con_int1")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1gunship")
                .ItemTag("NPC_Gunship1")
                .ShipModule("con_hvy1")
                .ShipModule("ion_cann_b")
                .ShipModule("msl_launch_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1platform")
                .ItemTag("NPC_Platform1")
                .ShipModule("con_hvy1")
                .ShipModule("npcautolas1")
                .ShipModule("beamcannon1")
                .ShipModule("beamcannon1");

            _builder.Create("t1cargo")
                .ItemTag("NPC_Cargo1")
                .ShipModule("con_hvy1")
                .ShipModule("ion_cann_b")
                .ShipModule("ion_cann_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1capital")
                .ItemTag("NPC_Capital1")
                .ShipModule("npc_cap1")
                .ShipModule("npcautolas1")
                .ShipModule("npc_quadlas1")
                .ShipModule("npc_quadlas1")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");
        }

        private void Tier2()
        {
            _builder.Create("t2bomber")
                .ItemTag("NPC_Bomber2")
                .ShipModule("con_bmb2")
                .ShipModule("msl_launch_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2fighter")
                .ItemTag("NPC_Fighter2")
                .ShipModule("con_fig2")
                .ShipModule("ion_cann_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2interceptor")
                .ItemTag("NPC_Interceptor2")
                .ShipModule("con_int2")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2gunship")
                .ItemTag("NPC_Gunship2")
                .ShipModule("con_hvy2")
                .ShipModule("ion_cann_1")
                .ShipModule("msl_launch_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2platform")
                .ItemTag("NPC_Platform2")
                .ShipModule("con_hvy2")
                .ShipModule("npcautolas2")
                .ShipModule("beamcannon2")
                .ShipModule("beamcannon2");

            _builder.Create("t2cargo")
                .ItemTag("NPC_Cargo2")
                .ShipModule("con_hvy2")
                .ShipModule("ion_cann_1")
                .ShipModule("ion_cann_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2capital")
                .ItemTag("NPC_Capital2")
                .ShipModule("npc_cap2")
                .ShipModule("npcautolas2")
                .ShipModule("npc_quadlas2")
                .ShipModule("npc_quadlas2")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");
        }

        private void Tier3()
        {
            _builder.Create("t3bomber")
                .ItemTag("NPC_Bomber3")
                .ShipModule("con_bmb3")
                .ShipModule("msl_launch_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3fighter")
                .ItemTag("NPC_Fighter3")
                .ShipModule("con_fig3")
                .ShipModule("ion_cann_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3interceptor")
                .ItemTag("NPC_Interceptor3")
                .ShipModule("con_int3")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3gunship")
                .ItemTag("NPC_Gunship3")
                .ShipModule("con_hvy3")
                .ShipModule("ion_cann_2")
                .ShipModule("msl_launch_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3platform")
                .ItemTag("NPC_Platform3")
                .ShipModule("con_hvy3")
                .ShipModule("npcautolas3")
                .ShipModule("beamcannon3")
                .ShipModule("beamcannon3");

            _builder.Create("t3cargo")
                .ItemTag("NPC_Cargo3")
                .ShipModule("con_hvy3")
                .ShipModule("ion_cann_2")
                .ShipModule("ion_cann_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3capital")
                .ItemTag("NPC_Capital3")
                .ShipModule("npcautolas3")
                .ShipModule("npc_quadlas3")
                .ShipModule("npc_quadlas3")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");
        }

        private void Tier4()
        {
            _builder.Create("t4bomber")
                .ItemTag("NPC_Bomber4")
                .ShipModule("con_bmb4")
                .ShipModule("msl_launch_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4fighter")
                .ItemTag("NPC_Fighter4")
                .ShipModule("con_fig4")
                .ShipModule("ion_cann_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4interceptor")
                .ItemTag("NPC_Interceptor4")
                .ShipModule("con_int4")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4gunship")
                .ItemTag("NPC_Gunship4")
                .ShipModule("con_hvy4")
                .ShipModule("ion_cann_3")
                .ShipModule("msl_launch_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4platform")
                .ItemTag("NPC_Platform4")
                .ShipModule("con_hvy4")
                .ShipModule("npcautolas4")
                .ShipModule("beamcannon4")
                .ShipModule("beamcannon4");

            _builder.Create("t4cargo")
                .ItemTag("NPC_Cargo4")
                .ShipModule("con_hvy4")
                .ShipModule("ion_cann_3")
                .ShipModule("ion_cann_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4capital")
                .ItemTag("NPC_Capital4")
                .ShipModule("npc_cap4")
                .ShipModule("npcautolas4")
                .ShipModule("npc_quadlas4")
                .ShipModule("npc_quadlas4")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");
        }

        private void Tier5()
        {
            _builder.Create("t5bomber")
                .ItemTag("NPC_Bomber5")
                .ShipModule("con_bmb5")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5fighter")
                .ItemTag("NPC_Fighter5")
                .ShipModule("con_fig5")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5interceptor")
                .ItemTag("NPC_Interceptor5")
                .ShipModule("con_int5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5gunship")
                .ItemTag("NPC_Gunship5")
                .ShipModule("con_hvy5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5platform")
                .ItemTag("NPC_Platform5")
                .ShipModule("con_hvy5")
                .ShipModule("npcautolas5")
                .ShipModule("beamcannon5")
                .ShipModule("beamcannon5");

            _builder.Create("t5cargo")
                .ItemTag("NPC_Cargo5")
                .ShipModule("con_hvy5")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5capital")
                .ItemTag("NPC_Capital5")
                .ShipModule("npc_cap5")
                .ShipModule("npcautolas5")
                .ShipModule("npc_quadlas5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("storm_cann");

            _builder.Create("t6bomber")
                .ItemTag("NPC_Bomber6")
                .ShipModule("con_bmb5")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6fighter")
                .ItemTag("NPC_Fighter6")
                .ShipModule("con_fig5")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6interceptor")
                .ItemTag("NPC_Interceptor6")
                .ShipModule("con_int5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6gunship")
                .ItemTag("NPC_Gunship6")
                .ShipModule("con_hvy6")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6platform")
                .ItemTag("NPC_Platform6")
                .ShipModule("con_hvy6")
                .ShipModule("npcautolas6")
                .ShipModule("beamcannon5")
                .ShipModule("beamcannon5");

            _builder.Create("t6cargo")
                .ItemTag("NPC_Cargo6")
                .ShipModule("con_hvy6")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("mandocap1")
                .ItemTag("NPC_MandoCap1")
                .ShipModule("con_cap3")
                .ShipModule("npcautolas8")
                .ShipModule("npc_quadlas8")
                .ShipModule("storm_cann")
                .ShipModule("turbolas1")
                .ShipModule("turbolas1");

            _builder.Create("mandocap2")
                .ItemTag("NPC_MandoCap2")
                .ShipModule("con_cap6")
                .ShipModule("npcautolas11")
                .ShipModule("npc_quadlas11")
                .ShipModule("npc_quadlas11")
                .ShipModule("turbolas3")
                .ShipModule("acm_launch_1");

            _builder.Create("mandocap3")
                .ItemTag("NPC_MandoCap3")
                .ShipModule("con_cap7")
                .ShipModule("npcautolas12")
                .ShipModule("npc_quadlas12")
                .ShipModule("npc_quadlas12")
                .ShipModule("turbolas3")
                .ShipModule("acm_launch_1")
                .ShipModule("storm_cann");

            _builder.Create("sithcap1")
                .ItemTag("NPC_SithCap1")
                .ShipModule("con_cap1")
                .ShipModule("npc_quadlas1")
                .ShipModule("npc_quadlas1")
                .ShipModule("turbolas1");

            _builder.Create("sithcap2")
                .ItemTag("NPC_SithCap2")
                .ShipModule("con_cap2")
                .ShipModule("npcautolas8")
                .ShipModule("npc_quadlas8")
                .ShipModule("npc_quadlas8")
                .ShipModule("npc_quadlas8")
                .ShipModule("turbolas1");

            _builder.Create("sithcap3")
                .ItemTag("NPC_SithCap3")
                .ShipModule("con_cap3")
                .ShipModule("npcautolas8")
                .ShipModule("npc_quadlas8")
                .ShipModule("storm_cann")
                .ShipModule("turbolas1")
                .ShipModule("turbolas1");

            _builder.Create("sithcap4")
                .ItemTag("NPC_SithCap4")
                .ShipModule("con_cap4")
                .ShipModule("npcautolas9")
                .ShipModule("npc_quadlas9")
                .ShipModule("npc_quadlas9")
                .ShipModule("turbolas2")
                .ShipModule("turbolas2")
                .ShipModule("turbolas1");

            _builder.Create("sithcap5")
                .ItemTag("NPC_SithCap5")
                .ShipModule("con_cap5")
                .ShipModule("npcautolas10")
                .ShipModule("npc_quadlas10")
                .ShipModule("npc_quadlas10")
                .ShipModule("turbolas3")
                .ShipModule("turbolas2")
                .ShipModule("acm_launch_1");

            _builder.Create("sithcap6")
                .ItemTag("NPC_SithCap6")
                .ShipModule("con_cap6")
                .ShipModule("npcautolas11")
                .ShipModule("npc_quadlas11")
                .ShipModule("npc_quadlas11")
                .ShipModule("turbolas3")
                .ShipModule("acm_launch_1");

            _builder.Create("sithcap7")
                .ItemTag("NPC_SithCap7")
                .ShipModule("con_cap7")
                .ShipModule("npcautolas12")
                .ShipModule("npc_quadlas12")
                .ShipModule("npc_quadlas12")
                .ShipModule("turbolas3")
                .ShipModule("acm_launch_1")
                .ShipModule("storm_cann");
        }
    }
}
