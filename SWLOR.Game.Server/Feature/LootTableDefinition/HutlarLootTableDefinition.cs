﻿using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class HutlarLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new LootTableBuilder();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Byysk();
            QionSlugs();
            QionTigers();
            FrostSaber();
            CaveYeti();

            return _builder.Build();
        }

        private void Byysk()
        {
            _builder.Create("HUTLAR_BYYSK")
                .AddItem("elec_good", 10)
                .AddItem("fiberp_good", 10)
                .AddItem("byysk_longsword", 5)
                .AddItem("byysk_knife", 5)
                .AddItem("byysk_gswd", 5)
                .AddItem("byysk_spear", 5)
                .AddItem("byysk_katar", 5)
                .AddItem("byysk_staff", 5)
                .AddItem("byysk_pistol", 5)
                .AddItem("byysk_shuriken", 5)
                .AddItem("byysk_twinblade", 5)
                .AddItem("byysk_rifle", 5)
                
                .AddItem("byysk_shield", 5)
                .AddItem("byysk_cloak", 5)
                .AddItem("byysk_belt", 5)
                .AddItem("byysk_ring", 5)
                .AddItem("byysk_necklace", 5)
                .AddItem("byysk_armor", 5)
                .AddItem("byysk_helmet", 5)
                .AddItem("byysk_bracer", 5)
                .AddItem("byysk_leggings", 5)

                .AddItem("inq_cloak", 5)
                .AddItem("inq_belt", 5)
                .AddItem("inq_ring", 5)
                .AddItem("inq_necklace", 5)
                .AddItem("inq_tunic", 5)
                .AddItem("inq_cap", 5)
                .AddItem("inq_gloves", 5)
                .AddItem("inq_boots", 5)

                .AddItem("agent_cloak", 5)
                .AddItem("agent_belt", 5)
                .AddItem("agent_ring", 5)
                .AddItem("agent_necklace", 5)
                .AddItem("agent_tunic", 5)
                .AddItem("agent_cap", 5)
                .AddItem("agent_gloves", 5)
                .AddItem("agent_boots", 5)

                .AddItem("byysk_meat", 20)
                .AddItem("byysk_tail", 10)

                .AddGold(40, 10)
                ;


            _builder.Create("HUTLAR_BYYSK_RARES")
                .AddItem("map_025", 20, 1, true)
                .AddItem("map_026", 20, 1, true)
                .AddItem("citrine", 5, 1, true);
        }

        private void QionSlugs()
        {
            _builder.Create("HUTLAR_QION_SLUGS")
                .AddItem("slug_bile", 5)
                .AddItem("slug_tooth", 20);
        }

        private void QionTigers()
        {
            _builder.Create("HUTLAR_QION_TIGERS")
                .AddItem("lth_good", 20)
                .AddItem("qion_tiger_fang", 10)
                .AddItem("tiger_blood", 8)
                .AddItem("tiger_meat", 5)
                .AddItem("q_tiger_paw", 2, 1, true);
        }

        private void FrostSaber()
        {
            _builder.Create("HUTLAR_FROST_SABER")
                .AddItem("lth_good", 20)
                .AddItem("f_saber_fur", 10)
                .AddItem("f_saber_claw", 8)
                .AddItem("f_saber_meat", 15)
                .AddItem("f_saber_tooth", 8)
                .AddItem("f_saber_heart", 2, 1, true);
        }
        private void CaveYeti()
        {
            _builder.Create("HUTLAR_CAVE_YETI")
                .AddItem("lth_good", 20)
                .AddItem("c_yeti_fur", 10)
                .AddItem("c_yeti_claw", 8)
                .AddItem("c_yeti_meat", 15)
                .AddItem("c_yeti_horn", 2, 1, true);
        }
    }
}
