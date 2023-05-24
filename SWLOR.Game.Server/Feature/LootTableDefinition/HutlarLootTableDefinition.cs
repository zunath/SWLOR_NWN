using System.Collections.Generic;
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
            QionHiveLoot();

            return _builder.Build();
        }

        private void Byysk()
        {
            _builder.Create("HUTLAR_BYYSK")
                .AddItem("elec_good", 10)
                .AddItem("fiberp_good", 10)
                .AddItem("byysk_meat", 20)
                .AddItem("byysk_tail", 10)

                .AddGold(40, 10);

            _builder.Create("HUTLAR_BYYSK_GEAR")
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
                .AddItem("agent_boots", 5);


            _builder.Create("HUTLAR_BYYSK_GEAR_RARES")
                .IsRare()
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
                .AddItem("agent_boots", 5);

            _builder.Create("HUTLAR_BYYSK_RARES")
                .IsRare()
                .AddItem("map_025", 2, 1, true)
                .AddItem("map_026", 2, 1, true)
                .AddItem("citrine", 1, 1, true);
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
                .AddItem("tiger_meat", 5);

            _builder.Create("HUTLAR_QION_TIGERS_RARES")
                .IsRare()
                .AddItem("q_tiger_paw", 2, 1, true);
        }

        private void QionHiveLoot()
        {
            _builder.Create("QIONHIVE_RESOURCES")
                .AddItem("ref_arkoxit", 1, 1, true)
                .AddItem("ref_jasioclase", 20)
                .AddItem("ref_gostian", 20)
                .AddItem("elec_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("emerald", 10)
                .AddItem("fiberp_high", 20)
                .AddItem("hyphae_wood", 20);

            _builder.Create("QIONHIVE_GEAR")
                .AddItem("dhar005s", 1)
                .AddItem("dlar005s", 1)
                .AddItem("dhhl005s", 1)
                .AddItem("dlhl005s", 1)
                .AddItem("dhbe005s", 1)
                .AddItem("dlbe005s", 1)
                .AddItem("dhlg005s", 1)
                .AddItem("dllg005s", 1)
                .AddItem("dhbr005s", 1)
                .AddItem("dhcl005s", 1)
                .AddItem("dlcl005s", 1)
                .AddItem("dlbr005s", 1)
                .AddItem("dhnk005s", 1)
                .AddItem("dlnk005s", 1)
                .AddItem("dhrg005s", 1)
                .AddItem("cara_armor", 1)
                .AddItem("cara_tunic", 1)
                .AddItem("cara_robes", 1)
                .AddItem("cara_cap", 1)
                .AddItem("cara_headdress", 1)
                .AddItem("cara_helmet", 1)
                .AddItem("cara_boots", 1)
                .AddItem("cara_leggings", 1)
                .AddItem("cara_shoes", 1)
                .AddItem("cara_bracer", 1)
                .AddItem("cara_gloves", 1)
                .AddItem("cara_wraps", 1)
                .AddItem("dlrg005s", 1);

            _builder.Create("QIONHIVE_BROODMOTHER")
                .AddItem("chiro_shard", 1);

            _builder.Create("QIONHIVE_CREDITS")
                .AddGold(500, 1);

            _builder.Create("QIONHIVE_CHAMPION_KEY")
                .AddItem("qion_championkey", 1);

            _builder.Create("QIONHIVE_SHAMAN_KEY")
                .AddItem("qion_shamankey", 1);

            _builder.Create("QIONHIVE_CHIEFTAIN_KEY")
                .AddItem("qion_chieftainke", 1);

            _builder.Create("QIONHIVE_BROODMOTHER_RECIPE")
                .AddItem("recipe_saberupg1", 10)
                .AddItem("recipe_staffupg1", 10)
                .AddItem("recipe_chigswd", 10)
                .AddItem("recipe_chispear", 10)
                .AddItem("recipe_chiknife", 10)
                .AddItem("recipe_chipistol", 10)
                .AddItem("recipe_chistaff", 10)
                .AddItem("recipe_chilngswd", 10)
                .AddItem("recipe_chikatar", 10)
                .AddItem("recipe_chishuri", 10)
                .AddItem("recipe_chirifle", 10)
                .AddItem("recipe_chitwinbl", 10)
                .AddItem("recipe_chielec", 10)
                .AddItem("recipe_chitelec", 10)
                .AddItem("recipe_chshield", 10)
                .AddItem("recipe_chcloak", 10)
                .AddItem("recipe_chbelt", 10)
                .AddItem("recipe_chring", 10)
                .AddItem("recipe_chneck", 10)
                .AddItem("recipe_chbreast", 10)
                .AddItem("recipe_chhelm", 10)
                .AddItem("recipe_chbracer", 10)
                .AddItem("recipe_chlegg", 10)
                .AddItem("recipe_mgcloak", 10)
                .AddItem("recipe_mgbelt", 10)
                .AddItem("recipe_mgring", 10)
                .AddItem("recipe_mgneck", 10)
                .AddItem("recipe_mgtunic", 10)
                .AddItem("recipe_mgcap", 10)
                .AddItem("recipe_mggloves", 10)
                .AddItem("recipe_mgboots", 10)
                .AddItem("recipe_imcloak", 10)
                .AddItem("recipe_imbelt", 10)
                .AddItem("recipe_imring", 10)
                .AddItem("recipe_imneck", 10)
                .AddItem("recipe_imtunic", 10)
                .AddItem("recipe_imcap", 10)
                .AddItem("recipe_imgloves", 10)
                .AddItem("recipe_imboots", 10)
                .AddItem("recipe_dhcl005c", 10)
                .AddItem("recipe_dhbe005c", 10)
                .AddItem("recipe_dhrg005c", 10)
                .AddItem("recipe_dhnk005c", 10)
                .AddItem("recipe_dhar005c", 10)
                .AddItem("recipe_dhhl005c", 10)
                .AddItem("recipe_dhbr005c", 10)
                .AddItem("recipe_dhlg005c", 10)
                .AddItem("recipe_dlcl005c", 10)
                .AddItem("recipe_dlbe005c", 10)
                .AddItem("recipe_dlrg005c", 10)
                .AddItem("recipe_dlnk005c", 10)
                .AddItem("recipe_dlar005c", 10)
                .AddItem("recipe_dlhl005c", 10)
                .AddItem("recipe_dlbr005c", 10)
                .AddItem("recipe_dllg005c", 10)
                .AddItem("recipe_brsushi", 10)
                .AddItem("recipe_ocsushi", 10)
                .AddItem("recipe_iksushi", 10)
                .AddItem("recipe_wisushi", 10)
                .AddItem("recipe_tesushi", 10)
                .AddItem("recipe_dosushi", 10);
        }
    }
}
