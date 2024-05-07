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

            return _builder.Build();
        }

        private void Tier1()
        {
            _builder.Create("SPACE_BASIC_T1")
                .AddItem("elec_ruined", 3, 1)
                .AddItem("ref_tilarium", 3, 1)
                .AddGold(300, 1)
                .AddItem("ref_veldite", 1, 1)
                .AddItem("ship_missile", 1, 1)
                .AddItem("jade", 1, 1, true)
                .AddItem("aluminum", 1, 1, true);

            _builder.Create("SPACE_ELITE_T1")
                .AddItem("elec_ruined", 1, 1)
                .AddItem("ref_tilarium", 1, 1)
                .AddGold(300, 1)
                .AddItem("ref_veldite", 1, 1)
                .AddItem("lth_ruined", 1, 1)
                .AddItem("jade", 1, 1)
                .AddItem("aluminum", 1, 1)
                .AddItem("wood", 1, 1)
                .AddItem("fiberp_ruined", 1, 1)
                .AddItem("quadrenium", 1, 1, true);

            _builder.Create("SPACE_BOSS_T1")
                .AddGold(1500, 1)
                .AddItem("quadrenium", 1, 2);
        }

        private void Tier2()
        {
            _builder.Create("SPACE_BASIC_T2")
                .AddItem("elec_flawed", 3, 1)
                .AddItem("ref_scordspar", 3, 1)
                .AddGold(600, 1)
                .AddItem("ref_currian", 1, 1)
                .AddItem("ship_missile", 1, 3)
                .AddItem("agate", 1, 1, true)
                .AddItem("steel", 1, 1, true);

            _builder.Create("SPACE_ELITE_T2")
                .AddItem("elec_flawed", 1, 1)
                .AddItem("ref_scordspar", 1, 1)
                .AddGold(600, 1)
                .AddItem("ref_currian", 1, 1)
                .AddItem("lth_flawed", 1, 1)
                .AddItem("agate", 1, 1)
                .AddItem("steel", 1, 1)
                .AddItem("fine_wood", 1, 1)
                .AddItem("fiberp_flawed", 1, 1)
                .AddItem("vintrium", 1, 1, true);

            _builder.Create("SPACE_BOSS_T2")
                .AddGold(3000, 1)
                .AddItem("vintrium", 1, 2);
        }

        private void Tier3()
        {
            _builder.Create("SPACE_BASIC_T3")
                .AddItem("elec_good", 3, 1)
                .AddItem("ref_plagionite", 3, 1)
                .AddGold(900, 1)
                .AddItem("ref_idailia", 1, 1)
                .AddItem("ship_missile", 1, 5)
                .AddItem("citrine", 1, 1, true)
                .AddItem("obsidian", 1, 1, true);

            _builder.Create("SPACE_ELITE_T3")
                .AddItem("elec_good", 1, 1)
                .AddItem("ref_plagionite", 1, 1)
                .AddGold(900, 1)
                .AddItem("ref_idailia", 1, 1)
                .AddItem("lth_good", 1, 1)
                .AddItem("citrine", 1, 1)
                .AddItem("obsidian", 1, 1)
                .AddItem("ancient_wood", 1, 1)
                .AddItem("fiberp_good", 1, 1)
                .AddItem("ionite", 1, 1, true);

            _builder.Create("SPACE_BOSS_T3")
                .AddGold(5000, 1)
                .AddItem("ionite", 1, 2);
        }

        private void Tier4()
        {
            _builder.Create("SPACE_BASIC_T4")
                .AddItem("elec_imperfect", 3, 1)
                .AddItem("ref_keromber", 3, 1)
                .AddGold(1500, 1)
                .AddItem("ref_barinium", 1, 1)
                .AddItem("ship_missile", 1, 7)
                .AddItem("crystal", 1, 1, true)
                .AddItem("ruby", 1, 1, true);

            _builder.Create("SPACE_ELITE_T4")
                .AddItem("elec_imperfect", 1, 1)
                .AddItem("ref_keromber", 1, 1)
                .AddGold(1500, 1)
                .AddItem("ref_barinium", 1, 1)
                .AddItem("lth_imperfect", 1, 1)
                .AddItem("crystal", 1, 1)
                .AddItem("ruby", 1, 1)
                .AddItem("aracia_wood", 1, 1)
                .AddItem("fiberp_imperfect", 1, 1)
                .AddItem("katrium", 1, 1, true);

            _builder.Create("SPACE_BOSS_T4")
                .AddGold(7000, 1)
                .AddItem("katrium", 1, 2);
        }

        private void Tier5()
        {
            _builder.Create("SPACE_BASIC_T5")
                .AddItem("elec_high", 3, 1)
                .AddItem("ref_gostian", 3, 1)
                .AddGold(2000, 1)
                .AddItem("ref_jasioclase", 1, 1)
                .AddItem("ship_missile", 1, 9)
                .AddItem("diamond", 1, 1, true)
                .AddItem("emerald", 1, 1, true);

            _builder.Create("SPACE_ELITE_T5")
                .AddItem("elec_high", 1, 1)
                .AddItem("ref_gostian", 1, 1)
                .AddGold(2000, 1)
                .AddItem("ref_jasioclase", 1, 1)
                .AddItem("lth_high", 1, 1)
                .AddItem("diamond", 1, 1)
                .AddItem("emerald", 1, 1)
                .AddItem("hyphae_wood", 1, 1)
                .AddItem("fiberp_high", 1, 1)
                .AddItem("proton_bomb", 1, 1, true)
                .AddItem("zinsiam", 1, 1, true)
                .AddItem("ref_arda", 1, 1, true);

            _builder.Create("SPACE_BOSS_T5")
                .AddGold(10000, 1)
                .AddItem("zinsiam", 1, 2);

            _builder.Create("SPACE_BASIC_T6")
                .AddItem("elec_high", 3, 2)
                .AddItem("ref_gostian", 3, 2)
                .AddGold(3000, 1)
                .AddItem("ref_jasioclase", 1, 2)
                .AddItem("ship_missile", 1, 11)
                .AddItem("diamond", 1, 1, true)
                .AddItem("emerald", 1, 1, true);

            _builder.Create("SPACE_ELITE_T6")
                .AddItem("elec_high", 1, 2)
                .AddItem("ref_gostian", 1, 2)
                .AddGold(3000, 1)
                .AddItem("ref_jasioclase", 1, 1)
                .AddItem("lth_high", 1, 1)
                .AddItem("diamond", 1, 1)
                .AddItem("emerald", 1, 1)
                .AddItem("hyphae_wood", 1, 1)
                .AddItem("fiberp_high", 1, 1)
                .AddItem("proton_bomb", 1, 1, true)
                .AddItem("zinsiam", 1, 1, true)
                .AddItem("ref_arda", 1, 1, true);

            _builder.Create("SPACE_BOSS_T6")
                .AddGold(15000, 1)
                .AddItem("zinsiam", 1, 2)
                .AddItem("recipe_modacmiss", 1)
                .AddItem("recipe_modacml", 1)
                .AddItem("recipe_modbulwar", 1)
                .AddItem("recipe_capmodcpu", 1)
                .AddItem("recipe_capreact", 1)
                .AddItem("recipe_capthrust", 1)
                .AddItem("recipe_capconst", 1)
                .AddItem("recipe_cappwrlay", 1)
                .AddItem("recipe_capshgen", 1)
                .AddItem("recipe_modadvthr", 1)
                .AddItem("recipe_capmodtec", 1)
                .AddItem("recipe_corv", 1)
                .AddItem("recipe_corveng", 1)
                .AddItem("recipe_corvhull", 1)
                .AddItem("recipe_corvpsys", 1)
                .AddItem("recipe_corvrebay", 1)
                .AddItem("recipe_conind", 1)
                .AddItem("recipe_modlasbat", 1)
                .AddItem("recipe_modquadlz", 1)
                .AddItem("recipe_modredsgn", 1)
                .AddItem("recipe_modreplat", 1)
                .AddItem("recipe_modrepfld", 1)
                .AddItem("recipe_conskrm", 1)
                .AddItem("recipe_modstmcan", 1)
                .AddItem("recipe_modtarget", 1)
                .AddItem("recipe_modturbo", 1)
                .AddItem("recipe_conwar", 1)
                .AddItem("recipe_modsmin", 1);

            _builder.Create("THORILIDE")
                .AddItem("thor_crys", 1);
        }
    }
}
