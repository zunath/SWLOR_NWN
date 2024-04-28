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
            _builder.Create("t1bomber")
                .ItemTag("NPC_Bomber1")
                .ShipModule("config_bmb1")
                .ShipModule("msl_launch_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1fighter")
                .ItemTag("NPC_Fighter1")
                .ShipModule("config_fig1")
                .ShipModule("ion_cann_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1interceptor")
                .ItemTag("NPC_Interceptor1")
                .ShipModule("config_int1")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("t1gunship")
                .ItemTag("NPC_Gunship1")
                .ShipModule("config_fig1")
                .ShipModule("ion_cann_b")
                .ShipModule("msl_launch_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("NPC_Platform1")
                .ItemTag("t1platform")
                .ShipModule("config_bmb1")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("NPC_Cargo1")
                .ItemTag("t1cargo")
                .ShipModule("config_ind1")
                .ShipModule("ion_cann_b")
                .ShipModule("ion_cann_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");

            _builder.Create("NPC_Capital1")
                .ItemTag("t1capital")
                .ShipModule("npc_cap1")
                .ShipModule("ion_cann_b")
                .ShipModule("msl_launch_b")
                .ShipModule("msl_launch_b")
                .ShipModule("com_laser_b")
                .ShipModule("com_laser_b");
        }

        private void Tier2()
        {
            _builder.Create("t2bomber")
                .ItemTag("NPC_Bomber2")
                .ShipModule("config_bmb2")
                .ShipModule("msl_launch_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2fighter")
                .ItemTag("NPC_Fighter2")
                .ShipModule("config_fig2")
                .ShipModule("ion_cann_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2interceptor")
                .ItemTag("NPC_Interceptor2")
                .ShipModule("config_int2")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("t2gunship")
                .ItemTag("NPC_Gunship2")
                .ShipModule("config_fig2")
                .ShipModule("ion_cann_1")
                .ShipModule("msl_launch_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("NPC_Platform2")
                .ItemTag("t2platform")
                .ShipModule("config_bmb2")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("NPC_Cargo2")
                .ItemTag("t2cargo")
                .ShipModule("config_ind2")
                .ShipModule("ion_cann_1")
                .ShipModule("ion_cann_1")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");

            _builder.Create("NPC_Capital2")
                .ItemTag("t2capital")
                .ShipModule("npc_cap1")
                .ShipModule("ion_cann_2")
                .ShipModule("msl_launch_2")
                .ShipModule("msl_launch_2")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1");
        }

        private void Tier3()
        {
            _builder.Create("t3bomber")
                .ItemTag("NPC_Bomber3")
                .ShipModule("config_bmb3")
                .ShipModule("msl_launch_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3fighter")
                .ItemTag("NPC_Fighter3")
                .ShipModule("config_fig3")
                .ShipModule("ion_cann_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3interceptor")
                .ItemTag("NPC_Interceptor3")
                .ShipModule("config_int3")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("t3gunship")
                .ItemTag("NPC_Gunship3")
                .ShipModule("config_fig3")
                .ShipModule("ion_cann_2")
                .ShipModule("msl_launch_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("NPC_Platform3")
                .ItemTag("t3platform")
                .ShipModule("config_bmb3")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("NPC_Cargo3")
                .ItemTag("t3cargo")
                .ShipModule("config_ind3")
                .ShipModule("ion_cann_2")
                .ShipModule("ion_cann_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");

            _builder.Create("NPC_Capital3")
                .ItemTag("t3capital")
                .ShipModule("npc_cap3")
                .ShipModule("ion_cann_2")
                .ShipModule("msl_launch_2")
                .ShipModule("msl_launch_2")
                .ShipModule("com_laser_2")
                .ShipModule("com_laser_2");
        }

        private void Tier4()
        {
            _builder.Create("t4bomber")
                .ItemTag("NPC_Bomber4")
                .ShipModule("config_bmb4")
                .ShipModule("msl_launch_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4fighter")
                .ItemTag("NPC_Fighter4")
                .ShipModule("config_fig4")
                .ShipModule("ion_cann_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4interceptor")
                .ItemTag("NPC_Interceptor4")
                .ShipModule("config_int4")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("t4gunship")
                .ItemTag("NPC_Gunship4")
                .ShipModule("config_fig4")
                .ShipModule("ion_cann_3")
                .ShipModule("msl_launch_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("NPC_Platform4")
                .ItemTag("t4platform")
                .ShipModule("config_bmb4")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("NPC_Cargo4")
                .ItemTag("t4cargo")
                .ShipModule("config_ind4")
                .ShipModule("ion_cann_3")
                .ShipModule("ion_cann_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");

            _builder.Create("NPC_Capital4")
                .ItemTag("t4capital")
                .ShipModule("npc_cap4")
                .ShipModule("ion_cann_3")
                .ShipModule("msl_launch_3")
                .ShipModule("msl_launch_3")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3");
        }

        private void Tier5()
        {
            _builder.Create("t5bomber")
                .ItemTag("NPC_Bomber5")
                .ShipModule("config_bmb5")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5fighter")
                .ItemTag("NPC_Fighter5")
                .ShipModule("config_fig5")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5interceptor")
                .ItemTag("NPC_Interceptor5")
                .ShipModule("config_int5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t5gunship")
                .ItemTag("NPC_Gunship5")
                .ShipModule("config_fig5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_Platform5")
                .ItemTag("t5platform")
                .ShipModule("config_bmb5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_Cargo5")
                .ItemTag("t5cargo")
                .ShipModule("config_ind5")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_Capital5")
                .ItemTag("t5capital")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6bomber")
                .ItemTag("NPC_Bomber6")
                .ShipModule("config_bmb5")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6fighter")
                .ItemTag("NPC_Fighter6")
                .ShipModule("config_fig5")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6interceptor")
                .ItemTag("NPC_Interceptor6")
                .ShipModule("config_int5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("t6gunship")
                .ItemTag("NPC_Gunship6")
                .ShipModule("config_fig5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_Platform6")
                .ItemTag("t6platform")
                .ShipModule("config_bmb5")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_Cargo6")
                .ItemTag("t6cargo")
                .ShipModule("config_ind5")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_MandoCap1")
                .ItemTag("mandocap1")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_MandoCap2")
                .ItemTag("mandocap2")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_MandoCap3")
                .ItemTag("mandocap3")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap1")
                .ItemTag("sithcap1")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap2")
                .ItemTag("sithcap2")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap3")
                .ItemTag("sithcap3")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap4")
                .ItemTag("sithcap4")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap5")
                .ItemTag("sithcap5")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap6")
                .ItemTag("sithcap6")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");

            _builder.Create("NPC_SithCap7")
                .ItemTag("sithcap7")
                .ShipModule("npc_cap5")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("msl_launch_4")
                .ShipModule("com_laser_4")
                .ShipModule("com_laser_4");
        }
    }
}
