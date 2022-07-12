using System.Collections.Generic;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.SpaceObjectDefinition
{
    public class NPCSpaceObjectDefinition: ISpaceObjectListDefinition
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
        }

        private void Tier2()
        {
            _builder.Create("pirate_night")
                .ItemTag("NPC_Nightmare")
                .ShipModule("com_laser_1")
                .ShipModule("com_laser_1")
                .ShipModule("shld_boost_1");

            _builder.Create("pirate_ferron")
                .ItemTag("NPC_Ferron")
                .ShipModule("com_laser_1")
                .ShipModule("msl_launch_1");
        }

        private void Tier3()
        {
            _builder.Create("pirate_storm")
                .ItemTag("NPC_Storm")
                .ShipModule("msl_launch_2")
                .ShipModule("msl_launch_2");

            _builder.Create("pirate_ranger")
                .ItemTag("NPC_Ranger")
                .ShipModule("ion_cann_2")
                .ShipModule("ion_cann_2")
                .ShipModule("msl_launch_2");
        }

        private void Tier4()
        {
            _builder.Create("pirate_hammer")
                .ItemTag("NPC_Hammer")
                .ShipModule("com_laser_3")
                .ShipModule("com_laser_3")
                .ShipModule("shld_boost_3");

            _builder.Create("pirate_drake")
                .ItemTag("NPC_Drake")
                .ShipModule("com_laser_3")
                .ShipModule("msl_launch_3")
                .ShipModule("tgt_sys_3");
        }

        private void Tier5()
        {
            _builder.Create("pirate_borealis")
                .ItemTag("NPC_Borealis")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("tgt_sys_4")
                .ShipModule("tgt_sys_4")
                .ShipModule("shld_boost_4");

            _builder.Create("pirate_eleyna")
                .ItemTag("NPC_Eleyna")
                .ShipModule("ion_cann_4")
                .ShipModule("ion_cann_4")
                .ShipModule("msl_launch_4")
                .ShipModule("tgt_sys_4")
                .ShipModule("shld_boost_4");
        }
    }
}
