using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class SpaceLootTableDefinition : ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Tier1();
            Tier2();
            Tier3();
            Tier4();
            Tier5();
            WildSpace();

            return _builder.Build();
        }

        private void Tier1()
        {
            _builder.Create("SPACE_BASIC_T1")
                .AddItem("elec_ruined", 7, 1)
                .AddItem("ref_tilarium", 5, 1)
                .AddGold(500, 2)
                .AddItem("ref_veldite", 2, 1)
                .AddItem("ship_missile", 2, 4)
                .AddItem("ship_fuelcapsule", 1, 1);

            _builder.Create("SPACE_BASIC_T1_RARES")
                .IsRare()
                .AddItem("jade", 1, 1, true)
                .AddItem("aluminum", 1, 1, true);

            _builder.Create("SPACE_ELITE_T1")
                .AddItem("elec_ruined", 3, 1)
                .AddItem("ref_tilarium", 3, 1)
                .AddGold(2000, 1)
                .AddItem("ref_veldite", 3, 1)
                .AddItem("lth_ruined", 2, 1)
                .AddItem("jade", 1, 1)
                .AddItem("aluminum", 1, 1)
                .AddItem("wood", 3, 1)
                .AddItem("fiberp_ruined", 3, 1)
                .AddItem("quadrenium", 1, 1, true);

            _builder.Create("SPACE_BOSS_T1")
                .AddGold(1500, 1)
                .AddItem("quadrenium", 1, 2);
        }

        private void Tier2()
        {
            _builder.Create("SPACE_BASIC_T2")
                .AddItem("elec_flawed", 7, 1)
                .AddItem("ref_scordspar", 5, 1)
                .AddGold(750, 2)
                .AddItem("ref_currian", 2, 1)
                .AddItem("ship_fuelcapsule", 1, 2)
                .AddItem("ship_missile", 2, 6);

            _builder.Create("SPACE_BASIC_T2_RARES")
                .IsRare()
                .AddItem("agate", 1, 1, true)
                .AddItem("steel", 1, 1, true);

            _builder.Create("SPACE_ELITE_T2")
                .AddItem("elec_flawed", 3, 1)
                .AddItem("ref_scordspar", 3, 1)
                .AddGold(1000, 1)
                .AddItem("ref_currian", 3, 1)
                .AddItem("lth_flawed", 3, 2)
                .AddItem("agate", 1, 1)
                .AddItem("steel", 1, 1)
                .AddItem("fine_wood", 3, 2)
                .AddItem("fiberp_flawed", 3, 2)
                .AddItem("vintrium", 1, 1, true);

            _builder.Create("SPACE_BOSS_T2")
                .AddGold(3000, 1)
                .AddItem("vintrium", 1, 2);
        }

        private void Tier3()
        {
            _builder.Create("SPACE_BASIC_T3")
                .AddItem("elec_good", 7, 1)
                .AddItem("ref_plagionite", 5, 1)
                .AddGold(1000, 2)
                .AddItem("ref_idailia", 2, 1)
                .AddItem("ship_fuelcapsule", 1, 3)
                .AddItem("ship_missile", 2, 8);

            _builder.Create("SPACE_BASIC_T3_RARES")
                .IsRare()
                .AddItem("citrine", 1, 1, true)
                .AddItem("obsidian", 1, 1, true);

            _builder.Create("SPACE_ELITE_T3")
                .AddItem("elec_good", 3, 1)
                .AddItem("ref_plagionite", 3, 1)
                .AddGold(1250, 1)
                .AddItem("ref_idailia", 3, 1)
                .AddItem("lth_good", 3, 2)
                .AddItem("citrine", 1, 1)
                .AddItem("obsidian", 1, 1)
                .AddItem("ancient_wood", 3, 2)
                .AddItem("fiberp_good", 3, 2)
                .AddItem("ionite", 1, 1, true);

            _builder.Create("SPACE_BOSS_T3")
                .AddGold(5000, 1)
                .AddItem("ionite", 1, 2);
        }

        private void Tier4()
        {
            _builder.Create("SPACE_BASIC_T4")
                .AddItem("elec_imperfect", 7, 1)
                .AddItem("ref_keromber", 5, 1)
                .AddGold(1250, 1)
                .AddItem("ref_barinium", 2, 1)
                .AddItem("ship_fuelcapsule", 1, 4)
                .AddItem("ship_missile", 2, 10);

            _builder.Create("SPACE_BASIC_T4_RARES")
                .IsRare()
                .AddItem("crystal", 1, 1, true)
                .AddItem("ruby", 1, 1, true);

            _builder.Create("SPACE_ELITE_T4")
                .AddItem("elec_imperfect", 3, 1)
                .AddItem("ref_keromber", 3, 1)
                .AddGold(1500, 1)
                .AddItem("ref_barinium", 3, 1)
                .AddItem("lth_imperfect", 3, 2)
                .AddItem("crystal", 1, 1)
                .AddItem("ruby", 1, 1)
                .AddItem("aracia_wood", 3, 2)
                .AddItem("fiberp_imperfect", 3, 2)
                .AddItem("katrium", 1, 1, true);

            _builder.Create("SPACE_BOSS_T4")
                .AddGold(7000, 1)
                .AddItem("katrium", 1, 2);
        }

        private void Tier5()
        {
            _builder.Create("SPACE_BASIC_T5")
                .AddItem("elec_high", 7, 1)
                .AddItem("ref_gostian", 5, 1)
                .AddGold(1500, 1)
                .AddItem("ref_jasioclase", 2, 1)
                .AddItem("ship_fuelcapsule", 1, 5)
                .AddItem("ship_missile", 2, 12);

            _builder.Create("SPACE_BASIC_T5_RARES")
                .IsRare()
                .AddItem("diamond", 1, 1, true)
                .AddItem("emerald", 1, 1, true);

            _builder.Create("SPACE_ELITE_T5")
                .AddItem("elec_high", 3, 1)
                .AddItem("ref_gostian", 3, 1)
                .AddGold(2000, 1)
                .AddItem("ref_jasioclase", 3, 1)
                .AddItem("lth_high", 3, 2)
                .AddItem("diamond", 1, 1)
                .AddItem("emerald", 1, 1)
                .AddItem("hyphae_wood", 3, 2)
                .AddItem("fiberp_high", 3, 2)
                .AddItem("proton_bomb", 1, 1, true)
                .AddItem("zinsiam", 1, 1, true)
                .AddItem("ref_arda", 2, 1, true);

            // Module Schematics for the named rank-50 modules drop at weight 1 each; the table's own entries are
            // weighted x150 so their odds stay as they were.
            _builder.Create("SPACE_BOSS_T5")
                .AddGold(10000, 150)
                .AddItem("zinsiam", 150, 2)
                .AddItem("recipe_laser50", 1)
                .AddItem("recipe_ion50", 1)
                .AddItem("recipe_beam50", 1)
                .AddItem("recipe_missl50", 1)
                .AddItem("recipe_torp50", 1)
                .AddItem("recipe_pdef50", 1)
                .AddItem("recipe_shbst50", 1)
                .AddItem("recipe_shrep50", 1)
                .AddItem("recipe_hard50", 1)
                .AddItem("recipe_capb50", 1)
                .AddItem("recipe_tcomp50", 1)
                .AddItem("recipe_plate50", 1)
                .AddItem("recipe_hrep50", 1)
                .AddItem("recipe_resall50", 1)
                .AddItem("recipe_capr50", 1)
                .AddItem("recipe_thrust50", 1);

            _builder.Create("SPACE_BASIC_T6")
                .AddItem("elec_high", 8, 2)
                .AddItem("ref_gostian", 5, 2)
                .AddGold(3000, 2)
                .AddItem("ref_jasioclase", 2, 2)
                .AddItem("ship_fuelcapsule", 1, 6)
                .AddItem("ship_missile", 2, 15);

            _builder.Create("SPACE_BASIC_T6_RARES")
                .IsRare()
                .AddItem("diamond", 1, 1, true)
                .AddItem("emerald", 1, 1, true);

            _builder.Create("SPACE_ELITE_T6")
                .AddItem("elec_high", 4, 2)
                .AddItem("ref_gostian", 4, 2)
                .AddGold(3000, 1)
                .AddItem("ref_jasioclase", 2, 1)
                .AddItem("lth_high", 2, 2)
                .AddItem("diamond", 3, 1)
                .AddItem("emerald", 3, 1)
                .AddItem("hyphae_wood", 3, 2)
                .AddItem("fiberp_high", 3, 2)
                .AddItem("proton_bomb", 2, 1, true)
                .AddItem("zinsiam", 2, 1, true)
                .AddItem("ref_arda", 3, 1, true);

            // Module Schematics at weight 1 each; the table's own entries are weighted x10 so their odds stay as they were.
            _builder.Create("SPACE_BOSS_T6")
                .AddGold(15000, 10)
                .AddItem("zinsiam", 10, 2)
                .AddItem("recipe_modacmiss", 10)
                .AddItem("recipe_modacml", 10)
                .AddItem("recipe_modbulwar", 10)
                .AddItem("recipe_capmodcpu", 10)
                .AddItem("recipe_capreact", 10)
                .AddItem("recipe_capthrust", 10)
                .AddItem("recipe_capconst", 10)
                .AddItem("recipe_cappwrlay", 10)
                .AddItem("recipe_capshgen", 10)
                .AddItem("recipe_modadvthr", 10)
                .AddItem("recipe_capmodtec", 10)
                .AddItem("recipe_corv", 10)
                .AddItem("recipe_corveng", 10)
                .AddItem("recipe_corvhull", 10)
                .AddItem("recipe_corvpsys", 10)
                .AddItem("recipe_corvrebay", 10)
                .AddItem("recipe_conind", 10)
                .AddItem("recipe_modlasbat", 10)
                .AddItem("recipe_modquadlz", 10)
                .AddItem("recipe_modredsgn", 10)
                .AddItem("recipe_modreplat", 10)
                .AddItem("recipe_modrepfld", 10)
                .AddItem("recipe_conskrm", 10)
                .AddItem("recipe_modstmcan", 10)
                .AddItem("recipe_modtarget", 10)
                .AddItem("recipe_modturbo", 10)
                .AddItem("recipe_conwar", 10)
                .AddItem("recipe_modsmin", 10)
                .AddItem("recipe_wcomp", 10)
                .AddItem("recipe_diverter", 10)
                .AddItem("recipe_ewar", 10)
                .AddItem("recipe_laser50", 1)
                .AddItem("recipe_ion50", 1)
                .AddItem("recipe_beam50", 1)
                .AddItem("recipe_missl50", 1)
                .AddItem("recipe_torp50", 1)
                .AddItem("recipe_pdef50", 1)
                .AddItem("recipe_shbst50", 1)
                .AddItem("recipe_shrep50", 1)
                .AddItem("recipe_hard50", 1)
                .AddItem("recipe_capb50", 1)
                .AddItem("recipe_tcomp50", 1)
                .AddItem("recipe_plate50", 1)
                .AddItem("recipe_hrep50", 1)
                .AddItem("recipe_resall50", 1)
                .AddItem("recipe_capr50", 1)
                .AddItem("recipe_thrust50", 1);

            _builder.Create("THORILIDE")
                .AddItem("thor_crys", 1);
        }

        /// <summary>
        /// The Rift Raiders' tables (Wild Space, plan section 23): tier 5 goods with Aurodium, and as rare drops Thorilide,
        /// Prototype Tech, the tier 5.5 Rift Key, the Rift Raider ships' Design and Ship Schematics (Wild Space only) and
        /// the Module Schematics of the named rank-50 modules. tools/SpaceRevampBalance/wildspace.py --sim prices them
        /// against tier 5 combat.
        /// </summary>
        private void WildSpace()
        {
            _builder.Create("SPACE_BASIC_WS")
                .AddItem("elec_high", 7, 1)
                .AddItem("ref_gostian", 5, 1)
                .AddGold(1350, 1)
                .AddItem("ref_jasioclase", 3, 1)
                .AddItem("aurodium", 2, 2)
                .AddItem("ship_fuelcapsule", 1, 5)
                .AddItem("ship_missile", 2, 12);

            _builder.Create("SPACE_BASIC_WS_RARES")
                .IsRare()
                .AddItem("diamond", 2, 1, true)
                .AddItem("emerald", 2, 1, true)
                .AddItem("thor_crys", 3, 1, true)
                .AddItem("proto_tech", 2, 1, true)
                .AddItem("rift_key_t55", 1, 1, true);

            _builder.Create("SPACE_ELITE_WS")
                .AddItem("elec_high", 3, 1)
                .AddItem("ref_gostian", 3, 1)
                .AddGold(2400, 1)
                .AddItem("ref_jasioclase", 3, 1)
                .AddItem("lth_high", 3, 2)
                .AddItem("diamond", 1, 1)
                .AddItem("emerald", 1, 1)
                .AddItem("hyphae_wood", 3, 2)
                .AddItem("fiberp_high", 3, 2)
                .AddItem("aurodium", 3, 2)
                .AddItem("proton_bomb", 1, 1, true)
                .AddItem("zinsiam", 1, 1, true)
                .AddItem("ref_arda", 2, 1, true)
                .AddItem("thor_crys", 2, 1, true)
                .AddItem("proto_tech", 2, 1, true)
                .AddItem("rift_key_t55", 1, 1, true)
                .AddItem("dsgn_10114", 1, 1, true)
                .AddItem("dsgn_10221", 1, 1, true)
                .AddItem("dsgn_10222", 1, 1, true)
                .AddItem("dsgn_10195", 1, 1, true)
                .AddItem("dsgn_10133", 1, 1, true)
                .AddItem("schm_raiderprowl", 1, 1, true)
                .AddItem("schm_raidermarau", 1, 1, true);

            _builder.Create("SPACE_BOSS_WS")
                .AddGold(12000, 150)
                .AddItem("zinsiam", 150, 2)
                .AddItem("thor_crys", 40, 2)
                .AddItem("proto_tech", 40)
                .AddItem("aurodium", 40, 5)
                .AddItem("rift_key_t55", 20)
                .AddItem("recipe_laser50", 3)
                .AddItem("recipe_ion50", 3)
                .AddItem("recipe_beam50", 3)
                .AddItem("recipe_missl50", 3)
                .AddItem("recipe_torp50", 3)
                .AddItem("recipe_pdef50", 3)
                .AddItem("recipe_shbst50", 3)
                .AddItem("recipe_shrep50", 3)
                .AddItem("recipe_hard50", 3)
                .AddItem("recipe_capb50", 3)
                .AddItem("recipe_tcomp50", 3)
                .AddItem("recipe_plate50", 3)
                .AddItem("recipe_hrep50", 3)
                .AddItem("recipe_resall50", 3)
                .AddItem("recipe_capr50", 3)
                .AddItem("recipe_thrust50", 3)
                .AddItem("dsgn_10114", 4)
                .AddItem("dsgn_10221", 4)
                .AddItem("dsgn_10222", 4)
                .AddItem("dsgn_10195", 4)
                .AddItem("dsgn_10133", 4)
                .AddItem("schm_raiderprowl", 5)
                .AddItem("schm_raidermarau", 5);
        }
    }
}
