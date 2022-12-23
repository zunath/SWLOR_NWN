using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class KorribanLootTableDefinition: ILootTableDefinition
    {
        private readonly LootTableBuilder _builder = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            Klorslug();
            MorabandSerpent();
            Shyrack();
            Wraid();
            PelkoSwarm();
            SithApprentice();

            SithCryptCrates();

            return _builder.Build();
        }

        private void Klorslug()
        {
            _builder.Create("KORRIBAN_KLORSLUG")
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 15)
                .AddItem("klorslug_meat", 10)
                .AddItem("klorslug_tail", 10)
                .AddItem("klorslug_innards", 5);

            _builder.Create("KORRIBAN_KLORSLUG_RARES")
                .IsRare()
                .AddItem("klorslug_skin2", 1, 1, true);
        }

        private void MorabandSerpent()
        {
            _builder.Create("KORRIBAN_MORABAND_SERPENT")
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 10)
                .AddItem("mserp_meat", 10)
                .AddItem("mserp_bile", 5)
                .AddItem("mserp_guts", 5);
        }

        private void Shyrack()
        {
            _builder.Create("KORRIBAN_SHYRACK")
                .AddItem("lth_flawed", 20)
                .AddItem("shyrack_wing", 10)
                .AddItem("shyrack_meat", 30)
                .AddItem("shyrack_tooth", 20);
        }

        private void Wraid()
        {
            _builder.Create("KORRIBAN_WRAID")
                .AddItem("lth_flawed", 20)
                .AddItem("lth_good", 5)
                .AddItem("wraid_claw2", 10)
                .AddItem("wraid_scale", 5)
                .AddItem("wraid_tooth", 5);

            _builder.Create("KORRIBAN_WRAID_RARES")
                .IsRare()
                .AddItem("wraid_spine", 1, 1, true);
        }

        private void PelkoSwarm()
        {
            _builder.Create("KORRIBAN_PELKO")
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 10)
                .AddItem("pelko_chitin", 20)
                .AddItem("pelko_tooth", 20)
                .AddItem("pelko_meat", 10)
                .AddItem("pelko_blood", 5);
        }

        private void SithApprentice()
        {
            _builder.Create("KORRIBAN_SITH_APPRENTICE")
                .AddItem("lth_ruined", 5)
                .AddItem("lth_flawed", 10)
                .AddItem("lth_good", 5)
                .AddItem("sith_longsword", 5)
                .AddItem("sith_knife", 5)
                .AddItem("sith_gswd", 5)
                .AddItem("sith_spear", 5)
                .AddItem("sith_katar", 5)
                .AddItem("sith_staff", 5)
                .AddItem("sith_pistol", 5)
                .AddItem("sith_twinblade", 5)
                .AddItem("sith_rifle", 5)
                .AddItem("sith_shuriken", 5)
                .AddItem("sith_electro", 1);

            _builder.Create("KORRIBAN_SITH_APPRENTICE_RARES")
                .IsRare()
                .AddItem("map_56", 11, 1, true)
                .AddItem("abdamaryllia", 22, 1, true)
                .AddItem("recipe_sithlngsd", 1, 1, true)
                .AddItem("recipe_sithknf", 1, 1, true)
                .AddItem("recipe_sithtwb", 1, 1, true)
                .AddItem("recipe_sithrif", 1, 1, true)
                .AddItem("recipe_sithgsw", 1, 1, true)
                .AddItem("recipe_sithsp", 1, 1, true)
                .AddItem("recipe_sithstf", 1, 1, true)
                .AddItem("recipe_sithpis", 1, 1, true)
                .AddItem("recipe_sithkat", 1, 1, true)
                .AddItem("recipe_sithshu", 1, 1, true)
                .AddItem("recipe_sithelec", 1, 1, true)
                .AddGold(32, 5);
        }

        private void SithCryptCrates()
        {
            _builder.Create("KORRIBAN_SITH_CRATE_1")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 5)
                .AddItem("fine_wood", 5)
                .AddItem("fiberp_flawed", 5)
                .AddItem("distilled_water", 10)
                .AddItem("b_flour", 2)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
                .AddItem("choco_cookies", 4)
                .AddItem("med_supplies", 3)
                .AddItem("jade", 1, 1, true)

                .AddGold(24, 15);

            _builder.Create("KORRIBAN_SITH_CRATE_2")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 5)
                .AddItem("fine_wood", 5)
                .AddItem("fiberp_flawed", 5)
                .AddItem("distilled_water", 10)
                .AddItem("b_flour", 10)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
                .AddItem("med_supplies", 3)
                .AddItem("cairn_sandwich", 3)

                .AddGold(22, 15);

            _builder.Create("KORRIBAN_SITH_CRATE_3")
                .AddItem("elec_flawed", 20)
                .AddItem("ref_scordspar", 12)
                .AddItem("fine_wood", 12)
                .AddItem("fiberp_flawed", 12)
                .AddItem("b_flour", 5)
                .AddItem("sweet_butter", 5)
                .AddItem("v_apple", 3)
                .AddItem("green_curry", 5)
                .AddItem("med_supplies", 3)
                .AddItem("agate", 10, 1, true)
                .AddItem("map_56", 5, 1, true)
                .AddItem("recipe_sithlngsd", 1, 1, true)
                .AddItem("recipe_sithknf", 1, 1, true)
                .AddItem("recipe_sithtwb", 1, 1, true)
                .AddItem("recipe_sithrif", 1, 1, true)
                .AddItem("recipe_sithgsw", 1, 1, true)
                .AddItem("recipe_sithsp", 1, 1, true)
                .AddItem("recipe_sithstf", 1, 1, true)
                .AddItem("recipe_sithpis", 1, 1, true)
                .AddItem("recipe_sithkat", 1, 1, true)
                .AddItem("recipe_sithshu", 1, 1, true)
                .AddItem("recipe_sithelec", 1, 1, true)

                .AddGold(40, 10);
        }
    }
}
