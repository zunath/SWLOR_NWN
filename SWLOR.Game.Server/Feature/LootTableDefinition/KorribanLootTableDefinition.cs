using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Feature.LootTableDefinition
{
    public class KorribanLootTableDefinition : ILootTableDefinition
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
            KorribanFortressLoot();
            SithCryptCrates();
            RogueSith();
            Tukata();
            FrogBoss();
            CapstoneKorribanDungeonRares();
            KorforgeRareElites();
            KorcryptRareElites();
            return _builder.Build();
        }

        private void KorcryptRareElites()
        {
            _builder.Create("KORCRYPT_CRYPT_RARES").IsRare()
                .AddItem("cryptwardenda", 1, 1, true).AddItem("cryptwardendb", 1, 1, true).AddItem("bp_cryptwarden", 1, 1, true);
            _builder.Create("KORCRYPT_HUNGER_RARES").IsRare()
                .AddItem("markahungerda", 1, 1, true).AddItem("markahungerdb", 1, 1, true).AddItem("bp_markahunger", 1, 1, true);
            _builder.Create("KORCRYPT_ECLIPSE_RARES").IsRare()
                .AddItem("eclipseshadeda", 1, 1, true).AddItem("eclipseshadedb", 1, 1, true).AddItem("bp_eclipseshade", 1, 1, true);
            _builder.Create("KORCRYPT_CRYPT_COMP").AddItem("cryptwardencm", 1, 1);
            _builder.Create("KORCRYPT_HUNGER_COMP").AddItem("markahungercm", 1, 1);
            _builder.Create("KORCRYPT_ECLIPSE_COMP").AddItem("eclipseshadecm", 1, 1);
        }

        private void KorforgeRareElites()
        {
            _builder.Create("KORFORGE_FORGE_RARES").IsRare()
                .AddItem("forgewrightda", 1, 1, true).AddItem("forgewrightdb", 1, 1, true).AddItem("bp_forgewright", 1, 1, true);
            _builder.Create("KORFORGE_FLAME_RARES").IsRare()
                .AddItem("flameweaverda", 1, 1, true).AddItem("flameweaverdb", 1, 1, true).AddItem("bp_flameweaver", 1, 1, true);
            _builder.Create("KORFORGE_BANE_RARES").IsRare()
                .AddItem("banecallerda", 1, 1, true).AddItem("banecallerdb", 1, 1, true).AddItem("bp_banecaller", 1, 1, true);
            _builder.Create("KORFORGE_FORGE_COMP").AddItem("forgewrightcm", 1, 1);
            _builder.Create("KORFORGE_FLAME_COMP").AddItem("flameweavercm", 1, 1);
            _builder.Create("KORFORGE_BANE_COMP").AddItem("banecallercm", 1, 1);
        }

        private void CapstoneKorribanDungeonRares()
        {
            _builder.Create("CAPSTONE_ABSDEF_RARES")
                .IsRare()
                .AddItem("absdef_l1", 1, 1, true)
                .AddItem("absdef_l2", 1, 1, true)
                .AddItem("absdef_l3", 1, 1, true)
                .AddItem("absdef_l4", 1, 1, true)
                .AddItem("absdef_l5", 1, 1, true)
                .AddItem("absdef_l6", 1, 1, true)
                .AddItem("absdef_l7", 1, 1, true)
                .AddItem("absdef_l8", 1, 1, true);
            _builder.Create("CAPSTONE_ABSDEF_WD_RARES")
                .IsRare()
                .AddItem("absdef_w1", 1, 1, true)
                .AddItem("absdef_w2", 1, 1, true)
                .AddItem("absdef_w3", 1, 1, true)
                .AddItem("absdef_w4", 1, 1, true)
                .AddItem("absdef_w5", 1, 1, true);

            _builder.Create("CAPSTONE_SOULASC_RARES")
                .IsRare()
                .AddItem("soulasc_l1", 1, 1, true)
                .AddItem("soulasc_l2", 1, 1, true)
                .AddItem("soulasc_l3", 1, 1, true)
                .AddItem("soulasc_l4", 1, 1, true)
                .AddItem("soulasc_l5", 1, 1, true)
                .AddItem("soulasc_l6", 1, 1, true)
                .AddItem("soulasc_l7", 1, 1, true)
                .AddItem("soulasc_l8", 1, 1, true);
            _builder.Create("CAPSTONE_SOULASC_WD_RARES")
                .IsRare()
                .AddItem("soulasc_w1", 1, 1, true)
                .AddItem("soulasc_w2", 1, 1, true)
                .AddItem("soulasc_w3", 1, 1, true)
                .AddItem("soulasc_w4", 1, 1, true)
                .AddItem("soulasc_w5", 1, 1, true);

            _builder.Create("CAPSTONE_FORCEBANE_RARES")
                .IsRare()
                .AddItem("forcebane_l1", 1, 1, true)
                .AddItem("forcebane_l2", 1, 1, true)
                .AddItem("forcebane_l3", 1, 1, true)
                .AddItem("forcebane_l4", 1, 1, true)
                .AddItem("forcebane_l5", 1, 1, true)
                .AddItem("forcebane_l6", 1, 1, true)
                .AddItem("forcebane_l7", 1, 1, true)
                .AddItem("forcebane_l8", 1, 1, true);
            _builder.Create("CAPSTONE_FORCEBANE_WD_RARES")
                .IsRare()
                .AddItem("forcebane_w1", 1, 1, true)
                .AddItem("forcebane_w2", 1, 1, true)
                .AddItem("forcebane_w3", 1, 1, true)
                .AddItem("forcebane_w4", 1, 1, true)
                .AddItem("forcebane_w5", 1, 1, true);

            _builder.Create("CAPSTONE_LIGHTSTAND_RARES")
                .IsRare()
                .AddItem("lightstand_l1", 1, 1, true)
                .AddItem("lightstand_l2", 1, 1, true)
                .AddItem("lightstand_l3", 1, 1, true)
                .AddItem("lightstand_l4", 1, 1, true)
                .AddItem("lightstand_l5", 1, 1, true)
                .AddItem("lightstand_l6", 1, 1, true)
                .AddItem("lightstand_l7", 1, 1, true)
                .AddItem("lightstand_l8", 1, 1, true);
            _builder.Create("CAPSTONE_LIGHTSTAND_WD_RARES")
                .IsRare()
                .AddItem("lightstand_w1", 1, 1, true)
                .AddItem("lightstand_w2", 1, 1, true)
                .AddItem("lightstand_w3", 1, 1, true)
                .AddItem("lightstand_w4", 1, 1, true)
                .AddItem("lightstand_w5", 1, 1, true);

            _builder.Create("CAPSTONE_DARKHUNG_RARES")
                .IsRare()
                .AddItem("darkhung_l1", 1, 1, true)
                .AddItem("darkhung_l2", 1, 1, true)
                .AddItem("darkhung_l3", 1, 1, true)
                .AddItem("darkhung_l4", 1, 1, true)
                .AddItem("darkhung_l5", 1, 1, true)
                .AddItem("darkhung_l6", 1, 1, true)
                .AddItem("darkhung_l7", 1, 1, true)
                .AddItem("darkhung_l8", 1, 1, true);
            _builder.Create("CAPSTONE_DARKHUNG_WD_RARES")
                .IsRare()
                .AddItem("darkhung_w1", 1, 1, true)
                .AddItem("darkhung_w2", 1, 1, true)
                .AddItem("darkhung_w3", 1, 1, true)
                .AddItem("darkhung_w4", 1, 1, true)
                .AddItem("darkhung_w5", 1, 1, true);

            _builder.Create("CAPSTONE_ECLIPSE_RARES")
                .IsRare()
                .AddItem("eclipse_l1", 1, 1, true)
                .AddItem("eclipse_l2", 1, 1, true)
                .AddItem("eclipse_l3", 1, 1, true)
                .AddItem("eclipse_l4", 1, 1, true)
                .AddItem("eclipse_l5", 1, 1, true)
                .AddItem("eclipse_l6", 1, 1, true)
                .AddItem("eclipse_l7", 1, 1, true)
                .AddItem("eclipse_l8", 1, 1, true);
            _builder.Create("CAPSTONE_ECLIPSE_WD_RARES")
                .IsRare()
                .AddItem("eclipse_w1", 1, 1, true)
                .AddItem("eclipse_w2", 1, 1, true)
                .AddItem("eclipse_w3", 1, 1, true)
                .AddItem("eclipse_w4", 1, 1, true)
                .AddItem("eclipse_w5", 1, 1, true);
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

        private void KorribanFortressLoot()
        {
            _builder.Create("KORRIBAN_FORTRESS_RESOURCES")
                .AddItem("ref_arkoxit", 1, 1, true)
                .AddItem("ref_jasioclase", 20)
                .AddItem("ref_gostian", 20)
                .AddItem("elec_high", 20)
                .AddItem("lth_high", 20)
                .AddItem("emerald", 10)
                .AddItem("fiberp_high", 20)
                .AddItem("hyphae_wood", 20);

            _builder.Create("KORRIBAN_FORTRESS_GEAR")
                .AddItem("dhar005s", 1)
                .AddItem("dlar005s", 1)
                .AddItem("sc_armor", 1)
                .AddItem("sc_robes", 1)
                .AddItem("sc_tunic", 1)
                .AddItem("dhhl005s", 1)
                .AddItem("dlhl005s", 1)
                .AddItem("sc_cap", 1)
                .AddItem("sc_crown", 1)
                .AddItem("sc_helmet", 1)
                .AddItem("sc_shield", 1)
                .AddItem("dhbe005s", 1)
                .AddItem("dlbe005s", 1)
                .AddItem("sc_belt", 1)
                .AddItem("sc_belt_ls", 1)
                .AddItem("sc_belt_lf", 1)
                .AddItem("dhlg005s", 1)
                .AddItem("dllg005s", 1)
                .AddItem("sc_boots", 1)
                .AddItem("sc_leggings", 1)
                .AddItem("sc_shoes", 1)
                .AddItem("dhbr005s", 1)
                .AddItem("sc_bracer", 1)
                .AddItem("dhcl005s", 1)
                .AddItem("dlcl005s", 1)
                .AddItem("sc_cape", 1)
                .AddItem("sc_cloak", 1)
                .AddItem("sc_mantle", 1)
                .AddItem("dlbr005s", 1)
                .AddItem("sc_gloves", 1)
                .AddItem("sc_wraps", 1)
                .AddItem("dhnk005s", 1)
                .AddItem("dlnk005s", 1)
                .AddItem("sc_amulet", 1)
                .AddItem("sc_necklace", 1)
                .AddItem("sc_pendant", 1)
                .AddItem("dhrg005s", 1)
                .AddItem("dlrg005s", 1)
                .AddItem("sc_band", 1)
                .AddItem("sc_ring", 1)
                .AddItem("sc_signet", 1)
                .AddItem("sc_knife", 1)
                .AddItem("sc_greatswor", 1)
                .AddItem("electroblade_s", 1)
                .AddItem("sc_longsword", 1)
                .AddItem("sc_katar", 1)
                .AddItem("sc_staff", 1)
                .AddItem("sc_twinblade", 1)
                .AddItem("sc_spear", 1)
                .AddItem("sc_rifle", 1)
                .AddItem("sc_pistol", 1)
                .AddItem("sc_shuriken", 1);

            _builder.Create("KORRIBAN_MASTER_RESOURCE")
                .AddItem("chiro_shard", 1);

            _builder.Create("KORRIBAN_FORTRESS_CREDITS")
                .AddGold(500, 1);

            _builder.Create("KORRIBAN_FORGE_KEY")
                .AddItem("valkorrdkey2", 1);

            _builder.Create("KORRIBAN_LORD_KEY")
                .AddItem("valkorrdkey3", 1);

            _builder.Create("KORRIBAN_COUNCIL_KEY")
                .AddItem("valkorrdkey4", 1);

            _builder.Create("KORRIBAN_GATE_KEY")
                .AddItem("valkorrdkey1", 1);

            _builder.Create("KORRIBAN_MASTER_RECIPE")
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
                .AddItem("recipe_chlsupg", 10)
                .AddItem("recipe_chssupg", 10)
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
                .AddItem("recipe_brsushi", 10)
                .AddItem("recipe_ocsushi", 10)
                .AddItem("recipe_iksushi", 10)
                .AddItem("recipe_wisushi", 10)
                .AddItem("recipe_tesushi", 10)
                .AddItem("recipe_dosushi", 10);
        }
        private void RogueSith()
        {
            _builder.Create("KORRIBAN_SITH_ROGUE")
                .AddItem("elec_flawed", 15)
                .AddItem("stolen_s_artifac", 10)
                .AddItem("lth_good", 5)
                .AddItem("sith_longsword", 5);

            _builder.Create("KORRIBAN_SITH_ROGUE_RARES")
                .IsRare()
                .AddItem("map_56", 11, 1, true)
                .AddItem("lockbox_t3", 4, 1, true)
                .AddGold(32, 5);
        }
        private void Tukata()
        {
            _builder.Create("KORRIBAN_TUKATA")
                .AddItem("lth_ruined", 5)
                .AddItem("tukata_hide", 5);


            _builder.Create("KORRIBAN_TUKATA_RARES")
                .IsRare()
                .AddGold(32, 5);

        }
        private void FrogBoss()
        {
            _builder.Create("FROG_BOSS")
                .AddItem("froglegs", 5)
                .AddItem("frogguts", 5)
                .AddItem("chiro_shard", 1);


            _builder.Create("FROG_BOSS_RARES")
                .IsRare()
                .AddItem("fnote_2052", 2, 1, true)
                .AddItem("chiro_shard", 1, 1, true)
                .AddGold(1000, 5);

            _builder.Create("FROG_BOSS_RECIPE")
                .AddItem("recipe_gumbo", 10)
                .AddItem("recipe_alcspear", 10)
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
                .AddItem("recipe_chlsupg", 10)
                .AddItem("recipe_chssupg", 10)
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
                .AddItem("recipe_imboots", 10);
        }
    }
}
