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
            { 0, new RewardDetails(23, 7)},
            { 1, new RewardDetails(84, 27)},
            { 2, new RewardDetails(122, 39)},
            { 3, new RewardDetails(184, 52)},
            { 4, new RewardDetails(245, 65)},
            { 5, new RewardDetails(312, 82)},
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

            // Tier 2 (Rank 1)
            BuildItemTask(builder, "agr_tsk_017", "raivor_mball", 1, 1);
            BuildItemTask(builder, "agr_tsk_018", "cairn_potpie", 1, 1);
            BuildItemTask(builder, "agr_tsk_019", "choco_cookies", 1, 1);
            BuildItemTask(builder, "agr_tsk_020", "apple_juice", 1, 1);
            BuildItemTask(builder, "agr_tsk_021", "pea_soup", 1, 1);
            BuildItemTask(builder, "agr_tsk_022", "raivor_broth", 1, 1);
            BuildItemTask(builder, "agr_tsk_023", "soba_noodles", 1, 1);
            BuildItemTask(builder, "agr_tsk_024", "cairn_sandwich", 1, 1);
            BuildItemTask(builder, "agr_tsk_025", "nash_mball", 1, 1);
            BuildItemTask(builder, "agr_tsk_026", "mystery_cookies", 1, 1);
            BuildItemTask(builder, "agr_tsk_027", "mando_herbsoup", 1, 1);
            BuildItemTask(builder, "agr_tsk_028", "green_curry", 1, 1);
            BuildItemTask(builder, "agr_tsk_029", "apple_aulait", 1, 1);
            BuildItemTask(builder, "agr_tsk_030", "raiv_bloodbroth", 1, 1);
            BuildItemTask(builder, "agr_tsk_031", "nash_sandwich", 1, 1);
            BuildItemTask(builder, "agr_tsk_032", "nash_stew", 1, 1);

            // Tier 3 (Rank 2)
            BuildItemTask(builder, "agr_tsk_033", "aradile_mball", 1, 2);
            BuildItemTask(builder, "agr_tsk_034", "tiger_potpie", 1, 2);
            BuildItemTask(builder, "agr_tsk_035", "acorn_cookies", 1, 2);
            BuildItemTask(builder, "agr_tsk_036", "pine_juice", 1, 2);
            BuildItemTask(builder, "agr_tsk_037", "veg_soup", 1, 2);
            BuildItemTask(builder, "agr_tsk_038", "ara_broth", 1, 2);
            BuildItemTask(builder, "agr_tsk_039", "ramen_noodles", 1, 2);
            BuildItemTask(builder, "agr_tsk_040", "ara_sandwich", 1, 2);
            BuildItemTask(builder, "agr_tsk_041", "byysk_mball", 1, 2);
            BuildItemTask(builder, "agr_tsk_042", "cinna_cookies", 1, 2);
            BuildItemTask(builder, "agr_tsk_043", "moncal_hsoup", 1, 2);
            BuildItemTask(builder, "agr_tsk_044", "red_curry", 1, 2);
            BuildItemTask(builder, "agr_tsk_045", "pine_aulait", 1, 2);
            BuildItemTask(builder, "agr_tsk_046", "amphi_bbroth", 1, 2);
            BuildItemTask(builder, "agr_tsk_047", "snake_sandwich", 1, 2);
            BuildItemTask(builder, "agr_tsk_048", "snake_stew", 1, 2);

            // Tier 4 (Rank 3)
            BuildItemTask(builder, "agr_tsk_049", "womp_mball", 1, 3);
            BuildItemTask(builder, "agr_tsk_050", "sanddem_potpie", 1, 3);
            BuildItemTask(builder, "agr_tsk_051", "ging_cookies", 1, 3);
            BuildItemTask(builder, "agr_tsk_052", "melon_juice", 1, 3);
            BuildItemTask(builder, "agr_tsk_053", "mush_soup", 1, 3);
            BuildItemTask(builder, "agr_tsk_054", "womp_broth", 1, 3);
            BuildItemTask(builder, "agr_tsk_055", "soy_ramen", 1, 3);
            BuildItemTask(builder, "agr_tsk_056", "surprise_sandwich", 1, 3);
            BuildItemTask(builder, "agr_tsk_057", "tusken_mball", 1, 3);
            BuildItemTask(builder, "agr_tsk_058", "walnut_cookies", 1, 3);
            BuildItemTask(builder, "agr_tsk_059", "des_herbsoup", 1, 3);
            BuildItemTask(builder, "agr_tsk_060", "yellow_curry", 1, 3);
            BuildItemTask(builder, "agr_tsk_061", "melon_aulait", 1, 3);
            BuildItemTask(builder, "agr_tsk_062", "tusk_b_broth", 1, 3);
            BuildItemTask(builder, "agr_tsk_063", "dem_sandwich", 1, 3);
            BuildItemTask(builder, "agr_tsk_064", "demon_stew", 1, 3);

            // Tier 5 (Rank 4)
            BuildItemTask(builder, "agr_tsk_065", "wild_mball", 1, 4);
            BuildItemTask(builder, "agr_tsk_066", "wild_potpie", 1, 4);
            BuildItemTask(builder, "agr_tsk_067", "wild_cookies", 1, 4);
            BuildItemTask(builder, "agr_tsk_068", "tomato_juice", 1, 4);
            BuildItemTask(builder, "agr_tsk_069", "miso_soup", 1, 4);
            BuildItemTask(builder, "agr_tsk_070", "wild_broth", 1, 4);
            BuildItemTask(builder, "agr_tsk_071", "miso_ramen", 1, 4);
            BuildItemTask(builder, "agr_tsk_072", "wild_sandwich", 1, 4);
            BuildItemTask(builder, "agr_tsk_073", "grand_mball", 1, 4);
            BuildItemTask(builder, "agr_tsk_074", "wizard_cookies", 1, 4);
            BuildItemTask(builder, "agr_tsk_075", "dath_hsoup", 1, 4);
            BuildItemTask(builder, "agr_tsk_076", "wild_curry", 1, 4);
            BuildItemTask(builder, "agr_tsk_077", "tomato_aulait", 1, 4);
            BuildItemTask(builder, "agr_tsk_078", "wild_bbroth", 1, 4);
            BuildItemTask(builder, "agr_tsk_079", "grand_sandwich", 1, 4);
            BuildItemTask(builder, "agr_tsk_080", "wild_stew", 1, 4);

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