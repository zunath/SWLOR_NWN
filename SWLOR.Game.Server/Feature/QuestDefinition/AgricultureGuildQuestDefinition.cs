using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Feature.QuestDefinition
{
    public class AgricultureGuildQuestDefinition : IQuestListDefinition
    {
        private class RewardDetails
        {
            public int Gold { get; }
            public int GP { get; }

            public RewardDetails(int gold, int gp)
            {
                Gold = gold;
                GP = gp;
            }
        }

        private readonly Dictionary<int, RewardDetails> _rewardDetails = new()
        {
            { 0, new RewardDetails(138, 21)},
            { 1, new RewardDetails(343, 81)},
            { 2, new RewardDetails(532, 117)},
            { 3, new RewardDetails(733, 156)},
            { 4, new RewardDetails(874, 195)},
            { 5, new RewardDetails(960, 246)},
        };

        public Dictionary<string, QuestDetail> BuildQuests()
        {
            var builder = new QuestBuilder();
            // Tier 1 (Rank 0)
            BuildItemTask(builder, "agr_tsk_001", "mynock_mball", 1, 0);
            BuildItemTask(builder, "agr_tsk_002", "waro_potpie", 1, 0);
            BuildItemTask(builder, "agr_tsk_003", "sugar_cookies", 1, 0);
            BuildItemTask(builder, "agr_tsk_004", "orange_juice", 1, 0);
            BuildItemTask(builder, "agr_tsk_005", "pebble_soup", 1, 0);
            BuildItemTask(builder, "agr_tsk_006", "mynock_broth", 1, 0);
            BuildItemTask(builder, "agr_tsk_007", "noodles", 1, 0);
            BuildItemTask(builder, "agr_tsk_008", "kath_sandwich", 1, 0);
            BuildItemTask(builder, "agr_tsk_009", "kinrath_mball", 1, 0);
            BuildItemTask(builder, "agr_tsk_010", "lemon_cookies", 1, 0);
            BuildItemTask(builder, "agr_tsk_011", "v_herb_soup", 1, 0);
            BuildItemTask(builder, "agr_tsk_012", "orange_curry", 1, 0);
            BuildItemTask(builder, "agr_tsk_013", "o_aulait", 1, 0);
            BuildItemTask(builder, "agr_tsk_014", "k_blood_broth", 1, 0);
            BuildItemTask(builder, "agr_tsk_015", "g_sandwich", 1, 0);
            BuildItemTask(builder, "agr_tsk_016", "g_stew", 1, 0);
            BuildItemTask(builder, "agr_tsk_017", "moat_carp", 4, 0);
            BuildItemTask(builder, "agr_tsk_018", "lamp_marimo", 4, 0);
            BuildItemTask(builder, "agr_tsk_019", "visc_urchin", 4, 0);
            BuildItemTask(builder, "agr_tsk_020", "phan_newt", 4, 0);
            BuildItemTask(builder, "agr_tsk_021", "cobalt_jellyfish", 4, 0);
            BuildItemTask(builder, "agr_tsk_022", "denizanasi", 4, 0);
            BuildItemTask(builder, "agr_tsk_023", "crayfish", 4, 0);
            BuildItemTask(builder, "agr_tsk_024", "cala_lobster", 4, 0);
            BuildItemTask(builder, "agr_tsk_025", "bibikibo", 4, 0);
            BuildItemTask(builder, "agr_tsk_026", "dath_sardine", 4, 0);
            BuildItemTask(builder, "agr_tsk_027", "hamsi", 4, 0);
            BuildItemTask(builder, "agr_tsk_028", "roast_carp", 1, 0);
            BuildItemTask(builder, "agr_tsk_029", "marimo_stew", 1, 0);
            BuildItemTask(builder, "agr_tsk_030", "urchin_sushi", 1, 0);
            BuildItemTask(builder, "agr_tsk_031", "blackened_newt", 1, 0);
            BuildItemTask(builder, "agr_tsk_032", "cooked_jellyfish", 1, 0);
            BuildItemTask(builder, "agr_tsk_033", "denzi_treat", 1, 0);
            BuildItemTask(builder, "agr_tsk_034", "cooked_crayfish", 1, 0);
            BuildItemTask(builder, "agr_tsk_035", "peeled_lobster", 1, 0);
            BuildItemTask(builder, "agr_tsk_036", "cooked_bibikibo", 1, 0);
            BuildItemTask(builder, "agr_tsk_037", "sliced_sardine", 1, 0);

            // Tier 2 (Rank 1)
            BuildItemTask(builder, "agr_tsk_200", "raivor_mball", 1, 1);
            BuildItemTask(builder, "agr_tsk_201", "cairn_potpie", 1, 1);
            BuildItemTask(builder, "agr_tsk_202", "choco_cookies", 1, 1);
            BuildItemTask(builder, "agr_tsk_203", "apple_juice", 1, 1);
            BuildItemTask(builder, "agr_tsk_204", "pea_soup", 1, 1);
            BuildItemTask(builder, "agr_tsk_205", "raivor_broth", 1, 1);
            BuildItemTask(builder, "agr_tsk_206", "soba_noodles", 1, 1);
            BuildItemTask(builder, "agr_tsk_207", "cairn_sandwich", 1, 1);
            BuildItemTask(builder, "agr_tsk_208", "nash_mball", 1, 1);
            BuildItemTask(builder, "agr_tsk_209", "mystery_cookies", 1, 1);
            BuildItemTask(builder, "agr_tsk_210", "mando_herbsoup", 1, 1);
            BuildItemTask(builder, "agr_tsk_211", "green_curry", 1, 1);
            BuildItemTask(builder, "agr_tsk_212", "apple_aulait", 1, 1);
            BuildItemTask(builder, "agr_tsk_213", "raiv_bloodbroth", 1, 1);
            BuildItemTask(builder, "agr_tsk_214", "nash_sandwich", 1, 1);
            BuildItemTask(builder, "agr_tsk_215", "nash_stew", 1, 1);
            BuildItemTask(builder, "agr_tsk_216", "sen_sardine", 4, 1);
            BuildItemTask(builder, "agr_tsk_217", "rakaz_shellfish", 4, 1);
            BuildItemTask(builder, "agr_tsk_218", "bast_sweeper", 4, 1);
            BuildItemTask(builder, "agr_tsk_219", "mackerel", 4, 1);
            BuildItemTask(builder, "agr_tsk_220", "greedie", 4, 1);
            BuildItemTask(builder, "agr_tsk_221", "copper_frog", 4, 1);
            BuildItemTask(builder, "agr_tsk_222", "yellow_globe", 4, 1);
            BuildItemTask(builder, "agr_tsk_223", "muddy_siredon", 4, 1);
            BuildItemTask(builder, "agr_tsk_224", "istavrit", 4, 1);
            BuildItemTask(builder, "agr_tsk_225", "fish_broth", 1, 1);
            BuildItemTask(builder, "agr_tsk_226", "cooked_sardine", 1, 1);
            BuildItemTask(builder, "agr_tsk_227", "rakaz_special", 1, 1);
            BuildItemTask(builder, "agr_tsk_228", "maringna", 1, 1);
            BuildItemTask(builder, "agr_tsk_229", "cooked_mackerel", 1, 1);
            BuildItemTask(builder, "agr_tsk_230", "greedie_stew", 1, 1);
            BuildItemTask(builder, "agr_tsk_231", "blackened_frog", 1, 1);
            BuildItemTask(builder, "agr_tsk_232", "brain_stew", 1, 1);
            BuildItemTask(builder, "agr_tsk_233", "cooked_siredon", 1, 1);
            BuildItemTask(builder, "agr_tsk_234", "cooked_istavrit", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "agr_tsk_400", "aradile_mball", 1, 2);
            BuildItemTask(builder, "agr_tsk_401", "tiger_potpie", 1, 2);
            BuildItemTask(builder, "agr_tsk_402", "acorn_cookies", 1, 2);
            BuildItemTask(builder, "agr_tsk_403", "pine_juice", 1, 2);
            BuildItemTask(builder, "agr_tsk_404", "veg_soup", 1, 2);
            BuildItemTask(builder, "agr_tsk_405", "ara_broth", 1, 2);
            BuildItemTask(builder, "agr_tsk_406", "ramen_noodles", 1, 2);
            BuildItemTask(builder, "agr_tsk_407", "ara_sandwich", 1, 2);
            BuildItemTask(builder, "agr_tsk_408", "byysk_mball", 1, 2);
            BuildItemTask(builder, "agr_tsk_409", "cinna_cookies", 1, 2);
            BuildItemTask(builder, "agr_tsk_410", "moncal_hsoup", 1, 2);
            BuildItemTask(builder, "agr_tsk_411", "red_curry", 1, 2);
            BuildItemTask(builder, "agr_tsk_412", "pine_aulait", 1, 2);
            BuildItemTask(builder, "agr_tsk_413", "amphi_bbroth", 1, 2);
            BuildItemTask(builder, "agr_tsk_414", "snake_sandwich", 1, 2);
            BuildItemTask(builder, "agr_tsk_415", "snake_stew", 1, 2);
            BuildItemTask(builder, "agr_tsk_416", "trans_salpa", 4, 2);
            BuildItemTask(builder, "agr_tsk_417", "quus", 4, 2);
            BuildItemTask(builder, "agr_tsk_418", "forest_carp", 4, 2);
            BuildItemTask(builder, "agr_tsk_419", "tiny_goldfish", 4, 2);
            BuildItemTask(builder, "agr_tsk_420", "hoptoad", 4, 2);
            BuildItemTask(builder, "agr_tsk_421", "cheval_salmon", 4, 2);
            BuildItemTask(builder, "agr_tsk_422", "yorchete", 4, 2);
            BuildItemTask(builder, "agr_tsk_423", "white_lobster", 4, 2);
            BuildItemTask(builder, "agr_tsk_424", "fat_greedie", 4, 2);
            BuildItemTask(builder, "agr_tsk_425", "moorish_idol", 4, 2);
            BuildItemTask(builder, "agr_tsk_426", "cooked_salpa", 1, 2);
            BuildItemTask(builder, "agr_tsk_427", "herb_quus", 1, 2);
            BuildItemTask(builder, "agr_tsk_428", "carp_sushi", 1, 2);
            BuildItemTask(builder, "agr_tsk_429", "goldfish_bowl", 1, 2);
            BuildItemTask(builder, "agr_tsk_430", "b_hoptoad", 1, 2);
            BuildItemTask(builder, "agr_tsk_431", "smoked_salmon", 1, 2);
            BuildItemTask(builder, "agr_tsk_432", "deep_broth", 1, 2);
            BuildItemTask(builder, "agr_tsk_433", "white_p_lobster", 1, 2);
            BuildItemTask(builder, "agr_tsk_434", "fat_greedie_stew", 1, 2);
            BuildItemTask(builder, "agr_tsk_435", "idol_sushi", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "agr_tsk_600", "womp_mball", 1, 3);
            BuildItemTask(builder, "agr_tsk_601", "sanddem_potpie", 1, 3);
            BuildItemTask(builder, "agr_tsk_602", "ging_cookies", 1, 3);
            BuildItemTask(builder, "agr_tsk_603", "melon_juice", 1, 3);
            BuildItemTask(builder, "agr_tsk_604", "mush_soup", 1, 3);
            BuildItemTask(builder, "agr_tsk_605", "womp_broth", 1, 3);
            BuildItemTask(builder, "agr_tsk_606", "soy_ramen", 1, 3);
            BuildItemTask(builder, "agr_tsk_607", "surprise_sandwic", 1, 3);
            BuildItemTask(builder, "agr_tsk_608", "tusken_mball", 1, 3);
            BuildItemTask(builder, "agr_tsk_609", "walnut_cookies", 1, 3);
            BuildItemTask(builder, "agr_tsk_610", "des_herbsoup", 1, 3);
            BuildItemTask(builder, "agr_tsk_611", "yellow_curry", 1, 3);
            BuildItemTask(builder, "agr_tsk_612", "melon_aulait", 1, 3);
            BuildItemTask(builder, "agr_tsk_613", "tusk_b_broth", 1, 3);
            BuildItemTask(builder, "agr_tsk_614", "dem_sandwich", 1, 3);
            BuildItemTask(builder, "agr_tsk_615", "demon_stew", 1, 3);
            BuildItemTask(builder, "agr_tsk_616", "gurnard", 4, 3);
            BuildItemTask(builder, "agr_tsk_617", "nebimonite", 4, 3);
            BuildItemTask(builder, "agr_tsk_618", "tricolored_carp", 4, 3);
            BuildItemTask(builder, "agr_tsk_619", "blindfish", 4, 3);
            BuildItemTask(builder, "agr_tsk_620", "pipira", 4, 3);
            BuildItemTask(builder, "agr_tsk_621", "tiger_cod", 4, 3);
            BuildItemTask(builder, "agr_tsk_622", "bonefish", 4, 3);
            BuildItemTask(builder, "agr_tsk_623", "giant_catfish", 4, 3);
            BuildItemTask(builder, "agr_tsk_624", "yayinbaligi", 4, 3);
            BuildItemTask(builder, "agr_tsk_625", "deadmoiselle", 4, 3);
            BuildItemTask(builder, "agr_tsk_626", "gurnard_stew", 1, 3);
            BuildItemTask(builder, "agr_tsk_627", "baked_nebimon", 1, 3);
            BuildItemTask(builder, "agr_tsk_628", "tricolored_sushi", 1, 3);
            BuildItemTask(builder, "agr_tsk_629", "fish_n_chips", 1, 3);
            BuildItemTask(builder, "agr_tsk_630", "roast_pipira", 1, 3);
            BuildItemTask(builder, "agr_tsk_631", "roast_cod", 1, 3);
            BuildItemTask(builder, "agr_tsk_632", "bonefish_broth", 1, 3);
            BuildItemTask(builder, "agr_tsk_633", "steamed_catfish", 1, 3);
            BuildItemTask(builder, "agr_tsk_634", "cooked_yayin", 1, 3);
            BuildItemTask(builder, "agr_tsk_635", "dead_stew", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "agr_tsk_800", "wild_mball", 1, 4);
            BuildItemTask(builder, "agr_tsk_801", "wild_potpie", 1, 4);
            BuildItemTask(builder, "agr_tsk_802", "wild_cookies", 1, 4);
            BuildItemTask(builder, "agr_tsk_803", "tomato_juice", 1, 4);
            BuildItemTask(builder, "agr_tsk_804", "miso_soup", 1, 4);
            BuildItemTask(builder, "agr_tsk_805", "wild_broth", 1, 4);
            BuildItemTask(builder, "agr_tsk_806", "miso_ramen", 1, 4);
            BuildItemTask(builder, "agr_tsk_807", "wild_sandwich", 1, 4);
            BuildItemTask(builder, "agr_tsk_808", "grand_mball", 1, 4);
            BuildItemTask(builder, "agr_tsk_809", "wizard_cookies", 1, 4);
            BuildItemTask(builder, "agr_tsk_810", "dath_hsoup", 1, 4);
            BuildItemTask(builder, "agr_tsk_811", "wild_curry", 1, 4);
            BuildItemTask(builder, "agr_tsk_812", "tomato_aulait", 1, 4);
            BuildItemTask(builder, "agr_tsk_813", "wild_bbroth", 1, 4);
            BuildItemTask(builder, "agr_tsk_814", "grand_sandwich", 1, 4);
            BuildItemTask(builder, "agr_tsk_815", "wild_stew", 1, 4);
            BuildItemTask(builder, "agr_tsk_816", "lungfish", 4, 4);
            BuildItemTask(builder, "agr_tsk_817", "dark_bass", 4, 4);
            BuildItemTask(builder, "agr_tsk_818", "crystal_bass", 4, 4);
            BuildItemTask(builder, "agr_tsk_819", "ogre_eel", 4, 4);
            BuildItemTask(builder, "agr_tsk_820", "shining_trout", 4, 4);
            BuildItemTask(builder, "agr_tsk_821", "blowfish", 4, 4);
            BuildItemTask(builder, "agr_tsk_822", "nosteau_herring", 4, 4);
            BuildItemTask(builder, "agr_tsk_823", "lakerda", 4, 4);
            BuildItemTask(builder, "agr_tsk_824", "zafmlug_bass", 4, 4);
            BuildItemTask(builder, "agr_tsk_825", "ruddy_seema", 4, 4);
            BuildItemTask(builder, "agr_tsk_826", "frigorifish", 4, 4);
            BuildItemTask(builder, "agr_tsk_827", "long_ling_lung", 1, 4);
            BuildItemTask(builder, "agr_tsk_828", "bass_meuniere", 1, 4);
            BuildItemTask(builder, "agr_tsk_829", "crystal_sushi", 1, 4);
            BuildItemTask(builder, "agr_tsk_830", "eelectric_soup", 1, 4);
            BuildItemTask(builder, "agr_tsk_831", "shining_stew", 1, 4);
            BuildItemTask(builder, "agr_tsk_832", "popper_bowl", 1, 4);
            BuildItemTask(builder, "agr_tsk_833", "pickled_herring", 1, 4);
            BuildItemTask(builder, "agr_tsk_834", "zoni_broth", 1, 4);
            BuildItemTask(builder, "agr_tsk_835", "sea_bass_croute", 1, 4);
            BuildItemTask(builder, "agr_tsk_836", "shimm_broth", 1, 4);
            BuildItemTask(builder, "agr_tsk_837", "rancid_broth", 1, 4);
            BuildItemTask(builder, "agr_tsk_838", "dan_carrotcake", 1, 4);
            BuildItemTask(builder, "agr_tsk_839", "munch_fungusb", 1, 4);
            BuildItemTask(builder, "agr_tsk_840", "dan_flapjack", 1, 4);

            return builder.Build();
        }

        private void BuildItemTask(
            QuestBuilder builder,
            string questId,
            string resref,
            int amount,
            int guildRank)
        {
            var itemName = Cache.GetItemNameByResref(resref);
            var rewardDetails = _rewardDetails[guildRank];

            builder.Create(questId, $"{amount}x {itemName}")
                .IsRepeatable()
                .IsGuildTask(GuildType.AgricultureGuild, guildRank)

                .AddState()
                .SetStateJournalText($"Collect {amount}x {itemName} and return to the Agriculture Guildmaster")
                .AddCollectItemObjective(resref, amount)

                .AddGoldReward(rewardDetails.Gold)
                .AddGPReward(GuildType.AgricultureGuild, rewardDetails.GP);
        }
    }
}